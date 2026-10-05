using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "InventoryDocuments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MaterialUsageRecorded",
                table: "Appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocuments_AppointmentId",
                table: "InventoryDocuments",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryDocuments_Appointments_AppointmentId",
                table: "InventoryDocuments",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryDocuments_Appointments_AppointmentId",
                table: "InventoryDocuments");

            migrationBuilder.DropIndex(
                name: "IX_InventoryDocuments_AppointmentId",
                table: "InventoryDocuments");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "InventoryDocuments");

            migrationBuilder.DropColumn(
                name: "MaterialUsageRecorded",
                table: "Appointments");
        }
    }
}
