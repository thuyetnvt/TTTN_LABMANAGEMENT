# Baseline audit

Ngày kiểm tra: 2026-08-24  
Branch: `codex/iot-lab-asset-upgrade`  
Commit gốc: `5f016448b435e166fcd94d5a7490ed3dfccf68d2`

## Kết quả môi trường

| Hạng mục | Kết quả |
|---|---|
| Git working tree | Branch đã tách từ `main`; có nhiều file local/untracked (migration cũ, thư mục export và bản sao lưu) được giữ nguyên, không tự xoá |
| Backend SDK | .NET SDK 10.0.300, runtime ASP.NET Core 9.0.18 |
| Backend restore | Đạt, `dotnet restore` exit 0 |
| Backend build | Đạt, `dotnet build --no-restore`: 0 warning, 0 error |
| Frontend runtime | Node v24.12.0, npm 11.6.2 |
| Frontend dependency | `npm ci --ignore-scripts` bị treo không có output và đã dừng; cần chạy lại với log/timeout kiểm soát |
| Frontend build | Chưa kết luận đạt vì dependency install chưa hoàn tất |
| Docker | Docker 29.6.2, Compose v5.3.1; `docker compose config --quiet` đạt |
| Container hiện tại | db, backend, frontend đều healthy; frontend publish `localhost:8081` |
| API health | `http://localhost:8081/health` trả HTTP 200 qua Nginx; backend không publish trực tiếp ở localhost:8080 |
| EF migrations | 3 migration đang được Git quản lý; workspace còn nhiều migration local/untracked cần phân loại trước khi commit |

## Chức năng đã có

- Vue 3, Vite, Ant Design Vue, Pinia, Vue Router và SignalR.
- ASP.NET Core 9, EF Core, MySQL, Docker Compose và Nginx.
- 11 controller backend, 16 view frontend.
- JWT authentication, token version invalidation, RBAC cho 5 vai trò.
- Tài sản, danh mục, vật tư, yêu cầu cấp phát, mượn trả một tài sản/phiếu, bảo trì, bồi thường, audit log, Excel export và SignalR toast.
- Transaction/concurrency claim cơ bản khi duyệt mượn và cấp phát vật tư.

## Lỗi và khoảng trống xác nhận từ code

### P0

- Nhiều status nghiệp vụ vẫn là chuỗi tiếng Việt rải rác (`Chờ duyệt`, `Đã trả`, `Hoàn tất`, `Bảo hành`...), trong khi frontend/backend có các biến thể khác nhau.
- `BorrowRecord` vẫn giữ `EquipmentId` ở header và `Details`, endpoint tạo/duyệt/trả mới xử lý một thiết bị.
- Sinh viên chưa bị backend bắt buộc chọn giảng viên bảo lãnh.
- Từ chối bảo lãnh chưa nhận lý do.
- Docker mặc định publish MySQL `3306`, bật seed mặc định và `ForwardedHeaders:TrustAll=true`.
- `User.Email` chỉ có index thường, chưa có unique nullable index và chưa chuẩn hóa email.
- Upload quyết định mới cần được harden thêm về MIME, magic bytes, tên file và endpoint tải.

### P1

- `Equipment` chưa có mã tài sản/QR token, thông tin IoT, ảnh, nguồn kinh phí, nhà cung cấp và ngày kiểm kê.
- `Location` là chuỗi tự do, chưa có cây vị trí và lịch sử điều chuyển.
- Chưa có bàn giao/nhận trả nhiều thiết bị với phụ kiện, ảnh, biên bản và xác nhận điện tử.
- Bảo trì chưa có kế hoạch định kỳ; hoàn tất bảo trì đang tự đưa tài sản về `AVAILABLE`.
- SignalR mới gửi toast, chưa có notification lưu database.
- UI còn hard-code role/status, table rộng và route guard dùng `window.alert`.
- Chưa có test backend/frontend/E2E hoặc GitHub Actions trong repository.

## Rủi ro dữ liệu

- Migration local/untracked tồn tại ngoài baseline Git; không được tự động đưa toàn bộ vào commit nếu chưa kiểm tra lịch sử schema.
- Đổi status cần migration dữ liệu cũ theo mapping có kiểm soát, giữ nguyên audit/history.
- Refactor header/detail phải chuyển các phiếu cũ thành đúng một detail/phiếu và không làm mất `EquipmentId` hiện tại.
- Thay `Location` tự do cần giữ giá trị cũ trong trường legacy hoặc tạo node “Chưa phân loại” trước khi chuyển đổi.

## Tiêu chí nghiệm thu giai đoạn đầu

- Backend dùng status code tập trung; frontend chỉ hiển thị nhãn tiếng Việt qua map/component dùng chung.
- Seed và dữ liệu cũ không còn tạo status mới bằng tiếng Việt.
- `dotnet restore/build` đạt; migration compile được.
- Docker production mặc định không mở MySQL ra ngoài, seed mặc định false và forwarded headers chỉ tin proxy cấu hình.
- Không sửa trực tiếp `main`; mọi commit nằm trên branch feature.

## Kế hoạch migration

1. Status/role/audit constants và mapping dữ liệu cũ.
2. IoT asset metadata, QR token và location hierarchy.
3. Borrow header/detail, teacher guarantee, state history, handover/return evidence.
4. Maintenance schedule, inventory, notifications và hardening upload.

Mỗi bước sẽ có migration riêng, build/test liên quan và ghi vào `docs/IMPLEMENTATION_PROGRESS.md`.
