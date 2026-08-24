# Mô hình dữ liệu

Các nhóm bảng chính: `Users`, `Equipments`, `AssetCategories`, `LocationNodes`, `BorrowRecords`, `BorrowRequestDetails`, `BorrowStatusHistories`, `MaintenanceRecords`, `Consumables`, `ConsumableRequests`, `ConsumableTransactions`, `InventorySessions`, `InventoryItems`, `Notifications`, `Penalties`, `AuditLogs`.

```mermaid
erDiagram
  BorrowRecords ||--|{ BorrowRequestDetails : contains
  BorrowRecords ||--o{ BorrowStatusHistories : changes
  Equipments ||--o{ BorrowRequestDetails : requested
  InventorySessions ||--|{ InventoryItems : snapshots
  Equipments ||--o{ InventoryItems : counted
  LocationNodes ||--o{ Equipments : stores
  Users ||--o{ Notifications : receives
  Consumables ||--o{ ConsumableTransactions : ledger
```

Migration phải chạy tuần tự bằng `dotnet ef database update` hoặc cơ chế `Database__ApplyMigrations=true`; không sửa schema thủ công trong production.

