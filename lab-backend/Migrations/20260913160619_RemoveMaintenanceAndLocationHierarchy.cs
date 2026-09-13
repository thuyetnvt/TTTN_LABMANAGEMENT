using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMaintenanceAndLocationHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumableTransactions_MaintenanceRecords_MaintenanceRecordId",
                table: "ConsumableTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_LocationNodes_LocationNodes_ParentId",
                table: "LocationNodes");

            // Preserve asset meaning before removing the retired workflow.
            migrationBuilder.Sql(
                "UPDATE `Equipments` SET `Status` = 'BROKEN' " +
                "WHERE `Status` IN ('MAINTENANCE_IN_PROGRESS', 'MAINTENANCE_COMPLETING', " +
                "'MAINTENANCE_COMPLETED', 'Bảo trì', 'Đang bảo trì', 'Hoàn tất bảo trì');");

            // Retired workflow messages must not remain visible after deployment.
            migrationBuilder.Sql(
                "DELETE FROM `Notifications` " +
                "WHERE `Type` LIKE 'MAINTENANCE%' OR `Url` LIKE '%maintenance%';");
            migrationBuilder.Sql(
                "DELETE FROM `AuditLogs` " +
                "WHERE `EntityType` IN ('MaintenanceRecord', 'MaintenanceSchedule') " +
                "OR `Action` LIKE 'MAINTENANCE%';");
            migrationBuilder.Sql(
                "DELETE FROM `AutomationDispatches` " +
                "WHERE `JobType` LIKE 'MAINTENANCE%' " +
                "OR `EntityType` IN ('MaintenanceRecord', 'MaintenanceSchedule');");

            // LAB-ROOT only represented the removed parent level. All real rooms stay intact.
            migrationBuilder.Sql(
                "UPDATE `InventoryItems` item " +
                "JOIN `LocationNodes` root ON item.`ExpectedLocationNodeId` = root.`Id` " +
                "SET item.`ExpectedLocationNodeId` = NULL WHERE root.`Code` = 'LAB-ROOT';");
            migrationBuilder.Sql("DELETE FROM `LocationNodes` WHERE `Code` = 'LAB-ROOT';");

            migrationBuilder.DropTable(
                name: "MaintenanceEvidence");

            migrationBuilder.DropTable(
                name: "MaintenancePartUsages");

            migrationBuilder.DropTable(
                name: "MaintenanceSchedules");

            migrationBuilder.DropTable(
                name: "MaintenanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_LocationNodes_ParentId",
                table: "LocationNodes");

            migrationBuilder.DropIndex(
                name: "IX_ConsumableTransactions_MaintenanceRecordId",
                table: "ConsumableTransactions");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "LocationNodes");

            migrationBuilder.DropColumn(
                name: "MaintenanceRecordId",
                table: "ConsumableTransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "LocationNodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaintenanceRecordId",
                table: "ConsumableTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    ActiveEquipmentKey = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Checklist = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChecklistResult = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaintenanceDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PerformedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Result = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Supplier = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MaintenanceSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Checklist = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    IntervalUnit = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastGeneratedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NextDueAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MaintenanceEvidence",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaintenanceRecordId = table.Column<int>(type: "int", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EvidenceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    OriginalFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StoredPath = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UploadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceEvidence_MaintenanceRecords_MaintenanceRecordId",
                        column: x => x.MaintenanceRecordId,
                        principalTable: "MaintenanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceEvidence_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MaintenancePartUsages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConsumableId = table.Column<int>(type: "int", nullable: false),
                    MaintenanceRecordId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Note = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenancePartUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenancePartUsages_Consumables_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "Consumables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenancePartUsages_MaintenanceRecords_MaintenanceRecordId",
                        column: x => x.MaintenanceRecordId,
                        principalTable: "MaintenanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LocationNodes_ParentId",
                table: "LocationNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableTransactions_MaintenanceRecordId",
                table: "ConsumableTransactions",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceEvidence_MaintenanceRecordId",
                table: "MaintenanceEvidence",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceEvidence_UploadedByUserId",
                table: "MaintenanceEvidence",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePartUsages_ConsumableId",
                table: "MaintenancePartUsages",
                column: "ConsumableId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenancePartUsages_MaintenanceRecordId",
                table: "MaintenancePartUsages",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_ActiveEquipmentKey",
                table: "MaintenanceRecords",
                column: "ActiveEquipmentKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_EquipmentId",
                table: "MaintenanceRecords",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_MaintenanceDate",
                table: "MaintenanceRecords",
                column: "MaintenanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_Status",
                table: "MaintenanceRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_CreatedByUserId",
                table: "MaintenanceSchedules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_EquipmentId",
                table: "MaintenanceSchedules",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_IsActive_NextDueAt",
                table: "MaintenanceSchedules",
                columns: new[] { "IsActive", "NextDueAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumableTransactions_MaintenanceRecords_MaintenanceRecordId",
                table: "ConsumableTransactions",
                column: "MaintenanceRecordId",
                principalTable: "MaintenanceRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LocationNodes_LocationNodes_ParentId",
                table: "LocationNodes",
                column: "ParentId",
                principalTable: "LocationNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
