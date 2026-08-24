using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMultiAssetBorrowingWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BorrowRequestDetails_BorrowRecordId",
                table: "BorrowRequestDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "CompensationAmount",
                table: "BorrowRequestDetails",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReturnCondition",
                table: "BorrowRequestDetails",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReturnNote",
                table: "BorrowRequestDetails",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedAt",
                table: "BorrowRequestDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BorrowRequestDetails",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ManagerDecisionNote",
                table: "BorrowRecords",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TeacherDecisionNote",
                table: "BorrowRecords",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BorrowStatusHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BorrowRecordId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BorrowStatusHistories_BorrowRecords_BorrowRecordId",
                        column: x => x.BorrowRecordId,
                        principalTable: "BorrowRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BorrowStatusHistories_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("INSERT INTO `BorrowRequestDetails` (`BorrowRecordId`, `EquipmentId`, `Quantity`, `Note`, `Status`, `ReturnCondition`, `ReturnNote`, `ReturnedAt`, `CompensationAmount`) SELECT r.`Id`, r.`EquipmentId`, 1, r.`Purpose`, r.`Status`, r.`ReturnCondition`, r.`ReturnInspectionNote`, r.`ActualReturnDate`, r.`CompensationAmount` FROM `BorrowRecords` r WHERE NOT EXISTS (SELECT 1 FROM `BorrowRequestDetails` d WHERE d.`BorrowRecordId` = r.`Id`);");
            migrationBuilder.Sql("UPDATE `BorrowRequestDetails` d INNER JOIN `BorrowRecords` r ON r.`Id` = d.`BorrowRecordId` SET d.`Status` = r.`Status` WHERE d.`Status` = '' OR d.`Status` IS NULL;");
            migrationBuilder.Sql("INSERT INTO `BorrowStatusHistories` (`BorrowRecordId`, `FromStatus`, `ToStatus`, `Note`, `ChangedByUserId`, `CreatedAt`) SELECT r.`Id`, NULL, r.`Status`, 'Lịch sử khởi tạo từ phiếu mượn trước khi chuẩn hóa.', NULL, r.`BorrowDate` FROM `BorrowRecords` r WHERE NOT EXISTS (SELECT 1 FROM `BorrowStatusHistories` h WHERE h.`BorrowRecordId` = r.`Id`);");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequestDetails_BorrowRecordId_EquipmentId",
                table: "BorrowRequestDetails",
                columns: new[] { "BorrowRecordId", "EquipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowStatusHistories_BorrowRecordId_CreatedAt",
                table: "BorrowStatusHistories",
                columns: new[] { "BorrowRecordId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowStatusHistories_ChangedByUserId",
                table: "BorrowStatusHistories",
                column: "ChangedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BorrowStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRequestDetails_BorrowRecordId_EquipmentId",
                table: "BorrowRequestDetails");

            migrationBuilder.DropColumn(
                name: "CompensationAmount",
                table: "BorrowRequestDetails");

            migrationBuilder.DropColumn(
                name: "ReturnCondition",
                table: "BorrowRequestDetails");

            migrationBuilder.DropColumn(
                name: "ReturnNote",
                table: "BorrowRequestDetails");

            migrationBuilder.DropColumn(
                name: "ReturnedAt",
                table: "BorrowRequestDetails");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BorrowRequestDetails");

            migrationBuilder.DropColumn(
                name: "ManagerDecisionNote",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "TeacherDecisionNote",
                table: "BorrowRecords");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequestDetails_BorrowRecordId",
                table: "BorrowRequestDetails",
                column: "BorrowRecordId");
        }
    }
}
