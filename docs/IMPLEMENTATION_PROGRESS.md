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
- Form tài sản đã chọn `LocationNode` từ dữ liệu thật; backend bắt buộc node hoạt động và lưu lịch sử điều chuyển (`EquipmentLocationHistory`) kèm người thực hiện, thời gian, lý do.
- Migration `20260824165820_AddIoTAssetMetadataAndLocations` tạo schema mới, gán mã/QR cho tài sản cũ và đưa vị trí tự do cũ vào node `LEGACY` để không mất dữ liệu.
- Backend build và frontend production build đạt; frontend vẫn có warning từ dependency SignalR/chunk lớn.

## 2026-08-25 — Giai đoạn 3: phiếu mượn nhiều tài sản

- Commit `6f070fa`: `feat: refactor multi-asset borrowing workflow`.
- Phiếu mượn hỗ trợ nhiều tài sản, giữ `EquipmentId` cho client cũ và chống trùng tài sản trong cùng phiếu.
- Đã thêm trạng thái chi tiết từng món, lịch sử chuyển trạng thái, ghi chú duyệt của giảng viên/quản lý và migration backfill dữ liệu cũ.
- Duyệt kho claim toàn bộ tài sản trong transaction; trả tài sản cho phép xử lý từng món, cập nhật bảo hành/bảo trì/bồi thường riêng.
- Frontend đã có giỏ chọn nhiều tài sản, duyệt bảo lãnh bắt buộc ghi chú và form kiểm tra trả theo từng món.
- Kiểm chứng: backend build đạt 0 warning/0 error; frontend production build đạt, còn warning chunk lớn và annotation từ dependency SignalR.

## 2026-08-25 — Giai đoạn 4: QR và kiểm kê

- Commit `42b1834`: `feat: add QR inventory management`.
- Đã thêm đợt kiểm kê, phạm vi theo vị trí/danh mục, danh sách tài sản dự kiến, quét QR token, kết quả tìm thấy/sai vị trí/hỏng/thiếu và tiến độ.
- QR tài sản dùng `QrToken` ngẫu nhiên; API kiểm kê kiểm tra token thuộc đúng đợt trước khi ghi nhận.
- Có màn hình kiểm kê thật và nút ghi nhận kiểm kê từng tài sản.

## 2026-08-25 — Giai đoạn 5: thông báo và bảo trì

- Commit `1b0d204`: `feat: persist realtime notifications`.
- Thông báo mượn/vật tư được lưu database, có API danh sách, unread count, đọc từng thông báo và đọc tất cả; SignalR chỉ còn là kênh realtime.
- Có chuông thông báo và màn hình lịch sử thông báo.
- Commit `c5972fa`: `feat: make maintenance outcomes explicit`.
- Khi hoàn tất bảo trì, người xử lý phải chọn trạng thái tiếp theo: rảnh, hỏng, bảo hành hoặc tiếp tục bảo trì; hệ thống không tự ép về `AVAILABLE`.

## Đang làm tiếp

- Hardening kho vật tư, kiểm tra migration/Docker, bổ sung tài liệu và test/kiểm chứng cuối.

## 2026-08-25 — Giai đoạn 6: hardening và tài liệu

- Commit `94aa048`: `fix: harden stock updates and file validation`.
- Cập nhật tồn kho trực tiếp trong transaction Serializable; upload quyết định kiểm tra kích thước, phần mở rộng và magic bytes, tên lưu vẫn ngẫu nhiên.
- Frontend giữ lại message lỗi cụ thể từ API thay vì thay bằng thông báo chung.
- Đã thêm workflow `.github/workflows/ci.yml` cho restore/build backend, migration script, npm build và Docker build.
- Đã bổ sung bộ tài liệu đồ án tại `docs/`: yêu cầu, use case, kiến trúc, database, API, RBAC, deployment, backup/restore, test plan, test results và user guide.

## 2026-08-25 — Giai đoạn 7: bàn giao tài sản

