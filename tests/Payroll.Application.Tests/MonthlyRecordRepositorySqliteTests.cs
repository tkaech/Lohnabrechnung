using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Settings;
using Payroll.Desktop.ViewModels;
using Payroll.Domain.Employees;
using Payroll.Domain.MonthlyRecords;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.MonthlyRecords;
using Payroll.Infrastructure.Persistence;
using System.Text.Json;

namespace Payroll.Application.Tests;

public sealed class MonthlyRecordRepositorySqliteTests
{
    [Fact]
    public async Task GetOrCreateAsync_EnforcesSingleMonthlyRecordPerEmployeeAndMonth()
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
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var first = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
        var second = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);

        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task SaveAndLoadDetailsAsync_PersistsTimeVehicleAndExpenseValuesWithinMonthlyContext()
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
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m, 3.00m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 5.6m,
            vehiclePauschalzone2RateChf: 16.8m,
            vehicleRegiezone1RateChf: 0.32m));
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 4, 5), 8m, 1m, 0m, 0m, 120m, 80m, 60m, "Fruehdienst");
        record.SaveExpenseEntry(18.50m);
        await repository.SaveChangesAsync(CancellationToken.None);

        var details = await repository.GetDetailsAsync(record.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Single(details!.TimeEntries);
        Assert.NotNull(details.ExpenseEntry);
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Basislohn" && line.AmountDisplay == "260,00 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Ferienentschaedigung" && line.AmountDisplay == "247,63 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Spezialzuschlag gemaess Vertrag" && line.AmountDisplay == "24,00 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Fahrzeitentschaedigung Pauschalzone 1" && line.AmountDisplay == "672,00 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Spesen gemaess Nachweis" && line.AmountDisplay == "18,50 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Total Auszahlung");
        Assert.Equal(8m, details.Header.TotalWorkedHours);
        Assert.Equal(18.50m, details.Header.TotalExpensesChf);
        Assert.Equal(260m, details.Header.TotalVehicleCompensationChf);
        Assert.Equal(120m, details.TimeEntries.Single().VehiclePauschalzone1Chf);
    }

    [Fact]
    public async Task GetDetailsAsync_UsesStoredMonthlyPayrollParameterSnapshot_AfterGlobalSettingsChange()
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
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m, 3.00m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 5.6m,
            vehiclePauschalzone2RateChf: 16.8m,
            vehicleRegiezone1RateChf: 0.32m));
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 4, 5), 8m, 1m, 0m, 0m, 120m, 0m, 0m, "Fruehdienst");
        await repository.SaveChangesAsync(CancellationToken.None);

        var currentSettings = await dbContext.PayrollSettings.SingleAsync();
        currentSettings.UpdateWorkTimeSupplementSettings(new WorkTimeSupplementSettings(0.75m, 0.90m, 1.20m));
        currentSettings.UpdateDeductionAndVehicleRates(
            0.06m,
            0.02m,
            0.01m,
            0.0003m,
            0.12m,
            0.14m,
            99m,
            88m,
            77m);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var details = await repository.GetDetailsAsync(record.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Contains(details!.PayrollPreview.Lines, line => line.Label == "Stunden mit Zeitzuschlag" && line.AmountDisplay == "8,13 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Stunden mit Zeitzuschlag" && line.RateDisplay == "Nacht 25 %");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Fahrzeitentschaedigung Pauschalzone 1" && line.AmountDisplay == "672,00 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Ferienentschaedigung" && line.RateDisplay == "10,64 %");
    }

    [Fact]
    public async Task GetDetailsAsync_HidesConfiguredHelpTextsInPayrollPreview()
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
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m, 3.00m));
        var settings = new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 5.6m,
            vehiclePauschalzone2RateChf: 16.8m,
            vehicleRegiezone1RateChf: 0.32m);
        settings.UpdatePayrollPreviewHelpVisibilityJson(JsonSerializer.Serialize(
            PayrollPreviewHelpCatalog.ToDomain(
            [
                new PayrollPreviewHelpOptionDto(PayrollPreviewHelpCatalog.SpecialSupplementCode, "Spezialzuschlag gemaess Vertrag", false, "Arbeitsstunden multipliziert mit dem vertraglichen Spezialzuschlag.")
            ])));
        dbContext.PayrollSettings.Add(settings);
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 4, 5), 8m, 0m, 0m, 0m, 0m, 0m, 0m, "Fruehdienst");
        await repository.SaveChangesAsync(CancellationToken.None);

        var details = await repository.GetDetailsAsync(record.Id, CancellationToken.None);

        var specialSupplementLine = Assert.Single(details!.PayrollPreview.Lines, line => line.Code == PayrollPreviewHelpCatalog.SpecialSupplementCode);
        Assert.Null(specialSupplementLine.Detail);
    }

    [Fact]
    public async Task GetDetailsAsync_UsesConfiguredHelpText_ForEnabledConfiguredPosition()
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
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m, 3.00m));
        var settings = new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 5.6m,
            vehiclePauschalzone2RateChf: 16.8m,
            vehicleRegiezone1RateChf: 0.32m);
        settings.UpdatePayrollPreviewHelpVisibilityJson(JsonSerializer.Serialize(
            PayrollPreviewHelpCatalog.ToDomain(
            [
                new PayrollPreviewHelpOptionDto(
                    PayrollPreviewHelpCatalog.TotalCode,
                    "Total",
                    true,
                    "Eigener Test-Hinweis fuer Total.")
            ])));
        dbContext.PayrollSettings.Add(settings);
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 4, 5), 8m, 0m, 0m, 0m, 0m, 0m, 0m, "Fruehdienst");
        await repository.SaveChangesAsync(CancellationToken.None);

        var details = await repository.GetDetailsAsync(record.Id, CancellationToken.None);

        var totalLine = Assert.Single(details!.PayrollPreview.Lines, line => line.Code == PayrollPreviewHelpCatalog.TotalCode);
        Assert.Equal("Eigener Test-Hinweis fuer Total.", totalLine.Detail);

        var expensesLine = Assert.Single(details.PayrollPreview.Lines, line => line.Code == PayrollPreviewHelpCatalog.ExpensesCode);
        Assert.Null(expensesLine.Detail);
    }

    [Fact]
    public async Task GetDetailsAsync_UsesExistingDetailText_WhenHelpTextIsEnabledWithoutOverride()
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
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m, 3.00m));
        var settings = new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 5.6m,
            vehiclePauschalzone2RateChf: 16.8m,
            vehicleRegiezone1RateChf: 0.32m);
        settings.UpdatePayrollPreviewHelpVisibilityJson(JsonSerializer.Serialize(
            PayrollPreviewHelpCatalog.ToDomain(
            [
                new PayrollPreviewHelpOptionDto(
                    PayrollPreviewHelpCatalog.SpecialSupplementCode,
                    "Spezialzuschlag gemaess Vertrag",
                    true,
                    string.Empty)
            ])));
        dbContext.PayrollSettings.Add(settings);
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 4, 5), 8m, 0m, 0m, 0m, 0m, 0m, 0m, "Fruehdienst");
        await repository.SaveChangesAsync(CancellationToken.None);

        var details = await repository.GetDetailsAsync(record.Id, CancellationToken.None);

        var specialSupplementLine = Assert.Single(details!.PayrollPreview.Lines, line => line.Code == PayrollPreviewHelpCatalog.SpecialSupplementCode);
        Assert.Equal("Arbeitsstunden multipliziert mit dem vertraglichen Spezialzuschlag.", specialSupplementLine.Detail);
    }

    [Fact]
    public async Task GetDetailsAsync_UsesStoredMonthlyContractSnapshot_AfterContractChange()
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
        var originalContract = new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m, 3.00m);
        dbContext.EmploymentContracts.Add(originalContract);
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 5.6m,
            vehiclePauschalzone2RateChf: 16.8m,
            vehicleRegiezone1RateChf: 0.32m));
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 4, 5), 8m, 0m, 0m, 0m, 0m, 0m, 0m, "Fruehdienst");
        await repository.SaveChangesAsync(CancellationToken.None);

        dbContext.EmploymentContracts.Remove(originalContract);
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 6, 1), null, 45m, 500m, 6m));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var details = await repository.GetDetailsAsync(record.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Contains(details!.PayrollPreview.Lines, line => line.Label == "Basislohn" && line.AmountDisplay == "260,00 CHF");
        Assert.Contains(details.PayrollPreview.Lines, line => line.Label == "Spezialzuschlag gemaess Vertrag" && line.AmountDisplay == "24,00 CHF");
    }

    [Fact]
    public async Task GetDetailsAsync_WithoutTimeEntries_ReturnsOnlyMonthNotRecordedHint()
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
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m, 3.00m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            workTimeSupplementSettings: new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            ahvIvEoRate: 0.053m,
            alvRate: 0.011m,
            sicknessAccidentInsuranceRate: 0.00821m,
            trainingAndHolidayRate: 0.00015m,
            vacationCompensationRate: 0.1064m,
            vacationCompensationRateAge50Plus: 0.1264m,
            vehiclePauschalzone1RateChf: 5.6m,
            vehiclePauschalzone2RateChf: 16.8m,
            vehicleRegiezone1RateChf: 0.32m));
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        record.SaveExpenseEntry(18.50m);
        await repository.SaveChangesAsync(CancellationToken.None);

        var details = await repository.GetDetailsAsync(record.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Empty(details!.PayrollPreview.Lines);
        Assert.Single(details.PayrollPreview.Notes, note => note == "Monat noch nicht erfasst");
    }

    [Fact]
    public async Task GetDetailsAsync_LoadsTimeAndExpenseHistoryAcrossMonthsChronologically()
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
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var aprilRecord = await repository.GetOrCreateAsync(employee.Id, 2026, 4, CancellationToken.None);
        aprilRecord.SaveTimeEntry(null, new DateOnly(2026, 4, 10), 7m, 0m, 0m, 0m, 0m, 0m, 0m, "April");
        aprilRecord.SaveExpenseEntry(18.50m);

        var mayRecord = await repository.GetOrCreateAsync(employee.Id, 2026, 5, CancellationToken.None);
        mayRecord.SaveTimeEntry(null, new DateOnly(2026, 5, 3), 8m, 0m, 0m, 0m, 0m, 0m, 0m, "Mai");
        mayRecord.SaveExpenseEntry(22m);
        await repository.SaveChangesAsync(CancellationToken.None);

        var details = await repository.GetDetailsAsync(mayRecord.Id, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(2, details!.TimeEntryHistory.Count);
        Assert.Equal(new DateOnly(2026, 4, 10), details.TimeEntryHistory.First().WorkDate);
        Assert.Equal(new DateOnly(2026, 5, 3), details.TimeEntryHistory.Last().WorkDate);
        Assert.Equal(2, details.ExpenseEntryHistory.Count);
        Assert.Equal((2026, 4), (details.ExpenseEntryHistory.First().Year, details.ExpenseEntryHistory.First().Month));
        Assert.Equal((2026, 5), (details.ExpenseEntryHistory.Last().Year, details.ExpenseEntryHistory.Last().Month));
    }

    [Fact]
    public async Task ListTimeCaptureOverviewAsync_ReturnsEmployeesWithAndWithoutMonthCapture()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var firstEmployee = CreateEmployee("9000", "Anna", "Aktiv");
        var secondEmployee = CreateEmployee("9001", "Bruno", "Leer");
        dbContext.Employees.Add(firstEmployee);
        dbContext.Employees.Add(secondEmployee);
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var aprilRecord = await repository.GetOrCreateAsync(firstEmployee.Id, 2026, 4, CancellationToken.None);
        aprilRecord.SaveTimeEntry(null, new DateOnly(2026, 4, 10), 7m, 1m, 0m, 0m, 2m, 3m, 4m, "April");
        await repository.SaveChangesAsync(CancellationToken.None);

        var rows = await repository.ListTimeCaptureOverviewAsync(2026, 4, CancellationToken.None);

        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, row => row.PersonnelNumber == "9000" && row.HasMonthCapture && row.HoursWorked == 7m && row.TimeEntryCount == 1);
        Assert.Contains(rows, row => row.PersonnelNumber == "9001" && !row.HasMonthCapture && row.HoursWorked == 0m && row.TimeEntryCount == 0);
    }

    [Fact]
    public async Task ExpenseEntries_ModelEnforcesUniqueMonthlyRecordReference()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var entityType = dbContext.Model.FindEntityType(typeof(global::Payroll.Domain.Expenses.ExpenseEntry));
        var uniqueForeignKeyIndex = entityType!
            .GetIndexes()
            .Single(index => index.Properties.Single().Name == nameof(global::Payroll.Domain.Expenses.ExpenseEntry.EmployeeMonthlyRecordId));

        Assert.True(uniqueForeignKeyIndex.IsUnique);
    }

    [Fact]
    public async Task UniqueIndex_RejectsDuplicateMonthlyRecordPerEmployeeAndMonth()
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
        await dbContext.SaveChangesAsync();

        dbContext.EmployeeMonthlyRecords.Add(new EmployeeMonthlyRecord(employee.Id, 2026, 4));
        dbContext.EmployeeMonthlyRecords.Add(new EmployeeMonthlyRecord(employee.Id, 2026, 4));

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task MonthlyRecordViewModel_SaveExpenseEntry_PersistsAgainstSqliteRepository()
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
        dbContext.EmploymentContracts.Add(new global::Payroll.Domain.Employees.EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 32.5m, 280m, 3.00m));
        await dbContext.SaveChangesAsync();

        var repository = new EmployeeMonthlyRecordRepository(dbContext);
        var service = new MonthlyRecordService(repository);
        var viewModel = new MonthlyRecordViewModel(service)
        {
            SelectedMonth = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero)
        };

        await viewModel.SetEmployeeAsync(employee.Id, "Anna Aktiv");
        viewModel.ExpensesTotal = "18.5";

        viewModel.SaveExpenseEntryCommand.Execute(null);

        var startedAt = DateTime.UtcNow;
        while (viewModel.ActionMessage != "Spesen gespeichert." && (DateTime.UtcNow - startedAt).TotalSeconds < 3)
        {
            await Task.Delay(20);
        }

        Assert.Equal("Spesen gespeichert.", viewModel.ActionMessage);
        Assert.Equal("18,50", viewModel.ExpensesTotal);
        Assert.Contains("Spesen 18,50 CHF", viewModel.TotalsSummary, StringComparison.Ordinal);
    }

    private static global::Payroll.Domain.Employees.Employee CreateEmployee(string personnelNumber = "9000", string firstName = "Anna", string lastName = "Aktiv")
    {
        return new global::Payroll.Domain.Employees.Employee(
            personnelNumber,
            firstName,
            lastName,
            new DateOnly(1990, 2, 1),
            new DateOnly(2025, 1, 1),
            null,
            true,
            new global::Payroll.Domain.Employees.EmployeeAddress("Teststrasse", "1", null, "8000", "Zuerich", "Schweiz"),
            "Schweiz",
            "CH",
            "B",
            "Ordentlich",
            false,
            "756.1234.5678.90",
            "CH9300762011623852957",
            "+41 79 123 45 67",
            "anna.aktiv@example.ch",
            null,
            null,
            null);
    }
}
