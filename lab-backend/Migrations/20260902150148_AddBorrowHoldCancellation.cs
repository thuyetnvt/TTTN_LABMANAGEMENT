using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowHoldCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "BorrowRecords",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "BorrowRecords",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancelledByUserId",
                table: "BorrowRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "HoldExpiresAt",
                table: "BorrowRecords",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_CancelledByUserId",
                table: "BorrowRecords",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_Status_HoldExpiresAt",
                table: "BorrowRecords",
                columns: new[] { "Status", "HoldExpiresAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowRecords_Users_CancelledByUserId",
                table: "BorrowRecords",
                column: "CancelledByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BorrowRecords_Users_CancelledByUserId",
                table: "BorrowRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_CancelledByUserId",
                table: "BorrowRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_Status_HoldExpiresAt",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "HoldExpiresAt",
                table: "BorrowRecords");
        }
    }
}
