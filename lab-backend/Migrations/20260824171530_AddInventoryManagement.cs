using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventorySessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LocationNodeId = table.Column<int>(type: "int", nullable: true),
                    AssetCategoryId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventorySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventorySessions_AssetCategories_AssetCategoryId",
                        column: x => x.AssetCategoryId,
                        principalTable: "AssetCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventorySessions_LocationNodes_LocationNodeId",
                        column: x => x.LocationNodeId,
                        principalTable: "LocationNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InventorySessions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    InventorySessionId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    ExpectedLocationNodeId = table.Column<int>(type: "int", nullable: true),
                    ExpectedLocationName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScannedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ScannedByUserId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryItems_InventorySessions_InventorySessionId",
                        column: x => x.InventorySessionId,
                        principalTable: "InventorySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Users_ScannedByUserId",
                        column: x => x.ScannedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_EquipmentId",
                table: "InventoryItems",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_InventorySessionId_EquipmentId",
                table: "InventoryItems",
                columns: new[] { "InventorySessionId", "EquipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_InventorySessionId_Status",
                table: "InventoryItems",
                columns: new[] { "InventorySessionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ScannedByUserId",
                table: "InventoryItems",
                column: "ScannedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySessions_AssetCategoryId",
                table: "InventorySessions",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySessions_Code",
                table: "InventorySessions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventorySessions_CreatedByUserId",
                table: "InventorySessions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySessions_LocationNodeId",
                table: "InventorySessions",
                column: "LocationNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventorySessions_Status",
                table: "InventorySessions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "InventorySessions");
        }
    }
}
