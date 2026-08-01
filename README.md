# LabManagement — bộ triển khai full-stack

Bộ này gồm:

- `lab-frontend`: Vue 3 + Vite + Ant Design Vue
- `lab-backend`: ASP.NET Core Web API + EF Core
- `docker-compose.yml`: frontend, backend và MySQL 8.4

## Khởi chạy nhanh

Yêu cầu máy đã cài Docker Engine/Desktop và Docker Compose.

```bash
cp .env.example .env
```

Mở `.env` và thay ít nhất:

- `MYSQL_ROOT_PASSWORD`
- `MYSQL_PASSWORD`
- `JWT_KEY` (ít nhất 32 ký tự ngẫu nhiên)
- `SEED_DEFAULT_PASSWORD` (ít nhất 8 ký tự)

Sau đó:

```bash
docker compose up -d --build
docker compose ps
```

Truy cập `http://localhost:8080` (hoặc cổng đặt trong `APP_PORT`).

Lần chạy đầu, EF Core tự tạo/nâng cấp database. Nếu `SEED_ENABLED=true`, có 5 tài khoản:

| Tài khoản | Vai trò |
|---|---|
| `admin` | Admin |
| `truonglab` | Trưởng lab |
| `pholab` | Phó lab |
| `giangvien1` | Giảng viên |
| `sv1` | Sinh viên |

Tất cả dùng mật khẩu trong `SEED_DEFAULT_PASSWORD`. Đăng nhập xong, bấm avatar góc phải để đổi mật khẩu.

## Lệnh vận hành

```bash
docker compose logs -f backend
docker compose logs -f frontend
docker compose restart backend
docker compose down
```

Không dùng `docker compose down -v` trên hệ thống thật vì tùy chọn `-v` xóa volume database và file upload.

## Backup

Database:

```bash
docker compose exec -T db \
  mysqldump -u root -p lab_management > lab_management_backup.sql
```

Khi chạy lệnh, MySQL sẽ yêu cầu mật khẩu root. Ngoài database, phải backup volume `equipment_uploads` vì volume chứa file quyết định mua/thêm thiết bị.

Nên dùng lịch backup tự động hằng ngày, lưu một bản ngoài server và thử khôi phục định kỳ.

## Đưa lên server/domain

1. Đổi `APP_ORIGIN` thành domain HTTPS, ví dụ `https://lab.example.edu.vn`.
2. Chỉ mở cổng frontend; không publish cổng MySQL hoặc backend ra Internet.
3. Đặt Nginx/Caddy/Traefik phía trước để cấp TLS.
4. Cấu hình SMTP thật để dùng quên mật khẩu và nhắc trả.
5. Đăng nhập đổi toàn bộ mật khẩu seed, sau đó đặt `SEED_ENABLED=false`.
6. Thiết lập firewall, backup, giám sát dung lượng và log.

## Kịch bản nghiệm thu nên quay video/chụp minh chứng

1. Đăng nhập đủ 5 vai trò và chứng minh menu/API bị chặn đúng quyền.
2. Admin tạo danh mục, thêm thiết bị kèm file quyết định và quét QR.
3. Sinh viên gửi mượn có giảng viên bảo lãnh → giảng viên duyệt → quản lý duyệt.
4. Trả tốt → thiết bị về `Rảnh`.
5. Trả hỏng còn bảo hành → tạo bảo trì → hoàn tất → thiết bị về `Rảnh`.
6. Trả hỏng hết bảo hành → tạo bồi thường → xác nhận đã thanh toán.
7. Gửi yêu cầu vật tư → duyệt → tồn kho giảm, không thể âm.
8. Thử hai trình duyệt duyệt cùng một phiếu; chỉ một request được chấp nhận.
9. Thử quên/đổi mật khẩu và kiểm tra token cũ bị từ chối.
10. Restart toàn bộ container, xác nhận dữ liệu và file upload vẫn còn.

## Mức điểm 9–10

Code và Docker chỉ là điều kiện cần. Để chứng minh “áp dụng được thật”, nên có thêm:

- domain HTTPS hoặc server của trường;
- dữ liệu tài sản thật đã được chuẩn hóa;
- biên bản người dùng thử/nghiệm thu của giảng viên hoặc người quản lý lab;
- quy trình phân quyền, bàn giao, backup/restore;
- minh chứng chạy ổn định và sửa lỗi sau thử nghiệm thực tế.

Không đưa file `.env`, mật khẩu, JWT key hoặc database backup có dữ liệu thật lên Git/public.
