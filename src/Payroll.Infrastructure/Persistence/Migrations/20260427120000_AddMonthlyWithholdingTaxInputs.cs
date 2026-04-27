using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddMonthlyWithholdingTaxInputs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "WithholdingTaxRatePercent",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "WithholdingTaxCorrectionAmountChf",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<string>(
            name: "WithholdingTaxCorrectionText",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "WithholdingTaxRatePercent",
            table: "EmployeeMonthlyRecords");

        migrationBuilder.DropColumn(
            name: "WithholdingTaxCorrectionAmountChf",
            table: "EmployeeMonthlyRecords");

        migrationBuilder.DropColumn(
            name: "WithholdingTaxCorrectionText",
            table: "EmployeeMonthlyRecords");
    }
}
