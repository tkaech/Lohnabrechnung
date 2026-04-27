using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddEmploymentContractMonthlySalaryAmount : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "MonthlySalaryAmountChf",
            table: "EmploymentContracts",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "EmploymentContractSnapshot_MonthlySalaryAmountChf",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MonthlySalaryAmountChf",
            table: "EmploymentContracts");

        migrationBuilder.DropColumn(
            name: "EmploymentContractSnapshot_MonthlySalaryAmountChf",
            table: "EmployeeMonthlyRecords");
    }
}
