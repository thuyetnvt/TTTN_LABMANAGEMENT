# Theo dõi yêu cầu cuộc họp ngày 10 tháng 9 năm 2026

Nguồn đối chiếu: [Biên bản Athena](https://app.myathena.net/meetings/p/2509d189-b644-4ab7-891f-506257d18274).

## Hạng mục phần mềm

| Hạng mục | Trạng thái trong mã nguồn |
| --- | --- |
| Rút gọn bảng rộng và đưa trường phụ vào chi tiết | Đã thực hiện cho thiết bị, phiếu mượn, lịch sử mượn trả và cấp phát vật tư |
| Đưa trạng thái vào vùng nhìn thấy, bỏ ghim cột trạng thái | Đã thực hiện; chỉ cột hành động được giữ ở mép phải khi cần |
| Bổ sung mục đích và lọc theo khoảng ngày | Đã thực hiện cho yêu cầu cấp phát và lịch sử mượn trả |
| Hiển thị đầy đủ người giữ chỗ và hạn giữ chỗ | Đã thực hiện; worker tự hết hạn và trả thiết bị về trạng thái rảnh |
| Người nhận xác nhận biên bản bàn giao | Đã thực hiện cho sinh viên và giảng viên |
| Hộp thoại xác nhận duyệt, từ chối và bàn giao | Đã thực hiện |
| Người chịu trách nhiệm lấy từ tài khoản trong database | Đã thực hiện; báo cáo có cửa sổ xem toàn bộ thiết bị phụ trách |
| Bỏ mã trong nhãn vị trí và bỏ cấp vị trí cha | Đã thực hiện; migration xóa trường `ParentId` và vị trí gốc `LAB-ROOT` |
| Bỏ tên seri, khấu hao và các trường phụ khỏi bảng thiết bị | Đã thực hiện; thông tin cần giữ nằm trong chi tiết |
| Xóa chức năng bảo trì | Đã xóa giao diện, API, model, seed, tác vụ nền và schema liên quan |
| Tự làm mới access token bằng refresh token | Đã thực hiện; refresh token xoay vòng và có hạn mặc định 7 ngày |
| Đặt lại mật khẩu bằng biểu tượng, không sửa trực tiếp | Đã thực hiện; tài khoản phải đổi mật khẩu ở lần đăng nhập tiếp theo |
| Lọc ngày và thao tác trong nhật ký hoạt động | Đã thực hiện; bảng chính không còn cột chi tiết |
| Chuẩn hóa kích thước các ô lọc | Đã thực hiện theo kích thước 150 × 40 tại màn hình được yêu cầu |
| Hướng dẫn sử dụng theo ba nhóm vai trò | Đã cập nhật tại `docs/USER_GUIDE.md` |
| Hướng dẫn cài đặt và triển khai | Có bản Word tại `docs/HUONG_DAN_CAI_DAT_VA_TRIEN_KHAI.docx` |

## Hạng mục bàn giao ngoài mã nguồn

Các việc sau không thể hoàn tất chỉ bằng commit Git và cần người phụ trách xác nhận:

- Gửi biên bản hoặc tài liệu qua Zalo.
- Bổ sung chữ ký, dấu xác nhận và bản giấy theo yêu cầu của đơn vị.
- Cấu hình GitHub Secrets và quyền SSH của VPS.
- Chạy UAT với tài khoản thật của từng nhóm vai trò.

## Điều kiện trước khi đẩy Git

1. `git status --short` chỉ liệt kê các file dự kiến bàn giao.
2. Backend build và toàn bộ kiểm thử .NET đều đạt.
3. Frontend unit test và production build đều đạt.
4. Docker Compose build được từ một bản clone sạch; E2E đạt.
5. `git diff --check` và `git diff --cached --check` không báo lỗi.
6. Không có `.env`, mật khẩu, khóa JWT, private key, database backup hoặc thư mục build trong commit.
