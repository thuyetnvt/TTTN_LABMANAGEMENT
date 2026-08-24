using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentLocationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveEquipmentKey",
                table: "MaintenanceRecords",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EquipmentLocationHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    FromLocationNodeId = table.Column<int>(type: "int", nullable: true),
                    ToLocationNodeId = table.Column<int>(type: "int", nullable: true),
                    FromLocationName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToLocationName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentLocationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentLocationHistories_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipmentLocationHistories_LocationNodes_FromLocationNodeId",
                        column: x => x.FromLocationNodeId,
                        principalTable: "LocationNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EquipmentLocationHistories_LocationNodes_ToLocationNodeId",
                        column: x => x.ToLocationNodeId,
                        principalTable: "LocationNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EquipmentLocationHistories_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_ActiveEquipmentKey",
                table: "MaintenanceRecords",
                column: "ActiveEquipmentKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentLocationHistories_ChangedByUserId",
                table: "EquipmentLocationHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentLocationHistories_EquipmentId_ChangedAt",
                table: "EquipmentLocationHistories",
                columns: new[] { "EquipmentId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentLocationHistories_FromLocationNodeId",
                table: "EquipmentLocationHistories",
                column: "FromLocationNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentLocationHistories_ToLocationNodeId",
                table: "EquipmentLocationHistories",
                column: "ToLocationNodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentLocationHistories");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRecords_ActiveEquipmentKey",
                table: "MaintenanceRecords");

            migrationBuilder.DropColumn(
                name: "ActiveEquipmentKey",
                table: "MaintenanceRecords");
        }
    }
}
