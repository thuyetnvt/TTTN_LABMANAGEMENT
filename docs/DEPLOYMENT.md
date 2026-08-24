# Triển khai

1. Sao chép `.env.example` thành `.env`, đặt mật khẩu MySQL, JWT key ngẫu nhiên tối thiểu 32 ký tự và mật khẩu seed.
2. Đặt `APP_ORIGIN` là domain HTTPS; chỉ publish cổng frontend.
3. Chạy `docker compose up -d --build`, kiểm tra `docker compose ps` và `/health`.
4. Production giữ `SEED_ENABLED=false`, cấu hình SMTP nếu dùng quên mật khẩu/nhắc trả. Có thể dùng local volume hoặc S3/MinIO bằng `STORAGE_PROVIDER=S3`; khi dùng S3 phải cấu hình bucket, endpoint/region và credentials qua secret manager.
5. Migration tự chạy khi `Database__ApplyMigrations=true`; nên chạy thử trên bản sao trước.
