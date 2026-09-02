using LabManagementAPI.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabManagementAPI.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260902000000_NormalizePersistedWorkflowStatuses")]
public partial class NormalizePersistedWorkflowStatuses : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Technical lock states must never survive a completed request. They
        // were previously inserted by demo data and left records unactionable.
        migrationBuilder.Sql("UPDATE `BorrowRecords` SET `Status` = 'BORROW_PENDING' WHERE `Status` = 'APPROVAL_PROCESSING';");
        migrationBuilder.Sql("UPDATE `BorrowRequestDetails` SET `Status` = 'BORROW_PENDING' WHERE `Status` = 'APPROVAL_PROCESSING';");
        migrationBuilder.Sql("UPDATE `ConsumableRequests` SET `Status` = 'CONSUMABLE_PENDING' WHERE `Status` = 'CONSUMABLE_PROCESSING';");
        migrationBuilder.Sql("UPDATE `MaintenanceRecords` SET `Status` = 'MAINTENANCE_IN_PROGRESS' WHERE `Status` = 'MAINTENANCE_COMPLETING';");

        // CONSUMABLE_ISSUED was the old terminal state. Preserve its meaning
        // while moving existing data to the auditable hand-over workflow.
        migrationBuilder.Sql(
            """
            UPDATE `ConsumableRequests`
            SET `Status` = 'CONSUMABLE_RECEIVED',
                `ApprovalDate` = COALESCE(`ApprovalDate`, `RequestDate`),
                `HandedOverAt` = COALESCE(`HandedOverAt`, `ApprovalDate`, `RequestDate`),
                `ReceivedAt` = COALESCE(`ReceivedAt`, `HandedOverAt`, `ApprovalDate`, `RequestDate`)
            WHERE `Status` = 'CONSUMABLE_ISSUED';
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Deliberately do not restore lock states. Rolling back schema should
        // not make valid business records unactionable again.
    }
}
