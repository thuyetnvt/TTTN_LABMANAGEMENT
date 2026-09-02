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
            migrationBuilder.CreateIndex(
                name: "IX_Penalties_Status_CreatedAt",
                table: "Penalties",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_UserId_CreatedAt",
                table: "Penalties",
                columns: new[] { "UserId", "CreatedAt" });

            // Keep the foreign-key-supporting index in place until the new
            // composite index has been created. MySQL rejects dropping the
            // only index that backs the Penalties.UserId foreign key.
            migrationBuilder.DropIndex(
                name: "IX_Penalties_UserId",
                table: "Penalties");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Penalties_Status_CreatedAt",
                table: "Penalties");

            migrationBuilder.CreateIndex(
                name: "IX_Penalties_UserId",
                table: "Penalties",
                column: "UserId");

            migrationBuilder.DropIndex(
                name: "IX_Penalties_UserId_CreatedAt",
                table: "Penalties");
        }
    }
}
