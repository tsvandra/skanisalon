using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class MakeGalleryTitleMultilingual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Régi Title oszlop törlése
            migrationBuilder.DropColumn(
                name: "Title",
                table: "GalleryImages");

            // 2. Új Title oszlop (JSONB)
            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "Title",
                table: "GalleryImages",
                type: "jsonb",
                nullable: false,
                defaultValue: new Dictionary<string, string>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "GalleryImages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "jsonb");
        }
    }
}
