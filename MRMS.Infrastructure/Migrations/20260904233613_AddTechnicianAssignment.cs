using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MRMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicianAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedTechnicianId",
                table: "RepairTickets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RepairTickets_AssignedTechnicianId",
                table: "RepairTickets",
                column: "AssignedTechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_RepairTickets_Users_AssignedTechnicianId",
                table: "RepairTickets",
                column: "AssignedTechnicianId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RepairTickets_Users_AssignedTechnicianId",
                table: "RepairTickets");

            migrationBuilder.DropIndex(
                name: "IX_RepairTickets_AssignedTechnicianId",
                table: "RepairTickets");

            migrationBuilder.DropColumn(
                name: "AssignedTechnicianId",
                table: "RepairTickets");
        }
    }
}
