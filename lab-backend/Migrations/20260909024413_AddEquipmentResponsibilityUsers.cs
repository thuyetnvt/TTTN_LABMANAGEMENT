using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentResponsibilityUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Equipments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponsibleUserId",
                table: "Equipments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_CreatedByUserId",
                table: "Equipments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_ResponsibleUserId",
                table: "Equipments",
                column: "ResponsibleUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Users_CreatedByUserId",
                table: "Equipments",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_Users_ResponsibleUserId",
                table: "Equipments",
                column: "ResponsibleUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Users_CreatedByUserId",
                table: "Equipments");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_Users_ResponsibleUserId",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_CreatedByUserId",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_ResponsibleUserId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "ResponsibleUserId",
                table: "Equipments");
        }
    }
}
