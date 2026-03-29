using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payroll.Application.Abstractions;
using Payroll.Application.Employees;
using Payroll.Application.Payroll;
using Payroll.Infrastructure.Payroll;
using Payroll.Infrastructure.Persistence;

namespace Payroll.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IPayrollCalculator, SwissPayrollCalculator>();
        services.AddScoped<IPayslipPdfGenerator, PlaceholderPayslipPdfGenerator>();
        services.AddScoped<EmployeeQueries>();
        services.AddScoped<PayrollRunService>();
        return services;
    }
}
