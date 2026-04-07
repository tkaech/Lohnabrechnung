using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Payroll.Application.Employees;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Reporting;
using Payroll.Application.Settings;
using Payroll.Application.BackupRestore;
using Payroll.Desktop.ViewModels;
using Payroll.Infrastructure.BackupRestore;
using Payroll.Infrastructure.Employees;
using Payroll.Infrastructure.MonthlyRecords;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Reporting;
using Payroll.Infrastructure.Settings;

namespace Payroll.Desktop.Bootstrapping;

public sealed class AppBootstrapper
{
    private const string AppDataDirectoryName = "PayrollApp";

    public MainWindowViewModel CreateMainWindowViewModel()
    {
        var isDevelopment = IsDevelopmentEnvironment();
        var databasePath = GetDatabasePath(isDevelopment);
        var dbContext = CreateDbContext(databasePath);
        var requiresFreshContext = EnsureDatabaseSchema(dbContext, databasePath, isDevelopment);

        if (requiresFreshContext)
        {
            dbContext.Dispose();
            dbContext = CreateDbContext(databasePath);
        }

        if (isDevelopment)
        {
            EmployeeDevelopmentDataSeeder.Seed(dbContext);
        }

        var repository = new EmployeeRepository(dbContext);
        var employeeService = new EmployeeService(repository);
        var payrollSettingsRepository = new PayrollSettingsRepository(dbContext);
        var payrollSettingsService = new PayrollSettingsService(payrollSettingsRepository);
        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        var monthlyRecordService = new MonthlyRecordService(monthlyRecordRepository);
        var backupDirectory = Path.Combine(GetAppDataRootDirectory(), "backups");
        var backupRestoreService = new BackupRestoreService(() => CreateDbContext(databasePath), employeeService, monthlyRecordService, payrollSettingsService, backupDirectory);
        var pdfExportService = new PdfExportService();
        var reportingService = new ReportingService(employeeService, monthlyRecordService, payrollSettingsService, pdfExportService);
        var monthlyRecordViewModel = new MonthlyRecordViewModel(monthlyRecordService);

        var workspaceLabel = isDevelopment
            ? $"Lokale Entwicklungsdatenbank mit Demo-Mitarbeitenden (`payroll.localdev.db`) unter `{databasePath}`. Produktive Daten bleiben davon getrennt."
            : $"Produktive Datenbank ohne Demo-Seeddaten (`payroll.db`) unter `{databasePath}`.";

        return new MainWindowViewModel(employeeService, backupRestoreService, payrollSettingsService, reportingService, monthlyRecordViewModel, workspaceLabel);
    }

    private static bool IsDevelopmentEnvironment()
    {
        var environmentName = Environment.GetEnvironmentVariable("PAYROLLAPP_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
    }

    private static bool EnsureDatabaseSchema(PayrollDbContext dbContext, string databasePath, bool isDevelopment)
    {
        if (!File.Exists(databasePath))
        {
            dbContext.Database.EnsureCreated();
            return false;
        }

        TryApplySafeSchemaUpgrades(dbContext);

        if (HasMonthlyRecordSchema(dbContext))
        {
            return false;
        }

        if (isDevelopment && string.Equals(Path.GetFileName(databasePath), "payroll.localdev.db", StringComparison.OrdinalIgnoreCase))
        {
            dbContext.Database.CloseConnection();
            File.Delete(databasePath);

            using var recreatedContext = CreateDbContext(databasePath);
            recreatedContext.Database.EnsureCreated();
            return true;
        }

        var databaseKind = isDevelopment ? "Entwicklungsdatenbank" : "Datenbank";
        throw new InvalidOperationException(
            $"{databaseKind} unter '{databasePath}' hat ein nicht unterstuetztes Schema. Die Datei wurde nicht veraendert. Bitte Daten sichern und eine explizite Migration oder einen manuellen Neuaufbau durchfuehren.");
    }

    private static string GetDatabasePath(bool isDevelopment)
    {
        var rootDirectory = GetAppDataRootDirectory();
        Directory.CreateDirectory(rootDirectory);

        var databaseFileName = isDevelopment ? "payroll.localdev.db" : "payroll.db";
        return Path.Combine(rootDirectory, databaseFileName);
    }

    private static string GetAppDataRootDirectory()
    {
        var specialFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(specialFolderPath))
        {
            return Path.Combine(specialFolderPath, AppDataDirectoryName);
        }

        var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (!string.IsNullOrWhiteSpace(xdgDataHome))
        {
            return Path.Combine(xdgDataHome, AppDataDirectoryName);
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            userProfile = AppContext.BaseDirectory;
        }

        return Path.Combine(userProfile, ".local", "share", AppDataDirectoryName);
    }

