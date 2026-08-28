# LabManagement — Hệ thống quản lý tài sản Phòng Lab IoT

LabManagement là hệ thống full-stack hỗ trợ quản lý tài sản, mượn trả, bàn giao, kiểm kê, bảo trì và vật tư tiêu hao cho Phòng Lab IoT thuộc Khoa Công nghệ Thông tin.

Hệ thống được xây dựng theo quy trình nghiệp vụ thực tế, có phân quyền theo vai trò, mã QR cho tài sản, lịch sử thay đổi, thông báo realtime, báo cáo và nhật ký kiểm toán.

## Chức năng chính

### Quản lý tài sản

- Quản lý danh mục, mã tài sản, serial, model và thông tin thiết bị IoT.
- Theo dõi nhà sản xuất, firmware, MAC, IMEI, nhà cung cấp và hạn bảo hành.
- Tổ chức vị trí theo cấu trúc cây: khu vực → phòng → tủ/kệ → vị trí cụ thể.
- Sinh và in mã QR riêng cho từng tài sản hoặc in hàng loạt.
- Import tài sản từ Excel với bước xem trước và kiểm tra dữ liệu trùng.
- Lưu lịch sử điều chuyển, người chịu trách nhiệm và trạng thái tài sản.

### Mượn, bàn giao và trả tài sản

- Một phiếu có thể mượn nhiều tài sản.
- Sinh viên bắt buộc chọn giảng viên bảo lãnh.
- Quy trình duyệt hai cấp: giảng viên → quản lý lab.
- Lập biên bản bàn giao, ghi nhận tình trạng và phụ kiện từng tài sản.
- Đính kèm ảnh, tài liệu hoặc bằng chứng bàn giao/nhận trả.
- Xử lý đồng thời bằng transaction, không cho một tài sản bị mượn trùng.
- Tự động cập nhật trạng thái khi trả tốt, trả hỏng, bảo hành hoặc bồi thường.

### Kiểm kê bằng QR

- Tạo đợt kiểm kê theo vị trí hoặc danh mục tài sản.
- Quét QR và đánh dấu: tìm thấy, thiếu, sai vị trí hoặc hỏng.
- Ghi chú và đính kèm bằng chứng cho từng dòng kiểm kê.
- Theo dõi tiến độ và xuất báo cáo chênh lệch Excel/PDF.

### Bảo trì

- Tạo và theo dõi phiếu bảo trì theo tài sản.
- Quản lý người thực hiện, nhà cung cấp, chi phí, checklist và vật tư sử dụng.
- Lập lịch bảo trì theo ngày, tuần, tháng, quý hoặc năm.
- Tự tính ngày bảo trì tiếp theo và sinh phiếu khi đến hạn.
- Cho phép chọn trạng thái tài sản sau khi hoàn tất bảo trì.

### Vật tư tiêu hao

- Quản lý mã vật tư, lô, hạn sử dụng, giá nhập và mức tồn tối thiểu.
- Tạo yêu cầu cấp phát và quy trình duyệt/từ chối.
- Ghi nhận tồn trước và sau giao dịch.
- Không cho xuất kho vượt tồn hoặc làm số lượng âm.
- Liên kết giao dịch với phiếu cấp phát và phiếu bảo trì.

### Quản trị và báo cáo

- Quản lý hồ sơ cá nhân và ảnh đại diện có tích hợp công cụ cắt ảnh (Image Cropper).
- Quản lý người dùng và khóa/mở khóa tài khoản.
- Phân quyền ở cả frontend và backend.
- Thông báo lưu trong database và cập nhật realtime bằng SignalR.
- Nhật ký kiểm toán các thao tác quan trọng.
- Báo cáo tài sản, mượn trả, kiểm kê, bảo trì và vật tư.
- Hỗ trợ xuất Excel/PDF theo từng nghiệp vụ.

## Vai trò và quyền hạn

| Vai trò | Quyền chính |
| --- | --- |
| Admin | Quản lý toàn bộ hệ thống, người dùng, tài sản, báo cáo và audit log |
| Trưởng lab | Duyệt mượn/trả, bàn giao, kiểm kê, bảo trì, vật tư và báo cáo |
| Phó lab | Thực hiện các nghiệp vụ vận hành lab theo phạm vi được cấp |
| Giảng viên | Bảo lãnh, duyệt hoặc từ chối yêu cầu của sinh viên |
| Sinh viên | Xem tài sản, gửi yêu cầu mượn/vật tư và theo dõi lịch sử của mình |

Chi tiết phân quyền xem tại [docs/RBAC_MATRIX.md](docs/RBAC_MATRIX.md).

