using Microsoft.EntityFrameworkCore;
using Payroll.Domain.Employees;
using Payroll.Domain.Expenses;
using Payroll.Domain.Imports;
using Payroll.Domain.MonthlyRecords;
using Payroll.Domain.Payroll;
using Payroll.Domain.SalaryCertificate;
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
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollRunLine> PayrollRunLines => Set<PayrollRunLine>();
    public DbSet<PayrollSettings> PayrollSettings => Set<PayrollSettings>();
    public DbSet<PayrollGeneralSettingsVersion> PayrollGeneralSettingsVersions => Set<PayrollGeneralSettingsVersion>();
    public DbSet<PayrollHourlySettingsVersion> PayrollHourlySettingsVersions => Set<PayrollHourlySettingsVersion>();
    public DbSet<PayrollMonthlySalarySettingsVersion> PayrollMonthlySalarySettingsVersions => Set<PayrollMonthlySalarySettingsVersion>();
    public DbSet<DepartmentOption> DepartmentOptions => Set<DepartmentOption>();
    public DbSet<EmploymentCategoryOption> EmploymentCategoryOptions => Set<EmploymentCategoryOption>();
    public DbSet<EmploymentLocationOption> EmploymentLocationOptions => Set<EmploymentLocationOption>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();
    public DbSet<ExpenseEntry> ExpenseEntries => Set<ExpenseEntry>();
    public DbSet<ImportMappingConfiguration> ImportMappingConfigurations => Set<ImportMappingConfiguration>();
    public DbSet<ImportExecutionStatus> ImportExecutionStatuses => Set<ImportExecutionStatus>();
    public DbSet<SalaryCertificateRecord> SalaryCertificateRecords => Set<SalaryCertificateRecord>();

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
            builder.Property(employee => employee.DepartmentOptionId);
            builder.Property(employee => employee.EmploymentCategoryOptionId);
            builder.Property(employee => employee.EmploymentLocationOptionId);
            builder.Property(employee => employee.WageType).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.OwnsOne(employee => employee.Address, addressBuilder =>
            {
                addressBuilder.Property(address => address.Street).HasColumnName("Street").HasMaxLength(150).IsRequired();
                addressBuilder.Property(address => address.HouseNumber).HasColumnName("HouseNumber").HasMaxLength(30);
                addressBuilder.Property(address => address.AddressLine2).HasColumnName("AddressLine2").HasMaxLength(150);
                addressBuilder.Property(address => address.PostalCode).HasColumnName("PostalCode").HasMaxLength(20).IsRequired();
                addressBuilder.Property(address => address.City).HasColumnName("City").HasMaxLength(100).IsRequired();
                addressBuilder.Property(address => address.Country).HasColumnName("Country").HasMaxLength(100).IsRequired();
            });
            builder.HasOne<DepartmentOption>()
                .WithMany()
                .HasForeignKey(employee => employee.DepartmentOptionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EmploymentCategoryOption>()
                .WithMany()
                .HasForeignKey(employee => employee.EmploymentCategoryOptionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EmploymentLocationOption>()
                .WithMany()
                .HasForeignKey(employee => employee.EmploymentLocationOptionId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(employee => employee.PersonnelNumber).IsUnique();
        });

        modelBuilder.Entity<EmploymentContract>(builder =>
        {
            builder.ToTable("EmploymentContracts");
            builder.HasKey(contract => contract.Id);
            builder.Property(contract => contract.EmployeeId).IsRequired();
            builder.Property(contract => contract.HourlyRateChf).HasColumnType("TEXT").IsRequired();
            builder.Property(contract => contract.MonthlySalaryAmountChf).HasColumnType("TEXT").IsRequired();
            builder.Property(contract => contract.MonthlyBvgDeductionChf).HasColumnType("TEXT").IsRequired();
            builder.Property(contract => contract.SpecialSupplementRateChf).HasColumnType("TEXT").IsRequired();
            builder.Property(contract => contract.WageType).HasConversion<string>().HasMaxLength(50).IsRequired();

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(contract => contract.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayrollSettings>(builder =>
        {
            builder.ToTable("PayrollSettings");
            builder.HasKey(settings => settings.Id);
            builder.Property(settings => settings.CompanyAddress).HasMaxLength(2000).IsRequired();
            builder.Property(settings => settings.AppFontFamily).HasMaxLength(500).IsRequired();
            builder.Property(settings => settings.AppFontSize).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.AppTextColorHex).HasMaxLength(20).IsRequired();
            builder.Property(settings => settings.AppMutedTextColorHex).HasMaxLength(20).IsRequired();
            builder.Property(settings => settings.AppBackgroundColorHex).HasMaxLength(20).IsRequired();
            builder.Property(settings => settings.AppAccentColorHex).HasMaxLength(20).IsRequired();
            builder.Property(settings => settings.AppLogoText).HasMaxLength(200).IsRequired();
            builder.Property(settings => settings.AppLogoPath).HasMaxLength(1000).IsRequired();
            builder.Property(settings => settings.AppPagePadding).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.AppPanelPadding).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.AppSectionSpacing).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.AppPanelCornerRadius).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.AppTableCellVerticalPadding).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.PrintFontFamily).HasMaxLength(500).IsRequired();
            builder.Property(settings => settings.PrintFontSize).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.PrintTextColorHex).HasMaxLength(20).IsRequired();
            builder.Property(settings => settings.PrintMutedTextColorHex).HasMaxLength(20).IsRequired();
            builder.Property(settings => settings.PrintAccentColorHex).HasMaxLength(20).IsRequired();
            builder.Property(settings => settings.PrintLogoText).HasMaxLength(200).IsRequired();
            builder.Property(settings => settings.PrintLogoPath).HasMaxLength(1000).IsRequired();
            builder.Property(settings => settings.PrintTemplate).HasMaxLength(20000).IsRequired();
            builder.Property(settings => settings.SalaryCertificatePdfTemplatePath).HasMaxLength(1000).IsRequired();
            builder.Property(settings => settings.DecimalSeparator).HasMaxLength(1).IsRequired();
            builder.Property(settings => settings.ThousandsSeparator).HasMaxLength(1).IsRequired();
            builder.Property(settings => settings.CurrencyCode).HasMaxLength(10).IsRequired();
            builder.Property(settings => settings.PayrollPreviewHelpVisibilityJson).HasMaxLength(8000).IsRequired();
            builder.Property(settings => settings.AhvIvEoRate).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.AlvRate).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.SicknessAccidentInsuranceRate).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.TrainingAndHolidayRate).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.VacationCompensationRate).HasColumnType("TEXT").IsRequired();
            builder.Property(settings => settings.VacationCompensationRateAge50Plus).HasColumnType("TEXT").IsRequired();
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

        modelBuilder.Entity<PayrollGeneralSettingsVersion>(builder =>
        {
            builder.ToTable("PayrollGeneralSettingsVersions");
            builder.HasKey(version => version.Id);
            builder.Property(version => version.ValidFrom).IsRequired();
            builder.Property(version => version.ValidTo);
            builder.Property(version => version.AhvIvEoRate).HasColumnType("TEXT").IsRequired();
            builder.Property(version => version.AlvRate).HasColumnType("TEXT").IsRequired();
            builder.Property(version => version.SicknessAccidentInsuranceRate).HasColumnType("TEXT").IsRequired();
            builder.Property(version => version.TrainingAndHolidayRate).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<PayrollHourlySettingsVersion>(builder =>
        {
            builder.ToTable("PayrollHourlySettingsVersions");
            builder.HasKey(version => version.Id);
            builder.Property(version => version.ValidFrom).IsRequired();
            builder.Property(version => version.ValidTo);
            builder.Property(version => version.NightSupplementRate).HasColumnType("TEXT");
            builder.Property(version => version.SundaySupplementRate).HasColumnType("TEXT");
            builder.Property(version => version.HolidaySupplementRate).HasColumnType("TEXT");
            builder.Property(version => version.VacationCompensationRate).HasColumnType("TEXT").IsRequired();
            builder.Property(version => version.VacationCompensationRateAge50Plus).HasColumnType("TEXT").IsRequired();
            builder.Property(version => version.VehiclePauschalzone1RateChf).HasColumnType("TEXT").IsRequired();
            builder.Property(version => version.VehiclePauschalzone2RateChf).HasColumnType("TEXT").IsRequired();
            builder.Property(version => version.VehicleRegiezone1RateChf).HasColumnType("TEXT").IsRequired();
        });

        modelBuilder.Entity<PayrollMonthlySalarySettingsVersion>(builder =>
        {
            builder.ToTable("PayrollMonthlySalarySettingsVersions");
            builder.HasKey(version => version.Id);
            builder.Property(version => version.ValidFrom).IsRequired();
            builder.Property(version => version.ValidTo);
        });

        modelBuilder.Entity<DepartmentOption>(builder =>
        {
            builder.ToTable("DepartmentOptions");
            builder.HasKey(option => option.Id);
            builder.Property(option => option.Name).HasMaxLength(200).IsRequired();
            builder.Property(option => option.IsGavMandatory).IsRequired();
            builder.HasIndex(option => option.Name).IsUnique();
        });

        modelBuilder.Entity<EmploymentCategoryOption>(builder =>
        {
            builder.ToTable("EmploymentCategoryOptions");
            builder.HasKey(option => option.Id);
            builder.Property(option => option.Name).HasMaxLength(200).IsRequired();
            builder.HasIndex(option => option.Name).IsUnique();
        });

        modelBuilder.Entity<EmploymentLocationOption>(builder =>
        {
            builder.ToTable("EmploymentLocationOptions");
            builder.HasKey(option => option.Id);
            builder.Property(option => option.Name).HasMaxLength(200).IsRequired();
            builder.HasIndex(option => option.Name).IsUnique();
        });

        modelBuilder.Entity<ImportMappingConfiguration>(builder =>
        {
            builder.ToTable("ImportMappingConfigurations");
            builder.HasKey(configuration => configuration.Id);
            builder.Property(configuration => configuration.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(configuration => configuration.Name).HasMaxLength(200).IsRequired();
            builder.Property(configuration => configuration.Delimiter).HasMaxLength(1).IsRequired();
            builder.Property(configuration => configuration.FieldsEnclosed).IsRequired();
            builder.Property(configuration => configuration.TextQualifier).HasMaxLength(1).IsRequired();
            builder.Property(configuration => configuration.FieldMappingsJson).HasMaxLength(20000).IsRequired();
            builder.HasIndex(configuration => new { configuration.Type, configuration.Name }).IsUnique();
        });

        modelBuilder.Entity<ImportExecutionStatus>(builder =>
        {
            builder.ToTable("ImportExecutionStatuses");
            builder.HasKey(status => status.Id);
            builder.Property(status => status.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(status => status.Year).IsRequired();
            builder.Property(status => status.Month).IsRequired();
            builder.Property(status => status.ImportedAtUtc).IsRequired();
            builder.HasIndex(status => new { status.Type, status.Year, status.Month }).IsUnique();
        });

        modelBuilder.Entity<SalaryCertificateRecord>(builder =>
        {
            builder.ToTable("SalaryCertificateRecords");
            builder.HasKey(record => record.Id);
            builder.Property(record => record.EmployeeId).IsRequired();
            builder.Property(record => record.Year).IsRequired();
            builder.Property(record => record.OutputFilePath).HasMaxLength(2000);
            builder.Property(record => record.FileHash).HasMaxLength(200);
            builder.Property(record => record.CreatedAtUtc).IsRequired();
            builder.Property(record => record.UpdatedAtUtc);
            builder.HasIndex(record => new { record.EmployeeId, record.Year, record.CreatedAtUtc });

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(record => record.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayrollRun>(builder =>
        {
            builder.ToTable("PayrollRuns");
            builder.HasKey(run => run.Id);
            builder.Property(run => run.PeriodKey).HasMaxLength(7).IsRequired();
            builder.Property(run => run.PaymentDate).IsRequired();
            builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(run => run.CancelledAtUtc);
            builder.HasIndex(run => run.PeriodKey);

            builder.HasMany(run => run.Lines)
                .WithOne()
                .HasForeignKey("PayrollRunId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayrollRunLine>(builder =>
        {
            builder.ToTable("PayrollRunLines");
            builder.HasKey(line => line.Id);
            builder.Property<Guid>("PayrollRunId").IsRequired();
            builder.Property(line => line.EmployeeId).IsRequired();
            builder.Property(line => line.LineType).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(line => line.ValueOrigin).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(line => line.Code).HasMaxLength(50).IsRequired();
            builder.Property(line => line.Description).HasMaxLength(200).IsRequired();
            builder.Property(line => line.Unit).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(line => line.Quantity).HasColumnType("TEXT");
            builder.Property(line => line.RateChf).HasColumnType("TEXT");
            builder.Property(line => line.AmountChf).HasColumnType("TEXT").IsRequired();
            builder.HasIndex("PayrollRunId");
            builder.HasIndex(line => line.EmployeeId);

            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(line => line.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EmployeeMonthlyRecord>(builder =>
        {
            builder.ToTable("EmployeeMonthlyRecords");
            builder.HasKey(record => record.Id);
            builder.Property(record => record.EmployeeId).IsRequired();
            builder.Property(record => record.Year).IsRequired();
            builder.Property(record => record.Month).IsRequired();
            builder.Property(record => record.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
            builder.Property(record => record.WithholdingTaxRatePercent).HasColumnType("TEXT").IsRequired();
            builder.Property(record => record.WithholdingTaxCorrectionAmountChf).HasColumnType("TEXT").IsRequired();
            builder.Property(record => record.WithholdingTaxCorrectionText).HasMaxLength(500);
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

            builder.OwnsOne(record => record.PayrollParameterSnapshot, snapshotBuilder =>
            {
                snapshotBuilder.Property(item => item.IsInitialized)
                    .HasColumnName("PayrollParameterSnapshot_IsInitialized")
                    .IsRequired();
                snapshotBuilder.Property(item => item.CapturedAtUtc)
                    .HasColumnName("PayrollParameterSnapshot_CapturedAtUtc");
                snapshotBuilder.Property(item => item.NightSupplementRate)
                    .HasColumnName("PayrollParameterSnapshot_NightSupplementRate")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.SundaySupplementRate)
                    .HasColumnName("PayrollParameterSnapshot_SundaySupplementRate")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.HolidaySupplementRate)
                    .HasColumnName("PayrollParameterSnapshot_HolidaySupplementRate")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.AhvIvEoRate)
                    .HasColumnName("PayrollParameterSnapshot_AhvIvEoRate")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.AlvRate)
                    .HasColumnName("PayrollParameterSnapshot_AlvRate")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.SicknessAccidentInsuranceRate)
                    .HasColumnName("PayrollParameterSnapshot_SicknessAccidentInsuranceRate")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.TrainingAndHolidayRate)
                    .HasColumnName("PayrollParameterSnapshot_TrainingAndHolidayRate")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.VacationCompensationRate)
                    .HasColumnName("PayrollParameterSnapshot_VacationCompensationRate")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.VacationCompensationRateAge50Plus)
                    .HasColumnName("PayrollParameterSnapshot_VacationCompensationRateAge50Plus")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.VehiclePauschalzone1RateChf)
                    .HasColumnName("PayrollParameterSnapshot_VehiclePauschalzone1RateChf")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.VehiclePauschalzone2RateChf)
                    .HasColumnName("PayrollParameterSnapshot_VehiclePauschalzone2RateChf")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.VehicleRegiezone1RateChf)
                    .HasColumnName("PayrollParameterSnapshot_VehicleRegiezone1RateChf")
                    .HasColumnType("TEXT");
            });

            builder.OwnsOne(record => record.EmploymentContractSnapshot, snapshotBuilder =>
            {
                snapshotBuilder.Property(item => item.IsInitialized)
                    .HasColumnName("EmploymentContractSnapshot_IsInitialized")
                    .IsRequired();
                snapshotBuilder.Property(item => item.CapturedAtUtc)
                    .HasColumnName("EmploymentContractSnapshot_CapturedAtUtc");
                snapshotBuilder.Property(item => item.ValidFrom)
                    .HasColumnName("EmploymentContractSnapshot_ValidFrom");
                snapshotBuilder.Property(item => item.ValidTo)
                    .HasColumnName("EmploymentContractSnapshot_ValidTo");
                snapshotBuilder.Property(item => item.HourlyRateChf)
                    .HasColumnName("EmploymentContractSnapshot_HourlyRateChf")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.MonthlySalaryAmountChf)
                    .HasColumnName("EmploymentContractSnapshot_MonthlySalaryAmountChf")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.MonthlyBvgDeductionChf)
                    .HasColumnName("EmploymentContractSnapshot_MonthlyBvgDeductionChf")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.SpecialSupplementRateChf)
                    .HasColumnName("EmploymentContractSnapshot_SpecialSupplementRateChf")
                    .HasColumnType("TEXT");
                snapshotBuilder.Property(item => item.WageType)
                    .HasColumnName("EmploymentContractSnapshot_WageType")
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();
            });
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
