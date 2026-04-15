using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddEmployeeWageType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "WageType",
            table: "Employees",
            type: "TEXT",
            maxLength: 50,
            nullable: false,
            defaultValue: "Hourly");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "WageType",
            table: "Employees");
    }
}
