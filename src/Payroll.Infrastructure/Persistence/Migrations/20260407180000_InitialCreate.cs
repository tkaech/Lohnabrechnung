using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    public const string MigrationIdValue = "20260407180000_InitialCreate";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DepartmentOptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DepartmentOptions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Employees",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PersonnelNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                BirthDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                EntryDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                ExitDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                ResidenceCountry = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Nationality = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                PermitCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                TaxStatus = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                IsSubjectToWithholdingTax = table.Column<bool>(type: "INTEGER", nullable: true),
                AhvNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Iban = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                DepartmentOptionId = table.Column<Guid>(type: "TEXT", nullable: true),
                EmploymentCategoryOptionId = table.Column<Guid>(type: "TEXT", nullable: true),
                EmploymentLocationOptionId = table.Column<Guid>(type: "TEXT", nullable: true),
                Street = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                HouseNumber = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                AddressLine2 = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                PostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Country = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Employees", x => x.Id);
                table.ForeignKey(
                    name: "FK_Employees_DepartmentOptions_DepartmentOptionId",
                    column: x => x.DepartmentOptionId,
                    principalTable: "DepartmentOptions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "EmploymentCategoryOptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmploymentCategoryOptions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EmploymentLocationOptions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmploymentLocationOptions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PayrollSettings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CompanyAddress = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                AppFontFamily = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                AppFontSize = table.Column<decimal>(type: "TEXT", nullable: false),
                AppTextColorHex = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                AppMutedTextColorHex = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                AppBackgroundColorHex = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                AppAccentColorHex = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                AppLogoText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                AppLogoPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                PrintFontFamily = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                PrintFontSize = table.Column<decimal>(type: "TEXT", nullable: false),
                PrintTextColorHex = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                PrintMutedTextColorHex = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                PrintAccentColorHex = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                PrintLogoText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                PrintLogoPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                PrintTemplate = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: false),
                AhvIvEoRate = table.Column<decimal>(type: "TEXT", nullable: false),
                AlvRate = table.Column<decimal>(type: "TEXT", nullable: false),
                SicknessAccidentInsuranceRate = table.Column<decimal>(type: "TEXT", nullable: false),
                TrainingAndHolidayRate = table.Column<decimal>(type: "TEXT", nullable: false),
                VacationCompensationRate = table.Column<decimal>(type: "TEXT", nullable: false),
                VehiclePauschalzone1RateChf = table.Column<decimal>(type: "TEXT", nullable: false),
                VehiclePauschalzone2RateChf = table.Column<decimal>(type: "TEXT", nullable: false),
                VehicleRegiezone1RateChf = table.Column<decimal>(type: "TEXT", nullable: false),
                WorkTimeSupplementSettings_NightSupplementRate = table.Column<decimal>(type: "TEXT", nullable: true),
                WorkTimeSupplementSettings_SundaySupplementRate = table.Column<decimal>(type: "TEXT", nullable: true),
                WorkTimeSupplementSettings_HolidaySupplementRate = table.Column<decimal>(type: "TEXT", nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PayrollSettings", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "EmploymentContracts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                ValidFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                ValidTo = table.Column<DateOnly>(type: "TEXT", nullable: true),
                HourlyRateChf = table.Column<decimal>(type: "TEXT", nullable: false),
                MonthlyBvgDeductionChf = table.Column<decimal>(type: "TEXT", nullable: false),
                SpecialSupplementRateChf = table.Column<decimal>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmploymentContracts", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmploymentContracts_Employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "EmployeeMonthlyRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                Year = table.Column<int>(type: "INTEGER", nullable: false),
                Month = table.Column<int>(type: "INTEGER", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_EmployeeMonthlyRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_EmployeeMonthlyRecords_Employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ExpenseEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeMonthlyRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                ExpensesTotalChf = table.Column<decimal>(type: "TEXT", nullable: false),
                ExpenseTypeCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExpenseEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExpenseEntries_EmployeeMonthlyRecords_EmployeeMonthlyRecordId",
                    column: x => x.EmployeeMonthlyRecordId,
                    principalTable: "EmployeeMonthlyRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "TimeEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeMonthlyRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                WorkDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                HoursWorked = table.Column<decimal>(type: "TEXT", nullable: false),
                NightHours = table.Column<decimal>(type: "TEXT", nullable: false),
                SundayHours = table.Column<decimal>(type: "TEXT", nullable: false),
                HolidayHours = table.Column<decimal>(type: "TEXT", nullable: false),
                VehiclePauschalzone1Chf = table.Column<decimal>(type: "TEXT", nullable: false),
                VehiclePauschalzone2Chf = table.Column<decimal>(type: "TEXT", nullable: false),
                VehicleRegiezone1Chf = table.Column<decimal>(type: "TEXT", nullable: false),
                Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TimeEntries", x => x.Id);
                table.ForeignKey(
                    name: "FK_TimeEntries_EmployeeMonthlyRecords_EmployeeMonthlyRecordId",
                    column: x => x.EmployeeMonthlyRecordId,
                    principalTable: "EmployeeMonthlyRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DepartmentOptions_Name",
            table: "DepartmentOptions",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Employees_DepartmentOptionId",
            table: "Employees",
            column: "DepartmentOptionId");

        migrationBuilder.CreateIndex(
            name: "IX_Employees_EmploymentCategoryOptionId",
            table: "Employees",
            column: "EmploymentCategoryOptionId");

        migrationBuilder.CreateIndex(
            name: "IX_Employees_EmploymentLocationOptionId",
            table: "Employees",
            column: "EmploymentLocationOptionId");

        migrationBuilder.CreateIndex(
            name: "IX_Employees_PersonnelNumber",
            table: "Employees",
            column: "PersonnelNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EmployeeMonthlyRecords_EmployeeId_Year_Month",
            table: "EmployeeMonthlyRecords",
            columns: new[] { "EmployeeId", "Year", "Month" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EmploymentCategoryOptions_Name",
            table: "EmploymentCategoryOptions",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_EmploymentContracts_EmployeeId",
            table: "EmploymentContracts",
            column: "EmployeeId");

        migrationBuilder.CreateIndex(
            name: "IX_EmploymentLocationOptions_Name",
            table: "EmploymentLocationOptions",
            column: "Name",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExpenseEntries_EmployeeMonthlyRecordId",
            table: "ExpenseEntries",
            column: "EmployeeMonthlyRecordId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TimeEntries_EmployeeMonthlyRecordId_WorkDate",
            table: "TimeEntries",
            columns: new[] { "EmployeeMonthlyRecordId", "WorkDate" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_Employees_EmploymentCategoryOptions_EmploymentCategoryOptionId",
            table: "Employees",
            column: "EmploymentCategoryOptionId",
            principalTable: "EmploymentCategoryOptions",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Employees_EmploymentLocationOptions_EmploymentLocationOptionId",
            table: "Employees",
            column: "EmploymentLocationOptionId",
            principalTable: "EmploymentLocationOptions",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "EmploymentContracts");
        migrationBuilder.DropTable(name: "ExpenseEntries");
        migrationBuilder.DropTable(name: "PayrollSettings");
        migrationBuilder.DropTable(name: "TimeEntries");
        migrationBuilder.DropTable(name: "EmployeeMonthlyRecords");
        migrationBuilder.DropTable(name: "Employees");
        migrationBuilder.DropTable(name: "DepartmentOptions");
        migrationBuilder.DropTable(name: "EmploymentCategoryOptions");
        migrationBuilder.DropTable(name: "EmploymentLocationOptions");
    }
}
