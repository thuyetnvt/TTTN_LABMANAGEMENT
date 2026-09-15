# Bộ tài liệu hướng dẫn sử dụng LabManagement

Trang này là mục lục hướng dẫn sử dụng theo vai trò. Hãy chọn đúng tài liệu tương ứng với tài khoản đang đăng nhập.

## Hướng dẫn theo vai trò

| Vai trò | Tài liệu | Phạm vi chính |
| --- | --- | --- |
| Quản trị viên | [Hướng dẫn dành cho Quản trị viên](HUONG_DAN_QUAN_TRI_VIEN.md) | Người dùng, thiết bị, vị trí, kiểm kê, mượn/trả, vật tư, báo cáo và nhật ký hoạt động |
| Giảng viên | [Hướng dẫn dành cho Giảng viên](HUONG_DAN_GIANG_VIEN.md) | Bảo lãnh sinh viên, mượn thiết bị, xác nhận bàn giao và yêu cầu vật tư |
| Sinh viên | [Hướng dẫn dành cho Sinh viên](HUONG_DAN_SINH_VIEN.md) | Gửi yêu cầu mượn, theo dõi phiếu, xác nhận bàn giao, trả thiết bị và yêu cầu vật tư |

Trưởng lab và Phó lab có thể dùng phần vận hành trong [Hướng dẫn dành cho Quản trị viên](HUONG_DAN_QUAN_TRI_VIEN.md). Hai vai trò này không có các chức năng riêng của Quản trị viên như quản lý tài khoản và xem nhật ký hoạt động.

## Đăng nhập lần đầu

1. Mở địa chỉ hệ thống do đơn vị cung cấp.
2. Nhập tài khoản và mật khẩu, sau đó chọn `Đăng nhập`.
3. Nếu là lần đăng nhập đầu tiên hoặc mật khẩu vừa được đặt lại, hệ thống chuyển đến màn hình đổi mật khẩu.
4. Nhập mật khẩu mới và xác nhận lại. Người dùng chỉ vào được Dashboard sau khi hoàn tất.
5. Chọn ảnh đại diện ở góc trên bên phải để mở hồ sơ, đổi mật khẩu hoặc đăng xuất.

Không gửi mật khẩu, token, chuỗi kết nối hoặc nội dung file `.env` qua ảnh chụp màn hình và không đưa các thông tin này lên Git.

## Thao tác chung trên giao diện

- Chọn một mục ở menu bên trái để mở chức năng tương ứng.
- Dùng ô tìm kiếm và bộ lọc phía trên bảng để thu hẹp dữ liệu.
- Chọn biểu tượng mắt để xem chi tiết một bản ghi.
- Chọn biểu tượng chuông để mở danh sách thông báo.
- Trên màn hình nhỏ, cuộn ngang trong vùng bảng để xem các cột còn lại.
- Sau khi thay đổi dữ liệu, tải lại danh sách nếu màn hình chưa cập nhật ngay.

Menu và nút được hiển thị theo quyền của tài khoản. Việc không thấy một chức năng có thể do vai trò không được cấp quyền hoặc quyền ủy quyền đã hết hiệu lực.

## Trạng thái thiết bị

| Trạng thái | Ý nghĩa |
| --- | --- |
| Rảnh | Thiết bị có thể được chọn để tạo yêu cầu mượn. |
| Đã giữ chỗ | Phiếu đã duyệt và thiết bị đang chờ bàn giao. |
| Đang mượn | Người nhận đã xác nhận biên bản bàn giao. |
| Hỏng | Thiết bị có sự cố và chưa sẵn sàng cho mượn. |
| Thất lạc | Chưa xác định được vị trí của thiết bị. |

## Trạng thái phiếu mượn

| Trạng thái | Ý nghĩa |
| --- | --- |
| Chờ giảng viên duyệt | Phiếu sinh viên đang chờ giảng viên bảo lãnh. |
| Chờ quản lý duyệt | Phiếu đang chờ quản lý lab quyết định. |
| Chờ nhận / Đã giữ chỗ | Phiếu đã duyệt và tài sản đang được giữ để bàn giao. |
| Đã bàn giao, chờ xác nhận | Quản lý đã lập biên bản; người mượn cần kiểm tra và xác nhận. |
| Đang mượn | Người nhận đã xác nhận nhận đủ thiết bị. |
| Đang xử lý trả | Quản lý đang kiểm tra việc trả thiết bị. |
| Đã trả | Quy trình trả đã hoàn tất. |
| Từ chối | Phiếu không được chấp thuận. |
| Đã hủy | Phiếu đã được hủy theo thao tác hợp lệ. |
| Hết hạn giữ chỗ | Người nhận không hoàn tất nhận đúng hạn; tài sản được giải phóng theo tác vụ tự động. |

## Trạng thái yêu cầu vật tư

| Trạng thái | Ý nghĩa |
| --- | --- |
| Chờ duyệt | Yêu cầu đang chờ người có quyền quyết định. |
| Chờ bàn giao | Yêu cầu đã duyệt và số lượng đang được giữ trong kho. |
| Chờ xác nhận nhận | Quản lý đã giao vật tư; người nhận cần kiểm tra và xác nhận. |
| Đã nhận | Người nhận đã xác nhận nhận đủ. |
| Từ chối | Yêu cầu không được chấp thuận hoặc không thể bàn giao. |

## Khi cần hỗ trợ

1. Ghi lại tên tài khoản, thời điểm, tên màn hình và thao tác gây lỗi.
2. Ghi mã phiếu hoặc mã tài sản nếu lỗi liên quan đến nghiệp vụ cụ thể.
3. Chụp thông báo lỗi nhưng che thông tin nhạy cảm.
4. Gửi cho Quản trị viên hoặc người phụ trách lab.

Quản trị viên nên kiểm tra thông báo, `Nhật ký hoạt động` và log backend trước khi thay đổi dữ liệu trực tiếp trong cơ sở dữ liệu.
