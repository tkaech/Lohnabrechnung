using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payroll.Infrastructure.Persistence.Migrations;

public partial class AddSalaryAdvances : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SalaryAdvances",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeMonthlyRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                Year = table.Column<int>(type: "INTEGER", nullable: false),
                Month = table.Column<int>(type: "INTEGER", nullable: false),
                AmountChf = table.Column<decimal>(type: "TEXT", nullable: false),
                Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SalaryAdvances", x => x.Id);
                table.ForeignKey(
                    name: "FK_SalaryAdvances_EmployeeMonthlyRecords_EmployeeMonthlyRecordId",
                    column: x => x.EmployeeMonthlyRecordId,
                    principalTable: "EmployeeMonthlyRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SalaryAdvances_Employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SalaryAdvanceSettlements",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeMonthlyRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                SalaryAdvanceId = table.Column<Guid>(type: "TEXT", nullable: false),
                EmployeeId = table.Column<Guid>(type: "TEXT", nullable: false),
                Year = table.Column<int>(type: "INTEGER", nullable: false),
                Month = table.Column<int>(type: "INTEGER", nullable: false),
                AmountChf = table.Column<decimal>(type: "TEXT", nullable: false),
                Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SalaryAdvanceSettlements", x => x.Id);
                table.ForeignKey(
                    name: "FK_SalaryAdvanceSettlements_EmployeeMonthlyRecords_EmployeeMonthlyRecordId",
                    column: x => x.EmployeeMonthlyRecordId,
                    principalTable: "EmployeeMonthlyRecords",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SalaryAdvanceSettlements_Employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SalaryAdvanceSettlements_SalaryAdvances_SalaryAdvanceId",
                    column: x => x.SalaryAdvanceId,
                    principalTable: "SalaryAdvances",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SalaryAdvances_EmployeeId_Year_Month",
            table: "SalaryAdvances",
            columns: new[] { "EmployeeId", "Year", "Month" });

        migrationBuilder.CreateIndex(
            name: "IX_SalaryAdvances_EmployeeMonthlyRecordId",
            table: "SalaryAdvances",
            column: "EmployeeMonthlyRecordId");

        migrationBuilder.CreateIndex(
            name: "IX_SalaryAdvanceSettlements_EmployeeId_Year_Month",
            table: "SalaryAdvanceSettlements",
            columns: new[] { "EmployeeId", "Year", "Month" });

        migrationBuilder.CreateIndex(
            name: "IX_SalaryAdvanceSettlements_EmployeeMonthlyRecordId",
            table: "SalaryAdvanceSettlements",
            column: "EmployeeMonthlyRecordId");

        migrationBuilder.CreateIndex(
            name: "IX_SalaryAdvanceSettlements_SalaryAdvanceId",
            table: "SalaryAdvanceSettlements",
            column: "SalaryAdvanceId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SalaryAdvanceSettlements");

        migrationBuilder.DropTable(
            name: "SalaryAdvances");
    }
}
