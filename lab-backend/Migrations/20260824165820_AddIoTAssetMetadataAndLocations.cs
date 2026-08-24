using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIoTAssetMetadataAndLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssetCode",
                table: "Equipments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "Equipments",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FirmwareVersion",
                table: "Equipments",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FundingSource",
                table: "Equipments",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Equipments",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Imei",
                table: "Equipments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInventoryAt",
                table: "Equipments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationNodeId",
                table: "Equipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MacAddress",
                table: "Equipments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Equipments",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Equipments",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "PurchaseValue",
                table: "Equipments",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QrToken",
                table: "Equipments",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Supplier",
                table: "Equipments",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LocationNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationNodes_LocationNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "LocationNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Preserve legacy free-text locations while introducing a safe root node.
            migrationBuilder.Sql("INSERT INTO `LocationNodes` (`Code`, `Name`, `Type`, `ParentId`, `Description`, `IsActive`, `CreatedAt`) VALUES ('LEGACY', 'Chưa phân loại', 'LEGACY', NULL, 'Tài sản chuyển từ vị trí nhập tự do trước đây.', 1, UTC_TIMESTAMP());");
            migrationBuilder.Sql("UPDATE `Equipments` SET `AssetCode` = CONCAT('LEGACY-', `Id`) WHERE `AssetCode` = '' OR `AssetCode` IS NULL;");
            migrationBuilder.Sql("UPDATE `Equipments` SET `QrToken` = REPLACE(UUID(), '-', '') WHERE `QrToken` = '' OR `QrToken` IS NULL;");
            migrationBuilder.Sql("UPDATE `Equipments` SET `LocationNodeId` = (SELECT `Id` FROM `LocationNodes` WHERE `Code` = 'LEGACY' LIMIT 1) WHERE `LocationNodeId` IS NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_AssetCode",
                table: "Equipments",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_LocationNodeId",
                table: "Equipments",
                column: "LocationNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_QrToken",
                table: "Equipments",
                column: "QrToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationNodes_Code",
                table: "LocationNodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationNodes_ParentId",
                table: "LocationNodes",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_LocationNodes_LocationNodeId",
                table: "Equipments",
                column: "LocationNodeId",
                principalTable: "LocationNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_LocationNodes_LocationNodeId",
                table: "Equipments");

            migrationBuilder.DropTable(
                name: "LocationNodes");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_AssetCode",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_LocationNodeId",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_QrToken",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "AssetCode",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "FirmwareVersion",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "FundingSource",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "Imei",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "LastInventoryAt",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "LocationNodeId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "MacAddress",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "PurchaseValue",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "QrToken",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "Supplier",
                table: "Equipments");
        }
    }
}
