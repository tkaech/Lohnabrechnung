using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddVacationCompensationRateAge50Plus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "VacationCompensationRateAge50Plus",
            table: "PayrollSettings",
            type: "TEXT",
            nullable: false,
            defaultValue: 0.1064m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "VacationCompensationRateAge50Plus",
            table: "PayrollSettings");
    }
}
