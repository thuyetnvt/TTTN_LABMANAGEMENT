using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBorrowRequestContactPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "BorrowRecords",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                """
                UPDATE `BorrowRecords` AS `borrow`
                INNER JOIN `Users` AS `user` ON `user`.`Id` = `borrow`.`UserId`
                SET `borrow`.`ContactPhone` = COALESCE(`user`.`Phone`, '')
                WHERE `borrow`.`ContactPhone` = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "BorrowRecords");
        }
    }
}
