using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddAppLayoutPaddingSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "AppPagePadding",
            table: "PayrollSettings",
            type: "TEXT",
            nullable: false,
            defaultValue: 20m);

        migrationBuilder.AddColumn<decimal>(
            name: "AppPanelPadding",
            table: "PayrollSettings",
            type: "TEXT",
            nullable: false,
            defaultValue: 12m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AppPagePadding",
            table: "PayrollSettings");

        migrationBuilder.DropColumn(
            name: "AppPanelPadding",
            table: "PayrollSettings");
    }
}
