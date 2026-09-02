# Ma trận phân quyền

Tài liệu này mô tả quyền theo API và phạm vi dữ liệu thực tế. `Trưởng lab` và
`Phó lab` dùng chung nhóm quyền vận hành (`Manager`); quyền `Quản trị viên`
(`Admin`) được tách riêng ở các thao tác xóa và quản trị tài khoản.

| Chức năng | Quản trị viên | Trưởng/Phó Lab | Giảng viên | Sinh viên | Phạm vi / điều kiện |
|---|---|---|---|---|---|
| Xem Dashboard | Có | Có | Có | Có | Admin/Trưởng/Phó xem số liệu toàn lab; Giảng viên xem việc bảo lãnh và phiếu của mình; Sinh viên chỉ xem phiếu, thiết bị và hạn trả của mình. |
| Xem thiết bị, danh mục, vị trí | Có | Có | Có | Có | Người mượn nhận DTO an toàn; không trả QR token, giá mua, nhà cung cấp, hóa đơn, nguồn kinh phí hoặc người phụ trách. |
| Tạo / nhập / sửa thiết bị | Có | Có | Không | Không | Manager. `BORROW_PENDING`/đang chờ duyệt, chờ bàn giao hoặc đang mượn thì không được sửa hay điều chuyển. |
| Xóa thiết bị | Có | Không | Không | Không | Admin; không xóa nếu đã có lịch sử mượn, bảo trì, phạt hoặc đang bị giữ bởi phiếu mượn. |
| Kiểm kê thiết bị | Có | Có | Không | Không | Manager; việc kết thúc và xử lý chênh lệch theo quy trình kiểm kê. |
| Tạo phiếu mượn | Không áp dụng | Không áp dụng | Có | Có | Chỉ Giảng viên/Sinh viên; phiếu của Sinh viên phải có giảng viên bảo lãnh khi cần. |
| Xem lịch sử mượn | Toàn lab | Toàn lab | Của mình | Của mình | Manager xem toàn lab; người mượn chỉ xem bản ghi có `UserId` của mình. |
| Duyệt / từ chối phiếu mượn | Có | Có | Không | Không | Manager duyệt quyết định cuối. Tài sản được giữ chỗ trước bàn giao. |
| Bảo lãnh phiếu Sinh viên | Không áp dụng | Không áp dụng | Của mình | Không | Giảng viên chỉ xem và quyết định các phiếu có `TeacherId` của mình. |
| Bàn giao và xác nhận nhận thiết bị | Bàn giao | Bàn giao | Xác nhận phiếu của mình | Xác nhận phiếu của mình | Manager lập biên bản; người mượn xác nhận nhận. Không chuyển sang `BORROWED` chỉ bằng bước duyệt. |
| Xử lý trả, hỏng, nhắc trả | Có | Có | Không | Không | Manager kiểm tra trả, cập nhật tình trạng, bằng chứng và bồi thường. |
| Xem / tạo / sửa vật tư | Có | Có | Xem | Xem | Manager quản lý tồn kho và lô; người mượn chỉ xem dữ liệu cấp phát an toàn. |
| Tạo phiếu cấp phát vật tư | Không áp dụng | Không áp dụng | Có | Có | Chỉ Giảng viên/Sinh viên. |
| Duyệt → giao → xác nhận vật tư | Duyệt/giao | Duyệt/giao | Xác nhận phiếu của mình | Xác nhận phiếu của mình | Manager duyệt và giao theo lô; người nhận xác nhận đã nhận. |
| Tạo / hoàn tất bảo trì | Có | Có | Không | Không | Manager. Không tạo bảo trì cho tài sản đang có phiếu mượn `BORROW_PENDING` (kể cả phiếu nhiều tài sản), chờ bàn giao hoặc đang mượn. |
| Xóa phiếu bảo trì | Có | Không | Không | Không | Admin; không xóa phiếu đang thực hiện. |
| Lập / sửa / sinh kế hoạch bảo trì | Tạo/sửa/sinh | Tạo/sửa/sinh | Không | Không | Manager; kế hoạch cũng không được gắn hoặc sinh phiếu cho tài sản đang bị giữ bởi quy trình mượn. |
| Kiểm kê, xử lý chênh lệch, xuất báo cáo | Có | Có | Không | Không | Manager; báo cáo không mở cho người mượn. |
| Xem bồi thường / phạt | Dữ liệu được phép xem | Dữ liệu được phép xem | Của mình | Của mình | API lọc bản ghi theo người mượn; Manager được xác nhận thanh toán. |
| Quản lý người dùng | Đầy đủ | Không | Không | Không | Admin tạo/sửa/xóa/kích hoạt và đặt lại tài khoản. |
| Hồ sơ, mật khẩu, avatar | Của mình | Của mình | Của mình | Của mình | Mỗi người chỉ sửa hồ sơ và mật khẩu của chính mình. |
| Nhật ký hoạt động | Có | Không | Không | Không | Chỉ Admin; dữ liệu audit không phải màn hình vận hành chung. |
| Thông báo | Của mình | Của mình | Của mình | Của mình | Mỗi tài khoản chỉ đọc/đánh dấu thông báo của mình. |

## Quy tắc khóa tài sản trong quy trình mượn

Các trạng thái phiếu sau khóa tài sản được tham chiếu:

- `BORROW_PENDING` — chờ duyệt.
- `TEACHER_PENDING` — chờ giảng viên bảo lãnh.
- `APPROVAL_PROCESSING` — đang xử lý duyệt.
- `APPROVED` — đã duyệt, chờ lập biên bản bàn giao.

Trong các trạng thái trên, API từ chối sửa, điều chuyển, xóa và tạo/sinh bảo
trì. Quy tắc kiểm tra cả `BorrowRecord.EquipmentId` và các dòng
`BorrowRequestDetails.EquipmentId`, vì một phiếu có thể chứa nhiều tài sản.

