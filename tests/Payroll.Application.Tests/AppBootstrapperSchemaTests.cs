using System.Data.Common;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Payroll.Desktop.Bootstrapping;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Application.Tests;

public sealed class AppBootstrapperSchemaTests
{
    [Fact]
    public void EnsureCompatibleSchema_AddsCancelledAtUtcToExistingPayrollRunsTable()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"payroll-schema-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            Execute(connection, "CREATE TABLE \"Employees\" (\"Id\" TEXT NOT NULL);");
            Execute(connection, "CREATE TABLE \"PayrollSettings\" (\"Id\" TEXT NOT NULL);");
            Execute(connection, "CREATE TABLE \"EmployeeMonthlyRecords\" (\"Id\" TEXT NOT NULL);");
            Execute(connection, "CREATE TABLE \"PayrollRuns\" (\"Id\" TEXT NOT NULL);");

            var options = new DbContextOptionsBuilder<PayrollDbContext>()
                .UseSqlite(connection)
                .Options;
            using var dbContext = new PayrollDbContext(options);

            var method = typeof(AppBootstrapper).GetMethod(
                "EnsureCompatibleSchema",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);
            method!.Invoke(null, [dbContext]);

            using var verifyConnection = new SqliteConnection(connectionString);
            verifyConnection.Open();

            Assert.True(ColumnExists(verifyConnection, "PayrollRuns", "CancelledAtUtc"));
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public void EnsureCompatibleSchema_AddsSalaryCertificatePdfTemplatePathToExistingPayrollSettingsTable()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"payroll-schema-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            Execute(connection, "CREATE TABLE \"Employees\" (\"Id\" TEXT NOT NULL);");
            Execute(connection, "CREATE TABLE \"PayrollSettings\" (\"Id\" TEXT NOT NULL);");
            Execute(connection, "CREATE TABLE \"EmployeeMonthlyRecords\" (\"Id\" TEXT NOT NULL);");
            Execute(connection, "CREATE TABLE \"PayrollRuns\" (\"Id\" TEXT NOT NULL);");

            var options = new DbContextOptionsBuilder<PayrollDbContext>()
                .UseSqlite(connection)
                .Options;
            using var dbContext = new PayrollDbContext(options);

            var method = typeof(AppBootstrapper).GetMethod(
                "EnsureCompatibleSchema",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);
            method!.Invoke(null, [dbContext]);

            using var verifyConnection = new SqliteConnection(connectionString);
            verifyConnection.Open();

            Assert.True(ColumnExists(verifyConnection, "PayrollSettings", "SalaryCertificatePdfTemplatePath"));
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public void EnsureCompatibleSchema_CreatesSalaryCertificateRecordsTable()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"payroll-schema-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath}";

        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            Execute(connection, "CREATE TABLE \"Employees\" (\"Id\" TEXT NOT NULL);");
            Execute(connection, "CREATE TABLE \"PayrollSettings\" (\"Id\" TEXT NOT NULL);");
            Execute(connection, "CREATE TABLE \"EmployeeMonthlyRecords\" (\"Id\" TEXT NOT NULL);");
            Execute(connection, "CREATE TABLE \"PayrollRuns\" (\"Id\" TEXT NOT NULL);");

            var options = new DbContextOptionsBuilder<PayrollDbContext>()
                .UseSqlite(connection)
                .Options;
            using var dbContext = new PayrollDbContext(options);

            var method = typeof(AppBootstrapper).GetMethod(
                "EnsureCompatibleSchema",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);
            method!.Invoke(null, [dbContext]);

            using var verifyConnection = new SqliteConnection(connectionString);
            verifyConnection.Open();

            Assert.True(TableExists(verifyConnection, "SalaryCertificateRecords"));
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    private static void Execute(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
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
}
