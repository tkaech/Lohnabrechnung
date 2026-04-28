using Microsoft.EntityFrameworkCore.Migrations;
using Payroll.Domain.Settings;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddSalaryCertificatePdfTemplatePath : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SalaryCertificatePdfTemplatePath",
            table: "PayrollSettings",
            type: "TEXT",
            maxLength: 1000,
            nullable: false,
            defaultValue: PayrollSettings.DefaultSalaryCertificatePdfTemplatePath);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "SalaryCertificatePdfTemplatePath",
            table: "PayrollSettings");
    }
}
