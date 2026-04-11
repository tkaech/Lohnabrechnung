using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.BackupRestore;
using Payroll.Application.Employees;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Reporting;
using Payroll.Application.Settings;
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
    public MainWindowViewModel CreateMainWindowViewModel(DesktopRuntimeOptions runtimeOptions)
    {
        var databasePath = runtimeOptions.DatabasePath;
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath) ?? AppContext.BaseDirectory);

        var dbContext = InitializeDatabase(databasePath, runtimeOptions.SeedTestData);

        var repository = new EmployeeRepository(dbContext);
        var employeeService = new EmployeeService(repository);
        var payrollSettingsRepository = new PayrollSettingsRepository(dbContext);
        var payrollSettingsService = new PayrollSettingsService(payrollSettingsRepository);
        EnsureConfigurationSeeded(payrollSettingsService);
        EnsureTestDataSeeded(dbContext, runtimeOptions.SeedTestData);
        var monthlyRecordRepository = new EmployeeMonthlyRecordRepository(dbContext);
        var monthlyRecordService = new MonthlyRecordService(monthlyRecordRepository);
        var backupDirectory = Path.Combine(Path.GetDirectoryName(databasePath) ?? AppContext.BaseDirectory, "backups");
        var backupRestoreService = new BackupRestoreService(() => CreateDbContext(databasePath), employeeService, monthlyRecordService, payrollSettingsService, backupDirectory);
        var pdfExportService = new PdfExportService();
        var reportingService = new ReportingService(employeeService, monthlyRecordService, payrollSettingsService, pdfExportService);
        var monthlyRecordViewModel = new MonthlyRecordViewModel(monthlyRecordService);
        var workspaceLabel = $"Datenbank unter `{databasePath}`. Schema wird ueber EF-Migrationen aktualisiert; bestehende Daten bleiben erhalten.";

        return new MainWindowViewModel(
            employeeService,
            backupRestoreService,
            payrollSettingsService,
            reportingService,
            monthlyRecordService,
            monthlyRecordViewModel,
            workspaceLabel,
            databasePath,
            runtimeOptions.EnvironmentName);
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

    private static PayrollDbContext InitializeDatabase(string databasePath, bool allowResetForInvalidTestDatabase)
    {
        var dbContext = CreateDbContext(databasePath);

        try
        {
            PrepareDatabase(dbContext);

            if (!HasCriticalTables(dbContext))
            {
                dbContext.Dispose();

                if (allowResetForInvalidTestDatabase)
                {
                    RecreateInvalidDatabase(databasePath);
                    dbContext = CreateDbContext(databasePath);
                    PrepareDatabase(dbContext);

                    if (!HasCriticalTables(dbContext))
                    {
                        dbContext.Dispose();
                        RecreateInvalidDatabase(databasePath);
                        dbContext = CreateDbContext(databasePath);
                        dbContext.Database.EnsureCreated();
                        EnsureCompatibleSchema(dbContext);

                        if (!HasCriticalTables(dbContext))
                        {
                            dbContext.Dispose();
                            RecreateInvalidDatabase(databasePath);
                            dbContext = CreateDbContext(databasePath);
                            var createScript = dbContext.Database.GenerateCreateScript();
                            dbContext.Database.OpenConnection();
                            try
                            {
                                dbContext.Database.ExecuteSqlRaw(createScript);
                            }
                            finally
                            {
                                dbContext.Database.CloseConnection();
                            }

                            EnsureCompatibleSchema(dbContext);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Die SQLite-Datenbank unter '{databasePath}' ist unvollstaendig. Kritische Tabellen wie 'PayrollSettings' fehlen nach der Migration.");
                }
            }

            if (!HasCriticalTables(dbContext))
            {
                throw new InvalidOperationException(
                    $"Die SQLite-Datenbank unter '{databasePath}' konnte nicht korrekt initialisiert werden. Kritische Tabellen wie 'PayrollSettings' fehlen weiterhin.");
            }

            return dbContext;
        }
        catch
        {
            dbContext.Dispose();
            throw;
        }
    }

    private static void PrepareDatabase(PayrollDbContext dbContext)
    {
        BaselineExistingDatabaseIfNeeded(dbContext);
        dbContext.Database.Migrate();
        EnsureCompatibleSchema(dbContext);
    }

    private static void BaselineExistingDatabaseIfNeeded(PayrollDbContext dbContext)
    {
        using var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
        if (shouldCloseConnection)
        {
            connection.Open();
        }

        try
        {
            if (TableExists(connection, "__EFMigrationsHistory"))
            {
                return;
            }

            if (!HasExistingApplicationTables(connection))
            {
                return;
            }

            using var createHistoryCommand = connection.CreateCommand();
            createHistoryCommand.CommandText =
                "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (\"MigrationId\" TEXT NOT NULL CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL);";
            createHistoryCommand.ExecuteNonQuery();

            using var insertBaselineCommand = connection.CreateCommand();
            insertBaselineCommand.CommandText =
                "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ($migrationId, $productVersion);";

            var migrationIdParameter = insertBaselineCommand.CreateParameter();
            migrationIdParameter.ParameterName = "$migrationId";
            migrationIdParameter.Value = global::Payroll.Infrastructure.Persistence.Migrations.InitialCreate.MigrationIdValue;
            insertBaselineCommand.Parameters.Add(migrationIdParameter);

            var productVersionParameter = insertBaselineCommand.CreateParameter();
            productVersionParameter.ParameterName = "$productVersion";
            productVersionParameter.Value = "8.0.10";
            insertBaselineCommand.Parameters.Add(productVersionParameter);

            insertBaselineCommand.ExecuteNonQuery();
        }
        finally
        {
            if (shouldCloseConnection)
            {
                connection.Close();
            }
        }
    }

    private static void EnsureConfigurationSeeded(PayrollSettingsService payrollSettingsService)
    {
        payrollSettingsService.GetAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    private static bool HasCriticalTables(PayrollDbContext dbContext)
    {
        using var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
        if (shouldCloseConnection)
        {
            connection.Open();
        }

        try
        {
            return TableExists(connection, "PayrollSettings")
                   && TableExists(connection, "Employees")
                   && TableExists(connection, "EmployeeMonthlyRecords");
        }
        finally
        {
            if (shouldCloseConnection)
            {
                connection.Close();
            }
        }
    }

    private static void RecreateInvalidDatabase(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }

    private static void EnsureTestDataSeeded(PayrollDbContext dbContext, bool seedTestData)
    {
        if (!seedTestData)
        {
            return;
        }

        EmployeeDevelopmentDataSeeder.Seed(dbContext);
    }

    private static bool HasExistingApplicationTables(DbConnection connection)
    {
        var applicationTables = new[]
        {
            "Employees",
            "EmploymentContracts",
            "EmployeeMonthlyRecords",
            "TimeEntries",
            "ExpenseEntries",
            "PayrollSettings"
        };

        return applicationTables.All(tableName => TableExists(connection, tableName));
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

    private static void EnsureCompatibleSchema(PayrollDbContext dbContext)
    {
        using var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != System.Data.ConnectionState.Open;
        if (shouldCloseConnection)
        {
            connection.Open();
        }

        try
        {
            EnsureTableColumn(
                connection,
                "Employees",
                "WageType",
                "ALTER TABLE \"Employees\" ADD COLUMN \"WageType\" TEXT NOT NULL DEFAULT 'Hourly';");

            EnsureTableColumn(
                connection,
                "PayrollSettings",
                "DecimalSeparator",
                "ALTER TABLE \"PayrollSettings\" ADD COLUMN \"DecimalSeparator\" TEXT NOT NULL DEFAULT ',';");

            EnsureTableColumn(
                connection,
                "PayrollSettings",
                "ThousandsSeparator",
                "ALTER TABLE \"PayrollSettings\" ADD COLUMN \"ThousandsSeparator\" TEXT NOT NULL DEFAULT '''';");

            EnsureTableColumn(
                connection,
                "PayrollSettings",
                "CurrencyCode",
                "ALTER TABLE \"PayrollSettings\" ADD COLUMN \"CurrencyCode\" TEXT NOT NULL DEFAULT 'CHF';");

            EnsureTableColumn(
                connection,
                "PayrollSettings",
                "VacationCompensationRateAge50Plus",
                "ALTER TABLE \"PayrollSettings\" ADD COLUMN \"VacationCompensationRateAge50Plus\" TEXT NOT NULL DEFAULT 0.1064;");

            EnsureTableColumn(
                connection,
                "PayrollSettings",
                "PayrollPreviewHelpVisibilityJson",
                "ALTER TABLE \"PayrollSettings\" ADD COLUMN \"PayrollPreviewHelpVisibilityJson\" TEXT NOT NULL DEFAULT '';");

            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_IsInitialized",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_IsInitialized\" INTEGER NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_CapturedAtUtc",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_CapturedAtUtc\" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00+00:00';");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_NightSupplementRate",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_NightSupplementRate\" TEXT NULL;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_SundaySupplementRate",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_SundaySupplementRate\" TEXT NULL;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_HolidaySupplementRate",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_HolidaySupplementRate\" TEXT NULL;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_AhvIvEoRate",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_AhvIvEoRate\" TEXT NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_AlvRate",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_AlvRate\" TEXT NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_SicknessAccidentInsuranceRate",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_SicknessAccidentInsuranceRate\" TEXT NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_TrainingAndHolidayRate",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_TrainingAndHolidayRate\" TEXT NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_VacationCompensationRate",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_VacationCompensationRate\" TEXT NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_VacationCompensationRateAge50Plus",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_VacationCompensationRateAge50Plus\" TEXT NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_VehiclePauschalzone1RateChf",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_VehiclePauschalzone1RateChf\" TEXT NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_VehiclePauschalzone2RateChf",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_VehiclePauschalzone2RateChf\" TEXT NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "PayrollParameterSnapshot_VehicleRegiezone1RateChf",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"PayrollParameterSnapshot_VehicleRegiezone1RateChf\" TEXT NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "EmploymentContractSnapshot_IsInitialized",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"EmploymentContractSnapshot_IsInitialized\" INTEGER NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "EmploymentContractSnapshot_CapturedAtUtc",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"EmploymentContractSnapshot_CapturedAtUtc\" TEXT NOT NULL DEFAULT '0001-01-01 00:00:00+00:00';");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "EmploymentContractSnapshot_ValidFrom",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"EmploymentContractSnapshot_ValidFrom\" TEXT NOT NULL DEFAULT '0001-01-01';");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "EmploymentContractSnapshot_ValidTo",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"EmploymentContractSnapshot_ValidTo\" TEXT NULL;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "EmploymentContractSnapshot_HourlyRateChf",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"EmploymentContractSnapshot_HourlyRateChf\" TEXT NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "EmploymentContractSnapshot_MonthlyBvgDeductionChf",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"EmploymentContractSnapshot_MonthlyBvgDeductionChf\" TEXT NOT NULL DEFAULT 0;");
            EnsureTableColumn(
                connection,
                "EmployeeMonthlyRecords",
                "EmploymentContractSnapshot_SpecialSupplementRateChf",
                "ALTER TABLE \"EmployeeMonthlyRecords\" ADD COLUMN \"EmploymentContractSnapshot_SpecialSupplementRateChf\" TEXT NOT NULL DEFAULT 0;");
        }
        finally
        {
            if (shouldCloseConnection)
            {
                connection.Close();
            }
        }
    }

    private static void EnsureTableColumn(DbConnection connection, string tableName, string columnName, string addColumnSql)
    {
        if (!TableExists(connection, tableName) || ColumnExists(connection, tableName, columnName))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = addColumnSql;
        command.ExecuteNonQuery();
    }

    private static bool ColumnExists(DbConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
