using System;
using LabManagementAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260729010000_AddAssetCategoriesAndBorrowInspection")]
    public partial class AddAssetCategoriesAndBorrowInspection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetCategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AssetCategoryId",
                table: "Equipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EntryDate",
                table: "Equipments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Equipments",
                type: "longtext",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponsiblePerson",
                table: "Equipments",
                type: "longtext",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SerialName",
                table: "Equipments",
                type: "longtext",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyExpiry",
                table: "Equipments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssetCategoryId",
                table: "Consumables",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EntryDate",
                table: "Consumables",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "Consumables",
                type: "longtext",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResponsiblePerson",
                table: "Consumables",
                type: "longtext",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CompensationAmount",
                table: "BorrowRecords",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InspectedByUserId",
                table: "BorrowRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnderWarrantyAtReturn",
                table: "BorrowRecords",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnCondition",
                table: "BorrowRecords",
                type: "longtext",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReturnInspectionNote",
                table: "BorrowRecords",
                type: "longtext",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WarrantyAction",
                table: "BorrowRecords",
                type: "longtext",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "BorrowRequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BorrowRecordId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowRequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BorrowRequestDetails_BorrowRecords_BorrowRecordId",
                        column: x => x.BorrowRecordId,
                        principalTable: "BorrowRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BorrowRequestDetails_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AssetCategories_Name",
                table: "AssetCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_AssetCategoryId",
                table: "Equipments",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_AssetCategoryId",
                table: "Consumables",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_InspectedByUserId",
                table: "BorrowRecords",
                column: "InspectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequestDetails_BorrowRecordId",
                table: "BorrowRequestDetails",
                column: "BorrowRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRequestDetails_EquipmentId",
                table: "BorrowRequestDetails",
                column: "EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_AssetCategories_AssetCategoryId",
                table: "Equipments",
                column: "AssetCategoryId",
                principalTable: "AssetCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Consumables_AssetCategories_AssetCategoryId",
                table: "Consumables",
                column: "AssetCategoryId",
                principalTable: "AssetCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowRecords_Users_InspectedByUserId",
                table: "BorrowRecords",
                column: "InspectedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BorrowRequestDetails");

            migrationBuilder.DropForeignKey(name: "FK_Equipments_AssetCategories_AssetCategoryId", table: "Equipments");
            migrationBuilder.DropForeignKey(name: "FK_Consumables_AssetCategories_AssetCategoryId", table: "Consumables");
            migrationBuilder.DropForeignKey(name: "FK_BorrowRecords_Users_InspectedByUserId", table: "BorrowRecords");

            migrationBuilder.DropTable(name: "AssetCategories");

            migrationBuilder.DropIndex(name: "IX_Equipments_AssetCategoryId", table: "Equipments");
            migrationBuilder.DropIndex(name: "IX_Consumables_AssetCategoryId", table: "Consumables");
            migrationBuilder.DropIndex(name: "IX_BorrowRecords_InspectedByUserId", table: "BorrowRecords");

            migrationBuilder.DropColumn(name: "AssetCategoryId", table: "Equipments");
            migrationBuilder.DropColumn(name: "EntryDate", table: "Equipments");
            migrationBuilder.DropColumn(name: "InvoiceNumber", table: "Equipments");
            migrationBuilder.DropColumn(name: "ResponsiblePerson", table: "Equipments");
            migrationBuilder.DropColumn(name: "SerialName", table: "Equipments");
            migrationBuilder.DropColumn(name: "WarrantyExpiry", table: "Equipments");

            migrationBuilder.DropColumn(name: "AssetCategoryId", table: "Consumables");
            migrationBuilder.DropColumn(name: "EntryDate", table: "Consumables");
            migrationBuilder.DropColumn(name: "InvoiceNumber", table: "Consumables");
            migrationBuilder.DropColumn(name: "ResponsiblePerson", table: "Consumables");

            migrationBuilder.DropColumn(name: "CompensationAmount", table: "BorrowRecords");
            migrationBuilder.DropColumn(name: "InspectedByUserId", table: "BorrowRecords");
            migrationBuilder.DropColumn(name: "IsUnderWarrantyAtReturn", table: "BorrowRecords");
            migrationBuilder.DropColumn(name: "ReturnCondition", table: "BorrowRecords");
            migrationBuilder.DropColumn(name: "ReturnInspectionNote", table: "BorrowRecords");
            migrationBuilder.DropColumn(name: "WarrantyAction", table: "BorrowRecords");
        }
    }
}
