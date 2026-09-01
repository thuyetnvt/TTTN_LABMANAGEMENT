using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class StrengthenInventoryReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActualLocationNodeId",
                table: "InventoryItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewNote",
                table: "InventoryItems",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReviewResolution",
                table: "InventoryItems",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "InventoryItems",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByUserId",
                table: "InventoryItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ActualLocationNodeId",
                table: "InventoryItems",
                column: "ActualLocationNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_ReviewedByUserId",
                table: "InventoryItems",
                column: "ReviewedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_LocationNodes_ActualLocationNodeId",
                table: "InventoryItems",
                column: "ActualLocationNodeId",
                principalTable: "LocationNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Users_ReviewedByUserId",
                table: "InventoryItems",
                column: "ReviewedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_LocationNodes_ActualLocationNodeId",
                table: "InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Users_ReviewedByUserId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_ActualLocationNodeId",
                table: "InventoryItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_ReviewedByUserId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ActualLocationNodeId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ReviewNote",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ReviewResolution",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "InventoryItems");
        }
    }
}
