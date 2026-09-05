using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CashSessionId",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CashSessionId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CashSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpenedByUserId = table.Column<int>(type: "int", nullable: false),
                    OpenedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true),
                    ClosedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CountedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Difference = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RepairTicketPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepairTicketId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CollectedByUserId = table.Column<int>(type: "int", nullable: false),
                    CollectedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CashSessionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepairTicketPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepairTicketPayments_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RepairTicketPayments_RepairTickets_RepairTicketId",
                        column: x => x.RepairTicketId,
                        principalTable: "RepairTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CashSessionId",
                table: "Sales",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CashSessionId",
                table: "Expenses",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_BusinessDate",
                table: "CashSessions",
                column: "BusinessDate");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTicketPayments_CashSessionId",
                table: "RepairTicketPayments",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairTicketPayments_RepairTicketId",
                table: "RepairTicketPayments",
                column: "RepairTicketId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_CashSessions_CashSessionId",
                table: "Expenses",
                column: "CashSessionId",
                principalTable: "CashSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_CashSessions_CashSessionId",
                table: "Sales",
                column: "CashSessionId",
                principalTable: "CashSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_CashSessions_CashSessionId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_CashSessions_CashSessionId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "RepairTicketPayments");

            migrationBuilder.DropTable(
                name: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_Sales_CashSessionId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_CashSessionId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "CashSessionId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "CashSessionId",
                table: "Expenses");
        }
    }
}
