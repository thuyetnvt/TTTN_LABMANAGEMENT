# Kết quả kiểm thử đã chạy

Ngày 25/08/2026 trên branch `codex/iot-lab-asset-upgrade`:

- `dotnet build --no-restore`: đạt, 0 warning/0 error.
- `npm run build`: đạt; còn warning dependency SignalR về `PURE` annotation và cảnh báo chunk lớn.
- Baseline Docker Compose/health: đã ghi tại `docs/BASELINE_AUDIT.md`; compose hợp lệ và `/health` từng trả HTTP 200.
- Docker sau module bàn giao: `docker compose up -d --build backend frontend` đạt; cả ba service healthy, migration bàn giao áp dụng thành công và backend `/health` trả `Healthy`.
- Docker sau module bảo trì định kỳ: backend healthy, migration `20260824174338_AddMaintenanceSchedules` áp dụng thành công; frontend build trong image đạt.
- Backend test project: đạt 5/5 test (`dotnet test lab-backend.Tests/LabManagementAPI.Tests.csproj --configuration Release --no-restore`).
- Frontend unit test: đạt 2/2 test (`npm test`).
- Đã build runtime backend cho import Excel, báo cáo và evidence bàn giao; migration `20260824180250_AddHandoverEvidence` áp dụng thành công trong Docker và `/health` trả `Healthy`.
- Docker xác nhận migration `20260824182131_AddEquipmentLocationHistory` (kèm unique key bảo trì đang xử lý), backend/frontend/db đều healthy và `/health` trả HTTP 200.
- Docker build có QuestPDF và font Unicode cho endpoint xuất PDF, backend publish đạt.
- Chưa chạy E2E browser trong môi trường này; các luồng E2E không được tuyên bố đạt.

## Kiểm chứng bổ sung ngày 25/08/2026

- `dotnet build --no-restore --configuration Release`: đạt, 0 warning/0 error.
- `dotnet test lab-backend.Tests/LabManagementAPI.Tests.csproj --configuration Release --no-restore`: đạt 5/5.
- `npm test`: đạt 2/2.
- `npm run build`: đạt; còn cảnh báo chunk lớn và `PURE` annotation từ dependency SignalR.
- `dotnet ef migrations has-pending-model-changes`: không có thay đổi model chưa migration.
- `docker compose up -d --build backend frontend`: build và khởi động đạt; migration mới cho inventory evidence, maintenance, traceability, user profile, return evidence và borrow header đã áp dụng.
- Docker backend `/health`: `Healthy`; frontend public endpoint trên port `8081` trả HTTP 200 theo cấu hình `.env` hiện tại.
- Trong lần chạy đầu, seed gặp dữ liệu hồ sơ cũ có `ClassName = NULL`; đã sửa model/seed tương thích nullable, rebuild và chạy lại thành công. Cảnh báo Data Protection key chưa persist trong volume vẫn còn và cần xử lý khi deploy production.
- Sau commit hồ sơ cá nhân và gom role guard: `dotnet build --no-restore` đạt 0 warning/0 error; `npm test` đạt 2/2; `npm run build` đạt, chỉ còn cảnh báo từ dependency SignalR/chunk lớn.
