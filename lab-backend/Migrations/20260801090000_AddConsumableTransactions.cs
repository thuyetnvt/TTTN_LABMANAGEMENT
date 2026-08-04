using System;
using LabManagementAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260801090000_AddConsumableTransactions")]
public partial class AddConsumableTransactions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConsumableTransactions",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation(
                        "MySql:ValueGenerationStrategy",
                        MySqlValueGenerationStrategy.IdentityColumn),
                ConsumableId = table.Column<int>(type: "int", nullable: false),
                Type = table.Column<string>(
                    type: "varchar(50)",
                    maxLength: 50,
                    nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Quantity = table.Column<int>(type: "int", nullable: false),
                BeforeQuantity = table.Column<int>(type: "int", nullable: false),
                AfterQuantity = table.Column<int>(type: "int", nullable: false),
                Reason = table.Column<string>(
                    type: "varchar(1000)",
                    maxLength: 1000,
                    nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                UserId = table.Column<int>(type: "int", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConsumableTransactions", item => item.Id);
                table.ForeignKey(
                    name: "FK_ConsumableTransactions_Consumables_ConsumableId",
                    column: item => item.ConsumableId,
                    principalTable: "Consumables",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ConsumableTransactions_Users_UserId",
                    column: item => item.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex(
            name: "IX_ConsumableTransactions_ConsumableId",
            table: "ConsumableTransactions",
            column: "ConsumableId");

        migrationBuilder.CreateIndex(
            name: "IX_ConsumableTransactions_CreatedAt",
            table: "ConsumableTransactions",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_ConsumableTransactions_UserId",
            table: "ConsumableTransactions",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ConsumableTransactions");
    }
}
