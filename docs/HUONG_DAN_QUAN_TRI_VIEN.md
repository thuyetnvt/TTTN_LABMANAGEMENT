# Hướng dẫn sử dụng dành cho Quản trị viên

Tài liệu này hướng dẫn tài khoản `Quản trị viên` sử dụng các chức năng quản trị và vận hành của hệ thống LabManagement. Trưởng lab và Phó lab có thể tham khảo các phần vận hành, nhưng không có quyền quản lý tài khoản, xem nhật ký hoạt động hoặc thực hiện một số thao tác xóa dành riêng cho Quản trị viên.

## 1. Đăng nhập và bảo mật tài khoản

1. Mở địa chỉ hệ thống do đơn vị cung cấp.
2. Nhập tên tài khoản và mật khẩu, sau đó chọn `Đăng nhập`.
3. Nếu là lần đăng nhập đầu tiên hoặc mật khẩu vừa được đặt lại, hệ thống yêu cầu đổi mật khẩu trước khi vào Dashboard.
4. Chọn ảnh đại diện ở góc trên bên phải để mở `Hồ sơ`, đổi mật khẩu hoặc đăng xuất.
5. Chọn biểu tượng chuông để xem thông báo mới.

Không gửi mật khẩu, chuỗi kết nối, token hoặc nội dung file `.env` qua ảnh chụp màn hình và không đưa các thông tin này lên Git.

## 2. Tổng quan giao diện

- `Tổng quan`: xem nhanh số lượng tài sản, thiết bị đang mượn, thiết bị hỏng và các công việc cần xử lý.
- `Thiết bị & Tài sản`: quản lý thiết bị và vật tư tiêu hao.
- `Vị trí`: quản lý phòng lab, tủ, kệ hoặc bàn lưu trữ.
- `Kiểm kê`: tạo và thực hiện các đợt kiểm kê.
- `Lịch sử mượn/trả`: tra cứu phiếu và xử lý trả thiết bị.
- `Phiếu chờ duyệt`: duyệt, từ chối và lập bàn giao thiết bị.
- `Yêu cầu cấp phát`: duyệt và bàn giao vật tư tiêu hao.
- `Ủy quyền duyệt`: thiết lập quyền duyệt thay khi nghiệp vụ cho phép.
- `Báo cáo`: xem và xuất số liệu vận hành.
- `Báo cáo sai lệch`: xử lý phản ánh của người nhận về biên bản bàn giao.
- `Quản lý người dùng`: tạo, sửa, khóa, mở khóa và hỗ trợ đặt lại mật khẩu.
- `Nhật ký hoạt động`: đối soát các thao tác đã thực hiện trong hệ thống.

Các bảng có thể cuộn ngang trên màn hình nhỏ. Biểu tượng mắt dùng để mở chi tiết; biểu tượng sắp xếp và lọc ở tiêu đề cột chỉ tác động lên danh sách đang xem.

## 3. Quản lý người dùng

### 3.1. Tạo tài khoản

1. Mở `Quản lý người dùng`.
2. Chọn `+ Thêm tài khoản`.
3. Nhập tài khoản, email, họ tên, mã cán bộ hoặc mã sinh viên, số điện thoại, đơn vị và vai trò.
4. Kiểm tra đúng vai trò trước khi chọn `Lưu`:
   - `Quản trị viên`: quản trị toàn hệ thống.
   - `Trưởng lab` hoặc `Phó lab`: vận hành lab.
   - `Giảng viên`: bảo lãnh sinh viên và tạo yêu cầu cá nhân.
   - `Sinh viên`: tạo và theo dõi yêu cầu cá nhân.
5. Cấp thông tin đăng nhập ban đầu cho người dùng bằng kênh an toàn.

### 3.2. Sửa, khóa và mở khóa

1. Tìm người dùng theo tên, mã hoặc email; có thể lọc thêm theo vai trò và trạng thái.
2. Chọn biểu tượng sửa để cập nhật thông tin.
3. Chọn biểu tượng khóa và xác nhận `Khóa` khi cần ngừng quyền truy cập. Người dùng sẽ bị đăng xuất và không thể đăng nhập cho tới khi được mở khóa.
4. Với tài khoản đã khóa, chọn thao tác mở khóa để kích hoạt lại.

Không thể khóa tài khoản quản trị hệ thống mặc định. Nên khóa thay vì xóa dữ liệu người dùng đã có lịch sử nghiệp vụ.

### 3.3. Hỗ trợ đặt lại mật khẩu

1. Bảo đảm tài khoản đang hoạt động và đã có email đúng.
2. Chọn biểu tượng chìa khóa.
3. Kiểm tra địa chỉ email trong hộp thoại `Gửi hướng dẫn đặt lại mật khẩu`.
4. Chọn `Gửi yêu cầu`. Liên kết đặt lại mật khẩu có hiệu lực trong thời gian hiển thị trên hộp thoại.

