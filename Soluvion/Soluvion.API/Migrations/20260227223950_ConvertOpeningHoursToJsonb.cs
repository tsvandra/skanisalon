using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class ConvertOpeningHoursToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Töröltük az EF Core által generált hibás AlterColumn sorokat!
            // 2. Helyette betesszük a nyers SQL-t, ami okosan konvertálja a meglévő szövegeket JSONB-vé:

            migrationBuilder.Sql(@"
        ALTER TABLE ""Companies"" ALTER COLUMN ""OpeningHoursTitle"" TYPE jsonb USING jsonb_build_object('hu', COALESCE(""OpeningHoursTitle"", ''));
        ALTER TABLE ""Companies"" ALTER COLUMN ""OpeningHoursDescription"" TYPE jsonb USING jsonb_build_object('hu', COALESCE(""OpeningHoursDescription"", ''));
        ALTER TABLE ""Companies"" ALTER COLUMN ""OpeningTimeSlots"" TYPE jsonb USING jsonb_build_object('hu', COALESCE(""OpeningTimeSlots"", ''));
        ALTER TABLE ""Companies"" ALTER COLUMN ""OpeningExtraInfo"" TYPE jsonb USING jsonb_build_object('hu', COALESCE(""OpeningExtraInfo"", ''));
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OpeningTimeSlots",
                table: "Companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "OpeningHoursTitle",
                table: "Companies",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "OpeningHoursDescription",
                table: "Companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "OpeningExtraInfo",
                table: "Companies",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(Dictionary<string, string>),
                oldType: "jsonb");
        }
    }
}
