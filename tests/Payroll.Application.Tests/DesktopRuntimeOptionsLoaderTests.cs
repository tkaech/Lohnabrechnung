using Payroll.Desktop.Bootstrapping;

namespace Payroll.Application.Tests;

public sealed class DesktopRuntimeOptionsLoaderTests
{
    [Fact]
    public void Load_UsesCommandLineOverrideForDatabasePath()
    {
        var options = DesktopRuntimeOptionsLoader.Load(["--db-path=/tmp/payroll-override.db"]);

        Assert.Equal(Path.GetFullPath("/tmp/payroll-override.db"), options.DatabasePath);
    }

    [Fact]
    public void Load_UsesEnvironmentOverrideForDatabasePath()
    {
        var previousValue = Environment.GetEnvironmentVariable("PAYROLLAPP_DATABASE_PATH");

        try
        {
            Environment.SetEnvironmentVariable("PAYROLLAPP_DATABASE_PATH", "/tmp/payroll-env.db");

            var options = DesktopRuntimeOptionsLoader.Load([]);

            Assert.Equal(Path.GetFullPath("/tmp/payroll-env.db"), options.DatabasePath);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PAYROLLAPP_DATABASE_PATH", previousValue);
        }
    }

    [Fact]
    public void Load_UsesEnvironmentOverrideToDisableTestDataSeeding()
    {
        var previousPath = Environment.GetEnvironmentVariable("PAYROLLAPP_DATABASE_PATH");
        var previousSeed = Environment.GetEnvironmentVariable("PAYROLLAPP_SEED_TESTDATA");

        try
        {
            Environment.SetEnvironmentVariable("PAYROLLAPP_DATABASE_PATH", "/tmp/payroll-env.db");
            Environment.SetEnvironmentVariable("PAYROLLAPP_SEED_TESTDATA", "false");

            var options = DesktopRuntimeOptionsLoader.Load([]);

            Assert.False(options.SeedTestData);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PAYROLLAPP_DATABASE_PATH", previousPath);
            Environment.SetEnvironmentVariable("PAYROLLAPP_SEED_TESTDATA", previousSeed);
        }
    }

    [Fact]
    public void Load_UsesEnvironmentOverrideForTestDataSeeding()
    {
        var previousValue = Environment.GetEnvironmentVariable("PAYROLLAPP_SEED_TESTDATA");

        try
        {
            Environment.SetEnvironmentVariable("PAYROLLAPP_SEED_TESTDATA", "true");

            var options = DesktopRuntimeOptionsLoader.Load([]);

            Assert.True(options.SeedTestData);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PAYROLLAPP_SEED_TESTDATA", previousValue);
        }
    }
}
