# Backup và restore

Backup database:

```bash
docker compose exec -T db mysqldump -u root -p lab_management > lab_management_backup.sql
```

Backup thêm volume `equipment_uploads`. Khi khôi phục, tạo database trống, import SQL, khôi phục volume upload rồi chạy backend với `Database__ApplyMigrations=true`. Phải thử đăng nhập, mở file quyết định, xem tài sản và kiểm tra health sau restore. Không dùng `docker compose down -v` trên môi trường thật.

