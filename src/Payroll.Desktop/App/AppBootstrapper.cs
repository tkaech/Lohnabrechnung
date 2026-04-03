using Microsoft.EntityFrameworkCore;
using Payroll.Application.Employees;
using Payroll.Desktop.ViewModels;
using Payroll.Infrastructure.Employees;
using Payroll.Infrastructure.Persistence;

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
        dbContext.Database.EnsureCreated();

        if (isDevelopment)
        {
            EmployeeDevelopmentDataSeeder.Seed(dbContext);
        }

        var repository = new EmployeeRepository(dbContext);
        var employeeService = new EmployeeService(repository);

        var workspaceLabel = isDevelopment
            ? "Lokale Entwicklungsdatenbank mit Demo-Mitarbeitenden (`payroll.localdev.db`). Produktive Daten bleiben davon getrennt."
            : "Produktive Datenbank ohne Demo-Seeddaten (`payroll.db`).";

        return new MainWindowViewModel(employeeService, workspaceLabel);
    }

    private static bool IsDevelopmentEnvironment()
    {
        var environmentName = Environment.GetEnvironmentVariable("PAYROLLAPP_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase);
    }
}
