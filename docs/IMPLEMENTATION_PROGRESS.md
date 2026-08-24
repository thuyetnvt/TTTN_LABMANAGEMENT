# Implementation progress

## 2026-08-24 — Baseline

- Đã tạo branch `codex/iot-lab-asset-upgrade` từ commit `5f01644`.
- Đã chạy `dotnet restore` và `dotnet build`: đạt, 0 warning/0 error.
- Đã kiểm tra Docker Compose: config hợp lệ; db/backend/frontend đang healthy.
- Đã kiểm tra health qua Nginx: `/health` trả HTTP 200 tại `localhost:8081`.
- Frontend `npm ci --ignore-scripts` bị treo không có output; chưa kết luận frontend build đạt.
- Đã ghi baseline chi tiết tại `docs/BASELINE_AUDIT.md`.

## 2026-08-24 — Giai đoạn 1: status và P0 defaults

- Commit `dd2c0b3`: `fix: standardize statuses and harden defaults`.
- Backend đã dùng status code tập trung cho tài sản, mượn trả, vật tư, bảo trì và bồi thường.
- Đã thêm migration `20260824164926_StandardizeBusinessStatusCodes` để chuyển status tiếng Việt cũ và giữ khả năng rollback.
- Đã thêm `StatusBadge` và map status/role ở frontend; các màn hình thiết bị, mượn trả, vật tư, bảo trì, bồi thường và audit không còn hiển thị code thô.
- Sinh viên đã bị backend bắt buộc chọn giảng viên bảo lãnh.
- Docker Compose mặc định không publish MySQL, tắt seed và không tin toàn bộ forwarded headers.
- Kiểm chứng: backend build đạt 0 warning/0 error; frontend build đạt, còn warning chunk lớn và annotation từ dependency SignalR.

## Đang làm tiếp

- Thêm metadata tài sản IoT, QR token và cây vị trí có migration an toàn.

## Quy tắc tiếp tục

- Không sửa hoặc push trực tiếp `main`.
- Không xoá migration/file local chưa phân loại.
- Mỗi giai đoạn phải chạy kiểm chứng thật và cập nhật tài liệu này.
