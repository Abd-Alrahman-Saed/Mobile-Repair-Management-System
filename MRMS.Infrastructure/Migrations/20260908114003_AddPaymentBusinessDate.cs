using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentBusinessDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BusinessDate",
                table: "RepairTicketPayments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessDate",
                table: "RepairTicketPayments");
        }
    }
}
