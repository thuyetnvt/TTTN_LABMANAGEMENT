# Backup và restore

Backup database:

```bash
docker compose exec -T db mysqldump -u root -p lab_management > lab_management_backup.sql
```

Có thể dùng script chạy thật để backup cả database và volume upload:

```powershell
pwsh ./scripts/backup.ps1 -OutputDirectory ./backups
```

Khôi phục phải thực hiện trên môi trường đã xác nhận đúng volume đích và có backup dự phòng:

```powershell
pwsh ./scripts/restore.ps1 `
  -DatabaseBackup ./backups/lab-management-YYYYMMDD-HHmmss.sql `
  -UploadsArchive ./backups/equipment-uploads-YYYYMMDD-HHmmss.tar.gz `
  -ConfirmRestore
```

Script backup cũng lưu volume Data Protection nếu volume `backend_data_protection` tồn tại. Restore dừng backend/frontend, nạp SQL, thay nội dung volume upload và tùy chọn khôi phục key Data Protection rồi khởi động lại service:

```powershell
pwsh ./scripts/restore.ps1 `
  -DatabaseBackup ./backups/lab-management-YYYYMMDD-HHmmss.sql `
  -UploadsArchive ./backups/equipment-uploads-YYYYMMDD-HHmmss.tar.gz `
  -DataProtectionArchive ./backups/data-protection-keys-YYYYMMDD-HHmmss.tar.gz `
  -ConfirmRestore
```

Không chạy trên production nếu chưa xác nhận file backup, tên volume và cửa sổ bảo trì.

Backup thêm volume `equipment_uploads`. Khi khôi phục, tạo database trống, import SQL, khôi phục volume upload rồi chạy backend với `Database__ApplyMigrations=true`. Phải thử đăng nhập, mở file quyết định, xem tài sản và kiểm tra health sau restore. Không dùng `docker compose down -v` trên môi trường thật.

Để mã hóa key Data Protection khi triển khai thật, mount một certificate PFX ngoài image và đặt `DATA_PROTECTION_CERTIFICATE_PATH` cùng `DATA_PROTECTION_CERTIFICATE_PASSWORD`; code sẽ fail-fast nếu path đã cấu hình nhưng file không tồn tại. Local compose chỉ persist key vào volume để giữ phiên/token sau restart.