Quản trị viên không xem được mật khẩu hiện tại của người dùng.

## 4. Quản lý thiết bị và vị trí

### 4.1. Tra cứu và xem chi tiết thiết bị

1. Mở `Thiết bị & Tài sản`, chọn tab `Thiết bị`.
2. Tìm theo tên thiết bị; lọc theo danh mục, vị trí hoặc trạng thái.
3. Chọn biểu tượng mắt để xem mã tài sản, số seri, danh mục, vị trí, người phụ trách, thông tin tài chính và hồ sơ liên quan.

### 4.2. Thêm, sửa và điều chuyển thiết bị

1. Chọn `+ Thêm thiết bị` và nhập đầy đủ các trường bắt buộc.
2. Chọn đúng người chịu trách nhiệm từ danh sách tài khoản trong hệ thống.
3. Chọn vị trí lưu trữ trực tiếp của thiết bị.
4. Sau khi lưu, dùng biểu tượng sửa để cập nhật hoặc thao tác điều chuyển để đổi vị trí và ghi lý do.

Thiết bị đang chờ duyệt, được giữ chỗ, chờ xác nhận bàn giao hoặc đang mượn sẽ bị khóa sửa, điều chuyển và xóa. Thiết bị đã có lịch sử nghiệp vụ cũng không được xóa cứng.

### 4.3. Nhập Excel, QR và kiểm kê nhanh

1. Chọn `Nhập Excel`, tải file đúng mẫu và xem bảng xem trước.
2. Sửa các dòng không hợp lệ trước khi xác nhận nhập.
3. Chọn nhiều thiết bị để in QR khi cần dán nhãn tài sản.
4. Dùng `Quét QR kiểm kê` để ghi nhận nhanh thiết bị trong đợt kiểm kê phù hợp.

### 4.4. Quản lý vị trí

1. Mở `Vị trí`.
2. Tìm theo mã hoặc tên; lọc theo loại vị trí và trạng thái.
3. Chọn `Thêm vị trí` để tạo mới hoặc biểu tượng sửa để cập nhật.
4. Kiểm tra cột `Số tài sản` trước khi ngừng sử dụng hoặc xóa vị trí.

## 5. Duyệt, bàn giao và nhận lại thiết bị

### 5.1. Duyệt yêu cầu mượn

1. Mở `Phiếu chờ duyệt`.
2. Dùng bộ lọc để tìm phiếu cần xử lý.
3. Chọn biểu tượng mắt và kiểm tra người mượn, số điện thoại, thiết bị, giảng viên bảo lãnh, mục đích và ngày trả dự kiến.
4. Chọn `Duyệt` nếu thông tin hợp lệ hoặc `Từ chối` và ghi rõ lý do.
5. Sau khi duyệt, tài sản chuyển sang trạng thái giữ chỗ để chờ bàn giao.

Nếu hết hạn giữ chỗ mà chưa hoàn tất nhận bàn giao, tác vụ nền sẽ tự hủy phiếu và đưa thiết bị về trạng thái `Rảnh`.

### 5.2. Lập biên bản bàn giao

1. Tại phiếu đã duyệt, chọn `Lập bàn giao`.
2. Ghi tình trạng, phụ kiện và ghi chú cho từng thiết bị.
3. Đính kèm ảnh hoặc tài liệu minh chứng khi cần.
4. Xác nhận lập biên bản và chờ người mượn kiểm tra.
5. Phiếu chỉ chuyển sang `Đang mượn` sau khi người mượn chọn `Xác nhận đã nhận đủ`.

Nếu chưa thể bàn giao, chọn `Hủy phiếu` và ghi lý do thay vì để phiếu tiếp tục giữ thiết bị.

### 5.3. Xử lý trả thiết bị

1. Mở `Lịch sử mượn/trả`.
2. Lọc phiếu đang mượn hoặc đang xử lý trả.
3. Chọn `Kiểm tra trả`.
4. Ghi tình trạng và ghi chú kiểm tra cho từng thiết bị, sau đó xác nhận.
5. Thiết bị bình thường trở về `Rảnh`; thiết bị có vấn đề chuyển sang trạng thái phù hợp để tiếp tục xử lý.
6. Dùng `Nhắc trả` cho phiếu sắp đến hạn hoặc quá hạn.
7. Dùng `Xuất Excel` để xuất danh sách theo bộ lọc hiện tại. Chỉ dùng `Nhập Excel` cho dữ liệu lịch sử đúng mẫu và đã được kiểm tra ở bước xem trước.

## 6. Quản lý vật tư tiêu hao

### 6.1. Danh mục, tồn kho và lô

