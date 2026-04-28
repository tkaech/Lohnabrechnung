using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.AnnualSalary;
using Payroll.Application.Payroll;
using Payroll.Application.Settings;
using Payroll.Domain.Employees;
using Payroll.Domain.Payroll;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.AnnualSalary;
using Payroll.Infrastructure.MonthlyRecords;
using Payroll.Infrastructure.Payroll;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Settings;

namespace Payroll.Application.Tests;

public sealed class PayrollRunRepositorySqliteTests
{
    [Fact]
    public async Task FinalizeMonthAsync_PersistsFinalizedPayrollRunLines()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        dbContext.EmploymentContracts.Add(new EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 10m, 0m, 0m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: WorkTimeSupplementSettings.Empty,
            ahvIvEoRate: 0m,
            alvRate: 0m,
            sicknessAccidentInsuranceRate: 0m,
            trainingAndHolidayRate: 0m,
            vacationCompensationRate: 0m,
            vacationCompensationRateAge50Plus: 0m,
            vehiclePauschalzone1RateChf: 0m,
            vehiclePauschalzone2RateChf: 0m,
            vehicleRegiezone1RateChf: 0m));
        await dbContext.SaveChangesAsync();

        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await monthlyRecordRepository.GetOrCreateAsync(employee.Id, 2026, 3, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 3, 31), 10m, 0m, 0m, 0m, 0m, 0m, 0m, null);
        record.SaveExpenseEntry(5m);
        await monthlyRecordRepository.SaveChangesAsync(CancellationToken.None);

        var service = new PayrollRunService(new PayrollRunRepository(dbContext));
        var result = await service.FinalizeMonthAsync(new FinalizePayrollMonthCommand(employee.Id, 2026, 3, new DateOnly(2026, 4, 5)));

        dbContext.ChangeTracker.Clear();
        var storedRun = await dbContext.PayrollRuns
            .Include(run => run.Lines)
            .SingleAsync(run => run.Id == result.PayrollRunId);

        Assert.Equal("2026-03", storedRun.PeriodKey);
        Assert.Equal(new DateOnly(2026, 4, 5), storedRun.PaymentDate);
        Assert.Equal(PayrollRunStatus.Finalized, storedRun.Status);
        Assert.Equal(2, storedRun.Lines.Count);
        Assert.Contains(storedRun.Lines, line => line.Code == "BASE" && line.AmountChf == 100m);
        Assert.Contains(storedRun.Lines, line => line.Code == "EXP" && line.AmountChf == 5m);
    }

    [Fact]
    public async Task PaymentDate_CanBeLoadedAndUpdatedForFinalizedPayrollRun()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = await CreateFinalizedMarchRunAsync(dbContext);
        var service = new PayrollRunService(new PayrollRunRepository(dbContext));

        var initialStatus = await service.GetMonthlyStatusAsync(new PayrollRunMonthlyStatusQuery(employee.Id, 2026, 3));
        await service.UpdatePaymentDateAsync(new UpdatePayrollRunPaymentDateCommand(employee.Id, 2026, 3, new DateOnly(2026, 4, 12)));
        dbContext.ChangeTracker.Clear();
        var updatedStatus = await service.GetMonthlyStatusAsync(new PayrollRunMonthlyStatusQuery(employee.Id, 2026, 3));
        var storedRun = await dbContext.PayrollRuns.SingleAsync(run => run.PeriodKey == "2026-03");

        Assert.Equal(new DateOnly(2026, 4, 5), initialStatus.PaymentDate);
        Assert.Equal(new DateOnly(2026, 4, 12), updatedStatus.PaymentDate);
        Assert.Equal(new DateOnly(2026, 4, 12), storedRun.PaymentDate);
    }

    [Fact]
    public async Task FinalizeMonthAsync_UsesRelevantContractForOpenMonth_AfterRetroactiveContractChange()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        var importedContract = new EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 1m, 0m, 0m);
        dbContext.EmploymentContracts.Add(importedContract);
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: WorkTimeSupplementSettings.Empty,
            ahvIvEoRate: 0m,
            alvRate: 0m,
            sicknessAccidentInsuranceRate: 0m,
            trainingAndHolidayRate: 0m,
            vacationCompensationRate: 0m,
            vacationCompensationRateAge50Plus: 0m,
            vehiclePauschalzone1RateChf: 0m,
            vehiclePauschalzone2RateChf: 0m,
            vehicleRegiezone1RateChf: 0m));
        await dbContext.SaveChangesAsync();

        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await monthlyRecordRepository.GetOrCreateAsync(employee.Id, 2026, 3, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 3, 31), 10m, 0m, 0m, 0m, 0m, 0m, 0m, null);
        await monthlyRecordRepository.SaveChangesAsync(CancellationToken.None);

        dbContext.EmploymentContracts.Remove(importedContract);
        dbContext.EmploymentContracts.Add(new EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 30m, 0m, 0m));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = new PayrollRunService(new PayrollRunRepository(dbContext));
        var result = await service.FinalizeMonthAsync(new FinalizePayrollMonthCommand(employee.Id, 2026, 3, new DateOnly(2026, 4, 5)));

        dbContext.ChangeTracker.Clear();
        var storedRun = await dbContext.PayrollRuns
            .Include(run => run.Lines)
            .SingleAsync(run => run.Id == result.PayrollRunId);

        Assert.Contains(storedRun.Lines, line => line.Code == "BASE" && line.RateChf == 30m && line.AmountChf == 300m);
    }

    [Fact]
    public async Task FinalizeMonthAsync_MonthlyWageUsesMonthlySalaryAmountWithoutBaseHours()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        dbContext.EmploymentContracts.Add(new EmploymentContract(
            employee.Id,
            new DateOnly(2026, 1, 1),
            null,
            30m,
            0m,
            0m,
            EmployeeWageType.Monthly,
            5200m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: WorkTimeSupplementSettings.Empty,
            ahvIvEoRate: 0m,
            alvRate: 0m,
            sicknessAccidentInsuranceRate: 0m,
            trainingAndHolidayRate: 0m,
            vacationCompensationRate: 0m,
            vacationCompensationRateAge50Plus: 0m,
            vehiclePauschalzone1RateChf: 0m,
            vehiclePauschalzone2RateChf: 0m,
            vehicleRegiezone1RateChf: 0m));
        await dbContext.SaveChangesAsync();

        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        await monthlyRecordRepository.GetOrCreateAsync(employee.Id, 2026, 3, CancellationToken.None);
        await monthlyRecordRepository.SaveChangesAsync(CancellationToken.None);

        var service = new PayrollRunService(new PayrollRunRepository(dbContext));
        var result = await service.FinalizeMonthAsync(new FinalizePayrollMonthCommand(employee.Id, 2026, 3, new DateOnly(2026, 4, 5)));

        var storedRun = await dbContext.PayrollRuns
            .Include(run => run.Lines)
            .SingleAsync(run => run.Id == result.PayrollRunId);

        Assert.Contains(storedRun.Lines, line => line.LineType == PayrollLineType.MonthlySalary && line.AmountChf == 5200m);
        Assert.DoesNotContain(storedRun.Lines, line => line.LineType == PayrollLineType.BaseHours);
    }

    [Fact]
    public async Task FinalizeMonthAsync_StoresWithholdingTaxValuesAsPayrollRunLines()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee(isSubjectToWithholdingTax: true);
        dbContext.Employees.Add(employee);
        dbContext.EmploymentContracts.Add(new EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 10m, 0m, 0m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: WorkTimeSupplementSettings.Empty,
            ahvIvEoRate: 0m,
            alvRate: 0m,
            sicknessAccidentInsuranceRate: 0m,
            trainingAndHolidayRate: 0m,
            vacationCompensationRate: 0m,
            vacationCompensationRateAge50Plus: 0m,
            vehiclePauschalzone1RateChf: 0m,
            vehiclePauschalzone2RateChf: 0m,
            vehicleRegiezone1RateChf: 0m));
        await dbContext.SaveChangesAsync();

        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await monthlyRecordRepository.GetOrCreateAsync(employee.Id, 2026, 3, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 3, 31), 100m, 0m, 0m, 0m, 0m, 0m, 0m, null);
        record.SaveWithholdingTaxInputs(7m, 25m, "Rueckzahlung");
        await monthlyRecordRepository.SaveChangesAsync(CancellationToken.None);

        var service = new PayrollRunService(new PayrollRunRepository(dbContext));
        var result = await service.FinalizeMonthAsync(new FinalizePayrollMonthCommand(employee.Id, 2026, 3, new DateOnly(2026, 4, 5)));

        var storedRun = await dbContext.PayrollRuns
            .Include(run => run.Lines)
            .SingleAsync(run => run.Id == result.PayrollRunId);

        Assert.Contains(storedRun.Lines, line => line.Code == "WITHHOLDING_TAX" && line.RateChf == 7m && line.Quantity == 1000m && line.AmountChf == -70m);
        Assert.Contains(storedRun.Lines, line => line.Code == "WITHHOLDING_TAX_CORRECTION" && line.AmountChf == 25m && line.Description.Contains("Rueckzahlung", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnnualSalary_ReadsOnlyFinalizedPayrollRunLines()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        dbContext.EmploymentContracts.Add(new EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 10m, 0m, 0m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: WorkTimeSupplementSettings.Empty,
            ahvIvEoRate: 0m,
            alvRate: 0m,
            sicknessAccidentInsuranceRate: 0m,
            trainingAndHolidayRate: 0m,
            vacationCompensationRate: 0m,
            vacationCompensationRateAge50Plus: 0m,
            vehiclePauschalzone1RateChf: 0m,
            vehiclePauschalzone2RateChf: 0m,
            vehicleRegiezone1RateChf: 0m));
        await dbContext.SaveChangesAsync();

        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await monthlyRecordRepository.GetOrCreateAsync(employee.Id, 2026, 3, CancellationToken.None);
        var timeEntry = record.SaveTimeEntry(null, new DateOnly(2026, 3, 31), 10m, 0m, 0m, 0m, 0m, 0m, 0m, null);
        await monthlyRecordRepository.SaveChangesAsync(CancellationToken.None);

        var payrollRunService = new PayrollRunService(new PayrollRunRepository(dbContext));
        await payrollRunService.FinalizeMonthAsync(new FinalizePayrollMonthCommand(employee.Id, 2026, 3, new DateOnly(2026, 4, 5)));

        timeEntry.Update(new DateOnly(2026, 3, 31), 20m, 0m, 0m, 0m, null, 0m, 0m, 0m);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var overview = await new AnnualSalaryRepository(dbContext).GetOverviewAsync(
            new AnnualSalaryOverviewQuery(employee.Id, 2026),
            CancellationToken.None);

        var march = overview.Months.Single(month => month.Month == 3);
        var april = overview.Months.Single(month => month.Month == 4);

        Assert.Equal(100m, march.GrossSalaryChf);
        Assert.Equal("abgeschlossen", march.StatusDisplay);
        Assert.True(march.IsStatusFinalized);
        Assert.False(march.IsStatusOpenWithRecordedData);
        Assert.Equal("offen", april.StatusDisplay);
        Assert.Equal(100m, overview.Totals.GrossSalaryChf);
    }

    [Fact]
    public async Task AnnualSalary_ShowsOpenMonthWithRecordedDataWithoutIncludingAmounts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        dbContext.EmploymentContracts.Add(new EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 10m, 0m, 0m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: WorkTimeSupplementSettings.Empty,
            ahvIvEoRate: 0m,
            alvRate: 0m,
            sicknessAccidentInsuranceRate: 0m,
            trainingAndHolidayRate: 0m,
            vacationCompensationRate: 0m,
            vacationCompensationRateAge50Plus: 0m,
            vehiclePauschalzone1RateChf: 0m,
            vehiclePauschalzone2RateChf: 0m,
            vehicleRegiezone1RateChf: 0m));
        await dbContext.SaveChangesAsync();

        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await monthlyRecordRepository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 4, 30), 8m, 0m, 0m, 0m, 0m, 0m, 0m, null);
        await monthlyRecordRepository.SaveChangesAsync(CancellationToken.None);
        dbContext.ChangeTracker.Clear();

        var overview = await new AnnualSalaryRepository(dbContext).GetOverviewAsync(
            new AnnualSalaryOverviewQuery(employee.Id, 2026),
            CancellationToken.None);

        var april = overview.Months.Single(month => month.Month == 4);

        Assert.True(april.HasRecordedMonthData);
        Assert.Equal("offen / Daten vorhanden", april.StatusDisplay);
        Assert.True(april.IsStatusOpenWithRecordedData);
        Assert.False(april.IsStatusFinalized);
        Assert.Equal(0m, april.GrossSalaryChf);
        Assert.Equal(0m, overview.Totals.GrossSalaryChf);
    }

    [Fact]
    public async Task GetMonthlyStatusAsync_ReturnsFinalizedAfterEmployeeMonthWasClosed()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        dbContext.EmploymentContracts.Add(new EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 10m, 0m, 0m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: WorkTimeSupplementSettings.Empty,
            ahvIvEoRate: 0m,
            alvRate: 0m,
            sicknessAccidentInsuranceRate: 0m,
            trainingAndHolidayRate: 0m,
            vacationCompensationRate: 0m,
            vacationCompensationRateAge50Plus: 0m,
            vehiclePauschalzone1RateChf: 0m,
            vehiclePauschalzone2RateChf: 0m,
            vehicleRegiezone1RateChf: 0m));
        await dbContext.SaveChangesAsync();

        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await monthlyRecordRepository.GetOrCreateAsync(employee.Id, 2026, 3, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 3, 31), 10m, 0m, 0m, 0m, 0m, 0m, 0m, null);
        await monthlyRecordRepository.SaveChangesAsync(CancellationToken.None);

        var service = new PayrollRunService(new PayrollRunRepository(dbContext));

        var openStatus = await service.GetMonthlyStatusAsync(new PayrollRunMonthlyStatusQuery(employee.Id, 2026, 3));
        await service.FinalizeMonthAsync(new FinalizePayrollMonthCommand(employee.Id, 2026, 3, new DateOnly(2026, 4, 5)));
        var finalizedStatus = await service.GetMonthlyStatusAsync(new PayrollRunMonthlyStatusQuery(employee.Id, 2026, 3));

        Assert.False(openStatus.IsFinalized);
        Assert.True(finalizedStatus.IsFinalized);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FinalizeMonthAsync(new FinalizePayrollMonthCommand(employee.Id, 2026, 3, new DateOnly(2026, 4, 5))));
    }

    [Fact]
    public async Task CancelMonthAsync_CancelsFinalizedRunAndKeepsStoredLines()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = await CreateFinalizedMarchRunAsync(dbContext);
        var service = new PayrollRunService(new PayrollRunRepository(dbContext));

        await service.CancelMonthAsync(new CancelPayrollRunCommand(employee.Id, 2026, 3));

        dbContext.ChangeTracker.Clear();
        var storedRun = await dbContext.PayrollRuns
            .Include(run => run.Lines)
            .SingleAsync(run => run.PeriodKey == "2026-03");
        var status = await service.GetMonthlyStatusAsync(new PayrollRunMonthlyStatusQuery(employee.Id, 2026, 3));

        Assert.Equal(PayrollRunStatus.Cancelled, storedRun.Status);
        Assert.NotNull(storedRun.CancelledAtUtc);
        Assert.NotEmpty(storedRun.Lines);
        Assert.False(status.IsFinalized);
        Assert.True(status.HasCancelledRun);
        Assert.Equal("storniert", status.StatusDisplay);
    }

    [Fact]
    public async Task AnnualSalary_IgnoresCancelledPayrollRunLines()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = await CreateFinalizedMarchRunAsync(dbContext);
        var service = new PayrollRunService(new PayrollRunRepository(dbContext));

        await service.CancelMonthAsync(new CancelPayrollRunCommand(employee.Id, 2026, 3));
        dbContext.ChangeTracker.Clear();

        var overview = await new AnnualSalaryRepository(dbContext).GetOverviewAsync(
            new AnnualSalaryOverviewQuery(employee.Id, 2026),
            CancellationToken.None);

        var march = overview.Months.Single(month => month.Month == 3);
        Assert.True(march.HasCancelledRun);
        Assert.True(march.HasRecordedMonthData);
        Assert.Equal("storniert", march.StatusDisplay);
        Assert.True(march.IsStatusCancelled);
        Assert.False(march.IsStatusOpenWithRecordedData);
        Assert.Equal(0m, march.GrossSalaryChf);
        Assert.Equal(0m, overview.Totals.GrossSalaryChf);
        Assert.Equal(0m, overview.Totals.SocialInsuranceDeductionChf);
    }

    [Fact]
    public async Task AnnualSalary_AggregatesSocialDeductionsPerMonthAndYear()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);

        var marchRun = new PayrollRun("2026-03", new DateOnly(2026, 3, 31));
        marchRun.AddLine(PayrollRunLine.CreateCalculatedHourlyLine(employee.Id, PayrollLineType.BaseHours, "BASE", "Basislohn", 10m, 100m));
        marchRun.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employee.Id, PayrollLineType.SocialContribution, "AHV_IV_EO", "AHV/IV/EO", 50m));
        marchRun.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employee.Id, PayrollLineType.SocialContribution, "ALV", "ALV", 10m));
        marchRun.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employee.Id, PayrollLineType.SocialContribution, "KTG_UVG", "Krankentaggeld/UVG", 20m));
        marchRun.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employee.Id, PayrollLineType.SocialContribution, "AUSBILDUNG_FERIEN", "Aus- und Weiterbildung inkl. Ferien", 30m));
        marchRun.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employee.Id, PayrollLineType.Tax, "WITHHOLDING_TAX", "Quellensteuer", 80m));
        marchRun.FinalizeRun();

        var aprilRun = new PayrollRun("2026-04", new DateOnly(2026, 4, 30));
        aprilRun.AddLine(PayrollRunLine.CreateCalculatedHourlyLine(employee.Id, PayrollLineType.BaseHours, "BASE", "Basislohn", 5m, 100m));
        aprilRun.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employee.Id, PayrollLineType.SocialContribution, "AHV_IV_EO", "AHV/IV/EO", 25m));
        aprilRun.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employee.Id, PayrollLineType.SocialContribution, "ALV", "ALV", 5m));
        aprilRun.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employee.Id, PayrollLineType.SocialContribution, "KTG_UVG", "Krankentaggeld/UVG", 10m));
        aprilRun.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employee.Id, PayrollLineType.SocialContribution, "AUSBILDUNG_FERIEN", "Aus- und Weiterbildung inkl. Ferien", 15m));
        aprilRun.AddLine(PayrollRunLine.CreateManualChfLine(employee.Id, PayrollLineType.Tax, "WITHHOLDING_TAX_CORRECTION", "Rueckzahlung", 20m));
        aprilRun.FinalizeRun();

        var cancelledRun = new PayrollRun("2026-05", new DateOnly(2026, 5, 31));
        cancelledRun.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employee.Id, PayrollLineType.SocialContribution, "AHV_IV_EO", "AHV/IV/EO", 999m));
        cancelledRun.FinalizeRun();
        cancelledRun.Cancel(DateTimeOffset.UtcNow);

        dbContext.PayrollRuns.AddRange(marchRun, aprilRun, cancelledRun);
        await dbContext.SaveChangesAsync();

        var overview = await new AnnualSalaryRepository(dbContext).GetOverviewAsync(
            new AnnualSalaryOverviewQuery(employee.Id, 2026),
            CancellationToken.None);

        var march = overview.Months.Single(month => month.Month == 3);
        var april = overview.Months.Single(month => month.Month == 4);
        var may = overview.Months.Single(month => month.Month == 5);

        Assert.Equal(50m, march.AhvIvEoDeductionChf);
        Assert.Equal(10m, march.AlvDeductionChf);
        Assert.Equal(20m, march.SicknessDailyAllowanceDeductionChf);
        Assert.Equal(30m, march.TrainingAndEducationDeductionChf);
        Assert.Equal(110m, march.TotalSocialDeductionChf);
        Assert.Equal(80m, march.WithholdingTaxChf);
        Assert.Equal(55m, april.TotalSocialDeductionChf);
        Assert.Equal(20m, april.WithholdingTaxChf);
        Assert.Equal(0m, may.TotalSocialDeductionChf);
        Assert.Equal(75m, overview.Totals.AhvIvEoDeductionChf);
        Assert.Equal(15m, overview.Totals.AlvDeductionChf);
        Assert.Equal(30m, overview.Totals.SicknessDailyAllowanceDeductionChf);
        Assert.Equal(45m, overview.Totals.TrainingAndEducationDeductionChf);
        Assert.Equal(165m, overview.Totals.SocialInsuranceDeductionChf);
        Assert.Equal(100m, overview.Totals.WithholdingTaxChf);
    }

    [Fact]
    public async Task FinalizeMonthAsync_AllowsNewFinalizedRunAfterCancellation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = await CreateFinalizedMarchRunAsync(dbContext);
        var service = new PayrollRunService(new PayrollRunRepository(dbContext));

        await service.CancelMonthAsync(new CancelPayrollRunCommand(employee.Id, 2026, 3));
        var newRun = await service.FinalizeMonthAsync(new FinalizePayrollMonthCommand(employee.Id, 2026, 3, new DateOnly(2026, 4, 5)));

        dbContext.ChangeTracker.Clear();
        var storedRuns = await dbContext.PayrollRuns
            .Include(run => run.Lines)
            .Where(run => run.PeriodKey == "2026-03")
            .ToListAsync();
        var status = await service.GetMonthlyStatusAsync(new PayrollRunMonthlyStatusQuery(employee.Id, 2026, 3));

        Assert.Equal(2, storedRuns.Count);
        Assert.Contains(storedRuns, run => run.Status == PayrollRunStatus.Cancelled);
        Assert.Contains(storedRuns, run => run.Id == newRun.PayrollRunId && run.Status == PayrollRunStatus.Finalized);
        Assert.True(status.IsFinalized);
        Assert.Equal("abgeschlossen", status.StatusDisplay);
    }

    [Fact]
    public async Task CancelMonthAsync_PreventsDoubleCancellation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = await CreateFinalizedMarchRunAsync(dbContext);
        var service = new PayrollRunService(new PayrollRunRepository(dbContext));

        await service.CancelMonthAsync(new CancelPayrollRunCommand(employee.Id, 2026, 3));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CancelMonthAsync(new CancelPayrollRunCommand(employee.Id, 2026, 3)));

        Assert.Contains("already cancelled", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenMonthlyPreview_UsesRetroactiveHourlySettingsInsteadOfStaleSnapshot()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        dbContext.EmploymentContracts.Add(new EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 10m, 0m, 0m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: WorkTimeSupplementSettings.Empty,
            ahvIvEoRate: 0m,
            alvRate: 0m,
            sicknessAccidentInsuranceRate: 0m,
            trainingAndHolidayRate: 0m,
            vacationCompensationRate: 0m,
            vacationCompensationRateAge50Plus: 0m,
            vehiclePauschalzone1RateChf: 0m,
            vehiclePauschalzone2RateChf: 0m,
            vehicleRegiezone1RateChf: 0m));
        await dbContext.SaveChangesAsync();

        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await monthlyRecordRepository.GetOrCreateAsync(employee.Id, 2026, 3, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 3, 31), 10m, 2m, 1m, 1m, 0m, 0m, 0m, null);
        await monthlyRecordRepository.SaveChangesAsync(CancellationToken.None);

        await new PayrollSettingsRepository(dbContext).SaveAsync(
            CreateSettingsCommand(
                hourlySettingsValidFrom: new DateOnly(2026, 1, 1),
                nightSupplementRate: 0.25m,
                sundaySupplementRate: 0.50m,
                holidaySupplementRate: 1.00m),
            CancellationToken.None);
        dbContext.ChangeTracker.Clear();

        var details = await new EmployeeMonthlyRecordRepository(dbContext).GetDetailsAsync(record.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.DoesNotContain(details.PayrollPreview.Notes, note => note.Contains("MISSING_NIGHT_RULE", StringComparison.Ordinal));
        Assert.DoesNotContain(details.PayrollPreview.Notes, note => note.Contains("MISSING_SUN_RULE", StringComparison.Ordinal));
        Assert.DoesNotContain(details.PayrollPreview.Notes, note => note.Contains("MISSING_HOL_RULE", StringComparison.Ordinal));
        Assert.Contains(details.PayrollPreview.Lines, line => line.Code == PayrollPreviewHelpCatalog.TimeSupplementCode && line.AmountDisplay.Contains("20.00", StringComparison.Ordinal));
    }

    private static Employee CreateEmployee(bool isSubjectToWithholdingTax = false)
    {
        return new Employee(
            "9001",
            "Noemi",
            "Meier",
            new DateOnly(1990, 1, 1),
            new DateOnly(2020, 1, 1),
            null,
            true,
            new EmployeeAddress("Dorfstrasse", "1", null, "6300", "Zug", "Schweiz"),
            "Schweiz",
            "CH",
            "B",
            isSubjectToWithholdingTax ? "B" : "Ordentlich",
            isSubjectToWithholdingTax,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            EmployeeWageType.Hourly);
    }

    private static async Task<Employee> CreateFinalizedMarchRunAsync(PayrollDbContext dbContext)
    {
        var employee = CreateEmployee();
        dbContext.Employees.Add(employee);
        dbContext.EmploymentContracts.Add(new EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 10m, 0m, 0m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: WorkTimeSupplementSettings.Empty,
            ahvIvEoRate: 0m,
            alvRate: 0m,
            sicknessAccidentInsuranceRate: 0m,
            trainingAndHolidayRate: 0m,
            vacationCompensationRate: 0m,
            vacationCompensationRateAge50Plus: 0m,
            vehiclePauschalzone1RateChf: 0m,
            vehiclePauschalzone2RateChf: 0m,
            vehicleRegiezone1RateChf: 0m));
        await dbContext.SaveChangesAsync();

        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await monthlyRecordRepository.GetOrCreateAsync(employee.Id, 2026, 3, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 3, 31), 10m, 0m, 0m, 0m, 0m, 0m, 0m, null);
        record.SaveExpenseEntry(5m);
        await monthlyRecordRepository.SaveChangesAsync(CancellationToken.None);

        var service = new PayrollRunService(new PayrollRunRepository(dbContext));
        await service.FinalizeMonthAsync(new FinalizePayrollMonthCommand(employee.Id, 2026, 3, new DateOnly(2026, 4, 5)));

        return employee;
    }

    private static SavePayrollSettingsCommand CreateSettingsCommand(
        DateOnly? hourlySettingsValidFrom = null,
        decimal? nightSupplementRate = null,
        decimal? sundaySupplementRate = null,
        decimal? holidaySupplementRate = null)
    {
        return new SavePayrollSettingsCommand(
            "Blesinger Sicherheits Dienste GmbH",
            "Aptos",
            14m,
            "#FF101820",
            "#FF667788",
            "#FFF6F8FB",
            "#FF224466",
            "BSD",
            "/tmp/app-logo.png",
            "Helvetica",
            10m,
            "#FF000000",
            "#FF556677",
            "#FFFFFF00",
            "BSD",
            "/tmp/print-logo.png",
            "",
            global::Payroll.Domain.Settings.PayrollSettings.DefaultSalaryCertificatePdfTemplatePath,
            ".",
            " ",
            "CHF",
            nightSupplementRate,
            sundaySupplementRate,
            holidaySupplementRate,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            PayrollPreviewHelpCatalog.GetDefaultOptions(),
            [new SettingOptionDto(Guid.NewGuid(), "Sicherheit")],
            [new SettingOptionDto(Guid.NewGuid(), "A")],
            [new SettingOptionDto(Guid.NewGuid(), "Zug")],
            null,
            new DateOnly(2026, 1, 1),
            null,
            null,
            hourlySettingsValidFrom ?? new DateOnly(2026, 1, 1),
            null,
            null,
            new DateOnly(2026, 1, 1),
            null);
    }
}
