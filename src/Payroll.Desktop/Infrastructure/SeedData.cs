using Payroll.Domain.Employees;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Desktop;

internal static class SeedData
{
    public static void Initialize(AppDbContext dbContext)
    {
        if (dbContext.Employees.Any())
        {
            return;
        }

        dbContext.Employees.AddRange(
            new Employee("1001", "Anna", "Muster", new DateOnly(1990, 5, 12), new DateOnly(2021, 1, 1), EmploymentType.MonthlySalary, 0m, 6500m),
            new Employee("1002", "Marco", "Beispiel", new DateOnly(1987, 8, 4), new DateOnly(2023, 3, 1), EmploymentType.Hourly, 38.50m, 0m));

        dbContext.SaveChanges();
    }
}
