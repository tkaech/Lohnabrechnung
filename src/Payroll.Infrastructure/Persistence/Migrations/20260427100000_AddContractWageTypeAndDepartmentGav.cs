using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddContractWageTypeAndDepartmentGav : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "WageType",
            table: "EmploymentContracts",
            type: "TEXT",
            maxLength: 50,
            nullable: false,
            defaultValue: "Hourly");

        migrationBuilder.AddColumn<bool>(
            name: "IsGavMandatory",
            table: "DepartmentOptions",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "EmploymentContractSnapshot_WageType",
            table: "EmployeeMonthlyRecords",
            type: "TEXT",
            maxLength: 50,
            nullable: false,
            defaultValue: "Hourly");

        migrationBuilder.Sql(
            """
            UPDATE EmploymentContracts
            SET WageType = (
                SELECT Employees.WageType
                FROM Employees
                WHERE Employees.Id = EmploymentContracts.EmployeeId
            )
            WHERE EXISTS (
                SELECT 1
                FROM Employees
                WHERE Employees.Id = EmploymentContracts.EmployeeId
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "WageType", table: "EmploymentContracts");
        migrationBuilder.DropColumn(name: "IsGavMandatory", table: "DepartmentOptions");
        migrationBuilder.DropColumn(name: "EmploymentContractSnapshot_WageType", table: "EmployeeMonthlyRecords");
    }
}
