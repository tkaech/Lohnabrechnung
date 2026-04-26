using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddPayrollRuns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PayrollRuns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PeriodKey = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                PaymentDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PayrollRuns", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PayrollRunLines",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                PayrollRunId = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                LineType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                ValueOrigin = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Unit = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Quantity = table.Column<decimal>(type: "TEXT", nullable: true),
                RateChf = table.Column<decimal>(type: "TEXT", nullable: true),
                AmountChf = table.Column<decimal>(type: "TEXT", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PayrollRunLines", x => x.Id);
                table.ForeignKey(
                    name: "FK_PayrollRunLines_Employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_PayrollRunLines_PayrollRuns_PayrollRunId",
                    column: x => x.PayrollRunId,
                    principalTable: "PayrollRuns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PayrollRunLines_EmployeeId",
            table: "PayrollRunLines",
            column: "EmployeeId");

        migrationBuilder.CreateIndex(
            name: "IX_PayrollRunLines_PayrollRunId",
            table: "PayrollRunLines",
            column: "PayrollRunId");

        migrationBuilder.CreateIndex(
            name: "IX_PayrollRuns_PeriodKey",
            table: "PayrollRuns",
            column: "PeriodKey");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PayrollRunLines");

        migrationBuilder.DropTable(
            name: "PayrollRuns");
    }
}
