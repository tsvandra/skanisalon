using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class FixGalleryImageModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GalleryImages_GalleryCategories_CategoryId",
                table: "GalleryImages");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "GalleryImages",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GalleryImages_GalleryCategories_CategoryId",
                table: "GalleryImages",
                column: "CategoryId",
                principalTable: "GalleryCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GalleryImages_GalleryCategories_CategoryId",
                table: "GalleryImages");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "GalleryImages",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_GalleryImages_GalleryCategories_CategoryId",
                table: "GalleryImages",
                column: "CategoryId",
                principalTable: "GalleryCategories",
                principalColumn: "Id");
        }
    }
}
