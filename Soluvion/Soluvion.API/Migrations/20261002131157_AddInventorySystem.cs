using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Soluvion.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInventorySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryDocuments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EAN = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsProfessional = table.Column<bool>(type: "boolean", nullable: false),
                    IsRetail = table.Column<bool>(type: "boolean", nullable: false),
                    Unit = table.Column<int>(type: "integer", nullable: false),
                    PackageSize = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RetailPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CurrentStock = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LowStockThreshold = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentUsedProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppointmentItemId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    QuantityUsed = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IsBilledToCustomer = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentUsedProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentUsedProducts_AppointmentItems_AppointmentItemId",
                        column: x => x.AppointmentItemId,
                        principalTable: "AppointmentItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentUsedProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryDocumentItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryDocumentId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryDocumentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryDocumentItems_InventoryDocuments_InventoryDocument~",
                        column: x => x.InventoryDocumentId,
                        principalTable: "InventoryDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryDocumentItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceVariantProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceVariantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    DefaultQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceVariantProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceVariantProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceVariantProducts_ServiceVariants_ServiceVariantId",
                        column: x => x.ServiceVariantId,
                        principalTable: "ServiceVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentUsedProducts_AppointmentItemId",
                table: "AppointmentUsedProducts",
                column: "AppointmentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentUsedProducts_ProductId",
                table: "AppointmentUsedProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocumentItems_InventoryDocumentId",
                table: "InventoryDocumentItems",
                column: "InventoryDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocumentItems_ProductId",
                table: "InventoryDocumentItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryDocuments_CompanyId",
                table: "InventoryDocuments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CompanyId",
                table: "Products",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceVariantProducts_ProductId",
                table: "ServiceVariantProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceVariantProducts_ServiceVariantId",
                table: "ServiceVariantProducts",
                column: "ServiceVariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentUsedProducts");

            migrationBuilder.DropTable(
                name: "InventoryDocumentItems");

            migrationBuilder.DropTable(
                name: "ServiceVariantProducts");

            migrationBuilder.DropTable(
                name: "InventoryDocuments");

            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
