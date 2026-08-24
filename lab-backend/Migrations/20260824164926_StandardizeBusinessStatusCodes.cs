using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class StandardizeBusinessStatusCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE `Equipments` SET `Status` = 'AVAILABLE' WHERE `Status` IN ('Rảnh', 'Sẵn sàng');");
            migrationBuilder.Sql("UPDATE `Equipments` SET `Status` = 'BORROWED' WHERE `Status` = 'Đang mượn';");
            migrationBuilder.Sql("UPDATE `Equipments` SET `Status` = 'BROKEN' WHERE `Status` = 'Hỏng';");
            migrationBuilder.Sql("UPDATE `Equipments` SET `Status` = 'UNDER_WARRANTY' WHERE `Status` = 'Bảo hành';");
            migrationBuilder.Sql("UPDATE `Equipments` SET `Status` = 'MAINTENANCE_IN_PROGRESS' WHERE `Status` = 'Bảo trì';");

            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'BORROW_PENDING' WHERE `Status` = 'Chờ duyệt';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'TEACHER_PENDING' WHERE `Status` = 'Chờ GV duyệt';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'APPROVAL_PROCESSING' WHERE `Status` = 'Đang xử lý duyệt';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'RETURN_PROCESSING' WHERE `Status` = 'Đang xử lý trả';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'BORROWED' WHERE `Status` = 'Đang mượn';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'RETURNED' WHERE `Status` = 'Đã trả';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'RETURNED_DAMAGED' WHERE `Status` IN ('Đã trả (Hỏng)', 'Đã trả (Bảo hành)');");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'REJECTED' WHERE `Status` = 'Từ chối';");

            migrationBuilder.Sql("UPDATE `ConsumableRequests` SET `Status` = 'CONSUMABLE_PENDING' WHERE `Status` = 'Chờ duyệt';");
            migrationBuilder.Sql("UPDATE `ConsumableRequests` SET `Status` = 'CONSUMABLE_PROCESSING' WHERE `Status` = 'Đang xử lý';");
            migrationBuilder.Sql("UPDATE `ConsumableRequests` SET `Status` = 'CONSUMABLE_ISSUED' WHERE `Status` = 'Đã cấp phát';");
            migrationBuilder.Sql("UPDATE `ConsumableRequests` SET `Status` = 'REJECTED' WHERE `Status` = 'Từ chối';");

            migrationBuilder.Sql("UPDATE `MaintenanceRecords` SET `Status` = 'MAINTENANCE_IN_PROGRESS' WHERE `Status` IN ('Đang xử lý', 'Đang hoàn tất');");
            migrationBuilder.Sql("UPDATE `MaintenanceRecords` SET `Status` = 'MAINTENANCE_COMPLETED' WHERE `Status` IN ('Hoàn tất', 'Hoàn thành');");

            migrationBuilder.Sql("UPDATE `Penalties` SET `Status` = 'UNPAID' WHERE `Status` = 'Chưa thanh toán';");
            migrationBuilder.Sql("UPDATE `Penalties` SET `Status` = 'PAID' WHERE `Status` = 'Đã thanh toán';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE `Equipments` SET `Status` = 'Rảnh' WHERE `Status` = 'AVAILABLE';");
            migrationBuilder.Sql("UPDATE `Equipments` SET `Status` = 'Đang mượn' WHERE `Status` = 'BORROWED';");
            migrationBuilder.Sql("UPDATE `Equipments` SET `Status` = 'Hỏng' WHERE `Status` = 'BROKEN';");
            migrationBuilder.Sql("UPDATE `Equipments` SET `Status` = 'Bảo hành' WHERE `Status` = 'UNDER_WARRANTY';");

            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'Chờ duyệt' WHERE `Status` = 'BORROW_PENDING';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'Chờ GV duyệt' WHERE `Status` = 'TEACHER_PENDING';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'Đang xử lý duyệt' WHERE `Status` = 'APPROVAL_PROCESSING';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'Đang xử lý trả' WHERE `Status` = 'RETURN_PROCESSING';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'Đang mượn' WHERE `Status` = 'BORROWED';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'Đã trả' WHERE `Status` = 'RETURNED';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'Đã trả (Hỏng)' WHERE `Status` = 'RETURNED_DAMAGED';");
            migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'Từ chối' WHERE `Status` = 'REJECTED';");

            migrationBuilder.Sql("UPDATE `ConsumableRequests` SET `Status` = 'Chờ duyệt' WHERE `Status` = 'CONSUMABLE_PENDING';");
            migrationBuilder.Sql("UPDATE `ConsumableRequests` SET `Status` = 'Đang xử lý' WHERE `Status` = 'CONSUMABLE_PROCESSING';");
            migrationBuilder.Sql("UPDATE `ConsumableRequests` SET `Status` = 'Đã cấp phát' WHERE `Status` = 'CONSUMABLE_ISSUED';");
            migrationBuilder.Sql("UPDATE `ConsumableRequests` SET `Status` = 'Từ chối' WHERE `Status` = 'REJECTED';");

            migrationBuilder.Sql("UPDATE `MaintenanceRecords` SET `Status` = 'Đang xử lý' WHERE `Status` = 'MAINTENANCE_IN_PROGRESS';");
            migrationBuilder.Sql("UPDATE `MaintenanceRecords` SET `Status` = 'Hoàn thành' WHERE `Status` = 'MAINTENANCE_COMPLETED';");
            migrationBuilder.Sql("UPDATE `Penalties` SET `Status` = 'Chưa thanh toán' WHERE `Status` = 'UNPAID';");
            migrationBuilder.Sql("UPDATE `Penalties` SET `Status` = 'Đã thanh toán' WHERE `Status` = 'PAID';");
        }
    }
}
