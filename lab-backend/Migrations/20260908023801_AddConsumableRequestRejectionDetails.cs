using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumableRequestRejectionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "ConsumableRequests",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedByUserId",
                table: "ConsumableRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ConsumableRequests",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RejectionStage",
                table: "ConsumableRequests",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableRequests_RejectedByUserId",
                table: "ConsumableRequests",
                column: "RejectedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumableRequests_Users_RejectedByUserId",
                table: "ConsumableRequests",
                column: "RejectedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumableRequests_Users_RejectedByUserId",
                table: "ConsumableRequests");

            migrationBuilder.DropIndex(
                name: "IX_ConsumableRequests_RejectedByUserId",
                table: "ConsumableRequests");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "ConsumableRequests");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "ConsumableRequests");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ConsumableRequests");

            migrationBuilder.DropColumn(
                name: "RejectionStage",
                table: "ConsumableRequests");
        }
    }
}
