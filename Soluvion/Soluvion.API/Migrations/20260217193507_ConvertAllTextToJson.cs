using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class ConvertAllTextToJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --------------------------------------------------------
            // 1. RÉSZ: CompanyType tábla átnevezése (Az eredeti kódod)
            // --------------------------------------------------------
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_CompanyType_CompanyTypeId",
                table: "Companies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CompanyType",
                table: "CompanyType");

            migrationBuilder.RenameTable(
                name: "CompanyType",
                newName: "CompanyTypes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CompanyTypes",
                table: "CompanyTypes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_CompanyTypes_CompanyTypeId",
                table: "Companies",
                column: "CompanyTypeId",
                principalTable: "CompanyTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);


            // --------------------------------------------------------
            // 2. RÉSZ: Hiányzó tábla létrehozása (UiTranslationOverrides)
            // --------------------------------------------------------
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"UiTranslationOverrides\" CASCADE;");

            migrationBuilder.CreateTable(
                name: "UiTranslationOverrides",
                columns: table => new
                {
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    TranslationKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TranslatedText = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UiTranslationOverrides", x => new { x.CompanyId, x.LanguageCode, x.TranslationKey });
                    table.ForeignKey(
                        name: "FK_UiTranslationOverrides_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });


            

            // --------------------------------------------------------
            // 4. RÉSZ: JSONB Konverzió (Drop + Add)
            // --------------------------------------------------------

            // --- SERVICES ---
            migrationBuilder.DropColumn(name: "Name", table: "Services");
            migrationBuilder.AddColumn<Dictionary<string, string>>(name: "Name", table: "Services", type: "jsonb", nullable: false, defaultValueSql: "'{}'");

            migrationBuilder.DropColumn(name: "Category", table: "Services");
            migrationBuilder.AddColumn<Dictionary<string, string>>(name: "Category", table: "Services", type: "jsonb", nullable: false, defaultValueSql: "'{}'");

            migrationBuilder.DropColumn(name: "Description", table: "Services");
            migrationBuilder.AddColumn<Dictionary<string, string>>(name: "Description", table: "Services", type: "jsonb", nullable: false, defaultValueSql: "'{}'");

            // --- GALLERY IMAGES ---
            migrationBuilder.DropColumn(name: "Title", table: "GalleryImages");
            migrationBuilder.AddColumn<Dictionary<string, string>>(name: "Title", table: "GalleryImages", type: "jsonb", nullable: false, defaultValueSql: "'{}'");

            // --- GALLERY CATEGORIES ---
            migrationBuilder.DropColumn(name: "Name", table: "GalleryCategories");
            migrationBuilder.AddColumn<Dictionary<string, string>>(name: "Name", table: "GalleryCategories", type: "jsonb", nullable: false, defaultValueSql: "'{}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A Down metódus most nem kritikus fejlesztés alatt, 
            // de a teljesség kedvéért visszaállítaná az eredeti állapotot.
            // Mivel adatvesztéses (DropColumn) műveletet csináltunk, a visszaállítás nehézkes.
            // Fejlesztésnél hagyhatod így üresen vagy minimális logikával.

            migrationBuilder.DropTable(name: "UiTranslationOverrides");

            migrationBuilder.RenameTable(
                name: "CompanyTypes",
                newName: "CompanyType");
        }
    }
}