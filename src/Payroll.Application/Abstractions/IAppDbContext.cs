using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Employees;
using Payroll.Domain.Expenses;
using Payroll.Domain.Payroll;
using Payroll.Domain.TimeTracking;

namespace Payroll.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<ImportedWorkTime> ImportedWorkTimes { get; }
    DbSet<ExpenseClaim> ExpenseClaims { get; }
    DbSet<PayrollRun> PayrollRuns { get; }
    DbSet<PayrollEntry> PayrollEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