## Công nghệ sử dụng

| Thành phần | Công nghệ |
| --- | --- |
| Frontend | Vue 3, Vite, Ant Design Vue, Pinia, Vue Router |
| Backend | ASP.NET Core Web API, Entity Framework Core |
| Database | MySQL 8.4 |
| Realtime | SignalR |
| Xác thực | JWT, token version và phân quyền theo vai trò |
| File storage | Local volume hoặc S3-compatible (AWS S3/MinIO) |
| Kiểm thử | .NET Test, Node Test Runner, Playwright |
| Triển khai | Docker Compose, Nginx |

## Cấu trúc dự án

```text
TTTN_LABMANAGEMENT/
├── lab-frontend/          # Giao diện Vue 3
├── lab-backend/           # ASP.NET Core Web API
├── lab-backend.Tests/     # Unit/integration test backend
├── docs/                  # Tài liệu nghiệp vụ và kỹ thuật
├── scripts/               # Script backup/restore
├── docker-compose.yml     # MySQL, backend và frontend
├── .env.example           # Cấu hình môi trường mẫu
└── README.md
```

## Khởi chạy bằng Docker

### 1. Yêu cầu

- Docker Desktop hoặc Docker Engine.
- Docker Compose v2.
- Git.

### 2. Tạo file cấu hình

Windows PowerShell:

```powershell
Copy-Item .env.example .env
```

macOS/Linux:

```bash
cp .env.example .env
```

Mở `.env` và thay tối thiểu các giá trị sau:

```dotenv
MYSQL_ROOT_PASSWORD=replace_with_strong_root_password
MYSQL_PASSWORD=replace_with_strong_app_password
JWT_KEY=replace_with_random_secret_at_least_32_characters
SEED_DEFAULT_PASSWORD=replace_with_strong_seed_password
```

Nếu cần tài khoản mẫu để demo, đặt:

```dotenv
SEED_ENABLED=true
```

Không sử dụng mật khẩu mẫu khi triển khai thật.

### 3. Build và chạy hệ thống

```bash
docker compose config --quiet
docker compose up -d --build
docker compose ps
```

Nếu `.env` sử dụng `APP_PORT=8080`, truy cập:

```text
http://localhost:8080
```

*(Lưu ý: Nếu cần gọi trực tiếp API Backend trong môi trường dev, Backend sẽ tự động chạy ở cổng `8081` để tránh xung đột với Frontend).*

EF Core sẽ tự áp dụng migration khi `Database__ApplyMigrations=true`.

## Tài khoản seed

Khi `SEED_ENABLED=true`, hệ thống tạo các tài khoản sau:

| Tài khoản | Vai trò |
| --- | --- |
| `admin` | Admin |
| `truonglab` | Trưởng lab |
| `pholab` | Phó lab |
| `giangvien1` | Giảng viên |
| `sv1` | Sinh viên |

Tất cả sử dụng mật khẩu trong biến `SEED_DEFAULT_PASSWORD`. Sau lần đăng nhập đầu tiên, cần đổi mật khẩu và tắt seed trước khi triển khai production.

## Lệnh vận hành thường dùng

```bash
# Xem trạng thái container
docker compose ps

# Theo dõi log
docker compose logs -f backend
docker compose logs -f frontend
docker compose logs -f db

# Khởi động lại dịch vụ
docker compose restart backend
docker compose restart frontend

# Dừng hệ thống nhưng giữ dữ liệu
docker compose down
```

> Không chạy `docker compose down -v` trên hệ thống có dữ liệu thật. Tùy chọn `-v` sẽ xóa database, file upload và Data Protection keys trong Docker volume.

## Kiểm thử

Backend:

```bash
dotnet test lab-backend.Tests/LabManagementAPI.Tests.csproj --configuration Release
```

Frontend:

```bash
cd lab-frontend
npm ci
npm test
npm run build
```

Playwright E2E:

```bash
npx playwright install chromium
npm run test:e2e -- --project=chromium --project=mobile-chromium
```

Các flow cần dữ liệu seed sử dụng biến môi trường như `E2E_ADMIN_USERNAME`, `E2E_ADMIN_PASSWORD`, `E2E_BASE_URL` và `E2E_BUSINESS_FLOW`. Không ghi credential vào source code, log hoặc commit.

Kết quả kiểm thử gần nhất được lưu tại [docs/TEST_RESULTS.md](docs/TEST_RESULTS.md).

## Lưu trữ file

### Local volume

Mặc định:

```dotenv
STORAGE_PROVIDER=Local
```

