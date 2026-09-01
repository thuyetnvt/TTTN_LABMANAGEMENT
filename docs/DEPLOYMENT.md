# Triển khai LabManagement

## Chạy local bằng Docker

`docker-compose.yml` chỉ công khai frontend. MySQL và backend chỉ có thể truy cập trong mạng Docker nội bộ.

Sau lần cập nhật cấu hình mạng đầu tiên, tạo lại container và network nhưng giữ nguyên volume dữ liệu:

```powershell
docker compose down
docker compose up -d --build
docker compose ps
```

Không thêm `-v` vào lệnh `down`, vì tùy chọn đó xóa database và file upload.

Frontend mặc định mở tại `http://localhost:8080`. Có thể đổi bằng `APP_PORT` trong `.env`. Health check đi qua frontend tại `/health`; backend không còn mở cổng riêng trên máy host.

Local cũng dùng key ring mới `backend_data_protection_v2`. Volume key cũ được giữ lại và không còn được mount; các liên kết reset mật khẩu tạo bằng key cũ có thể mất hiệu lực.

## Chuẩn bị VPS production

1. Trỏ bản ghi DNS `A` của domain về IPv4 VPS.
2. Chỉ mở firewall cho SSH, TCP 80, TCP 443 và UDP 443. Không mở 3306 hoặc 8080.
3. Sao chép `.env.production.example` thành `.env.production` và thay toàn bộ giá trị mẫu.
4. Tạo certificate PFX riêng để mã hóa Data Protection keys. Không dùng certificate HTTPS của Caddy cho mục đích này.

Ví dụ trên Ubuntu với OpenSSL:

```bash
sudo install -d -m 700 /opt/labmanagement/secrets
sudo openssl req -x509 -newkey rsa:4096 -sha256 -days 3650 -nodes \
  -subj "/CN=LabManagement Data Protection" \
  -keyout /opt/labmanagement/secrets/data-protection.key \
  -out /opt/labmanagement/secrets/data-protection.crt
sudo openssl pkcs12 -export \
  -out /opt/labmanagement/secrets/data-protection.pfx \
  -inkey /opt/labmanagement/secrets/data-protection.key \
  -in /opt/labmanagement/secrets/data-protection.crt
sudo chmod 600 /opt/labmanagement/secrets/data-protection.pfx
sudo rm /opt/labmanagement/secrets/data-protection.key
```

Mật khẩu nhập khi export PFX phải trùng với `DATA_PROTECTION_CERTIFICATE_PASSWORD` trong `.env.production`.

## Kiểm tra và khởi động production

```bash
docker compose --env-file .env.production -f docker-compose.prod.yml config
docker compose --env-file .env.production -f docker-compose.prod.yml up -d --build
docker compose --env-file .env.production -f docker-compose.prod.yml ps
```

Caddy tự lấy và gia hạn certificate HTTPS cho `APP_DOMAIN`. Domain phải truy cập được từ Internet và cổng 80/443 phải đi tới VPS.

Sau khi khởi động:

```bash
curl -I https://your-domain.example
curl https://your-domain.example/health
```

Trong kết quả `docker compose ps`, chỉ service `caddy` được publish cổng host. MySQL, backend và frontend không được có địa chỉ dạng `0.0.0.0:...`.

## Rotate Data Protection key

Khóa từng bị commit trong repository được coi là đã lộ. Cấu hình production dùng volume mới `backend_data_protection_v2`, vì vậy lần triển khai mới sẽ tự tạo key ring mới và mã hóa nó bằng PFX.

Hệ quả mong đợi của rotate:

- Các liên kết reset mật khẩu hoặc payload được bảo vệ bằng khóa cũ có thể mất hiệu lực.
- Database, tài sản và file upload không bị xóa.
- Không sao chép file XML cũ vào volume `backend_data_protection_v2`.

Sau khi xác nhận hệ thống mới hoạt động và đã có backup, volume key cũ có thể được lưu trữ ngoại tuyến hoặc xóa theo chính sách vận hành.

## Cập nhật ứng dụng

Trước khi cập nhật production:

1. Chạy backup database, upload và key ring.
2. Kiểm tra migration trên bản sao database.
3. Đặt `APPLY_MIGRATIONS=true` cho lần triển khai cần migration.
4. Sau khi database đã cập nhật ổn định, đặt `APPLY_MIGRATIONS=false` để các lần restart thông thường không tự thay đổi schema.
5. Kiểm tra đăng nhập, SignalR, upload/download, báo cáo và `/health`.

Production luôn giữ `SEED_ENABLED=false`. SMTP phải được cấu hình nếu sử dụng quên mật khẩu và gửi email nhắc trả.

## Tác vụ tự động

Backend kiểm tra định kỳ để tự sinh phiếu bảo trì đến hạn và tạo thông báo nhắc trả trước hạn/quá hạn. Mỗi lần xử lý được ghi vào `AutomationDispatches` với khóa chống gửi trùng.

- `AUTOMATION_ENABLED=true`: bật tác vụ nền.
- `AUTOMATION_POLL_MINUTES=5`: chu kỳ kiểm tra.
- `RETURN_REMINDER_DAYS_BEFORE=3`: nhắc trước hạn ba ngày; phiếu quá hạn được nhắc một lần mỗi ngày.
- `AUTOMATION_SEND_EMAIL_REMINDERS=false`: thông báo trong ứng dụng vẫn chạy; chỉ đổi thành `true` sau khi SMTP hoạt động.

Kiểm tra log sau khi triển khai và xác nhận tài khoản thử nhận đúng một thông báo cho mỗi mốc thời gian, không bị lặp sau khi restart container.
