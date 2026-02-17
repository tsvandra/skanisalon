using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class MakeVariantNameMultilingual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VariantName",
                table: "ServiceVariants");

            // 2. Új (JSONB) oszlop létrehozása
            migrationBuilder.AddColumn<Dictionary<string, string>>(
                name: "VariantName",
                table: "ServiceVariants",
                type: "jsonb",
                nullable: false,
                defaultValue: new Dictionary<string, string>());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "VariantName",
                table: "ServiceVariants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "jsonb");
        }
    }
}
