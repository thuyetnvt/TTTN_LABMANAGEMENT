using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class Update1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$VCMb7nyBq3BwD2q3n4w.vuCcckSDaW/EJfns4Wh1UWc4MP2xHPylO");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$UXCBxV9EzXhh5CQb6gWnmO5/HxtjKtVnf7H1tdzweLOZcuCS8mT1K");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$7bFzwG0SD/F/93OdR.vbtekKqN4QHNuyqMFUTMbviJ9Xslgy.fyOi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$GpfHGgpLzXxbnoOYUMBcBel5Tku2e5TOSoxkJsTMN7qk0IZKM6p0y");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "PasswordHash",
                value: "$2a$11$xu7VtLwr8TCEb5URdSDJK.z79NmI0dz1GdAR0wcaHs34vErJGf75q");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3,
                column: "PasswordHash",
                value: "$2a$11$uzrvWlAkY.yfTv1WhNr.VuRy1N0xgVGnqBKQzBTk35IN7KGfCN3Kq");
        }
    }
}
