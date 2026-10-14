using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceBookableAndPictograms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PictogramLink",
                table: "Services",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "CategoryPictogram",
                table: "Services",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsBookable",
                table: "Services",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryPictogram",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "IsBookable",
                table: "Services");

            migrationBuilder.AlterColumn<string>(
                name: "PictogramLink",
                table: "Services",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(4000)",
                oldMaxLength: 4000);
        }
    }
}

