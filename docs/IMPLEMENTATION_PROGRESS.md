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

- ~~Thêm metadata tài sản IoT, QR token và cây vị trí có migration an toàn.~~ Đã hoàn thành.

## 2026-08-25 — Giai đoạn 2: tài sản IoT và vị trí

- Commit `693a3b4`: `feat: add IoT asset metadata and structured locations`.
- `Equipment` đã có mã tài sản, QR token ngẫu nhiên, loại thiết bị, MAC/IMEI/firmware tùy chọn, nhà sản xuất/nhà cung cấp, nguồn kinh phí, giá trị mua, ghi chú và ngày kiểm kê.
- Đã thêm `LocationNode` dạng cây, API CRUD có kiểm tra mã trùng, parent tồn tại và vòng lặp, cùng màn hình quản lý vị trí thật ở frontend.
- Migration `20260824165820_AddIoTAssetMetadataAndLocations` tạo schema mới, gán mã/QR cho tài sản cũ và đưa vị trí tự do cũ vào node `LEGACY` để không mất dữ liệu.
- Backend build và frontend production build đạt; frontend vẫn có warning từ dependency SignalR/chunk lớn.

## 2026-08-25 — Giai đoạn 3: phiếu mượn nhiều tài sản

- Commit `6f070fa`: `feat: refactor multi-asset borrowing workflow`.
- Phiếu mượn hỗ trợ nhiều tài sản, giữ `EquipmentId` cho client cũ và chống trùng tài sản trong cùng phiếu.
- Đã thêm trạng thái chi tiết từng món, lịch sử chuyển trạng thái, ghi chú duyệt của giảng viên/quản lý và migration backfill dữ liệu cũ.
- Duyệt kho claim toàn bộ tài sản trong transaction; trả tài sản cho phép xử lý từng món, cập nhật bảo hành/bảo trì/bồi thường riêng.
- Frontend đã có giỏ chọn nhiều tài sản, duyệt bảo lãnh bắt buộc ghi chú và form kiểm tra trả theo từng món.
- Kiểm chứng: backend build đạt 0 warning/0 error; frontend production build đạt, còn warning chunk lớn và annotation từ dependency SignalR.

## Đang làm tiếp

- QR/kiểm kê/bảo trì, vật tư có transaction, notification realtime và hardening các màn hình còn lại.

## Quy tắc tiếp tục

- Không sửa hoặc push trực tiếp `main`.
- Không xoá migration/file local chưa phân loại.
- Mỗi giai đoạn phải chạy kiểm chứng thật và cập nhật tài liệu này.
