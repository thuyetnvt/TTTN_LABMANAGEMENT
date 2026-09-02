using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class OptimizePagedListQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Penalties_UserId",
                table: "Penalties");

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_Status_CreatedAt",
                table: "Penalties",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_UserId_CreatedAt",
                table: "Penalties",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Penalties_Status_CreatedAt",
                table: "Penalties");

            migrationBuilder.DropIndex(
                name: "IX_Penalties_UserId_CreatedAt",
                table: "Penalties");

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_UserId",
                table: "Penalties",
                column: "UserId");
        }
    }
}
