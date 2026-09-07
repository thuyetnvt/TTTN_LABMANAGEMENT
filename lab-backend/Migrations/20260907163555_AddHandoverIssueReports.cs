using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddHandoverIssueReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HandoverIssueReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    HandoverRecordId = table.Column<int>(type: "int", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    ReportedByUserId = table.Column<int>(type: "int", nullable: false),
                    IssueType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResolutionAction = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResolutionNote = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResolvedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverIssueReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HandoverIssueReports_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoverIssueReports_HandoverRecords_HandoverRecordId",
                        column: x => x.HandoverRecordId,
                        principalTable: "HandoverRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HandoverIssueReports_Users_ReportedByUserId",
                        column: x => x.ReportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HandoverIssueReports_Users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverIssueReports_EquipmentId",
                table: "HandoverIssueReports",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverIssueReports_HandoverRecordId_EquipmentId_Status",
                table: "HandoverIssueReports",
                columns: new[] { "HandoverRecordId", "EquipmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_HandoverIssueReports_HandoverRecordId_Status",
                table: "HandoverIssueReports",
                columns: new[] { "HandoverRecordId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_HandoverIssueReports_ReportedByUserId",
                table: "HandoverIssueReports",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HandoverIssueReports_ResolvedByUserId",
                table: "HandoverIssueReports",
                column: "ResolvedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HandoverIssueReports");
        }
    }
}
