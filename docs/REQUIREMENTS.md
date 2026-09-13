# Yêu cầu nghiệp vụ

Hệ thống phục vụ quản lý tài sản định danh và vật tư số lượng cho phòng Lab IoT. Phạm vi gồm tài sản/QR, vị trí, mượn trả, kiểm kê, vật tư, thông báo, người dùng và audit log.

## Quy tắc chính

- Tài sản định danh có `AssetCode`, `QrToken`, serial và trạng thái code; vật tư quản lý theo số lượng.
- Phiếu mượn có nhiều detail; sinh viên bắt buộc chọn giảng viên bảo lãnh; duyệt nhiều tài sản là nguyên tử.
- Nhận trả có thể xử lý từng detail; phiếu chỉ hoàn tất khi tất cả detail đã trả.
- Kiểm kê tạo snapshot phạm vi, xác thực QR thuộc đợt rồi mới ghi nhận kết quả.
- Tồn kho không âm và mọi thay đổi số lượng phải tạo giao dịch.

