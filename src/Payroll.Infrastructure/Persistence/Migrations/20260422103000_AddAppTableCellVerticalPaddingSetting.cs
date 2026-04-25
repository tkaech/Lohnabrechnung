using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddAppTableCellVerticalPaddingSetting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "AppTableCellVerticalPadding",
            table: "PayrollSettings",
            type: "TEXT",
            nullable: false,
            defaultValue: 6m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AppTableCellVerticalPadding",
            table: "PayrollSettings");
    }
}
