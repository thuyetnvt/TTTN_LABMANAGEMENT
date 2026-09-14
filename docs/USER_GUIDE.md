# Hướng dẫn sử dụng theo vai trò

Tài liệu chia năm vai trò của hệ thống thành ba nhóm sử dụng: quản trị và vận hành lab, giảng viên, sinh viên. Tên nút và trạng thái trong tài liệu khớp với giao diện hiện tại.

## Đăng nhập lần đầu

1. Mở địa chỉ hệ thống và nhập tài khoản được cấp.
2. Nếu đây là lần đăng nhập đầu tiên hoặc quản trị viên vừa đặt lại mật khẩu, hệ thống chuyển thẳng đến màn hình đổi mật khẩu.
3. Nhập mật khẩu mới đủ độ mạnh và xác nhận lại. Người dùng không thể mở Dashboard trước khi hoàn tất bước này.
4. Sau khi đăng nhập, mở menu ảnh đại diện để cập nhật hồ sơ hoặc tự đổi mật khẩu.

Không gửi mật khẩu qua ảnh chụp màn hình, báo cáo hoặc kho Git. Khi quên mật khẩu, dùng `Quên mật khẩu` hoặc liên hệ quản trị viên để đặt lại.

## Nhóm quản trị và vận hành lab

Nhóm này gồm `Quản trị viên`, `Trưởng lab` và `Phó lab`. Quản trị viên có thêm quyền quản lý tài khoản, xem nhật ký hoạt động và xóa dữ liệu khi quy tắc nghiệp vụ cho phép.

### Quản lý thiết bị

1. Mở `Thiết bị & Tài sản` và chọn tab `Thiết bị`.
2. Dùng bộ lọc tên, danh mục, vị trí hoặc trạng thái để tìm thiết bị.
3. Chọn biểu tượng mắt để xem đầy đủ số seri, danh mục, vị trí, ngày nhập, file quyết định và thông tin người chịu trách nhiệm.
4. Chọn `Thêm thiết bị` để tạo mới. Các trường bắt buộc phải được nhập trước khi lưu.
5. Chọn người chịu trách nhiệm từ danh sách tài khoản trong cơ sở dữ liệu. Tên và mã định danh chỉ dùng để hiển thị; hệ thống lưu liên kết đến đúng tài khoản.
6. Khi điều chuyển vị trí, chọn một vị trí trực tiếp trong phòng lab và nhập lý do. Hệ thống không dùng cấp vị trí cha.
7. Dùng `Nhập Excel` để xem trước dữ liệu. Chỉ xác nhận nhập khi toàn bộ dòng cần thiết đã hợp lệ.
8. Chọn nhiều dòng rồi in QR hoặc dùng `Quét QR kiểm kê` khi cần đối soát.

Thiết bị đang được giữ chỗ, chờ bàn giao hoặc đang mượn không thể sửa, điều chuyển hay xóa. Thiết bị đã có lịch sử nghiệp vụ cũng không được xóa cứng.

### Duyệt và bàn giao phiếu mượn

1. Mở `Phiếu chờ duyệt`. Danh sách chính chỉ giữ các cột cần xử lý; số điện thoại, số seri và ngày nằm trong cửa sổ chi tiết.
2. Mở biểu tượng mắt để kiểm tra người mượn, thiết bị, giảng viên bảo lãnh, số điện thoại liên hệ, mục đích và thời hạn.
3. Chọn `Duyệt` hoặc `Từ chối`. Đọc lại hộp thoại xác nhận trước khi đồng ý.
4. Sau khi duyệt, thiết bị chuyển sang `Đã giữ chỗ`. Thời hạn giữ chỗ hiển thị trong chi tiết phiếu.
5. Lập biên bản bàn giao, ghi tình trạng và phụ kiện của từng thiết bị. Có thể đính kèm ảnh hoặc tài liệu hợp lệ.
6. Người nhận phải xác nhận đã nhận. Chỉ sau bước này phiếu mới chuyển sang `Đang mượn`.
7. Nếu hết hạn giữ chỗ mà người nhận chưa xác nhận bàn giao, worker tự động chuyển phiếu sang `Đã hủy` và trả thiết bị về `Rảnh`.

