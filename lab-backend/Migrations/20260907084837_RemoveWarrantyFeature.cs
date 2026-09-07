using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWarrantyFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE `Equipments` SET `Status` = 'BROKEN' WHERE `Status` IN ('UNDER_WARRANTY', 'Bảo hành');");
            migrationBuilder.Sql(
                "UPDATE `BorrowRecords` SET `Status` = 'RETURNED_DAMAGED' WHERE `Status` = 'Đã trả (Bảo hành)';");

            migrationBuilder.DropColumn(
                name: "WarrantyExpiry",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "IsUnderWarrantyAtReturn",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "WarrantyAction",
                table: "BorrowRecords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyExpiry",
                table: "Equipments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnderWarrantyAtReturn",
                table: "BorrowRecords",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyAction",
                table: "BorrowRecords",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
