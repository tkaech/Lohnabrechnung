using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddMonthlyPayrollParameterSnapshot : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "PayrollParameterSnapshot_AhvIvEoRate",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "PayrollParameterSnapshot_AlvRate",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "PayrollParameterSnapshot_CapturedAtUtc",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: DateTimeOffset.MinValue);

        migrationBuilder.AddColumn<decimal?>(
            name: "PayrollParameterSnapshot_HolidaySupplementRate",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "PayrollParameterSnapshot_IsInitialized",
            table: "EmployeeMonthlyRecords",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<decimal?>(
            name: "PayrollParameterSnapshot_NightSupplementRate",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "PayrollParameterSnapshot_SicknessAccidentInsuranceRate",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal?>(
            name: "PayrollParameterSnapshot_SundaySupplementRate",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "PayrollParameterSnapshot_TrainingAndHolidayRate",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "PayrollParameterSnapshot_VacationCompensationRate",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "PayrollParameterSnapshot_VacationCompensationRateAge50Plus",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "PayrollParameterSnapshot_VehiclePauschalzone1RateChf",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "PayrollParameterSnapshot_VehiclePauschalzone2RateChf",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "PayrollParameterSnapshot_VehicleRegiezone1RateChf",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_AhvIvEoRate", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_AlvRate", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_CapturedAtUtc", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_HolidaySupplementRate", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_IsInitialized", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_NightSupplementRate", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_SicknessAccidentInsuranceRate", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_SundaySupplementRate", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_TrainingAndHolidayRate", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_VacationCompensationRate", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_VacationCompensationRateAge50Plus", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_VehiclePauschalzone1RateChf", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_VehiclePauschalzone2RateChf", table: "EmployeeMonthlyRecords");
        migrationBuilder.DropColumn(name: "PayrollParameterSnapshot_VehicleRegiezone1RateChf", table: "EmployeeMonthlyRecords");
    }
}
