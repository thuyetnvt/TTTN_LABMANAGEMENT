using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumableResponsibilityUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Consumables",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponsibleUserId",
                table: "Consumables",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_CreatedByUserId",
                table: "Consumables",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Consumables_ResponsibleUserId",
                table: "Consumables",
                column: "ResponsibleUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Consumables_Users_CreatedByUserId",
                table: "Consumables",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Consumables_Users_ResponsibleUserId",
                table: "Consumables",
                column: "ResponsibleUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Consumables_Users_CreatedByUserId",
                table: "Consumables");

            migrationBuilder.DropForeignKey(
                name: "FK_Consumables_Users_ResponsibleUserId",
                table: "Consumables");

            migrationBuilder.DropIndex(
                name: "IX_Consumables_CreatedByUserId",
                table: "Consumables");

            migrationBuilder.DropIndex(
                name: "IX_Consumables_ResponsibleUserId",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Consumables");

            migrationBuilder.DropColumn(
                name: "ResponsibleUserId",
                table: "Consumables");
        }
    }
}