    private static string BuildConnectionString(string databasePath)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        };

        return connectionStringBuilder.ToString();
    }

    private static PayrollDbContext CreateDbContext(string databasePath)
    {
        var dbContextOptions = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite(BuildConnectionString(databasePath))
            .Options;

        return new PayrollDbContext(dbContextOptions);
    }

    private static bool HasMonthlyRecordSchema(PayrollDbContext dbContext)
    {
        var requiredTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EmployeeMonthlyRecords",
            "TimeEntries",
            "ExpenseEntries",
            "PayrollSettings",
            "DepartmentOptions",
            "EmploymentCategoryOptions",
            "EmploymentLocationOptions"
        };

        using var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
        if (shouldCloseConnection)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var tableName = reader.GetString(0);
                requiredTables.Remove(tableName);
            }

            if (requiredTables.Count > 0)
            {
                return false;
            }

            return HasExpectedEmployeeColumns(connection)
                && HasExpectedEmploymentContractColumns(connection)
                && HasExpectedExpenseEntryColumns(connection)
                && HasExpectedTimeEntryColumns(connection)
                && HasExpectedPayrollSettingsColumns(connection);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                connection.Close();
            }
        }
    }

    private static void TryApplySafeSchemaUpgrades(PayrollDbContext dbContext)
    {
        using var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
        if (shouldCloseConnection)
        {
            connection.Open();
        }

        try
        {
            if (TableExists(connection, "PayrollSettings"))
            {
                EnsureColumnExists(connection, "PayrollSettings", "CompanyAddress", "TEXT NOT NULL DEFAULT ''");
                EnsureColumnExists(connection, "PayrollSettings", "AppFontFamily", "TEXT NOT NULL DEFAULT 'Segoe UI, DejaVu Sans, Arial'");
                EnsureColumnExists(connection, "PayrollSettings", "AppFontSize", "TEXT NOT NULL DEFAULT '13'");
                EnsureColumnExists(connection, "PayrollSettings", "AppTextColorHex", "TEXT NOT NULL DEFAULT '#FF1A2530'");
                EnsureColumnExists(connection, "PayrollSettings", "AppMutedTextColorHex", "TEXT NOT NULL DEFAULT '#FF5F6B7A'");
                EnsureColumnExists(connection, "PayrollSettings", "AppBackgroundColorHex", "TEXT NOT NULL DEFAULT '#FFF5F7FA'");
                EnsureColumnExists(connection, "PayrollSettings", "AppAccentColorHex", "TEXT NOT NULL DEFAULT '#FF14324A'");
                EnsureColumnExists(connection, "PayrollSettings", "AppLogoText", "TEXT NOT NULL DEFAULT 'PA'");
                EnsureColumnExists(connection, "PayrollSettings", "AppLogoPath", "TEXT NOT NULL DEFAULT ''");
                EnsureColumnExists(connection, "PayrollSettings", "PrintFontFamily", "TEXT NOT NULL DEFAULT 'Helvetica'");
                EnsureColumnExists(connection, "PayrollSettings", "PrintFontSize", "TEXT NOT NULL DEFAULT '9'");
                EnsureColumnExists(connection, "PayrollSettings", "PrintTextColorHex", "TEXT NOT NULL DEFAULT '#FF000000'");
                EnsureColumnExists(connection, "PayrollSettings", "PrintMutedTextColorHex", "TEXT NOT NULL DEFAULT '#FF4B5563'");
                EnsureColumnExists(connection, "PayrollSettings", "PrintAccentColorHex", "TEXT NOT NULL DEFAULT '#FFFFFF00'");
                EnsureColumnExists(connection, "PayrollSettings", "PrintLogoText", "TEXT NOT NULL DEFAULT 'PA'");
                EnsureColumnExists(connection, "PayrollSettings", "PrintLogoPath", "TEXT NOT NULL DEFAULT ''");
                EnsureColumnExists(connection, "PayrollSettings", "PrintTemplate", "TEXT NOT NULL DEFAULT ''");
            }
        }
        finally
        {
            if (shouldCloseConnection)
            {
                connection.Close();
            }
        }
    }

    private static bool TableExists(DbConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName;";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static bool ColumnExists(DbConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info('{tableName}');";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void EnsureColumnExists(DbConnection connection, string tableName, string columnName, string columnDefinition)
    {
        if (ColumnExists(connection, tableName, columnName))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        command.ExecuteNonQuery();
    }

    private static bool HasExpectedEmployeeColumns(DbConnection connection)
    {
        var expectedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "PersonnelNumber",
            "FirstName",
            "LastName",
            "BirthDate",
            "EntryDate",
            "ExitDate",
            "IsActive",
            "ResidenceCountry",
            "Nationality",
            "PermitCode",
            "TaxStatus",
            "IsSubjectToWithholdingTax",
            "AhvNumber",
            "Iban",
            "PhoneNumber",
            "Email",
            "DepartmentOptionId",
            "EmploymentCategoryOptionId",
            "EmploymentLocationOptionId",
            "Street",
            "HouseNumber",
            "AddressLine2",
            "PostalCode",
            "City",
            "Country",
            "CreatedAtUtc",
            "UpdatedAtUtc"
        };

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('Employees');";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            expectedColumns.Remove(reader.GetString(1));
        }

        return expectedColumns.Count == 0;
    }

    private static bool HasExpectedEmploymentContractColumns(DbConnection connection)
    {
        var expectedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "EmployeeId",
            "ValidFrom",
            "ValidTo",
            "HourlyRateChf",
            "MonthlyBvgDeductionChf",
            "SpecialSupplementRateChf",
            "CreatedAtUtc",
            "UpdatedAtUtc"
        };

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('EmploymentContracts');";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            expectedColumns.Remove(reader.GetString(1));
        }

        return expectedColumns.Count == 0;
    }

    private static bool HasExpectedExpenseEntryColumns(DbConnection connection)
    {
        var expectedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "EmployeeMonthlyRecordId",
            "EmployeeId",
            "ExpensesTotalChf",
            "ExpenseTypeCode",
            "Description",
            "CreatedAtUtc",
            "UpdatedAtUtc"
        };

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('ExpenseEntries');";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            expectedColumns.Remove(reader.GetString(1));
        }

        return expectedColumns.Count == 0;
    }

    private static bool HasExpectedTimeEntryColumns(DbConnection connection)
    {
        var expectedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "EmployeeMonthlyRecordId",
            "EmployeeId",
            "WorkDate",
            "HoursWorked",
            "NightHours",
            "SundayHours",
            "HolidayHours",
            "VehiclePauschalzone1Chf",
            "VehiclePauschalzone2Chf",
            "VehicleRegiezone1Chf",
            "Note",
            "CreatedAtUtc",
            "UpdatedAtUtc"
        };

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('TimeEntries');";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            expectedColumns.Remove(reader.GetString(1));
        }

        return expectedColumns.Count == 0;
    }

    private static bool HasExpectedPayrollSettingsColumns(DbConnection connection)
    {
        var expectedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "CompanyAddress",
            "AppFontFamily",
            "AppFontSize",
            "AppTextColorHex",
            "AppMutedTextColorHex",
            "AppBackgroundColorHex",
            "AppAccentColorHex",
            "AppLogoText",
            "AppLogoPath",
            "PrintFontFamily",
            "PrintFontSize",
            "PrintTextColorHex",
            "PrintMutedTextColorHex",
            "PrintAccentColorHex",
            "PrintLogoText",
            "PrintLogoPath",
            "PrintTemplate",
            "AhvIvEoRate",
            "AlvRate",
            "SicknessAccidentInsuranceRate",
            "TrainingAndHolidayRate",
            "VacationCompensationRate",
            "VehiclePauschalzone1RateChf",
            "VehiclePauschalzone2RateChf",
            "VehicleRegiezone1RateChf",
            "WorkTimeSupplementSettings_NightSupplementRate",
            "WorkTimeSupplementSettings_SundaySupplementRate",
            "WorkTimeSupplementSettings_HolidaySupplementRate",
            "CreatedAtUtc",
            "UpdatedAtUtc"
        };

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info('PayrollSettings');";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            expectedColumns.Remove(reader.GetString(1));
        }

        return expectedColumns.Count == 0;
    }
}
