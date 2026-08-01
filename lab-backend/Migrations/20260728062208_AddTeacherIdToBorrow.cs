using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherIdToBorrow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeacherId",
                table: "BorrowRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowRecords_TeacherId",
                table: "BorrowRecords",
                column: "TeacherId");

            migrationBuilder.AddForeignKey(
                name: "FK_BorrowRecords_Users_TeacherId",
                table: "BorrowRecords",
                column: "TeacherId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BorrowRecords_Users_TeacherId",
                table: "BorrowRecords");

            migrationBuilder.DropIndex(
                name: "IX_BorrowRecords_TeacherId",
                table: "BorrowRecords");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "BorrowRecords");
        }
    }
}
