using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Payroll.Domain.MonthlyRecords;
using Payroll.Infrastructure.Persistence;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PayrollDbContext))]
partial class PayrollDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "8.0.10");

        modelBuilder.Entity("Payroll.Domain.Employees.Employee", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<string>("AhvNumber").HasMaxLength(50).HasColumnType("TEXT");
            b.Property<string>("Email").HasMaxLength(200).HasColumnType("TEXT");
            b.Property<Guid?>("DepartmentOptionId").HasColumnType("TEXT");
            b.Property<DateOnly?>("BirthDate").HasColumnType("TEXT");
            b.Property<string>("Country").HasMaxLength(100).HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<string>("FirstName").IsRequired().HasMaxLength(100).HasColumnType("TEXT");
            b.Property<string>("LastName").IsRequired().HasMaxLength(100).HasColumnType("TEXT");
            b.Property<string>("PersonnelNumber").IsRequired().HasMaxLength(50).HasColumnType("TEXT");
            b.Property<bool>("IsActive").HasColumnType("INTEGER");
            b.Property<string>("WageType").IsRequired().HasMaxLength(50).HasColumnType("TEXT");
            b.Property<DateOnly>("EntryDate").HasColumnType("TEXT");
            b.Property<DateOnly?>("ExitDate").HasColumnType("TEXT");
            b.Property<Guid?>("EmploymentCategoryOptionId").HasColumnType("TEXT");
            b.Property<Guid?>("EmploymentLocationOptionId").HasColumnType("TEXT");
            b.Property<string>("Iban").HasMaxLength(50).HasColumnType("TEXT");
            b.Property<bool?>("IsSubjectToWithholdingTax").HasColumnType("INTEGER");
            b.Property<string>("Nationality").HasMaxLength(100).HasColumnType("TEXT");
            b.Property<string>("PermitCode").HasMaxLength(50).HasColumnType("TEXT");
            b.Property<string>("PhoneNumber").HasMaxLength(50).HasColumnType("TEXT");
            b.Property<string>("ResidenceCountry").HasMaxLength(100).HasColumnType("TEXT");
            b.Property<string>("TaxStatus").HasMaxLength(100).HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("TEXT");
            b.Property<string>("AddressLine2").HasColumnName("AddressLine2").HasMaxLength(150).HasColumnType("TEXT");
            b.Property<string>("City").HasColumnName("City").IsRequired().HasMaxLength(100).HasColumnType("TEXT");
            b.Property<string>("HouseNumber").HasColumnName("HouseNumber").HasMaxLength(30).HasColumnType("TEXT");
            b.Property<string>("PostalCode").HasColumnName("PostalCode").IsRequired().HasMaxLength(20).HasColumnType("TEXT");
            b.Property<string>("Street").HasColumnName("Street").IsRequired().HasMaxLength(150).HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("DepartmentOptionId");
            b.HasIndex("EmploymentCategoryOptionId");
            b.HasIndex("EmploymentLocationOptionId");
            b.HasIndex("PersonnelNumber").IsUnique();
            b.ToTable("Employees");
        });

        modelBuilder.Entity("Payroll.Domain.Employees.EmploymentContract", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<Guid>("EmployeeId").HasColumnType("TEXT");
            b.Property<decimal>("HourlyRateChf").HasColumnType("TEXT");
            b.Property<decimal>("MonthlySalaryAmountChf").HasColumnType("TEXT");
            b.Property<decimal>("MonthlyBvgDeductionChf").HasColumnType("TEXT");
            b.Property<decimal>("SpecialSupplementRateChf").HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("TEXT");
            b.Property<DateOnly>("ValidFrom").HasColumnType("TEXT");
            b.Property<DateOnly?>("ValidTo").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("EmployeeId");
            b.ToTable("EmploymentContracts");
        });

        modelBuilder.Entity("Payroll.Domain.Expenses.ExpenseEntry", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<string>("Description").IsRequired().HasMaxLength(500).HasColumnType("TEXT");
            b.Property<Guid>("EmployeeId").HasColumnType("TEXT");
            b.Property<Guid>("EmployeeMonthlyRecordId").HasColumnType("TEXT");
            b.Property<string>("ExpenseTypeCode").IsRequired().HasMaxLength(50).HasColumnType("TEXT");
            b.Property<decimal>("ExpensesTotalChf").HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("EmployeeMonthlyRecordId").IsUnique();
            b.ToTable("ExpenseEntries");
        });

        modelBuilder.Entity("Payroll.Domain.MonthlyRecords.EmployeeMonthlyRecord", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<DateTimeOffset>("EmploymentContractSnapshot_CapturedAtUtc").HasColumnType("TEXT");
            b.Property<decimal>("EmploymentContractSnapshot_HourlyRateChf").HasColumnType("TEXT");
            b.Property<bool>("EmploymentContractSnapshot_IsInitialized").HasColumnType("INTEGER");
            b.Property<decimal>("EmploymentContractSnapshot_MonthlySalaryAmountChf").HasColumnType("TEXT");
            b.Property<decimal>("EmploymentContractSnapshot_MonthlyBvgDeductionChf").HasColumnType("TEXT");
            b.Property<decimal>("EmploymentContractSnapshot_SpecialSupplementRateChf").HasColumnType("TEXT");
            b.Property<DateOnly>("EmploymentContractSnapshot_ValidFrom").HasColumnType("TEXT");
            b.Property<DateOnly?>("EmploymentContractSnapshot_ValidTo").HasColumnType("TEXT");
            b.Property<Guid>("EmployeeId").HasColumnType("TEXT");
            b.Property<int>("Month").HasColumnType("INTEGER");
            b.Property<decimal>("PayrollParameterSnapshot_AhvIvEoRate").HasColumnType("TEXT");
            b.Property<decimal>("PayrollParameterSnapshot_AlvRate").HasColumnType("TEXT");
            b.Property<DateTimeOffset>("PayrollParameterSnapshot_CapturedAtUtc").HasColumnType("TEXT");
            b.Property<decimal?>("PayrollParameterSnapshot_HolidaySupplementRate").HasColumnType("TEXT");
            b.Property<bool>("PayrollParameterSnapshot_IsInitialized").HasColumnType("INTEGER");
            b.Property<decimal?>("PayrollParameterSnapshot_NightSupplementRate").HasColumnType("TEXT");
            b.Property<decimal>("PayrollParameterSnapshot_SicknessAccidentInsuranceRate").HasColumnType("TEXT");
            b.Property<decimal?>("PayrollParameterSnapshot_SundaySupplementRate").HasColumnType("TEXT");
            b.Property<decimal>("PayrollParameterSnapshot_TrainingAndHolidayRate").HasColumnType("TEXT");
            b.Property<decimal>("PayrollParameterSnapshot_VacationCompensationRate").HasColumnType("TEXT");
            b.Property<decimal>("PayrollParameterSnapshot_VacationCompensationRateAge50Plus").HasColumnType("TEXT");
            b.Property<decimal>("PayrollParameterSnapshot_VehiclePauschalzone1RateChf").HasColumnType("TEXT");
            b.Property<decimal>("PayrollParameterSnapshot_VehiclePauschalzone2RateChf").HasColumnType("TEXT");
            b.Property<decimal>("PayrollParameterSnapshot_VehicleRegiezone1RateChf").HasColumnType("TEXT");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("TEXT");
            b.Property<decimal>("WithholdingTaxCorrectionAmountChf").HasColumnType("TEXT");
            b.Property<string>("WithholdingTaxCorrectionText").HasMaxLength(500).HasColumnType("TEXT");
            b.Property<decimal>("WithholdingTaxRatePercent").HasColumnType("TEXT");
            b.Property<int>("Year").HasColumnType("INTEGER");
            b.HasKey("Id");
            b.HasIndex("EmployeeId", "Year", "Month").IsUnique();
            b.ToTable("EmployeeMonthlyRecords");
        });

        modelBuilder.Entity("Payroll.Domain.Settings.DepartmentOption", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("Name").IsUnique();
            b.ToTable("DepartmentOptions");
        });

        modelBuilder.Entity("Payroll.Domain.Settings.EmploymentCategoryOption", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("Name").IsUnique();
            b.ToTable("EmploymentCategoryOptions");
        });

        modelBuilder.Entity("Payroll.Domain.Settings.EmploymentLocationOption", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("Name").IsUnique();
            b.ToTable("EmploymentLocationOptions");
        });

        modelBuilder.Entity("Payroll.Domain.Settings.PayrollSettings", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<decimal>("AhvIvEoRate").HasColumnType("TEXT");
            b.Property<string>("AppAccentColorHex").IsRequired().HasMaxLength(20).HasColumnType("TEXT");
            b.Property<string>("AppBackgroundColorHex").IsRequired().HasMaxLength(20).HasColumnType("TEXT");
            b.Property<string>("AppFontFamily").IsRequired().HasMaxLength(500).HasColumnType("TEXT");
            b.Property<decimal>("AppFontSize").HasColumnType("TEXT");
            b.Property<string>("AppLogoPath").IsRequired().HasMaxLength(1000).HasColumnType("TEXT");
            b.Property<string>("AppLogoText").IsRequired().HasMaxLength(200).HasColumnType("TEXT");
            b.Property<string>("AppMutedTextColorHex").IsRequired().HasMaxLength(20).HasColumnType("TEXT");
            b.Property<decimal>("AppPanelCornerRadius").HasColumnType("TEXT");
            b.Property<decimal>("AppPagePadding").HasColumnType("TEXT");
            b.Property<decimal>("AppPanelPadding").HasColumnType("TEXT");
            b.Property<decimal>("AppSectionSpacing").HasColumnType("TEXT");
            b.Property<decimal>("AppTableCellVerticalPadding").HasColumnType("TEXT");
            b.Property<string>("AppTextColorHex").IsRequired().HasMaxLength(20).HasColumnType("TEXT");
            b.Property<decimal>("AlvRate").HasColumnType("TEXT");
            b.Property<string>("CompanyAddress").IsRequired().HasMaxLength(2000).HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<string>("PrintAccentColorHex").IsRequired().HasMaxLength(20).HasColumnType("TEXT");
            b.Property<string>("PrintFontFamily").IsRequired().HasMaxLength(500).HasColumnType("TEXT");
            b.Property<decimal>("PrintFontSize").HasColumnType("TEXT");
            b.Property<string>("PrintLogoPath").IsRequired().HasMaxLength(1000).HasColumnType("TEXT");
            b.Property<string>("PrintLogoText").IsRequired().HasMaxLength(200).HasColumnType("TEXT");
            b.Property<string>("PrintMutedTextColorHex").IsRequired().HasMaxLength(20).HasColumnType("TEXT");
            b.Property<string>("PrintTemplate").IsRequired().HasMaxLength(20000).HasColumnType("TEXT");
            b.Property<string>("PrintTextColorHex").IsRequired().HasMaxLength(20).HasColumnType("TEXT");
            b.Property<decimal>("SicknessAccidentInsuranceRate").HasColumnType("TEXT");
            b.Property<decimal>("TrainingAndHolidayRate").HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("TEXT");
            b.Property<decimal>("VacationCompensationRate").HasColumnType("TEXT");
            b.Property<decimal>("VacationCompensationRateAge50Plus").HasColumnType("TEXT");
            b.Property<decimal>("VehiclePauschalzone1RateChf").HasColumnType("TEXT");
            b.Property<decimal>("VehiclePauschalzone2RateChf").HasColumnType("TEXT");
            b.Property<decimal>("VehicleRegiezone1RateChf").HasColumnType("TEXT");
            b.Property<decimal?>("WorkTimeSupplementSettings_HolidaySupplementRate").HasColumnType("TEXT");
            b.Property<decimal?>("WorkTimeSupplementSettings_NightSupplementRate").HasColumnType("TEXT");
            b.Property<decimal?>("WorkTimeSupplementSettings_SundaySupplementRate").HasColumnType("TEXT");
            b.HasKey("Id");
            b.ToTable("PayrollSettings");
        });

        modelBuilder.Entity("Payroll.Domain.Payroll.PayrollRun", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("CancelledAtUtc").HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<DateOnly>("PaymentDate").HasColumnType("TEXT");
            b.Property<string>("PeriodKey").IsRequired().HasMaxLength(7).HasColumnType("TEXT");
            b.Property<string>("Status").IsRequired().HasMaxLength(50).HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("PeriodKey");
            b.ToTable("PayrollRuns");
        });

        modelBuilder.Entity("Payroll.Domain.Payroll.PayrollRunLine", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<decimal>("AmountChf").HasColumnType("TEXT");
            b.Property<string>("Code").IsRequired().HasMaxLength(50).HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<string>("Description").IsRequired().HasMaxLength(200).HasColumnType("TEXT");
            b.Property<Guid>("EmployeeId").HasColumnType("TEXT");
            b.Property<string>("LineType").IsRequired().HasMaxLength(50).HasColumnType("TEXT");
            b.Property<Guid>("PayrollRunId").HasColumnType("TEXT");
            b.Property<decimal?>("Quantity").HasColumnType("TEXT");
            b.Property<decimal?>("RateChf").HasColumnType("TEXT");
            b.Property<string>("Unit").IsRequired().HasMaxLength(50).HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("TEXT");
            b.Property<string>("ValueOrigin").IsRequired().HasMaxLength(50).HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("EmployeeId");
            b.HasIndex("PayrollRunId");
            b.ToTable("PayrollRunLines");
        });

        modelBuilder.Entity("Payroll.Domain.TimeTracking.TimeEntry", b =>
        {
            b.Property<Guid>("Id").ValueGeneratedOnAdd().HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAtUtc").HasColumnType("TEXT");
            b.Property<Guid>("EmployeeId").HasColumnType("TEXT");
            b.Property<Guid>("EmployeeMonthlyRecordId").HasColumnType("TEXT");
            b.Property<decimal>("HolidayHours").HasColumnType("TEXT");
            b.Property<decimal>("HoursWorked").HasColumnType("TEXT");
            b.Property<decimal>("NightHours").HasColumnType("TEXT");
            b.Property<string>("Note").HasMaxLength(500).HasColumnType("TEXT");
            b.Property<decimal>("SundayHours").HasColumnType("TEXT");
            b.Property<DateTimeOffset?>("UpdatedAtUtc").HasColumnType("TEXT");
            b.Property<decimal>("VehiclePauschalzone1Chf").HasColumnType("TEXT");
            b.Property<decimal>("VehiclePauschalzone2Chf").HasColumnType("TEXT");
            b.Property<decimal>("VehicleRegiezone1Chf").HasColumnType("TEXT");
            b.Property<DateOnly>("WorkDate").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("EmployeeMonthlyRecordId", "WorkDate").IsUnique();
            b.ToTable("TimeEntries");
        });

        modelBuilder.Entity("Payroll.Domain.Employees.Employee", b =>
        {
            b.HasOne("Payroll.Domain.Settings.DepartmentOption", null)
                .WithMany()
                .HasForeignKey("DepartmentOptionId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne("Payroll.Domain.Settings.EmploymentCategoryOption", null)
                .WithMany()
                .HasForeignKey("EmploymentCategoryOptionId")
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne("Payroll.Domain.Settings.EmploymentLocationOption", null)
                .WithMany()
                .HasForeignKey("EmploymentLocationOptionId")
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity("Payroll.Domain.Employees.EmploymentContract", b =>
        {
            b.HasOne("Payroll.Domain.Employees.Employee", null)
                .WithMany()
                .HasForeignKey("EmployeeId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Payroll.Domain.Expenses.ExpenseEntry", b =>
        {
            b.HasOne("Payroll.Domain.MonthlyRecords.EmployeeMonthlyRecord", null)
                .WithOne("ExpenseEntry")
                .HasForeignKey("Payroll.Domain.Expenses.ExpenseEntry", "EmployeeMonthlyRecordId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Payroll.Domain.MonthlyRecords.EmployeeMonthlyRecord", b =>
        {
            b.HasOne("Payroll.Domain.Employees.Employee", null)
                .WithMany()
                .HasForeignKey("EmployeeId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Payroll.Domain.Payroll.PayrollRunLine", b =>
        {
            b.HasOne("Payroll.Domain.Employees.Employee", null)
                .WithMany()
                .HasForeignKey("EmployeeId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            b.HasOne("Payroll.Domain.Payroll.PayrollRun", null)
                .WithMany("Lines")
                .HasForeignKey("PayrollRunId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Payroll.Domain.TimeTracking.TimeEntry", b =>
        {
            b.HasOne("Payroll.Domain.MonthlyRecords.EmployeeMonthlyRecord", null)
                .WithMany("TimeEntries")
                .HasForeignKey("EmployeeMonthlyRecordId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Payroll.Domain.MonthlyRecords.EmployeeMonthlyRecord", b =>
        {
            b.Navigation("ExpenseEntry");
            b.Navigation("TimeEntries");
        });

        modelBuilder.Entity("Payroll.Domain.Payroll.PayrollRun", b =>
        {
            b.Navigation("Lines");
        });
#pragma warning restore 612, 618
    }
}
