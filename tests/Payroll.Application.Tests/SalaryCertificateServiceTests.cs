using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.AnnualSalary;
using Payroll.Application.SalaryCertificate;
using Payroll.Domain.Employees;
using Payroll.Domain.Payroll;
using Payroll.Infrastructure.AnnualSalary;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Application.Tests;

public sealed class SalaryCertificateServiceTests
{
    [Fact]
    public async Task CreateAsync_UsesOnlyFinalizedRuns()
    {
        await using var context = await CreateContextAsync();
        var employee = CreateEmployee();
        context.Employees.Add(employee);

        var finalizedRun = CreateRun("2026-03", new DateOnly(2026, 3, 31), employee.Id, grossSalaryChf: 1000m);
        finalizedRun.FinalizeRun();
        var openRun = CreateRun("2026-04", new DateOnly(2026, 4, 30), employee.Id, grossSalaryChf: 999m);

        context.PayrollRuns.AddRange(finalizedRun, openRun);
        await context.SaveChangesAsync();

        var certificate = await CreateService(context).CreateAsync(new SalaryCertificateQuery(employee.Id, 2026));

        Assert.Equal(1000m, GetAmount(certificate, SalaryCertificateFieldCodes.SalaryGrossWageTotalCode8));
    }

    [Fact]
    public async Task CreateAsync_IgnoresCancelledRuns()
    {
        await using var context = await CreateContextAsync();
        var employee = CreateEmployee();
        context.Employees.Add(employee);

        var finalizedRun = CreateRun("2026-03", new DateOnly(2026, 3, 31), employee.Id, grossSalaryChf: 1000m);
        finalizedRun.FinalizeRun();
        var cancelledRun = CreateRun("2026-05", new DateOnly(2026, 5, 31), employee.Id, grossSalaryChf: 888m);
        cancelledRun.FinalizeRun();
        cancelledRun.Cancel(DateTimeOffset.UtcNow);

        context.PayrollRuns.AddRange(finalizedRun, cancelledRun);
        await context.SaveChangesAsync();

        var certificate = await CreateService(context).CreateAsync(new SalaryCertificateQuery(employee.Id, 2026));

        Assert.Equal(1000m, GetAmount(certificate, SalaryCertificateFieldCodes.SalaryGrossWageTotalCode8));
    }

    [Fact]
    public async Task CreateAsync_MapsPayrollTotalsToCertificateFields()
    {
        await using var context = await CreateContextAsync();
        var employee = CreateEmployee();
        context.Employees.Add(employee);

        var finalizedRun = CreateRun(
            "2026-03",
            new DateOnly(2026, 3, 31),
            employee.Id,
            grossSalaryChf: 1000m,
            socialDeductionChf: 100m,
            bvgDeductionChf: 50m,
            withholdingTaxChf: 25m,
            expensesChf: 30m);
        finalizedRun.FinalizeRun();

        context.PayrollRuns.Add(finalizedRun);
        await context.SaveChangesAsync();

        var certificate = await CreateService(context).CreateAsync(new SalaryCertificateQuery(employee.Id, 2026));

        Assert.Equal("2026", GetText(certificate, SalaryCertificateFieldCodes.CertificateYear));
        Assert.Equal("756.1234.5678.97", GetText(certificate, SalaryCertificateFieldCodes.EmployeeAhvNumber));
        Assert.Equal(new DateOnly(1990, 1, 1), GetDate(certificate, SalaryCertificateFieldCodes.EmployeeBirthDate));
        Assert.Equal(1000m, GetAmount(certificate, SalaryCertificateFieldCodes.SalaryWageCode1));
        Assert.Equal(1000m, GetAmount(certificate, SalaryCertificateFieldCodes.SalaryGrossWageTotalCode8));
        Assert.Equal(100m, GetAmount(certificate, SalaryCertificateFieldCodes.DeductionsSocialSecurityCode9));
        Assert.Equal(50m, GetAmount(certificate, SalaryCertificateFieldCodes.DeductionsPensionFundCode10));
        Assert.Equal(825m, GetAmount(certificate, SalaryCertificateFieldCodes.SalaryNetWageCode11));
        Assert.Equal(25m, GetAmount(certificate, SalaryCertificateFieldCodes.TaxSourceTaxCode12));
        Assert.Equal(30m, GetAmount(certificate, SalaryCertificateFieldCodes.ExpensesCode13));
    }