### Kiểm tra trả

1. Mở `Lịch sử mượn/trả`, lọc theo người mượn, thiết bị, trạng thái hoặc khoảng thời gian.
2. Với phiếu đang mượn, chọn `Kiểm tra trả`.
3. Ghi tình trạng và ghi chú cho từng thiết bị. Tình trạng trả đầy đủ được lưu trong cửa sổ chi tiết, không chiếm cột ở bảng chính.
4. Xác nhận thao tác. Thiết bị bình thường về `Rảnh`; thiết bị có vấn đề chuyển sang `Hỏng`.
5. Dùng `Nhắc trả` cho phiếu sắp hoặc đã quá hạn. Hệ thống cũng có worker gửi thông báo theo cấu hình.
6. Có thể xuất lịch sử theo bộ lọc hiện tại hoặc nhập lịch sử đã kết thúc qua bước xem trước Excel.

### Quản lý vật tư tiêu hao

1. Mở tab `Vật tư tiêu hao` trong `Thiết bị & Tài sản` để quản lý tên, đơn vị, tồn kho, tồn tối thiểu, lô và hạn sử dụng.
2. Mở `Yêu cầu cấp phát` để lọc theo từ khóa, trạng thái hoặc khoảng ngày gửi.
3. Kiểm tra tên vật tư, người yêu cầu, số lượng và mục đích. Chọn biểu tượng mắt để xem toàn bộ thông tin.
4. Chọn `Duyệt` hoặc `Từ chối` và xác nhận trong hộp thoại.
5. Khi giao vật tư, chọn đúng lô. Số lượng khả dụng không được âm và hệ thống luôn ghi giao dịch kho.
6. Người nhận xác nhận đã nhận để hoàn tất quy trình.

### Kiểm kê và báo cáo

1. Tạo đợt tại `Kiểm kê`, chọn phạm vi vị trí hoặc danh mục rồi bắt đầu.
2. Quét QR trong chi tiết đợt. Có thể ghi nhận tìm thấy, thiếu, sai vị trí hoặc hỏng và đính kèm minh chứng.
3. Khi kết thúc, các thiết bị thuộc phạm vi nhưng chưa quét được đánh dấu thiếu để người quản lý rà soát.
4. Mở `Báo cáo` để xem tổng tài sản, đang mượn, hỏng, vật tư sắp hết và chi tiết người chịu trách nhiệm.
5. Chọn biểu tượng mắt tại người chịu trách nhiệm để xem toàn bộ thiết bị người đó đang phụ trách.
6. Xuất Excel hoặc PDF sau khi áp dụng bộ lọc ngày, danh mục và vị trí.

### Quản lý tài khoản và nhật ký

Chỉ quản trị viên thực hiện các bước sau:

1. Mở `Quản lý người dùng` để tạo tài khoản, sửa thông tin hoặc khóa tài khoản.
2. Dùng biểu tượng chìa khóa để đặt lại mật khẩu. Quản trị viên không xem hoặc sửa trực tiếp mật khẩu hiện tại.
3. Người vừa được đặt lại mật khẩu phải đổi mật khẩu ở lần đăng nhập tiếp theo.
4. Mở `Nhật ký hoạt động` để lọc theo người thao tác, hành động, đối tượng và khoảng ngày.
5. Chọn `Tải lại` sau khi thực hiện nghiệp vụ cần đối soát. Bảng hiển thị thời gian, người thao tác, hành động, đối tượng và địa chỉ IP.

## Nhóm giảng viên

### Bảo lãnh yêu cầu của sinh viên