- Commit `66f0b39`: `feat: add handover records for borrowed assets`.
- Đã thêm `HandoverRecord` và `HandoverItem`, lưu mã bàn giao, người giao/nhận, thời điểm xác nhận, tình trạng từng tài sản, phụ kiện và ghi chú.
- Quản lý có thể tạo bàn giao cho toàn bộ tài sản trong phiếu đã mượn; hệ thống chống thiếu/trùng tài sản, chống bàn giao lặp và ghi audit/notification.
- Frontend đã có form `Bàn giao` theo từng tài sản.
- Đã thêm `HandoverEvidence`, storage abstraction `IFileStorage`/`LocalFileStorage`, upload/download/delete evidence có xác thực, tên ngẫu nhiên và kiểm tra magic bytes.
- Frontend cho phép đính kèm ảnh/tài liệu/chữ ký điện tử khi lập biên bản.

## 2026-08-25 — Kiểm chứng sau giai đoạn 7

- Backend build `dotnet build --no-restore`: đạt, 0 warning/0 error.
- Frontend `npm run build`: đạt; còn warning dependency SignalR về `PURE` annotation và chunk lớn.
- `docker compose up -d --build backend frontend`: đạt; backend, frontend và database healthy.
- Migration `20260824173650_AddHandoverRecords` đã áp dụng được trên database local có chuỗi migration legacy; `/health` trả `Healthy`.

## 2026-08-25 — Giai đoạn 8: bảo trì định kỳ

- Đã thêm `MaintenanceSchedule`, migration `20260824174338_AddMaintenanceSchedules` và API quản lý kế hoạch theo thiết bị.
- Quản lý có thể đặt chu kỳ/ngày đến hạn, bật tắt kế hoạch và tạo phiếu bảo trì từ kế hoạch; ngày kế tiếp được tự động tính lại sau khi phát sinh phiếu.
- Frontend đã có màn hình `Bảo trì định kỳ`, cảnh báo kế hoạch đến hạn và thao tác tạo phiếu.

## 2026-08-25 — Giai đoạn 9: import, QR, báo cáo và kiểm thử

- Đã thêm `POST /equipment/import/preview` đọc file `.xlsx`, kiểm tra cột bắt buộc, trùng serial/mã tài sản và trả lỗi theo từng dòng; `POST /equipment/import` chỉ ghi các dòng đã xem trước hợp lệ.
- Danh sách tài sản hỗ trợ chọn nhiều dòng và in QR hàng loạt bằng token QR ngẫu nhiên.
- Đã thêm `ReportsController`, màn hình báo cáo và xuất Excel có chống formula injection cho dữ liệu dạng text.
- Đã thêm xuất PDF báo cáo chính bằng QuestPDF, cài font Unicode trong image backend và nối nút xuất PDF ở frontend.
- Đã thêm test project backend (5 test) và frontend unit test (2 test), CI đã chạy cả hai.

## Phần chưa hoàn thành/chưa thể tuyên bố đạt

- Bàn giao đã có module core, file/ảnh/chữ ký điện tử dạng evidence và storage local; chưa có adapter MinIO/S3 và chưa có evidence ảnh riêng cho từng bước nhận trả.
- E2E browser chưa triển khai đầy đủ; báo cáo PDF hiện tập trung vào báo cáo tài sản chính, chưa có mẫu PDF riêng cho từng biên bản bàn giao/nhận trả.
- Test backend/frontend unit đã có; chưa tuyên bố các ca E2E hoặc kiểm thử production đạt.
- Chưa chạy migration mới trên database production; chỉ kiểm tra build, sinh idempotent script và kiểm tra Docker/health môi trường local.
- Đã gặp và xử lý hai rủi ro migration trên database local: thêm baseline tương thích cho chuỗi migration legacy và giữ index FK cũ khi thêm unique index detail. Sau khi sửa, `docker compose up -d --build` đạt; migration mới nhất `20260824182131_AddEquipmentLocationHistory` cũng đã được áp dụng.

