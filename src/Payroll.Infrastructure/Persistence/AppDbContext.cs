using Microsoft.EntityFrameworkCore;
using Payroll.Application.Abstractions;
using Payroll.Domain.Employees;
using Payroll.Domain.Expenses;
using Payroll.Domain.Payroll;
using Payroll.Domain.TimeTracking;

namespace Payroll.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ImportedWorkTime> ImportedWorkTimes => Set<ImportedWorkTime>();
    public DbSet<ExpenseClaim> ExpenseClaims => Set<ExpenseClaim>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollEntry> PayrollEntries => Set<PayrollEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EmployeeNumber).HasMaxLength(20).IsRequired();
            entity.Property(item => item.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(item => item.LastName).HasMaxLength(100).IsRequired();
            entity.HasIndex(item => item.EmployeeNumber).IsUnique();
        });

        modelBuilder.Entity<ImportedWorkTime>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SourceFileName).HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<ExpenseClaim>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Description).HasMaxLength(250).IsRequired();
        });

        modelBuilder.Entity<PayrollRun>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasMany(item => item.Entries)
                .WithOne()
                .HasForeignKey(entry => entry.PayrollRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayrollEntry>(entity =>
        {
            entity.HasKey(item => item.Id);
        });
    }
}
