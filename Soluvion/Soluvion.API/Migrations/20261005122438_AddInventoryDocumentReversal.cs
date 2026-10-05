using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryDocumentReversal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReversed",
                table: "InventoryDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReversalOfDocumentId",
                table: "InventoryDocuments",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReversed",
                table: "InventoryDocuments");

            migrationBuilder.DropColumn(
                name: "ReversalOfDocumentId",
                table: "InventoryDocuments");
        }
    }
}