1. Mở `Giảng viên phê duyệt` từ Dashboard hoặc menu bên trái.
2. Mở chi tiết phiếu để kiểm tra sinh viên, thiết bị, mục đích và thời hạn.
3. Chọn duyệt hoặc từ chối, nhập ghi chú khi cần và xác nhận thao tác.
4. Phiếu được giảng viên duyệt sẽ chuyển đến quản lý lab quyết định cuối.

Giảng viên chỉ thấy các phiếu có mình là người bảo lãnh. Số liệu Dashboard và hoạt động gần đây cũng chỉ thuộc phạm vi cá nhân.

### Mượn thiết bị và yêu cầu vật tư

1. Mở danh sách thiết bị, chọn thiết bị `Rảnh` rồi thêm vào phiếu mượn.
2. Nhập số điện thoại liên hệ 10 chữ số, ngày trả dự kiến và mục đích.
3. Gửi phiếu và theo dõi trong `Lịch sử mượn/trả`.
4. Khi quản lý đã bàn giao, mở chi tiết biên bản và xác nhận nhận thiết bị.
5. Với vật tư, mở `Yêu cầu vật tư của tôi`, nhập số lượng và mục đích rồi gửi. Xác nhận đã nhận sau khi quản lý giao vật tư.

## Nhóm sinh viên

### Gửi yêu cầu mượn

1. Mở `Thiết bị & Tài sản`, lọc thiết bị có trạng thái `Rảnh`.
2. Chọn biểu tượng giỏ mượn ở từng thiết bị hoặc quét QR.
3. Kiểm tra danh sách thiết bị, chọn giảng viên bảo lãnh, nhập số điện thoại liên hệ 10 chữ số, ngày trả dự kiến và mục đích.
4. Gửi phiếu. Trạng thái lần lượt có thể là `Chờ giảng viên`, `Chờ duyệt`, `Đã giữ chỗ`, `Đang mượn`, `Đã trả`, `Từ chối`, `Đã hủy` hoặc `Hết hạn giữ chỗ`.
5. Có thể hủy phiếu khi trạng thái và quyền hiện tại cho phép; phải nhập lý do và xác nhận.

### Xác nhận nhận và theo dõi trả

1. Mở `Lịch sử mượn/trả` và dùng bộ lọc trạng thái để tìm phiếu.
2. Khi trạng thái là `Đã bàn giao, chờ người nhận xác nhận`, mở biểu tượng mắt.
3. Kiểm tra thiết bị, tình trạng, phụ kiện và minh chứng rồi chọn xác nhận nhận.
4. Theo dõi hạn trả trên Dashboard hoặc trong chi tiết. Liên hệ quản lý lab nếu thông tin bàn giao không đúng.
5. Khi trả thiết bị, chờ quản lý kiểm tra và cập nhật kết quả; người mượn không tự sửa tình trạng trả.

### Yêu cầu vật tư

1. Mở `Yêu cầu vật tư của tôi` và tạo yêu cầu mới.
2. Chọn vật tư, nhập số lượng và mục đích sử dụng.
3. Theo dõi trạng thái. Nếu được giao, mở chi tiết và xác nhận đã nhận.

## Tra cứu trạng thái thiết bị

| Trạng thái | Ý nghĩa |
| --- | --- |
| Rảnh | Có thể tạo yêu cầu mượn |
| Đã giữ chỗ | Phiếu đã duyệt và đang chờ bàn giao |
| Đang mượn | Người nhận đã xác nhận biên bản |
| Hỏng | Thiết bị cần được quản lý xử lý ngoài hệ thống |
| Thất lạc | Thiết bị không xác định được vị trí |

## Khi cần hỗ trợ

Ghi lại tài khoản, thời điểm, màn hình và thao tác gây lỗi; không gửi mật khẩu hoặc secret. Quản trị viên kiểm tra `Nhật ký hoạt động`, thông báo và log backend trước khi thay đổi dữ liệu trực tiếp trong database.