1. Mở `Thiết bị & Tài sản`, chọn tab `Vật tư tiêu hao`.
2. Tìm theo mã hoặc tên; lọc theo tình trạng tồn và danh mục.
3. Chọn `+ Thêm vật tư` để tạo mới.
4. Dùng các biểu tượng trong cột `Hành động` để xem lịch sử nhập - xuất, quản lý lô, sửa hoặc xóa khi được phép.
5. Kiểm tra số lượng khả dụng và hạn sử dụng của từng lô trước khi cấp phát.

### 6.2. Duyệt và bàn giao vật tư

1. Mở `Yêu cầu cấp phát`.
2. Kiểm tra tên vật tư, người yêu cầu, số lượng, mục đích và chi tiết phiếu.
3. Chọn `Duyệt` hoặc `Từ chối`. Khi duyệt, số lượng tương ứng được giữ trong kho.
4. Với phiếu chờ bàn giao, chọn `Bàn giao`.
5. Chọn đúng lô và nhập số lượng giao; tổng số lượng phải đúng với phiếu và không vượt quá số khả dụng.
6. Chọn `Xác nhận bàn giao` và chờ người nhận chọn `Tôi đã nhận đủ`.
7. Dùng `Xuất báo cáo` để tải danh sách yêu cầu theo bộ lọc hiện tại.

## 7. Kiểm kê

1. Mở `Kiểm kê` và chọn `Tạo đợt kiểm kê`.
2. Đặt tên dễ hiểu, chọn phạm vi vị trí hoặc danh mục rồi bắt đầu đợt.
3. Mở chi tiết đợt và quét QR từng thiết bị.
4. Ghi nhận thiết bị tìm thấy, sai vị trí, hỏng hoặc thiếu; đính kèm minh chứng khi cần.
5. Rà lại tiến độ và danh sách chưa quét trước khi kết thúc.
6. Khi kết thúc, hệ thống đánh dấu các tài sản thuộc phạm vi nhưng chưa quét để người quản lý đối soát.

## 8. Báo cáo và sai lệch bàn giao

### 8.1. Báo cáo vận hành

1. Mở `Báo cáo`.
2. Áp dụng bộ lọc ngày, danh mục hoặc vị trí trước khi đọc số liệu.
3. Xem các nhóm tài sản rảnh, giữ chỗ, đang mượn, hỏng, thất lạc và vật tư cần chú ý.
4. Chọn biểu tượng mắt ở các thẻ hoặc bảng chi tiết để xem danh sách liên quan.
5. Xuất Excel hoặc PDF nếu màn hình cung cấp thao tác xuất.

### 8.2. Xử lý báo cáo sai lệch

1. Mở `Báo cáo sai lệch`.
2. Chọn ảnh thu nhỏ để xem bằng chứng hoặc biểu tượng mắt để mở toàn bộ nội dung.
3. Đối chiếu biên bản bàn giao, mô tả thực tế và ảnh đính kèm.
4. Chọn `Ghi nhận sai lệch` hoặc `Từ chối báo cáo`, nhập ghi chú xử lý và chọn `Lưu kết quả`.

## 9. Nhật ký hoạt động

1. Mở `Nhật ký hoạt động`.
2. Lọc theo người thao tác, loại hành động, đối tượng hoặc thời gian.
3. Kiểm tra thời điểm, tài khoản, hành động, đối tượng và địa chỉ IP.
4. Chọn tải lại sau khi thực hiện nghiệp vụ cần đối soát.

Nhật ký dùng để truy vết; không chỉnh sửa trực tiếp dữ liệu nhật ký.

## 10. Xử lý tình huống thường gặp

| Tình huống | Cách xử lý |
| --- | --- |
| Không sửa hoặc điều chuyển được thiết bị | Kiểm tra thiết bị có đang nằm trong phiếu chờ duyệt, giữ chỗ, chờ nhận hoặc đang mượn hay không. |
| Phiếu đã duyệt nhưng chưa thành `Đang mượn` | Kiểm tra biên bản đã lập và người mượn đã xác nhận nhận đủ hay chưa. |
| Phiếu hết hạn vẫn giữ thiết bị | Tải lại dữ liệu, kiểm tra tác vụ nền và log backend; không sửa trạng thái trực tiếp trong cơ sở dữ liệu. |
| Không bàn giao được vật tư | Kiểm tra lô còn hạn, số lượng khả dụng và tổng số lượng phân bổ. |
| Không gửi được liên kết đặt lại mật khẩu | Kiểm tra email tài khoản và cấu hình gửi thư của hệ thống. |
| Số liệu báo cáo chưa khớp | Kiểm tra bộ lọc, tải lại trang và đối chiếu `Nhật ký hoạt động` trước khi can thiệp dữ liệu. |

Khi báo lỗi, ghi rõ tài khoản, thời điểm, màn hình, thao tác và thông báo lỗi; không gửi mật khẩu hoặc secret.