## Quy tắc tiếp tục

- Không sửa hoặc push trực tiếp `main`.
- Không xoá migration/file local chưa phân loại.
- Mỗi giai đoạn phải chạy kiểm chứng thật và cập nhật tài liệu này.

## 2026-08-25 — Giai đoạn 10: kiểm kê, bảo trì, kho và hồ sơ người dùng

- Đã thêm evidence cho từng dòng kiểm kê, camera QR, xuất báo cáo chênh lệch Excel/PDF; file được lưu qua `IFileStorage` và kiểm tra magic bytes.
- Bảo trì đã có chu kỳ ngày/tuần/tháng/quý/năm, checklist, nhà cung cấp, vật tư sử dụng trừ kho trong cùng transaction, giao dịch liên kết và file evidence.
- Vật tư đã có mã duy nhất, nhà cung cấp, giá nhập, vị trí lưu, số lô, hạn dùng và transaction có liên kết phiếu cấp phát/bảo trì.
- Nhận trả đã có evidence trước/sau theo từng tài sản; header phiếu mượn được chuẩn hóa nullable để không còn trùng định danh với danh sách detail.
- Hồ sơ người dùng đã có họ tên, mã sinh viên/mã cán bộ, điện thoại, khoa/bộ môn, lớp; email và mã định danh nullable nhưng unique khi có giá trị. Khóa tài khoản tăng `TokenVersion` để vô hiệu hóa token hiện tại.
- Các migration mới đã áp dụng thành công trên database Docker local:
  `20260824182827_AddInventoryEvidence`, `20260824183215_CompleteMaintenanceAndInventoryEvidence`,
  `20260824183420_AddConsumableTraceabilityFields`, `20260824183758_AddUniversityUserProfileFields`,
  `20260824184021_AddReturnEvidence`, `20260824184339_NormalizeBorrowHeader`,
  `20260824185702_AlignOptionalUserProfileFields`.
- Kiểm tra `dotnet ef migrations has-pending-model-changes`: không còn thay đổi model chưa có migration.
- Docker build lại thành công; MySQL/backend/frontend chạy, backend `/health` trả `Healthy`, frontend trả HTTP 200 tại `http://localhost:8081` theo `.env` hiện tại.

## Phạm vi còn phải hoàn thiện trước khi tuyên bố nghiệm thu toàn bộ

- Chưa có E2E browser chạy thật trong môi trường này; chưa tuyên bố các kịch bản E2E đạt.
- Storage mới có adapter local; MinIO/S3 và diễn tập backup/restore production chưa được kiểm chứng.
- Một số component frontend cũ còn cần tiếp tục gom các điều kiện role vào helper dùng chung và tách nhỏ bảng lớn; chức năng nghiệp vụ đã nối API thật.
- Chưa chạy migration trên database production; chỉ database Docker local đã được kiểm chứng.

## 2026-08-25 — Giai đoạn 11: hồ sơ cá nhân và gom phân quyền frontend

- Commit `40eab95`: `feat: add self-service user profile`; thêm `GET /api/users/me`, `PUT /api/users/me/profile`, màn hình hồ sơ cá nhân và giữ đổi mật khẩu với token invalidation.
- Commit `aa4f905`: `refactor: centralize frontend role guards`; các route và màn hình chính dùng `ROLE`, `MANAGER_ROLES`, `BORROWER_ROLES` cùng helper `isManagerRole/isAdminRole/...`, không còn rải mảng role literal trong điều kiện UI.
- Frontend production build và unit test tiếp tục đạt sau hai commit này.
- Vòng Docker cuối sau hai commit trên đạt; startup không còn lỗi seed/migration hoặc pending model changes. Working tree chỉ còn các file untracked local đã tồn tại từ trước, không thuộc thay đổi của đợt này.

