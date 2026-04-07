using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddMonthlyContractSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "EmploymentContractSnapshot_CapturedAtUtc",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: DateTimeOffset.MinValue);

        migrationBuilder.AddColumn<decimal>(
            name: "EmploymentContractSnapshot_HourlyRateChf",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<bool>(
            name: "EmploymentContractSnapshot_IsInitialized",
            table: "EmployeeMonthlyRecords",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<decimal>(
            name: "EmploymentContractSnapshot_MonthlyBvgDeductionChf",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "EmploymentContractSnapshot_SpecialSupplementRateChf",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<DateOnly>(
            name: "EmploymentContractSnapshot_ValidFrom",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: new DateOnly(1, 1, 1));

        migrationBuilder.AddColumn<DateOnly>(
            name: "EmploymentContractSnapshot_ValidTo",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "EmploymentContractSnapshot_CapturedAtUtc", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "EmploymentContractSnapshot_HourlyRateChf", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "EmploymentContractSnapshot_IsInitialized", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "EmploymentContractSnapshot_MonthlyBvgDeductionChf", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "EmploymentContractSnapshot_SpecialSupplementRateChf", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "EmploymentContractSnapshot_ValidFrom", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "EmploymentContractSnapshot_ValidTo", table: "EmployeeMonthlyRecords");
    }
}