File được lưu trong Docker volume `equipment_uploads`.

### S3 hoặc MinIO

```dotenv
STORAGE_PROVIDER=S3
STORAGE_S3_SERVICE_URL=http://minio:9000
STORAGE_S3_BUCKET_NAME=labmanagement
STORAGE_S3_REGION=us-east-1
STORAGE_S3_ACCESS_KEY=replace_with_access_key
STORAGE_S3_SECRET_KEY=replace_with_secret_key
STORAGE_S3_FORCE_PATH_STYLE=true
STORAGE_S3_KEY_PREFIX=labmanagement
```

Credential production phải được lưu bằng secret manager hoặc biến môi trường an toàn, không commit vào repository.

## Backup và khôi phục

Backup database, file upload và Data Protection keys:

```powershell
pwsh ./scripts/backup.ps1 -OutputDirectory ./backups
```

Khôi phục dữ liệu:

```powershell
pwsh ./scripts/restore.ps1 `
  -DatabaseBackup ./backups/lab-management-YYYYMMDD-HHmmss.sql `
  -UploadsArchive ./backups/equipment-uploads-YYYYMMDD-HHmmss.tar.gz `
  -DataProtectionArchive ./backups/data-protection-keys-YYYYMMDD-HHmmss.tar.gz `
  -ConfirmRestore
```

Restore sẽ ghi đè dữ liệu đích. Phải kiểm tra đúng server, database, Docker volume và tạo một bản backup dự phòng trước khi thực hiện.

Khi dùng S3/MinIO, SQL backup không chứa file object. Cần bật versioning/retention và backup bucket riêng.

Đọc đầy đủ [docs/BACKUP_RESTORE.md](docs/BACKUP_RESTORE.md) trước khi thao tác trên môi trường thật.

## Triển khai production

Trước khi đưa lên server/domain:

1. Đặt `APP_ORIGIN` thành domain HTTPS thực tế.
2. Chỉ public cổng frontend; không public MySQL hoặc backend trực tiếp.
3. Sử dụng Nginx, Caddy hoặc Traefik làm reverse proxy và cấp TLS.
4. Thay toàn bộ JWT key, mật khẩu database và mật khẩu tài khoản seed.
5. Đặt `SEED_ENABLED=false` sau khi hoàn tất dữ liệu ban đầu.
6. Cấu hình SMTP để dùng quên mật khẩu và email nhắc hạn.
7. Cấu hình S3/MinIO production hoặc backup volume upload định kỳ.
8. Mã hóa Data Protection keys bằng certificate được quản lý an toàn.
9. Thiết lập backup tự động, monitoring, cảnh báo lỗi và giám sát dung lượng.
10. Thực hiện restore drill trên staging trước khi nghiệm thu production.

Hướng dẫn chi tiết: [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md).

## Tài liệu dự án

| Tài liệu | Nội dung |
| --- | --- |
| [REQUIREMENTS.md](docs/REQUIREMENTS.md) | Yêu cầu nghiệp vụ |
| [USE_CASES.md](docs/USE_CASES.md) | Các use case chính |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Kiến trúc hệ thống |
| [DATABASE.md](docs/DATABASE.md) | Mô hình dữ liệu |
| [API.md](docs/API.md) | Danh sách API chính |
| [RBAC_MATRIX.md](docs/RBAC_MATRIX.md) | Ma trận phân quyền |
| [USER_GUIDE.md](docs/USER_GUIDE.md) | Hướng dẫn sử dụng |
| [TEST_PLAN.md](docs/TEST_PLAN.md) | Kế hoạch kiểm thử |
| [TEST_RESULTS.md](docs/TEST_RESULTS.md) | Kết quả kiểm thử |
| [DEPLOYMENT.md](docs/DEPLOYMENT.md) | Hướng dẫn triển khai |
| [BACKUP_RESTORE.md](docs/BACKUP_RESTORE.md) | Backup và khôi phục |

## Lưu ý bảo mật

- Không commit `.env`, mật khẩu, JWT key, certificate hoặc access key S3.
- Không đưa database backup có dữ liệu thật lên repository public.
- Không sử dụng tài khoản/mật khẩu seed trên production.
- Không public cổng MySQL ra Internet.
- Kiểm tra quyền truy cập file evidence trước khi bàn giao hệ thống.

## Phạm vi sử dụng

Repository hiện phù hợp cho đồ án, demo và chạy thử nội bộ. Trước khi vận hành chính thức cần hoàn tất cấu hình domain HTTPS, secret production, SMTP, object storage, backup tự động, monitoring và UAT đầy đủ với người dùng thật.