## 2026-08-25 — Giai đoạn 12: hardening, backup/restore và E2E smoke

- Bổ sung Data Protection key persistence qua volume `backend_data_protection`; production có thể chỉ định `Security:DataProtectionKeysPath` và cần cấu hình encryptor/certificate phù hợp.
- Bổ sung rate limit cho các endpoint nhạy cảm: profile, đổi mật khẩu, upload evidence và import Excel; login tiếp tục có giới hạn riêng theo IP.
- Upload local kiểm tra đồng thời phần mở rộng, MIME khai báo, magic bytes, kích thước và đường dẫn lưu ngẫu nhiên; đã thêm test cho MIME giả mạo và đường dẫn an toàn.
- Thêm `scripts/backup.ps1` và `scripts/restore.ps1` để backup database + volume upload và khôi phục có cờ xác nhận rõ ràng; hướng dẫn đã cập nhật tại `docs/BACKUP_RESTORE.md`.
- Notification đã phát sinh cho tạo/hoàn tất bảo trì, tạo/đóng kiểm kê và sinh phiếu từ kế hoạch định kỳ; backend test đạt 7/7.
- Thêm Playwright smoke E2E responsive desktop/mobile và route RBAC; local đạt 4 pass/2 skip khi không truyền credential, flow admin với database Docker đạt 1 pass. CI đã có job cài Chromium và chạy E2E.
- Đã nâng `nanoid` và `postcss` gián tiếp qua lockfile; `npm audit` hiện báo 0 vulnerability.
- Landing page đã bỏ các số liệu thống kê giả, thay bằng mô tả năng lực hệ thống.

## Trạng thái nghiệm thu

- Đã hoàn thiện và kiểm chứng local các module nghiệp vụ chính, backend/frontend build, unit test, Docker health, E2E smoke và audit dependency.
- Chưa tuyên bố production-ready tuyệt đối: chưa diễn tập restore trên production, chưa có adapter MinIO/S3, chưa có chứng thư mã hóa Data Protection trong deployment thật và các E2E business flow đầy đủ vẫn cần credential/test data riêng.

## 2026-08-25 — Giai đoạn 13: audit cuối, design system và business E2E

- Bổ sung shared UI components cho page header, filter, empty/error state, data table/list responsive, confirm dialog, upload, location tree, notification bell và audit action label; nối vào các màn hình báo cáo, kiểm kê, thông báo, audit log, thiết bị và dashboard.
- Chuẩn hóa token giao diện theo design system xanh navy/trắng/xám; loại bỏ số liệu marketing giả khỏi landing/login/forgot password, giữ toàn bộ nhãn người dùng bằng tiếng Việt.
- Bổ sung correlation ID cho mọi request backend và kiểm tra giữ header trong container; thêm test AuthController để xác minh login, forgot-password generic response và reset token một lần.
- Bổ sung test transaction/IDOR/RBAC cho borrow và consumable; phát hiện và sửa lỗi thiếu `SaveChangesAsync` khi ghi `ConsumableTransaction`.
- Thêm business E2E opt-in có dữ liệu prefix `E2E-`, đã chạy đạt flow mượn nhiều tài sản → teacher/manager approval → handover → return → maintenance → QR inventory.
- Kết quả cuối: backend **18/18**, frontend **2/2**, E2E business **1/1**, E2E responsive smoke **4 pass/4 skip**, Docker healthy.
- `docker compose config --quiet` và parse script backup/restore đạt; restore có guard bắt buộc `-ConfirmRestore`. Chưa giả lập restore thật trên volume đang chạy để tránh ghi đè dữ liệu local ngoài phạm vi kiểm chứng an toàn.

Các giới hạn production còn lại giữ nguyên: chưa có adapter MinIO/S3, chưa diễn tập restore production, chưa cấu hình certificate Data Protection trong deployment thật và frontend vẫn còn một số file view lớn cần tách tiếp nếu tiếp tục tối ưu maintainability.
