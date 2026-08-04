using System;
using LabManagementAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260731090000_ProductionHardening")]
public partial class ProductionHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE Equipments SET DecisionFileName = '' WHERE DecisionFileName IS NULL");
        migrationBuilder.Sql("UPDATE Equipments SET DecisionFilePath = '' WHERE DecisionFilePath IS NULL");
        migrationBuilder.Sql("UPDATE Consumables SET Quantity = 0 WHERE Quantity < 0");
        migrationBuilder.Sql("UPDATE Consumables SET MinQuantity = 0 WHERE MinQuantity < 0");

        migrationBuilder.AddColumn<DateTime>(
            name: "CreatedAt",
            table: "Users",
            type: "datetime(6)",
            nullable: false,
            defaultValueSql: "CURRENT_TIMESTAMP(6)");

        migrationBuilder.AddColumn<bool>(
            name: "IsActive",
            table: "Users",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<int>(
            name: "TokenVersion",
            table: "Users",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AlterColumn<string>(
            name: "Username",
            table: "Users",
            type: "varchar(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Role",
            table: "Users",
            type: "varchar(50)",
            maxLength: 50,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Email",
            table: "Users",
            type: "varchar(256)",
            maxLength: 256,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        AlterEquipmentColumns(migrationBuilder);
        AlterConsumableColumns(migrationBuilder);
        AlterBorrowColumns(migrationBuilder);
        AlterOtherBusinessColumns(migrationBuilder);

        migrationBuilder.AddColumn<DateTime>(
            name: "CompletedAt",
            table: "MaintenanceRecords",
            type: "datetime(6)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Result",
            table: "MaintenanceRecords",
            type: "varchar(2000)",
            maxLength: 2000,
            nullable: false,
            defaultValue: "")
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "MaintenanceRecords",
            type: "varchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "Hoàn thành")
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation(
                        "MySql:ValueGenerationStrategy",
                        MySqlValueGenerationStrategy.IdentityColumn),
                UserId = table.Column<int>(type: "int", nullable: true),
                Username = table.Column<string>(
                    type: "varchar(100)",
                    maxLength: 100,
                    nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Action = table.Column<string>(
                    type: "varchar(100)",
                    maxLength: 100,
                    nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                EntityType = table.Column<string>(
                    type: "varchar(100)",
                    maxLength: 100,
                    nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                EntityId = table.Column<string>(
                    type: "varchar(100)",
                    maxLength: 100,
                    nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Details = table.Column<string>(type: "longtext", nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                IpAddress = table.Column<string>(
                    type: "varchar(64)",
                    maxLength: 64,
                    nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditLogs", item => item.Id);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateTable(
            name: "PasswordResetTokens",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation(
                        "MySql:ValueGenerationStrategy",
                        MySqlValueGenerationStrategy.IdentityColumn),
                UserId = table.Column<int>(type: "int", nullable: false),
                TokenHash = table.Column<string>(
                    type: "varchar(64)",
                    maxLength: 64,
                    nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                UsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasswordResetTokens", item => item.Id);
                table.ForeignKey(
                    name: "FK_PasswordResetTokens_Users_UserId",
                    column: item => item.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        CreateIndexes(migrationBuilder);
        ReplaceCascadeForeignKeys(migrationBuilder);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Consumables_MinQuantity",
            table: "Consumables",
            sql: "MinQuantity >= 0");
        migrationBuilder.AddCheckConstraint(
            name: "CK_Consumables_Quantity",
            table: "Consumables",
            sql: "Quantity >= 0");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Consumables_MinQuantity",
            table: "Consumables");
        migrationBuilder.DropCheckConstraint(
            name: "CK_Consumables_Quantity",
            table: "Consumables");

        RestoreCascadeForeignKeys(migrationBuilder);

        migrationBuilder.DropTable(name: "AuditLogs");
        migrationBuilder.DropTable(name: "PasswordResetTokens");

        migrationBuilder.DropIndex(name: "IX_Users_Role_IsActive", table: "Users");
        migrationBuilder.DropIndex(name: "IX_Users_Email", table: "Users");
        migrationBuilder.DropIndex(name: "IX_Users_Username", table: "Users");
        migrationBuilder.DropIndex(name: "IX_Equipments_Serial", table: "Equipments");
        migrationBuilder.DropIndex(name: "IX_Equipments_Status", table: "Equipments");
        migrationBuilder.DropIndex(name: "IX_BorrowRecords_Status", table: "BorrowRecords");
        migrationBuilder.DropIndex(name: "IX_BorrowRecords_ExpectedReturnDate", table: "BorrowRecords");
        migrationBuilder.DropIndex(name: "IX_ConsumableRequests_Status", table: "ConsumableRequests");
        migrationBuilder.DropIndex(name: "IX_MaintenanceRecords_Status", table: "MaintenanceRecords");
        migrationBuilder.DropIndex(name: "IX_Penalties_Status", table: "Penalties");

        migrationBuilder.DropColumn(name: "CreatedAt", table: "Users");
        migrationBuilder.DropColumn(name: "IsActive", table: "Users");
        migrationBuilder.DropColumn(name: "TokenVersion", table: "Users");
        migrationBuilder.DropColumn(name: "CompletedAt", table: "MaintenanceRecords");
        migrationBuilder.DropColumn(name: "Result", table: "MaintenanceRecords");
        migrationBuilder.DropColumn(name: "Status", table: "MaintenanceRecords");

        migrationBuilder.AlterColumn<string>(
            name: "Username",
            table: "Users",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(100)",
            oldMaxLength: 100)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AlterColumn<string>(
            name: "Role",
            table: "Users",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(50)",
            oldMaxLength: 50)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");
        migrationBuilder.AlterColumn<string>(
            name: "Email",
            table: "Users",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(256)",
            oldMaxLength: 256)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");
    }

    private static void AlterEquipmentColumns(MigrationBuilder migrationBuilder)
    {
        AlterString(migrationBuilder, "Equipments", "Name", "varchar(255)", 255, "longtext");
        AlterString(migrationBuilder, "Equipments", "Model", "varchar(255)", 255, "longtext");
        AlterString(migrationBuilder, "Equipments", "Serial", "varchar(100)", 100, "longtext");
        AlterString(migrationBuilder, "Equipments", "SerialName", "varchar(255)", 255, "longtext");
        AlterString(migrationBuilder, "Equipments", "Location", "varchar(255)", 255, "longtext");
        AlterString(migrationBuilder, "Equipments", "ResponsiblePerson", "varchar(255)", 255, "longtext");
        AlterString(migrationBuilder, "Equipments", "InvoiceNumber", "varchar(100)", 100, "longtext");
        AlterString(migrationBuilder, "Equipments", "Status", "varchar(50)", 50, "longtext");
        AlterString(migrationBuilder, "Equipments", "DecisionFileName", "varchar(255)", 255, "longtext");
        AlterString(migrationBuilder, "Equipments", "DecisionFilePath", "varchar(1000)", 1000, "longtext");
        AlterString(migrationBuilder, "AssetCategories", "Name", "varchar(150)", 150, "varchar(255)");
        AlterString(migrationBuilder, "AssetCategories", "Description", "varchar(1000)", 1000, "longtext");
    }

    private static void AlterConsumableColumns(MigrationBuilder migrationBuilder)
    {
        AlterString(migrationBuilder, "Consumables", "Name", "varchar(255)", 255, "longtext");
        AlterString(migrationBuilder, "Consumables", "Unit", "varchar(50)", 50, "longtext");
        AlterString(migrationBuilder, "Consumables", "ResponsiblePerson", "varchar(255)", 255, "longtext");
        AlterString(migrationBuilder, "Consumables", "InvoiceNumber", "varchar(100)", 100, "longtext");
        AlterString(migrationBuilder, "ConsumableRequests", "Reason", "varchar(1000)", 1000, "longtext");
        AlterString(migrationBuilder, "ConsumableRequests", "Status", "varchar(50)", 50, "longtext");
    }

    private static void AlterBorrowColumns(MigrationBuilder migrationBuilder)
    {
        AlterString(migrationBuilder, "BorrowRecords", "Purpose", "varchar(1000)", 1000, "longtext");
        AlterString(migrationBuilder, "BorrowRecords", "Status", "varchar(50)", 50, "longtext");
        AlterString(migrationBuilder, "BorrowRecords", "ReturnCondition", "varchar(50)", 50, "longtext");
        AlterString(migrationBuilder, "BorrowRecords", "ReturnInspectionNote", "varchar(2000)", 2000, "longtext");
        AlterString(migrationBuilder, "BorrowRecords", "WarrantyAction", "varchar(255)", 255, "longtext");
        AlterString(migrationBuilder, "BorrowRequestDetails", "Note", "varchar(1000)", 1000, "longtext");

        migrationBuilder.AlterColumn<decimal>(
            name: "CompensationAmount",
            table: "BorrowRecords",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(65,30)");
    }

    private static void AlterOtherBusinessColumns(MigrationBuilder migrationBuilder)
    {
        AlterString(migrationBuilder, "MaintenanceRecords", "Description", "varchar(2000)", 2000, "longtext");
        AlterString(migrationBuilder, "MaintenanceRecords", "PerformedBy", "varchar(255)", 255, "longtext");
        migrationBuilder.AlterColumn<decimal>(
            name: "Cost",
            table: "MaintenanceRecords",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(65,30)");

        AlterString(migrationBuilder, "Penalties", "Reason", "varchar(2000)", 2000, "longtext");
        AlterString(migrationBuilder, "Penalties", "Status", "varchar(50)", 50, "longtext");
        migrationBuilder.AlterColumn<decimal>(
            name: "Amount",
            table: "Penalties",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(65,30)");
    }

    private static void AlterString(
        MigrationBuilder migrationBuilder,
        string table,
        string column,
        string newType,
        int maxLength,
        string oldType)
    {
        migrationBuilder.AlterColumn<string>(
            name: column,
            table: table,
            type: newType,
            maxLength: maxLength,
            nullable: false,
            oldClrType: typeof(string),
            oldType: oldType)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");
    }

    private static void CreateIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_CreatedAt",
            table: "AuditLogs",
            column: "CreatedAt");
        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_UserId",
            table: "AuditLogs",
            column: "UserId");
        migrationBuilder.CreateIndex(
            name: "IX_PasswordResetTokens_ExpiresAt",
            table: "PasswordResetTokens",
            column: "ExpiresAt");
        migrationBuilder.CreateIndex(
            name: "IX_PasswordResetTokens_TokenHash",
            table: "PasswordResetTokens",
            column: "TokenHash",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_PasswordResetTokens_UserId",
            table: "PasswordResetTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Username",
            table: "Users",
            column: "Username",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email");
        migrationBuilder.CreateIndex(
            name: "IX_Users_Role_IsActive",
            table: "Users",
            columns: new[] { "Role", "IsActive" });
        migrationBuilder.CreateIndex(
            name: "IX_Equipments_Serial",
            table: "Equipments",
            column: "Serial",
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_Equipments_Status",
            table: "Equipments",
            column: "Status");
        migrationBuilder.CreateIndex(
            name: "IX_BorrowRecords_Status",
            table: "BorrowRecords",
            column: "Status");
        migrationBuilder.CreateIndex(
            name: "IX_BorrowRecords_ExpectedReturnDate",
            table: "BorrowRecords",
            column: "ExpectedReturnDate");
        migrationBuilder.CreateIndex(
            name: "IX_ConsumableRequests_Status",
            table: "ConsumableRequests",
            column: "Status");
        migrationBuilder.CreateIndex(
            name: "IX_MaintenanceRecords_Status",
            table: "MaintenanceRecords",
            column: "Status");
        migrationBuilder.CreateIndex(
            name: "IX_Penalties_Status",
            table: "Penalties",
            column: "Status");
    }

    private static void ReplaceCascadeForeignKeys(MigrationBuilder migrationBuilder)
    {
        ReplaceForeignKey(migrationBuilder, "BorrowRecords", "Users", "UserId");
        ReplaceForeignKey(migrationBuilder, "BorrowRecords", "Equipments", "EquipmentId");
        migrationBuilder.DropForeignKey(
            name: "FK_BorrowRecords_Users_TeacherId",
            table: "BorrowRecords");
        migrationBuilder.AddForeignKey(
            name: "FK_BorrowRecords_Users_TeacherId",
            table: "BorrowRecords",
            column: "TeacherId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
        ReplaceForeignKey(migrationBuilder, "BorrowRequestDetails", "Equipments", "EquipmentId");
        ReplaceForeignKey(migrationBuilder, "ConsumableRequests", "Users", "UserId");
        ReplaceForeignKey(migrationBuilder, "ConsumableRequests", "Consumables", "ConsumableId");
        ReplaceForeignKey(migrationBuilder, "MaintenanceRecords", "Equipments", "EquipmentId");
        ReplaceForeignKey(migrationBuilder, "Penalties", "Users", "UserId");
        ReplaceForeignKey(migrationBuilder, "Penalties", "Equipments", "EquipmentId");
        ReplaceForeignKey(migrationBuilder, "Penalties", "BorrowRecords", "BorrowRecordId");
    }

    private static void RestoreCascadeForeignKeys(MigrationBuilder migrationBuilder)
    {
        RestoreCascadeForeignKey(migrationBuilder, "BorrowRecords", "Users", "UserId");
        RestoreCascadeForeignKey(migrationBuilder, "BorrowRecords", "Equipments", "EquipmentId");
        migrationBuilder.DropForeignKey(
            name: "FK_BorrowRecords_Users_TeacherId",
            table: "BorrowRecords");
        migrationBuilder.AddForeignKey(
            name: "FK_BorrowRecords_Users_TeacherId",
            table: "BorrowRecords",
            column: "TeacherId",
            principalTable: "Users",
            principalColumn: "Id");
        RestoreCascadeForeignKey(migrationBuilder, "BorrowRequestDetails", "Equipments", "EquipmentId");
        RestoreCascadeForeignKey(migrationBuilder, "ConsumableRequests", "Users", "UserId");
        RestoreCascadeForeignKey(migrationBuilder, "ConsumableRequests", "Consumables", "ConsumableId");
        RestoreCascadeForeignKey(migrationBuilder, "MaintenanceRecords", "Equipments", "EquipmentId");
        RestoreCascadeForeignKey(migrationBuilder, "Penalties", "Users", "UserId");
        RestoreCascadeForeignKey(migrationBuilder, "Penalties", "Equipments", "EquipmentId");
        RestoreCascadeForeignKey(migrationBuilder, "Penalties", "BorrowRecords", "BorrowRecordId");
    }

    private static void ReplaceForeignKey(
        MigrationBuilder migrationBuilder,
        string dependentTable,
        string principalTable,
        string column)
    {
        var foreignKeyName = $"FK_{dependentTable}_{principalTable}_{column}";
        migrationBuilder.DropForeignKey(name: foreignKeyName, table: dependentTable);
        migrationBuilder.AddForeignKey(
            name: foreignKeyName,
            table: dependentTable,
            column: column,
            principalTable: principalTable,
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    private static void RestoreCascadeForeignKey(
        MigrationBuilder migrationBuilder,
        string dependentTable,
        string principalTable,
        string column)
    {
        var foreignKeyName = $"FK_{dependentTable}_{principalTable}_{column}";
        migrationBuilder.DropForeignKey(name: foreignKeyName, table: dependentTable);
        migrationBuilder.AddForeignKey(
            name: foreignKeyName,
            table: dependentTable,
            column: column,
            principalTable: principalTable,
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
