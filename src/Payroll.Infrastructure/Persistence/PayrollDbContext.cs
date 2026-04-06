using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Employees;
using Payroll.Domain.Expenses;
using Payroll.Domain.MonthlyRecords;
using Payroll.Domain.Settings;
using Payroll.Domain.TimeTracking;

namespace Payroll.Infrastructure.Persistence;

public sealed class PayrollDbContext : DbContext
{
    public PayrollDbContext(DbContextOptions<PayrollDbContext> options)
        : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmploymentContract> EmploymentContracts => Set<EmploymentContract>();
    public DbSet<EmployeeMonthlyRecord> EmployeeMonthlyRecords => Set<EmployeeMonthlyRecord>();
    public DbSet<PayrollSettings> PayrollSettings => Set<PayrollSettings>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<ExpenseEntry> ExpenseEntries => Set<ExpenseEntry>();

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
            builder.Property(contract => contract.SpecialSupplementRateChf).HasColumnType("TEXT").IsRequired();

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(contract => contract.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayrollSettings>(builder =>
        {
            builder.ToTable("PayrollSettings");
            builder.HasKey(settings => settings.Id);
            builder.Property(settings => settings.AhvIvEoRate).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.AlvRate).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.SicknessAccidentInsuranceRate).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.TrainingAndHolidayRate).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.VacationCompensationRate).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.VehiclePauschalzone1RateChf).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.VehiclePauschalzone2RateChf).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.VehicleRegiezone1RateChf).HasColumnType("TEXT").IsRequired();
            builder.OwnsOne(settings => settings.WorkTimeSupplementSettings, supplementBuilder =>
            {
                supplementBuilder.Property(item => item.NightSupplementRate).HasColumnType("TEXT");
                supplementBuilder.Property(item => item.SundaySupplementRate).HasColumnType("TEXT");
                supplementBuilder.Property(item => item.HolidaySupplementRate).HasColumnType("TEXT");
            });
        });

        modelBuilder.Entity<EmployeeMonthlyRecord>(builder =>
        {
            builder.ToTable("EmployeeMonthlyRecords");
            builder.HasKey(record => record.Id);
            builder.Property(record => record.EmployeeId).IsRequired();
            builder.Property(record => record.Year).IsRequired();
            builder.Property(record => record.Month).IsRequired();
            builder.Property(record => record.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.HasIndex(record => new { record.EmployeeId, record.Year, record.Month }).IsUnique();

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(record => record.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(record => record.TimeEntries)
                .WithOne()
                .HasForeignKey(entry => entry.EmployeeMonthlyRecordId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(record => record.ExpenseEntry)
                .WithOne()
                .HasForeignKey<ExpenseEntry>(entry => entry.EmployeeMonthlyRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TimeEntry>(builder =>
        {
            builder.ToTable("TimeEntries");
            builder.HasKey(entry => entry.Id);
            builder.Property(entry => entry.EmployeeMonthlyRecordId).IsRequired();
            builder.Property(entry => entry.EmployeeId).IsRequired();
            builder.Property(entry => entry.WorkDate).IsRequired();
            builder.Property(entry => entry.HoursWorked).HasColumnType("TEXT").IsRequired();
            builder.Property(entry => entry.NightHours).HasColumnType("TEXT").IsRequired();
            builder.Property(entry => entry.SundayHours).HasColumnType("TEXT").IsRequired();
            builder.Property(entry => entry.HolidayHours).HasColumnType("TEXT").IsRequired();
            builder.Property(entry => entry.VehiclePauschalzone1Chf).HasColumnType("TEXT").IsRequired();
            builder.Property(entry => entry.VehiclePauschalzone2Chf).HasColumnType("TEXT").IsRequired();
            builder.Property(entry => entry.VehicleRegiezone1Chf).HasColumnType("TEXT").IsRequired();
            builder.Property(entry => entry.Note).HasMaxLength(500);
            builder.HasIndex(entry => new { entry.EmployeeMonthlyRecordId, entry.WorkDate }).IsUnique();
        });

        modelBuilder.Entity<ExpenseEntry>(builder =>
        {
            builder.ToTable("ExpenseEntries");
            builder.HasKey(entry => entry.Id);
            builder.Property(entry => entry.EmployeeMonthlyRecordId).IsRequired();
            builder.Property(entry => entry.EmployeeId).IsRequired();
            builder.Property(entry => entry.ExpensesTotalChf).HasColumnType("TEXT").IsRequired();
            builder.Property(entry => entry.ExpenseTypeCode).HasMaxLength(50).IsRequired();
            builder.Property(entry => entry.Description).HasMaxLength(500).IsRequired();
            builder.HasIndex(entry => entry.EmployeeMonthlyRecordId).IsUnique();
        });
    }
}
