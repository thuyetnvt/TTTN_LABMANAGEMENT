using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations;

public partial class AddReturnEvidence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ReturnEvidence",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                BorrowRecordId = table.Column<int>(type: "int", nullable: false),
                EquipmentId = table.Column<int>(type: "int", nullable: true),
                EvidenceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                OriginalFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                StoredPath = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                ContentType = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                FileSize = table.Column<long>(type: "bigint", nullable: false),
                UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                UploadedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReturnEvidence", x => x.Id);
                table.ForeignKey("FK_ReturnEvidence_BorrowRecords_BorrowRecordId", x => x.BorrowRecordId,
                    "BorrowRecords", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_ReturnEvidence_Equipments_EquipmentId", x => x.EquipmentId,
                    "Equipments", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_ReturnEvidence_Users_UploadedByUserId", x => x.UploadedByUserId,
                    "Users", "Id", onDelete: ReferentialAction.Restrict);
            })
            .Annotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.CreateIndex("IX_ReturnEvidence_BorrowRecordId_EquipmentId", "ReturnEvidence",
            new[] { "BorrowRecordId", "EquipmentId" });
        migrationBuilder.CreateIndex("IX_ReturnEvidence_EquipmentId", "ReturnEvidence", "EquipmentId");
        migrationBuilder.CreateIndex("IX_ReturnEvidence_UploadedByUserId", "ReturnEvidence", "UploadedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ReturnEvidence");
    }
}
