# Các thay đổi trong bản đã rà soát

## Đã sửa

- Đồng bộ `package.json` và `package-lock.json`; `npm ci` hoạt động.
- Sửa route tìm kiếm thiết bị từ `Device` thành `Devices`.
- Thêm trang 404.
- Dùng biến môi trường cho API và SignalR.
- Truyền JWT cho kết nối SignalR.
- Xử lý phiên hết hạn khi API trả 401.
- Làm cho tùy chọn “Ghi nhớ đăng nhập” có tác dụng.
- Bỏ nút xuất Excel trùng và các thư viện biểu đồ/QR/Excel không sử dụng.
- Chuẩn hóa tên “Vật tư tiêu hao”.
- Bỏ mục lịch sử mượn bị lặp và đồng bộ menu với route hiện tại.
- Thêm kiểm tra loại file, dung lượng file, ngày trả và các trường bắt buộc.
- Ngăn chọn thủ công trạng thái “Đang mượn”.
- Bổ sung trạng thái gửi form để hạn chế gửi yêu cầu lặp.
- Bổ sung giao diện responsive cho sidebar, header và dashboard.
- Sửa các chuỗi tiếng Việt bị lỗi mã hóa.
- Thay chức năng quên mật khẩu giả bằng API thật và thêm trang đặt lại mật khẩu.
- Bổ sung Dockerfile, cấu hình Nginx và hướng dẫn chạy.
- Tự động import component Ant Design Vue để giảm bundle tải ban đầu.
- Thêm giao diện đổi mật khẩu và hoàn tất phiếu bảo trì.
- Sửa xử lý ngày trả/hạn bảo hành và giá trị DatePicker khi sửa dữ liệu.
- Thêm CSP/security headers và bỏ phụ thuộc Google Fonts khi chạy production.

## Đã đồng bộ với backend

- Quên/đặt lại/đổi mật khẩu thật.
- SignalR có JWT và gửi thông báo theo người/nhóm quyền.
- Phân quyền bắt buộc ở từng API.
- Upload có giới hạn loại file, kích thước và tên lưu ngẫu nhiên.
- Transaction chống duyệt trùng cho mượn thiết bị và cấp vật tư.
- Audit log, health check, migration và Docker deployment.

## Việc nên làm tiếp khi vận hành quy mô lớn

- Cân nhắc cookie `HttpOnly`/BFF thay cho token trong Web Storage.
- Tích hợp quét mã độc cho file upload.
- Thêm test tự động và giám sát tập trung.
