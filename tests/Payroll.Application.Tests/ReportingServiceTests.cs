using Payroll.Application.Employees;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Reporting;
using Payroll.Application.Settings;
using Payroll.Domain.Employees;
using Payroll.Domain.Settings;
using Payroll.Infrastructure.Employees;
using Payroll.Infrastructure.MonthlyRecords;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Settings;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Payroll.Application.Tests;

public sealed class ReportingServiceTests
{
    [Fact]
    public async Task CreatePayrollStatementPdfAsync_BuildsDocumentFromMonthlyPreviewAndSettings()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new PayrollDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var employee = new Employee(
            "9000",
            "Yvonne",
            "Kaech",
            new DateOnly(1990, 2, 1),
            new DateOnly(2020, 3, 1),
            null,
            true,
            new EmployeeAddress("Unterer Chaemletenweg", "8", null, "6333", "Huenenberg See", "Schweiz"),
            "Schweiz",
            "CH",
            "B",
            "Ordentlich",
            false,
            "756.5327.5578.88",
            null,
            null,
            null,
            null,
            null,
            null,
            EmployeeWageType.Hourly);
        dbContext.Employees.Add(employee);
        dbContext.EmploymentContracts.Add(new EmploymentContract(employee.Id, new DateOnly(2026, 1, 1), null, 33m, 0m, 3m));
        dbContext.PayrollSettings.Add(new PayrollSettings(
            "Blesinger Sicherheits Dienste GmbH\nPostfach 28\n6314 Unteraegeri",
            new WorkTimeSupplementSettings(0.25m, 0.50m, 1.00m),
            0.053m,
            0.011m,
            0.00821m,
            0.00015m,
            0.1064m,
            5.6m,
            16.8m,
            0.32m));

        await dbContext.SaveChangesAsync();

        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        var record = await monthlyRecordRepository.GetOrCreateAsync(employee.Id, 2026, 3, CancellationToken.None);
        record.SaveTimeEntry(null, new DateOnly(2026, 3, 12), 8m, 0m, 0m, 0m, 1m, 2m, 3m, null);
        record.SaveExpenseEntry(42m);
        await monthlyRecordRepository.SaveChangesAsync(CancellationToken.None);

        var employeeService = new EmployeeService(new EmployeeRepository(dbContext));
        var monthlyRecordService = new MonthlyRecordService(monthlyRecordRepository);
        var settingsService = new PayrollSettingsService(new PayrollSettingsRepository(dbContext));
        var pdfExportService = new CapturePdfExportService();
        var reportingService = new ReportingService(employeeService, monthlyRecordService, settingsService, pdfExportService);

        var exportPath = await reportingService.CreatePayrollStatementPdfAsync(employee.Id, 2026, 3);

        Assert.Equal("/tmp/report.pdf", exportPath);
        Assert.NotNull(pdfExportService.LastDocument);
        Assert.Equal("Maerz 2026", pdfExportService.LastDocument!.MonthLabel);
        Assert.Contains("PAYROLL_LINE", pdfExportService.LastDocument.TemplateContent, StringComparison.Ordinal);
        Assert.Equal("PA", pdfExportService.LastDocument.TemplatePlaceholders["Logo"]);
        Assert.Contains("Blesinger Sicherheits Dienste GmbH", pdfExportService.LastDocument.CompanyAddress, StringComparison.Ordinal);
        Assert.Equal("Yvonne Kaech", pdfExportService.LastDocument.EmployeeFullName);
        Assert.Equal("Unterer Chaemletenweg 8", pdfExportService.LastDocument.EmployeeAddressLine1);
        Assert.Equal("6333 Huenenberg See", pdfExportService.LastDocument.EmployeeAddressLine3);
        Assert.Equal("Yvonne Kaech", pdfExportService.LastDocument.TemplatePlaceholders["MitarbeiterName"]);
        Assert.Equal("42,00 CHF", pdfExportService.LastDocument.TemplatePlaceholders["Spesen"]);
        Assert.Contains("Blesinger Sicherheits Dienste GmbH", pdfExportService.LastDocument.TemplatePlaceholders["Firmenadresse"], StringComparison.Ordinal);
        Assert.Contains(pdfExportService.LastDocument.Lines, line => line.Label == "Basislohn");
        Assert.Contains(pdfExportService.LastDocument.Lines, line => line.Label == "Total Auszahlung");
    }

    private sealed class CapturePdfExportService : IPdfExportService
    {
        public PayrollStatementPdfDocument? LastDocument { get; private set; }

        public Task<string> ExportPayrollStatementAsync(PayrollStatementPdfDocument document, CancellationToken cancellationToken = default)
        {
            LastDocument = document;
            return Task.FromResult("/tmp/report.pdf");
        }
    }
}
