using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumableTraceabilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsumableRequestId",
                table: "ConsumableTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaintenanceRecordId",
                table: "ConsumableTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Consumables",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Consumables",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "Consumables",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "StorageLocation",
                table: "Consumables",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "Consumables",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "Consumables",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("UPDATE `Consumables` SET `Code` = CONCAT('VT-', LPAD(`Id`, 6, '0')) WHERE `Code` = '' OR `Code` IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableTransactions_ConsumableRequestId",
                table: "ConsumableTransactions",
                column: "ConsumableRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumableTransactions_MaintenanceRecordId",
                table: "ConsumableTransactions",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_Code",
                table: "Consumables",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumableTransactions_ConsumableRequests_ConsumableRequestId",
                table: "ConsumableTransactions",
                column: "ConsumableRequestId",
                principalTable: "ConsumableRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ConsumableTransactions_MaintenanceRecords_MaintenanceRecordId",
                table: "ConsumableTransactions",
                column: "MaintenanceRecordId",
                principalTable: "MaintenanceRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsumableTransactions_ConsumableRequests_ConsumableRequestId",
                table: "ConsumableTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_ConsumableTransactions_MaintenanceRecords_MaintenanceRecordId",
                table: "ConsumableTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ConsumableTransactions_ConsumableRequestId",
                table: "ConsumableTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ConsumableTransactions_MaintenanceRecordId",
                table: "ConsumableTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Consumables_Code",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "ConsumableRequestId",
                table: "ConsumableTransactions");

            migrationBuilder.DropColumn(
                name: "MaintenanceRecordId",
                table: "ConsumableTransactions");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "StorageLocation",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "Consumables");
        }
    }
}
