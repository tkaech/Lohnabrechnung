using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Payroll.Application.Employees;
using Payroll.Application.MonthlyRecords;
using Payroll.Application.Settings;
using Payroll.Desktop.ViewModels;
using Payroll.Infrastructure.Employees;
using Payroll.Infrastructure.MonthlyRecords;
using Payroll.Infrastructure.Persistence;
using Payroll.Infrastructure.Settings;

namespace Payroll.Desktop.Bootstrapping;

public sealed class AppBootstrapper
{
    public MainWindowViewModel CreateMainWindowViewModel()
    {
        var isDevelopment = IsDevelopmentEnvironment();
        var databaseFileName = isDevelopment ? "payroll.localdev.db" : "payroll.db";
        var databasePath = Path.Combine(AppContext.BaseDirectory, databaseFileName);
        var dbContextOptions = new DbContextOptionsBuilder<PayrollDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        var dbContext = new PayrollDbContext(dbContextOptions);
        EnsureDatabaseSchema(dbContext, isDevelopment);

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
        var monthlyRecordViewModel = new MonthlyRecordViewModel(monthlyRecordService);

        var workspaceLabel = isDevelopment
            ? "Lokale Entwicklungsdatenbank mit Demo-Mitarbeitenden (`payroll.localdev.db`). Produktive Daten bleiben davon getrennt."
            : "Produktive Datenbank ohne Demo-Seeddaten (`payroll.db`).";

        return new MainWindowViewModel(employeeService, payrollSettingsService, monthlyRecordViewModel, workspaceLabel);
    }

    private static bool IsDevelopmentEnvironment()
    {
        var environmentName = Environment.GetEnvironmentVariable("PAYROLLAPP_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureDatabaseSchema(PayrollDbContext dbContext, bool isDevelopment)
    {
        dbContext.Database.EnsureCreated();

        if (!isDevelopment || HasMonthlyRecordSchema(dbContext))
        {
            return;
        }

        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();
    }

    private static bool HasMonthlyRecordSchema(PayrollDbContext dbContext)
    {
        var requiredTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "EmployeeMonthlyRecords",
            "TimeEntries",
            "ExpenseEntries",
            "VehicleCompensations",
            "PayrollSettings"
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

            return HasExpectedExpenseEntryColumns(connection);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                connection.Close();
            }
        }
    }

    private static bool HasExpectedExpenseEntryColumns(DbConnection connection)
    {
        var expectedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "EmployeeMonthlyRecordId",
            "EmployeeId",
            "ExpenseDate",
            "AmountChf",
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
}
