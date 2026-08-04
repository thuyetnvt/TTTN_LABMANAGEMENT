using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class Update2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "$2a$11$VCMb7nyBq3BwD2q3n4w.vuCcckSDaW/EJfns4Wh1UWc4MP2xHPylO", "Admin", "admin" },
                    { 2, "$2a$11$UXCBxV9EzXhh5CQb6gWnmO5/HxtjKtVnf7H1tdzweLOZcuCS8mT1K", "Lab Manager", "manager" },
                    { 3, "$2a$11$7bFzwG0SD/F/93OdR.vbtekKqN4QHNuyqMFUTMbviJ9Xslgy.fyOi", "Student", "sinhvien" }
                });
        }
    }
}
