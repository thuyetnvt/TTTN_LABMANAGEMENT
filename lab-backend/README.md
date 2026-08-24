# LabManagement API

Backend ASP.NET Core Web API cho hệ thống quản lý phòng lab.

## Công nghệ

- ASP.NET Core Web API, C#, .NET 9
- Entity Framework Core 9 + Pomelo MySQL
- JWT Bearer, BCrypt
- SignalR, MailKit, EPPlus
- MySQL 8

## Những điểm đã được hoàn thiện

- Không còn cơ chế đăng nhập dự phòng bằng tài khoản test khi database lỗi.
- Không lưu mật khẩu MySQL, JWT key hay SMTP credential trong source code.
- JWT kiểm tra trạng thái tài khoản, vai trò và `TokenVersion` ở mỗi request.
- Có đổi mật khẩu, quên mật khẩu bằng token SHA-256, hết hạn 30 phút và chỉ dùng một lần.
- Xóa tài khoản là khóa mềm, không xóa lịch sử nghiệp vụ.
- Duyệt mượn và cấp vật tư dùng transaction + atomic update, tránh duyệt trùng và tồn kho âm.
- Upload quyết định có giới hạn kích thước, whitelist phần mở rộng, tên lưu ngẫu nhiên và đường dẫn tải an toàn.
- Bảo trì có vòng đời `MAINTENANCE_IN_PROGRESS` → `MAINTENANCE_COMPLETED`; khi hoàn tất người xử lý phải chọn trạng thái thiết bị tiếp theo (`AVAILABLE`, `BROKEN`, `UNDER_WARRANTY` hoặc tiếp tục bảo trì).
- Phiếu mượn hỗ trợ nhiều tài sản, lịch sử trạng thái, trả từng món và kiểm kê QR theo đợt.
- Thông báo được lưu database; SignalR chỉ dùng để đẩy cập nhật realtime.
- Có audit log, rate limit đăng nhập, health check MySQL và SignalR theo đúng người/nhóm quyền.
- Database được nâng cấp bằng EF migration, không tự `ALTER TABLE` thủ công khi khởi động.

## Chạy local

Yêu cầu:

- .NET SDK 9
- MySQL 8

Tạo database và user riêng, không dùng tài khoản `root` cho ứng dụng:

```sql
CREATE DATABASE lab_management
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;

CREATE USER 'lab_app'@'localhost' IDENTIFIED BY 'YOUR_STRONG_PASSWORD';
GRANT ALL PRIVILEGES ON lab_management.* TO 'lab_app'@'localhost';
FLUSH PRIVILEGES;
```

Sao chép file cấu hình mẫu:

```bash
cp appsettings.Development.example.json appsettings.Development.json
```

Sửa connection string, JWT key và cấu hình email trong file vừa tạo, sau đó:

```bash
dotnet restore
dotnet run
```

Ứng dụng tự chạy migration khi khởi động. Swagger chỉ mở trong môi trường Development:

- API: `http://localhost:5248`
- Swagger: `http://localhost:5248/swagger`
- Health check: `http://localhost:5248/health`

## Tài khoản demo

Khi `Seed:Enabled=true`, hệ thống chỉ tạo tài khoản còn thiếu và không đổi mật khẩu của tài khoản đã tồn tại:

| Tài khoản | Vai trò |
|---|---|
| `admin` | Admin |
| `truonglab` | Trưởng lab |
| `pholab` | Phó lab |
| `giangvien1` | Giảng viên |
| `sv1` | Sinh viên |

Mật khẩu lấy từ `Seed:DefaultPassword` và phải có ít nhất 8 ký tự. Khi triển khai thật:

1. Đăng nhập lần đầu và đổi mật khẩu bằng menu avatar.
2. Đặt `Seed__Enabled=false`.
3. Không đưa mật khẩu demo vào báo cáo công khai hoặc commit Git.

## Biến môi trường production

Các biến bắt buộc hoặc nên cấu hình:

```dotenv
ConnectionStrings__DefaultConnection=Server=db;Port=3306;Database=lab_management;Uid=lab_app;Pwd=...
Jwt__Key=RANDOM_SECRET_AT_LEAST_32_CHARACTERS
Jwt__Issuer=LabManagementAPI
Jwt__Audience=LabManagementApp
App__FrontendBaseUrl=https://lab.example.edu.vn
Cors__AllowedOrigins__0=https://lab.example.edu.vn
Email__Host=smtp.example.edu.vn
Email__Port=587
Email__UseStartTls=true
Email__Username=...
Email__Password=...
Email__FromEmail=noreply@example.edu.vn
Seed__Enabled=false
Security__UseHttpsRedirection=true
```

Sinh secret JWT bằng trình tạo số ngẫu nhiên an toàn, ví dụ:

```bash
openssl rand -base64 48
```

## Chạy container backend

```bash
docker build -t labmanagement-api .
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection='Server=host.docker.internal;Port=3306;Database=lab_management;Uid=lab_app;Pwd=YOUR_PASSWORD;' \
  -e Jwt__Key='YOUR_RANDOM_SECRET_AT_LEAST_32_CHARACTERS' \
  -e Security__UseHttpsRedirection=false \
  -v lab_uploads:/app/uploads \
  labmanagement-api
```

File quyết định được lưu trong `/app/uploads`; volume này phải được backup cùng database.

## Quy tắc nghiệp vụ cần giữ

- Trạng thái thiết bị chỉ gồm: `Rảnh`, `Đang mượn`, `Hỏng`, `Bảo hành`.
- Thiết bị đang mượn chỉ đổi trạng thái qua quy trình trả.
- Thiết bị đang có phiếu bảo trì chỉ về `Rảnh` qua thao tác hoàn tất bảo trì.
- Thiết bị/vật tư đã có lịch sử nghiệp vụ không được xóa cứng.
- Sinh viên và giảng viên chỉ xem phiếu mượn, cấp phát và bồi thường của mình.
- Chỉ Admin/Trưởng lab/Phó lab được duyệt mượn, cấp vật tư, bảo trì và xác nhận bồi thường.

## Checklist trước khi nghiệm thu

- Chạy migration trên bản sao database và xử lý dữ liệu trùng `Username`/`Serial` nếu migration báo lỗi unique.
- Thử đủ 5 vai trò, đặc biệt kiểm tra API trả `403` khi truy cập sai quyền.
- Thử hai trình duyệt duyệt cùng một phiếu để xác nhận chỉ một thao tác thành công.
- Upload/tải file quyết định, kiểm tra volume và quy trình backup/restore.
- Cấu hình SMTP thật và thử quên mật khẩu, nhắc trả.
- Dùng HTTPS ở reverse proxy, không mở cổng backend hoặc MySQL trực tiếp ra Internet.
- Đổi tất cả mật khẩu/secret demo trước khi đưa vào sử dụng.
- Thiết lập backup MySQL hằng ngày và diễn tập khôi phục.

## Lưu ý giấy phép và vòng đời

EPPlus 8 trong source được cấu hình theo giấy phép phi thương mại cho dự án giáo dục. Nếu triển khai cho mục đích thương mại, cần mua giấy phép EPPlus hoặc thay thư viện xuất Excel phù hợp.

.NET 9 là bản STS. Sau đợt nghiệm thu nên lên kế hoạch nâng sang .NET LTS kế tiếp và kiểm tra tương thích EF Core/Pomelo trước khi nâng.
