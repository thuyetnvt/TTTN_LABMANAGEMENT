# Hướng dẫn cài đặt và triển khai LabManagement

Tài liệu này dành cho người tiếp nhận mã nguồn và quản trị hệ thống. Nội dung hướng dẫn cách tải mã nguồn từ GitHub, chạy hệ thống bằng Docker, kiểm tra sau cài đặt, cập nhật phiên bản và triển khai lên VPS.

Docker Compose là cách chạy được khuyến nghị vì không cần cài riêng .NET, Node.js và MySQL.

## 1. Thông tin dự án

| Nội dung | Thông tin |
| --- | --- |
| Tên hệ thống | LabManagement - Quản lý tài sản và thiết bị Phòng Lab IoT |
| Mã nguồn | [github.com/thuyetnvt/TTTN_LABMANAGEMENT](https://github.com/thuyetnvt/TTTN_LABMANAGEMENT) |
| Frontend | Vue 3, Vite, Ant Design Vue |
| Backend | ASP.NET Core Web API, Entity Framework Core |
| Cơ sở dữ liệu | MySQL 8.4 |
| Cách chạy khuyến nghị | Docker Compose v2 |

## 2. Chuẩn bị

- Máy tính đã cài Git và Docker Desktop; Docker Desktop phải ở trạng thái Running.
- Có quyền đọc repository GitHub. Nếu repository riêng tư, đăng nhập GitHub trước khi tải.
- Bảo đảm các cổng dự kiến sử dụng chưa bị ứng dụng khác chiếm dụng.
- Không sao chép mật khẩu, JWT key hoặc file `.env` lên GitHub.

## 3. Tải mã nguồn từ GitHub

### 3.1. Tải bằng Git

1. Mở PowerShell tại thư mục sẽ lưu dự án.
2. Clone repository và mở thư mục dự án:

```powershell
git clone https://github.com/thuyetnvt/TTTN_LABMANAGEMENT.git
cd TTTN_LABMANAGEMENT
```

3. Kiểm tra nhánh. Nhánh dùng để chạy chính thức là `main`.

```powershell
git branch --show-current
git status
```

### 3.2. Tải bằng Download ZIP

Mở trang GitHub của dự án, chọn **Code**, chọn **Download ZIP**, giải nén rồi mở PowerShell tại thư mục vừa giải nén. Cách này phù hợp để xem hoặc chạy thử; nên dùng Git nếu cần cập nhật phiên bản thường xuyên.

## 4. Chạy hệ thống trên máy cá nhân

### Bước 1. Tạo file cấu hình

Sao chép file mẫu thành `.env`:

```powershell
Copy-Item .env.example .env
```

### Bước 2. Đổi các giá trị bảo mật

Mở `.env` và thay tối thiểu các biến sau:

```dotenv
MYSQL_ROOT_PASSWORD=<mat_khau_root_manh>
MYSQL_PASSWORD=<mat_khau_ung_dung_manh>
JWT_KEY=<chuoi_ngau_nhien_toi_thieu_32_ky_tu>
JWT_REFRESH_TOKEN_DAYS=7
SEED_ENABLED=true
SEED_DEFAULT_PASSWORD=<mat_khau_demo_manh>
```

`SEED_ENABLED=true` chỉ dùng khi cần tạo tài khoản mẫu lần đầu. Sau khi đăng nhập và đổi mật khẩu, đặt lại thành `false`.

### Bước 3. Kiểm tra cấu hình Docker

Lệnh sau phải kết thúc mà không báo lỗi:

```powershell
docker compose config --quiet
```

### Bước 4. Build và khởi động

Lần đầu có thể mất vài phút để tải image và build ứng dụng.

```powershell
docker compose up -d --build
docker compose ps
```

### Bước 5. Mở hệ thống

Truy cập địa chỉ mặc định: [http://localhost:8080](http://localhost:8080).

## 5. Kiểm tra sau khi cài đặt

| STT | Nội dung kiểm tra | Kết quả mong đợi |
| ---: | --- | --- |
| 1 | `docker compose ps` | Các service ở trạng thái `Up` hoặc `healthy` |
| 2 | Mở `http://localhost:8080` | Trang đăng nhập hiển thị bình thường |
| 3 | Đăng nhập tài khoản mẫu | Hệ thống yêu cầu đổi mật khẩu lần đầu |
| 4 | Mở thiết bị, mượn trả và báo cáo | Không có lỗi tải dữ liệu hoặc lỗi 403 sai quyền |
| 5 | Mở `/health` qua frontend | Backend và database phản hồi khỏe |

## 6. Các lệnh vận hành thường dùng

| Mục đích | Lệnh |
| --- | --- |
| Xem trạng thái | `docker compose ps` |
| Xem log backend | `docker compose logs -f backend` |
| Xem log frontend | `docker compose logs -f frontend` |
| Khởi động lại | `docker compose restart backend frontend` |
| Dừng nhưng giữ dữ liệu | `docker compose down` |
| Build lại sau khi sửa code | `docker compose up -d --build` |

> **Lưu ý quan trọng:** Không chạy `docker compose down -v` trên hệ thống có dữ liệu thật vì tùy chọn `-v` sẽ xóa các volume dữ liệu.

## 7. Cập nhật mã nguồn

Trước khi cập nhật, phải sao lưu database, file upload và khóa Data Protection.

```powershell
git status
git pull --ff-only origin main
docker compose up -d --build
docker compose ps
```

Nếu `git status` hiển thị file đã sửa trên máy chủ, không tự xóa hoặc ghi đè. Cần sao lưu thay đổi đó và xử lý cùng người quản trị repository.

## 8. Triển khai trên VPS

### 8.1. Kiểm tra quyền truy cập

```bash
ssh -p <SSH_PORT> <USERNAME>@<SERVER_IP>
cd /lab
docker ps
```

Nếu `docker ps` hiển thị danh sách container thì tài khoản đã có quyền đọc Docker. Nếu báo `permission denied`, liên hệ quản trị viên máy chủ để cấp quyền; không tự dùng `sudo` khi chưa được phép.

### 8.2. Triển khai bằng GitHub Actions

Repository đã có workflow CI. Khi push lên nhánh triển khai, hệ thống chạy kiểm thử backend, frontend, E2E và build Docker. Job **Deploy VPS** chỉ chạy khi các job trước thành công và GitHub Secrets đã được cấu hình.

| Secret | Ý nghĩa |
| --- | --- |
| `VPS_HOST` | Địa chỉ máy chủ |
| `VPS_USER` | Tài khoản SSH |
| `VPS_PORT` | Cổng SSH |
| `VPS_SSH_KEY` | Private key dùng riêng cho CI |
| `VPS_DEPLOY_DIR` | Thư mục dự án trên VPS; máy chủ hiện tại dùng `/lab` |
| `SMTP_USERNAME` / `SMTP_PASSWORD` | Tài khoản gửi email nếu bật SMTP |
| `GOOGLE_CLIENT_ID` | Client ID nếu bật đăng nhập Google |

Sau khi đã kiểm tra các file thay đổi:

```powershell
git add <cac_file_da_kiem_tra>
git commit -m "Mo ta noi dung cap nhat"
git push origin main
```

Chỉ xác nhận triển khai thành công khi toàn bộ workflow màu xanh, `docker compose ps` trên VPS ổn định và trang `/health` phản hồi bình thường.

### 8.3. Kiểm tra bản Git sạch trước khi đẩy

Chạy kiểm tra từ thư mục gốc. Chỉ tạo commit khi toàn bộ lệnh kết thúc thành công và danh sách file không chứa secret, file cấu hình local hoặc kết quả build.

```powershell
git status --short
git diff --check
dotnet test .\lab-backend.Tests\LabManagementAPI.Tests.csproj --configuration Release
Set-Location .\lab-frontend
npm ci
npm test
npm run build
Set-Location ..
```

Kiểm tra nội dung chuẩn bị commit rồi mới đẩy:

```powershell
git add -A
git diff --cached --check
git diff --cached --stat
git commit -m "Mo ta noi dung cap nhat"
git status --short
git push origin main
```

Sau khi commit, `git status --short` phải không in ra dòng nào. Nếu còn file ngoài phạm vi bàn giao, dừng lại và kiểm tra trước khi push.

## 9. Xử lý lỗi thường gặp

| Hiện tượng | Cách xử lý |
| --- | --- |
| `SSH connection timed out` | Kiểm tra lại mạng, IP, cổng SSH và firewall phía VPS; dùng `Test-NetConnection <IP> -Port <PORT>`. |
| `docker ps` báo `permission denied` | Tài khoản chưa có quyền Docker; liên hệ quản trị viên máy chủ. |
| Cổng 8080 đã được sử dụng | Đổi `APP_PORT` trong `.env` hoặc dừng ứng dụng đang chiếm cổng. |
| Backend không kết nối MySQL | Kiểm tra `MYSQL_DATABASE`, `MYSQL_USER`, `MYSQL_PASSWORD` và log service `db`/`backend`. |
| Đăng nhập xong bị trả về trang login | Kiểm tra `JWT_KEY`, thời gian máy chủ và migration bảng `RefreshTokens`. |
| Migration lỗi | Sao lưu database, đọc log migration; không sửa schema production thủ công khi chưa có bản sao. |

## 10. Checklist bàn giao

- [ ] Mã nguồn đã được push lên GitHub và workflow kiểm thử đạt.
- [ ] File `.env` và các secret không nằm trong commit.
- [ ] Database, upload và Data Protection keys đã có bản sao lưu.
- [ ] Năm vai trò đã được kiểm tra: Admin, Trưởng lab, Phó lab, Giảng viên và Sinh viên.
- [ ] Luồng mượn, duyệt, bàn giao, xác nhận nhận, trả và cấp phát đã được thử.
- [ ] Import/export Excel, báo cáo PDF, thông báo và quên mật khẩu đã được kiểm tra.
- [ ] Domain HTTPS và `/health` hoạt động trước khi nghiệm thu.

## Tài liệu liên quan

- [README tổng quan](../README.md)
- [Hướng dẫn triển khai chi tiết](DEPLOYMENT.md)
- [Hướng dẫn backup và khôi phục](BACKUP_RESTORE.md)
- [Kế hoạch kiểm thử](TEST_PLAN.md)
- [Kết quả kiểm thử](TEST_RESULTS.md)
- [Hướng dẫn sử dụng](USER_GUIDE.md)
