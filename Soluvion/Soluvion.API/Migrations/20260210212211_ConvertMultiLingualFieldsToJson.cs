using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class ConvertMultiLingualFieldsToJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Service.Name konvertálása (String -> JSONB)
            // Itt nem kell CASE, mert a Name kötelező volt, de a biztonság kedvéért castoljuk
            migrationBuilder.Sql(
                @"ALTER TABLE ""Services"" 
                  ALTER COLUMN ""Name"" TYPE jsonb 
                  USING json_build_object('hu', ""Name"")::jsonb;");

            // 2. Service.Category konvertálása
            // JAVÍTÁS: A json_build_object végére odatettük a ::jsonb kényszerítést
            migrationBuilder.Sql(
                @"ALTER TABLE ""Services"" 
                  ALTER COLUMN ""Category"" TYPE jsonb 
                  USING CASE 
                    WHEN ""Category"" IS NULL OR ""Category"" = '' THEN '{}'::jsonb 
                    ELSE json_build_object('hu', ""Category"")::jsonb
                  END;");

            // 3. Service.Description konvertálása
            // JAVÍTÁS: Itt is ::jsonb castolás
            migrationBuilder.Sql(
                @"ALTER TABLE ""Services"" 
                  ALTER COLUMN ""Description"" TYPE jsonb 
                  USING CASE 
                    WHEN ""Description"" IS NULL OR ""Description"" = '' THEN '{}'::jsonb 
                    ELSE json_build_object('hu', ""Description"")::jsonb
                  END;");

            // 4. GalleryCategory.Name konvertálása
            migrationBuilder.Sql(
                @"ALTER TABLE ""GalleryCategories"" 
                  ALTER COLUMN ""Name"" TYPE jsonb 
                  USING json_build_object('hu', ""Name"")::jsonb;");

            // --- Default értékek beállítása (ez a rész változatlan) ---

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Services",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Services",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Services",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "GalleryCategories",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Visszaállítás: A 'hu' kulcs értékét vesszük ki a JSON-ből

            migrationBuilder.Sql(
                @"ALTER TABLE ""GalleryCategories"" 
                  ALTER COLUMN ""Name"" TYPE character varying(50) 
                  USING ""Name""->>'hu';");

            migrationBuilder.Sql(
                @"ALTER TABLE ""Services"" 
                  ALTER COLUMN ""Description"" TYPE character varying(500) 
                  USING ""Description""->>'hu';");

            migrationBuilder.Sql(
                @"ALTER TABLE ""Services"" 
                  ALTER COLUMN ""Category"" TYPE character varying(50) 
                  USING ""Category""->>'hu';");

            migrationBuilder.Sql(
                @"ALTER TABLE ""Services"" 
                  ALTER COLUMN ""Name"" TYPE character varying(50) 
                  USING ""Name""->>'hu';");
        }
    }
}