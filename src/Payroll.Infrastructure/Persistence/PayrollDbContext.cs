using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Employees;

namespace Payroll.Infrastructure.Persistence;

public sealed class PayrollDbContext : DbContext
{
    public PayrollDbContext(DbContextOptions<PayrollDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmploymentContract> EmploymentContracts => Set<EmploymentContract>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Employee>(builder =>
        {
            builder.ToTable("Employees");
            builder.HasKey(employee => employee.Id);
            builder.Property(employee => employee.PersonnelNumber).HasMaxLength(50).IsRequired();
            builder.Property(employee => employee.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(employee => employee.LastName).HasMaxLength(100).IsRequired();
            builder.Property(employee => employee.EntryDate).IsRequired();
            builder.Property(employee => employee.IsActive).IsRequired();
            builder.Property(employee => employee.ResidenceCountry).HasMaxLength(100);
            builder.Property(employee => employee.Nationality).HasMaxLength(100);
            builder.Property(employee => employee.PermitCode).HasMaxLength(50);
            builder.Property(employee => employee.TaxStatus).HasMaxLength(100);
            builder.Property(employee => employee.AhvNumber).HasMaxLength(50);
            builder.Property(employee => employee.Iban).HasMaxLength(50);
            builder.Property(employee => employee.PhoneNumber).HasMaxLength(50);
            builder.Property(employee => employee.Email).HasMaxLength(200);
            builder.OwnsOne(employee => employee.Address, addressBuilder =>
            {
                addressBuilder.Property(address => address.Street).HasColumnName("Street").HasMaxLength(150).IsRequired();
                addressBuilder.Property(address => address.HouseNumber).HasColumnName("HouseNumber").HasMaxLength(30);
                addressBuilder.Property(address => address.AddressLine2).HasColumnName("AddressLine2").HasMaxLength(150);
                addressBuilder.Property(address => address.PostalCode).HasColumnName("PostalCode").HasMaxLength(20).IsRequired();
                addressBuilder.Property(address => address.City).HasColumnName("City").HasMaxLength(100).IsRequired();
                addressBuilder.Property(address => address.Country).HasColumnName("Country").HasMaxLength(100).IsRequired();
            });
            builder.HasIndex(employee => employee.PersonnelNumber).IsUnique();
        });

        modelBuilder.Entity<EmploymentContract>(builder =>
        {
            builder.ToTable("EmploymentContracts");
            builder.HasKey(contract => contract.Id);
            builder.Property(contract => contract.EmployeeId).IsRequired();
            builder.Property(contract => contract.HourlyRateChf).HasColumnType("TEXT").IsRequired();
            builder.Property(contract => contract.MonthlyBvgDeductionChf).HasColumnType("TEXT").IsRequired();
            builder.OwnsOne(contract => contract.SupplementSettings, supplementBuilder =>
            {
                supplementBuilder.Property(settings => settings.NightSupplementRate).HasColumnType("TEXT");
                supplementBuilder.Property(settings => settings.SundaySupplementRate).HasColumnType("TEXT");
                supplementBuilder.Property(settings => settings.HolidaySupplementRate).HasColumnType("TEXT");
            });

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(contract => contract.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
