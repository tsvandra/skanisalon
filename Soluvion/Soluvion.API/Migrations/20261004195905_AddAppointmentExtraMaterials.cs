using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentExtraMaterials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtraMaterials",
                table: "Appointments",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExtraMaterials",
                table: "Appointments");
        }
    }
}
