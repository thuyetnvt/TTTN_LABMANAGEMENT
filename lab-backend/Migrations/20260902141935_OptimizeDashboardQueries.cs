using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDashboardQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_TeacherId",
                table: "BorrowRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_UserId",
                table: "BorrowRecords");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_MaintenanceDate",
                table: "MaintenanceRecords",
                column: "MaintenanceDate");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_Status_ExpectedReturnDate",
                table: "BorrowRecords",
                columns: new[] { "Status", "ExpectedReturnDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_TeacherId_Status_BorrowDate",
                table: "BorrowRecords",
                columns: new[] { "TeacherId", "Status", "BorrowDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_UserId_Status_BorrowDate",
                table: "BorrowRecords",
                columns: new[] { "UserId", "Status", "BorrowDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRecords_MaintenanceDate",
                table: "MaintenanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_Status_ExpectedReturnDate",
                table: "BorrowRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_TeacherId_Status_BorrowDate",
                table: "BorrowRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_UserId_Status_BorrowDate",
                table: "BorrowRecords");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_TeacherId",
                table: "BorrowRecords",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_UserId",
                table: "BorrowRecords",
                column: "UserId");
        }
    }
}
