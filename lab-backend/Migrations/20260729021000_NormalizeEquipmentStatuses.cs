using LabManagementAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260729021000_NormalizeEquipmentStatuses")]
    public partial class NormalizeEquipmentStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Equipments SET Status = 'Rảnh' WHERE Status IN ('Sẵn sàng', 'OK sử dụng', 'Dùng tốt')");
            migrationBuilder.Sql("UPDATE Equipments SET Status = 'Đang mượn' WHERE Status = 'Đang cho mượn'");
            migrationBuilder.Sql("UPDATE Equipments SET Status = 'Bảo hành' WHERE Status IN ('Đang bảo hành', 'Bảo trì', 'Bảo trì định kỳ')");
            migrationBuilder.Sql("UPDATE Equipments SET Status = 'Hỏng' WHERE Status = 'Hỏng/chờ bồi thường'");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Equipments SET Status = 'Sẵn sàng' WHERE Status = 'Rảnh'");
            migrationBuilder.Sql("UPDATE Equipments SET Status = 'Đang cho mượn' WHERE Status = 'Đang mượn'");
            migrationBuilder.Sql("UPDATE Equipments SET Status = 'Đang bảo hành' WHERE Status = 'Bảo hành'");
        }
    }
}
