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
- Vòng Docker cuối sau các commit `40eab95` và `aa4f905`: backend image/frontend image build đạt, backend healthy, database healthy, `/health` trong backend trả `Healthy`, không có log `fail` hoặc `pending changes` khi khởi động; frontend public port `8081` trả HTTP 200.

## Kiểm chứng hardening và E2E ngày 25/08/2026

- `dotnet test lab-backend.Tests/LabManagementAPI.Tests.csproj --configuration Release`: đạt 7/7.
- `npm test`: đạt 2/2; `npm run build`: đạt. Vẫn còn cảnh báo chunk lớn từ bundle hiện tại.
- `npm audit`: đạt 0 vulnerability sau khi cập nhật lockfile.
- `npm run test:e2e -- --project=chromium --project=mobile-chromium`: đạt 4 pass, 2 skip do flow admin được gate bởi credential môi trường.
- Với `E2E_BASE_URL=http://localhost:8081` và credential seed admin không in ra log: flow đăng nhập admin + truy cập `/dashboard/admin/users` đạt 1/1.
- `docker compose up -d --build backend frontend`: đạt; backend healthy, `/health` trả `Healthy`, frontend port `8081` trả HTTP 200, migration không pending.
- Docker còn cảnh báo mặc định “No XML encryptor configured” của ASP.NET Data Protection; key đã được persist vào volume, nhưng deployment production phải cấu hình certificate/encryptor để mã hóa key at rest.
- CI đã thêm job E2E độc lập; job chạy smoke suite không cần seed credential, còn business flow cần môi trường test có dữ liệu/secret riêng.

## Kiểm chứng cuối phiên nâng cấp ngày 25/08/2026

- Backend test project: đạt **18/18**; bao phủ đăng nhập, reset password/token version, IDOR/RBAC mượn trả, transaction đa tài sản, tồn kho vật tư và upload MIME/path.
- `dotnet build --configuration Release --no-restore`: đạt, 0 warning/0 error.
- Frontend `npm test`: đạt 2/2; `npm run build`: đạt, chỉ còn cảnh báo chunk lớn do bundle ApexCharts.
- `npm audit` trong `lab-frontend`: 0 vulnerability.
- E2E smoke chạy cả Chromium desktop và mobile: **4 pass, 4 skip**; các skip là test business/admin cần credential khi chạy không truyền biến môi trường.
- E2E business có dữ liệu cô lập prefix `E2E-`: **1/1 pass trong 1,4 phút**, đi qua đăng nhập các vai trò bằng API, UI sinh viên, mượn nhiều tài sản, giảng viên duyệt, quản lý duyệt/bàn giao/trả, bảo trì và kiểm kê QR.
- Docker sau rebuild: db/backend/frontend healthy; backend `/health` trả `Healthy`; kiểm tra trực tiếp trong container xác nhận `X-Correlation-ID` được phản hồi đúng giá trị gửi vào; frontend `http://localhost:8081` trả HTTP 200.
- Các dữ liệu E2E được tạo có chủ đích để giữ trace/audit; có thể lọc bằng tiền tố `E2E-` khi dọn môi trường kiểm thử, không dùng làm số liệu giao diện.

Các kiểm chứng trên là local/Docker, không thay thế diễn tập restore trên production, triển khai MinIO/S3 hoặc cấu hình certificate mã hóa Data Protection trong môi trường thật.
