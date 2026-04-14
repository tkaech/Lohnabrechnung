using Microsoft.EntityFrameworkCore;
using Payroll.Application.BackupRestore;
using Payroll.Application.Employees;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Settings;
using Payroll.Domain.Employees;
using Payroll.Infrastructure.BackupRestore;
using Payroll.Infrastructure.Employees;
using Payroll.Infrastructure.MonthlyRecords;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Settings;

namespace Payroll.Application.Tests;

public sealed class BackupRestoreServiceTests
{
    [Fact]
    public async Task CreateAndRestoreAsync_RoundTripsConfigurationAndUserData()
    {
        var rootDirectory = Path.Combine(Path.GetTempPath(), $"payroll-backup-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootDirectory);

        try
        {
            var databasePath = Path.Combine(rootDirectory, "payroll.db");
            await using (var dbContext = CreateDbContext(databasePath))
            {
                await dbContext.Database.EnsureCreatedAsync();
            }

            var settingsService = new PayrollSettingsService(new PayrollSettingsRepository(CreateDbContext(databasePath)));
            var employeeService = new EmployeeService(new EmployeeRepository(CreateDbContext(databasePath)));
            var monthlyRecordService = new MonthlyRecordService(new EmployeeMonthlyRecordRepository(CreateDbContext(databasePath)));
            var backupRestoreService = new BackupRestoreService(
                () => CreateDbContext(databasePath),
                employeeService,
                monthlyRecordService,
                settingsService,
                Path.Combine(rootDirectory, "backups"));

            var departmentId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var locationId = Guid.NewGuid();
            await settingsService.SaveAsync(new SavePayrollSettingsCommand(
                "Firma Alt\nBahnhofstrasse 1\n6000 Luzern",
                "Segoe UI",
                13m,
                "#FF1A2530",
                "#FF5F6B7A",
                "#FFF5F7FA",
                "#FF14324A",
                "PA",
                string.Empty,
                "Helvetica",
                9m,
                "#FF000000",
                "#FF4B5563",
                "#FFFFFF00",
                "PA",
                string.Empty,
                "BANNER|Lohnblatt|{{Monat}}",
                ",",
                "'",
                "CHF",
                null,
                new DateOnly(2026, 1, 1),
                null,
                0.25m,
                0.50m,
                1.00m,
                0.053m,
                0.011m,
                0.00821m,
                0.00015m,
                0.1064m,
                0.1264m,
                1.10m,
                2.20m,
                3.30m,
                PayrollPreviewHelpCatalog.GetDefaultOptions(),
                [new SettingOptionDto(departmentId, "Sicherheit")],
                [new SettingOptionDto(categoryId, "A")],
                [new SettingOptionDto(locationId, "Schachenstr. 7, Emmenbruecke")]));

            var employee = await employeeService.SaveAsync(new SaveEmployeeCommand(
                null,
                null,
                "1000",
                "Nora",
                "Feld",
                new DateOnly(1990, 1, 1),
                new DateOnly(2025, 1, 1),
                null,
                true,
                "Birkenweg",
                "12",
                null,
                "8005",
                "Zuerich",
                "Schweiz",
                "Schweiz",
                "CH",
                "B",
                "Ordentlich",
                false,
                "756.1000.1000.01",
                "CH9300762011623852957",
                "+41 79 000 00 00",
                "nora.feld@example.ch",
                departmentId,
                categoryId,
                locationId,
                EmployeeWageType.Hourly,
                new DateOnly(2025, 1, 1),
                null,
                33.50m,
                310m,
                3.00m));

            var monthlyRecord = await monthlyRecordService.GetOrCreateAsync(new MonthlyRecordQuery(employee.EmployeeId, 2026, 4));
            monthlyRecord = await monthlyRecordService.SaveTimeEntryAsync(new SaveMonthlyTimeEntryCommand(
                monthlyRecord.Header.MonthlyRecordId,
                null,
                new DateOnly(2026, 4, 6),
                120m,
                0m,
                34m,
                0m,
                0m,
                0m,
                55m,
                "Test"));
            await monthlyRecordService.SaveExpenseEntryAsync(new SaveMonthlyExpenseEntryCommand(
                monthlyRecord.Header.MonthlyRecordId,
                60m));

            var backup = await backupRestoreService.CreateBackupAsync(new CreateBackupCommand(
                Path.Combine(rootDirectory, "exports"),
                "backup_2026-04-07_14-35",
                BackupContentType.Both));

            await settingsService.SaveAsync(new SavePayrollSettingsCommand(
                "Firma Neu\nPostfach 99\n9999 Test",
                "Aptos",
                14m,
                "#FF101820",
                "#FF667788",
                "#FFF6F8FB",
                "#FF224466",
                "BSD",
                string.Empty,
                "Helvetica",
                10m,
                "#FF000000",
                "#FF556677",
                "#FFFFFF00",
                "BSD",
                string.Empty,
                "BANNER|Neu|{{Monat}}",
                ".",
                " ",
                "EUR",
                null,
                new DateOnly(2026, 5, 1),
                null,
                0.20m,
                0.30m,
                0.40m,
                0.054m,
                0.012m,
                0.009m,
                0.0002m,
                0.12m,
                0.14m,
                4m,
                5m,
                6m,
                PayrollPreviewHelpCatalog.GetDefaultOptions(),
                [new SettingOptionDto(departmentId, "Sicherheit")],
                [new SettingOptionDto(categoryId, "A")],
                [new SettingOptionDto(locationId, "Schachenstr. 7, Emmenbruecke")]));

            await using (var dbContext = CreateDbContext(databasePath))
            {
                dbContext.TimeEntries.RemoveRange(dbContext.TimeEntries);
                dbContext.ExpenseEntries.RemoveRange(dbContext.ExpenseEntries);
                dbContext.EmployeeMonthlyRecords.RemoveRange(dbContext.EmployeeMonthlyRecords);
                dbContext.EmploymentContracts.RemoveRange(dbContext.EmploymentContracts);
                dbContext.Employees.RemoveRange(dbContext.Employees);
                await dbContext.SaveChangesAsync();
            }

            await backupRestoreService.RestoreBackupAsync(new RestoreBackupCommand(backup.FilePath, BackupContentType.Both));

            var restoredSettings = await settingsService.GetAsync();
            var restoredEmployees = await employeeService.ListAsync(new EmployeeListQuery(null, null));
            var restoredEmployee = await employeeService.GetByIdAsync(employee.EmployeeId);
            var restoredMonthlyRecord = await monthlyRecordService.GetOrCreateAsync(new MonthlyRecordQuery(employee.EmployeeId, 2026, 4));

            Assert.Contains("Firma Alt", restoredSettings.CompanyAddress, StringComparison.Ordinal);
            Assert.Single(restoredEmployees);
            Assert.NotNull(restoredEmployee);
            Assert.Equal("1000", restoredEmployee!.PersonnelNumber);
            Assert.Single(restoredMonthlyRecord.TimeEntries);
            Assert.Equal(60m, restoredMonthlyRecord.ExpenseEntry?.ExpensesTotalChf);
        }
        finally
        {
            if (Directory.Exists(rootDirectory))
            {
                Directory.Delete(rootDirectory, recursive: true);
            }
        }
    }

    private static PayrollDbContext CreateDbContext(string databasePath)
    {
        var options = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new PayrollDbContext(options);
    }
}
