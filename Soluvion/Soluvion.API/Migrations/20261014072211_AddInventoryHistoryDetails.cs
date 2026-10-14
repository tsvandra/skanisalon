using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryHistoryDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "InventoryDocuments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "InventoryDocuments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemNote",
                table: "InventoryDocumentItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StockAfter",
                table: "InventoryDocumentItems",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StockBefore",
                table: "InventoryDocumentItems",
                type: "numeric(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "InventoryDocuments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "InventoryDocuments");

            migrationBuilder.DropColumn(
                name: "ItemNote",
                table: "InventoryDocumentItems");

            migrationBuilder.DropColumn(
                name: "StockAfter",
                table: "InventoryDocumentItems");

            migrationBuilder.DropColumn(
                name: "StockBefore",
                table: "InventoryDocumentItems");
        }
    }
}
