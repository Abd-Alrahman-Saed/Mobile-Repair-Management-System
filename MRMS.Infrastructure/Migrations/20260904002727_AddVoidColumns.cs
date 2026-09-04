using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVoidColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "SupplierPayments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "SupplierPayments",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "SupplierPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoidedByUserId",
                table: "SupplierPayments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "Sales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Sales",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "Sales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoidedByUserId",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "Purchases",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Purchases",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "Purchases",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoidedByUserId",
                table: "Purchases",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "Expenses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "Expenses",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "Expenses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VoidedByUserId",
                table: "Expenses",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "VoidedByUserId",
                table: "SupplierPayments");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "VoidedByUserId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "VoidedByUserId",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "VoidedByUserId",
                table: "Expenses");
        }
    }
}
