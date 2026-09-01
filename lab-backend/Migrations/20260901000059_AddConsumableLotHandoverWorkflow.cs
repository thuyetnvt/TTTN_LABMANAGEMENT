using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumableLotHandoverWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReservedQuantity",
                table: "Consumables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "HandedOverAt",
                table: "ConsumableRequests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HandedOverByUserId",
                table: "ConsumableRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceivedAt",
                table: "ConsumableRequests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReceivedByUserId",
                table: "ConsumableRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsumableLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConsumableId = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InitialQuantity = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Supplier = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InvoiceNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StorageLocation = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumableLots", x => x.Id);
                    table.CheckConstraint("CK_ConsumableLots_InitialQuantity", "InitialQuantity >= 0");
                    table.CheckConstraint("CK_ConsumableLots_Quantity", "Quantity >= 0");
                    table.ForeignKey(
                        name: "FK_ConsumableLots_Consumables_ConsumableId",
                        column: x => x.ConsumableId,
                        principalTable: "Consumables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConsumableRequestLotAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ConsumableRequestId = table.Column<int>(type: "int", nullable: false),
                    ConsumableLotId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumableRequestLotAllocations", x => x.Id);
                    table.CheckConstraint("CK_ConsumableRequestLotAllocations_Quantity", "Quantity > 0");
                    table.ForeignKey(
                        name: "FK_ConsumableRequestLotAllocations_ConsumableLots_ConsumableLot~",
                        column: x => x.ConsumableLotId,
                        principalTable: "ConsumableLots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsumableRequestLotAllocations_ConsumableRequests_Consumabl~",
                        column: x => x.ConsumableRequestId,
                        principalTable: "ConsumableRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Existing databases only stored one aggregate stock row. Create a
            // traceable legacy lot so that this stock can be handed over using
            // the new lot-based workflow immediately after the migration.
            migrationBuilder.Sql(
                """
                INSERT INTO ConsumableLots
                    (ConsumableId, LotNumber, InitialQuantity, Quantity, EntryDate,
                     ExpiryDate, Supplier, InvoiceNumber, UnitCost, StorageLocation, CreatedAt)
                SELECT
                    Id,
                    CONCAT('LEGACY-', Id),
                    Quantity,
                    Quantity,
                    COALESCE(EntryDate, CreatedAt),
                    ExpiryDate,
                    COALESCE(Supplier, ''),
                    COALESCE(InvoiceNumber, ''),
                    UnitCost,
                    COALESCE(StorageLocation, ''),
                    CreatedAt
                FROM Consumables
                WHERE Quantity > 0;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Consumables_ReservedQuantity",
                table: "Consumables",
                sql: "ReservedQuantity >= 0 AND ReservedQuantity <= Quantity");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableRequests_HandedOverByUserId",
                table: "ConsumableRequests",
                column: "HandedOverByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableRequests_ReceivedByUserId",
                table: "ConsumableRequests",
                column: "ReceivedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableLots_ConsumableId_ExpiryDate",
                table: "ConsumableLots",
                columns: new[] { "ConsumableId", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableLots_ConsumableId_LotNumber",
                table: "ConsumableLots",
                columns: new[] { "ConsumableId", "LotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableRequestLotAllocations_ConsumableLotId",
                table: "ConsumableRequestLotAllocations",
                column: "ConsumableLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableRequestLotAllocations_ConsumableRequestId_Consumab~",
                table: "ConsumableRequestLotAllocations",
                columns: new[] { "ConsumableRequestId", "ConsumableLotId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumableRequests_Users_HandedOverByUserId",
                table: "ConsumableRequests",
                column: "HandedOverByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumableRequests_Users_ReceivedByUserId",
                table: "ConsumableRequests",
                column: "ReceivedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumableRequests_Users_HandedOverByUserId",
                table: "ConsumableRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsumableRequests_Users_ReceivedByUserId",
                table: "ConsumableRequests");

            migrationBuilder.DropTable(
                name: "ConsumableRequestLotAllocations");

            migrationBuilder.DropTable(
                name: "ConsumableLots");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Consumables_ReservedQuantity",
                table: "Consumables");

            migrationBuilder.DropIndex(
                name: "IX_ConsumableRequests_HandedOverByUserId",
                table: "ConsumableRequests");

            migrationBuilder.DropIndex(
                name: "IX_ConsumableRequests_ReceivedByUserId",
                table: "ConsumableRequests");

            migrationBuilder.DropColumn(
                name: "ReservedQuantity",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "HandedOverAt",
                table: "ConsumableRequests");

            migrationBuilder.DropColumn(
                name: "HandedOverByUserId",
                table: "ConsumableRequests");

            migrationBuilder.DropColumn(
                name: "ReceivedAt",
                table: "ConsumableRequests");

            migrationBuilder.DropColumn(
                name: "ReceivedByUserId",
                table: "ConsumableRequests");
        }
    }
}
