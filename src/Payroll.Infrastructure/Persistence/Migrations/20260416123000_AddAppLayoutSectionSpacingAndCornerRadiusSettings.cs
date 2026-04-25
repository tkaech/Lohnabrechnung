using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddAppLayoutSectionSpacingAndCornerRadiusSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "AppSectionSpacing",
            table: "PayrollSettings",
            type: "TEXT",
            nullable: false,
            defaultValue: 12m);

        migrationBuilder.AddColumn<decimal>(
            name: "AppPanelCornerRadius",
            table: "PayrollSettings",
            type: "TEXT",
            nullable: false,
            defaultValue: 8m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AppSectionSpacing",
            table: "PayrollSettings");

        migrationBuilder.DropColumn(
            name: "AppPanelCornerRadius",
            table: "PayrollSettings");
    }
}