    [Fact]
    public async Task CreateAsync_DoesNotUseOpenMonths()
    {
        await using var context = await CreateContextAsync();
        var employee = CreateEmployee();
        context.Employees.Add(employee);

        var openRun = CreateRun(
            "2026-04",
            new DateOnly(2026, 4, 30),
            employee.Id,
            grossSalaryChf: 999m,
            socialDeductionChf: 99m,
            bvgDeductionChf: 9m,
            withholdingTaxChf: 8m,
            expensesChf: 7m);

        context.PayrollRuns.Add(openRun);
        await context.SaveChangesAsync();

        var certificate = await CreateService(context).CreateAsync(new SalaryCertificateQuery(employee.Id, 2026));

        Assert.Equal(0m, GetAmount(certificate, SalaryCertificateFieldCodes.SalaryGrossWageTotalCode8));
        Assert.Equal(0m, GetAmount(certificate, SalaryCertificateFieldCodes.DeductionsSocialSecurityCode9));
        Assert.Equal(0m, GetAmount(certificate, SalaryCertificateFieldCodes.TaxSourceTaxCode12));
        Assert.Equal(0m, GetAmount(certificate, SalaryCertificateFieldCodes.SalaryNetWageCode11));
    }

    private static async Task<PayrollDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new PayrollDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static SalaryCertificateService CreateService(PayrollDbContext context)
    {
        var repository = new AnnualSalaryRepository(context);
        return new SalaryCertificateService(new AnnualSalaryService(repository));
    }

    private static Employee CreateEmployee()
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
            "Ordentlich",
            false,
            "756.1234.5678.97",
            null,
            null,
            null,
            null,
            null,
            null,
            EmployeeWageType.Hourly);
    }

    private static PayrollRun CreateRun(
        string periodKey,
        DateOnly paymentDate,
        Guid employeeId,
        decimal grossSalaryChf,
        decimal socialDeductionChf = 0m,
        decimal bvgDeductionChf = 0m,
        decimal withholdingTaxChf = 0m,
        decimal expensesChf = 0m)
    {
        var run = new PayrollRun(periodKey, paymentDate);
        run.AddLine(PayrollRunLine.CreateDirectChfLine(employeeId, PayrollLineType.BaseHours, "BASE", "Basislohn", grossSalaryChf));

        if (socialDeductionChf > 0m)
        {
            run.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employeeId, PayrollLineType.SocialContribution, "AHV_IV_EO", "AHV/IV/EO", socialDeductionChf));
        }

        if (bvgDeductionChf > 0m)
        {
            run.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employeeId, PayrollLineType.BvgDeduction, "BVG", "BVG", bvgDeductionChf));
        }

        if (withholdingTaxChf > 0m)
        {
            run.AddLine(PayrollRunLine.CreateCalculatedFixedDeduction(employeeId, PayrollLineType.Tax, "WITHHOLDING_TAX", "Quellensteuer", withholdingTaxChf));
        }

        if (expensesChf > 0m)
        {
            run.AddLine(PayrollRunLine.CreateDirectChfLine(employeeId, PayrollLineType.Expense, "EXPENSES", "Spesen", expensesChf));
        }

        return run;
    }

    private static decimal? GetAmount(SalaryCertificateDto certificate, string code)
    {
        return certificate.Fields.Single(field => field.Code == code).AmountChf;
    }

    private static string? GetText(SalaryCertificateDto certificate, string code)
    {
        return certificate.Fields.Single(field => field.Code == code).TextValue;
    }

    private static DateOnly? GetDate(SalaryCertificateDto certificate, string code)
    {
        return certificate.Fields.Single(field => field.Code == code).DateValue;
    }
}
