using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddHandoverRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HandoverRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BorrowRecordId = table.Column<int>(type: "int", nullable: false),
                    HandedOverByUserId = table.Column<int>(type: "int", nullable: false),
                    ReceivedByUserId = table.Column<int>(type: "int", nullable: false),
                    HandoverAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Notes = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverRecords_BorrowRecords_BorrowRecordId",
                        column: x => x.BorrowRecordId,
                        principalTable: "BorrowRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoverRecords_Users_HandedOverByUserId",
                        column: x => x.HandedOverByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoverRecords_Users_ReceivedByUserId",
                        column: x => x.ReceivedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "HandoverItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    HandoverRecordId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Condition = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Accessories = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverItems_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoverItems_HandoverRecords_HandoverRecordId",
                        column: x => x.HandoverRecordId,
                        principalTable: "HandoverRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverItems_EquipmentId",
                table: "HandoverItems",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverItems_HandoverRecordId_EquipmentId",
                table: "HandoverItems",
                columns: new[] { "HandoverRecordId", "EquipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandoverRecords_BorrowRecordId",
                table: "HandoverRecords",
                column: "BorrowRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandoverRecords_Code",
                table: "HandoverRecords",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandoverRecords_HandedOverByUserId",
                table: "HandoverRecords",
                column: "HandedOverByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverRecords_ReceivedByUserId",
                table: "HandoverRecords",
                column: "ReceivedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HandoverItems");

            migrationBuilder.DropTable(
                name: "HandoverRecords");
        }
    }
}
