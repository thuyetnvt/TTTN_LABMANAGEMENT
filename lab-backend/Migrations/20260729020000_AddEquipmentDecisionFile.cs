using System;
using LabManagementAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260729020000_AddEquipmentDecisionFile")]
    public partial class AddEquipmentDecisionFile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DecisionFileName",
                table: "Equipments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DecisionFilePath",
                table: "Equipments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DecisionUploadedAt",
                table: "Equipments",
                type: "datetime(6)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DecisionFileName",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "DecisionFilePath",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "DecisionUploadedAt",
                table: "Equipments");
        }
    }
}
