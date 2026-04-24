# Kịch Bản Test API Và Chức Năng

## Mục tiêu

Tài liệu này dùng để kiểm tra nhanh và đầy đủ các luồng chính của hệ thống đặt vé máy bay, bao gồm test API và test chức năng theo kịch bản nghiệp vụ.

## Phạm vi

- Xác thực người dùng
- Tìm kiếm chuyến bay
- Đặt vé, cập nhật, hủy vé
- Thanh toán và callback thanh toán
- Tra cứu và tải vé
- Hoàn tiền
- Quản trị flight, booking, promotion, user, report, dashboard
- Kiểm tra lỗi, phân quyền và validate dữ liệu

## Điều Kiện Trước Khi Test

- Ứng dụng chạy ở `http://localhost:5000`
- Swagger UI hoạt động
- Database đã migrate và seed dữ liệu mẫu
- Có sẵn công cụ Postman hoặc cURL
- Có tài khoản user và admin từ dữ liệu mẫu

## Dữ Liệu Mẫu Khuyến Nghị

- User thường: `user1@gmail.com` / `Test@1234`
- Admin: `admin@flightbooking.vn` / `Test@1234`
- Tuyến bay mẫu: `SGN -> HAN`, `HAN -> SGN`, `SGN -> DAD`

## Luồng Test Ưu Tiên

1. Đăng nhập lấy token
2. Tìm chuyến bay
3. Tạo booking
4. Thanh toán booking
5. Xem vé và tải vé
6. Hủy booking và kiểm tra refund
7. Test quyền admin
8. Test lỗi và validate

## Kịch Bản Test API

### 1. Authentication

#### TC-AUTH-01: Đăng ký tài khoản mới
- Method: `POST`
- Endpoint: `/api/v1/users/register`
- Dữ liệu vào: email hợp lệ, mật khẩu đạt chuẩn, tên, số điện thoại
- Kỳ vọng: `201 Created`, trả về thông tin user và token
- Kiểm tra: token tồn tại, email chưa trùng, dữ liệu được lưu đúng

#### TC-AUTH-02: Đăng nhập thành công
- Method: `POST`
- Endpoint: `/api/v1/users/login`
- Dữ liệu vào: email, password đúng
- Kỳ vọng: `200 OK`, trả về JWT token
- Kiểm tra: token dùng được ở endpoint yêu cầu xác thực

#### TC-AUTH-03: Đăng nhập sai mật khẩu
- Method: `POST`
- Endpoint: `/api/v1/users/login`
- Kỳ vọng: `400 Bad Request` hoặc lỗi nghiệp vụ tương ứng
- Kiểm tra: không trả token, thông báo rõ ràng

#### TC-AUTH-04: Verify email không cần đăng nhập
- Method: `POST`
- Endpoint: `/api/v1/users/verify-email?code={code}`
- Kỳ vọng: `200 OK`
- Kiểm tra: không cần Authorization header, code bị xóa sau khi xác minh

#### TC-AUTH-05: Quên mật khẩu
- Method: `POST`
- Endpoint: `/api/v1/users/forgot-password`
- Kỳ vọng: `200 OK`
- Kiểm tra: không lộ việc email có tồn tại hay không

#### TC-AUTH-06: Reset mật khẩu bằng mã
- Method: `POST`
- Endpoint: `/api/v1/users/reset-password`
- Kỳ vọng: `200 OK`
- Kiểm tra: mật khẩu mới đăng nhập được

#### TC-AUTH-07: Đổi mật khẩu khi đã đăng nhập
- Method: `POST`
- Endpoint: `/api/v1/users/change-password`
- Header: `Authorization: Bearer <token>`
- Kỳ vọng: `200 OK`
- Kiểm tra: mật khẩu cũ không còn đăng nhập được

### 2. Flight Search

#### TC-FLIGHT-01: Tìm chuyến bay hợp lệ
- Method: `POST`
- Endpoint: `/api/v1/flights/search`
- Dữ liệu vào: sân bay đi, sân bay đến, ngày bay, số khách, hạng ghế
- Kỳ vọng: `200 OK`, trả về danh sách chuyến bay phù hợp
- Kiểm tra: đúng tuyến bay, còn ghế, giá hiển thị đúng

#### TC-FLIGHT-02: Tìm chuyến bay không có kết quả
- Method: `POST`
- Endpoint: `/api/v1/flights/search`
- Kỳ vọng: `200 OK` với danh sách rỗng
- Kiểm tra: không lỗi 500, vẫn trả `success` đúng nếu hệ thống dùng wrapper

#### TC-FLIGHT-03: Tìm chuyến bay với dữ liệu không hợp lệ
- Method: `POST`
- Endpoint: `/api/v1/flights/search`
- Kỳ vọng: `400 Bad Request`
- Kiểm tra: sân bay trùng nhau, ngày sai định dạng, passengerCount <= 0 đều bị chặn

#### TC-FLIGHT-04: Xem chi tiết chuyến bay
- Method: `GET`
- Endpoint: `/api/v1/flights/{id}`
- Kỳ vọng: `200 OK`
- Kiểm tra: trả đúng flight number, thời gian, thông tin ghế

#### TC-FLIGHT-05: Xem số ghế theo hạng
- Method: `GET`
- Endpoint: `/api/v1/flights/{flightId}/seats/{seatClassId}`
- Kỳ vọng: `200 OK`
- Kiểm tra: số ghế đúng với dữ liệu chuyến bay

### 3. Booking

#### TC-BOOK-01: Tạo booking thành công
- Method: `POST`
- Endpoint: `/api/v1/bookings`
- Header: `Authorization: Bearer <token>`
- Dữ liệu vào: flightId, passenger list, seat class, contact info
- Kỳ vọng: `201 Created`
- Kiểm tra: bookingId sinh ra, ghế được giữ/giảm đúng

#### TC-BOOK-02: Tạo booking khi thiếu token
- Method: `POST`
- Endpoint: `/api/v1/bookings`
- Kỳ vọng: `401 Unauthorized`
- Kiểm tra: không tạo bản ghi mới

#### TC-BOOK-03: Xem danh sách booking của tôi
- Method: `GET`
- Endpoint: `/api/v1/bookings?page=1&pageSize=10`
- Header: `Authorization: Bearer <token>`
- Kỳ vọng: `200 OK`
- Kiểm tra: chỉ thấy booking của chính user đó

#### TC-BOOK-04: Xem chi tiết booking
- Method: `GET`
- Endpoint: `/api/v1/bookings/{id}`
- Kỳ vọng: `200 OK` với booking hợp lệ, `404` nếu không tồn tại
- Kiểm tra: user khác không xem được booking của người khác

#### TC-BOOK-05: Cập nhật booking
- Method: `PUT`
- Endpoint: `/api/v1/bookings/{id}`
- Kỳ vọng: `200 OK`
- Kiểm tra: thay đổi được thông tin cho phép, dữ liệu bị validate đúng

#### TC-BOOK-06: Hủy booking
- Method: `DELETE`
- Endpoint: `/api/v1/bookings/{id}`
- Kỳ vọng: `200 OK` hoặc `409 Conflict` tùy trạng thái booking
- Kiểm tra: lý do hủy được ghi nhận, booking chuyển trạng thái đúng

### 4. Payment

#### TC-PAY-01: Khởi tạo thanh toán
- Method: `POST`
- Endpoint: `/api/v1/payments`
- Header: `Authorization: Bearer <token>`
- Dữ liệu vào: bookingId, payment method, amount
- Kỳ vọng: `200 OK`
- Kiểm tra: trả đúng provider, trạng thái khởi tạo hợp lệ

#### TC-PAY-02: Xem trạng thái thanh toán
- Method: `GET`
- Endpoint: `/api/v1/payments/{paymentId}`
- Kỳ vọng: `200 OK`
- Kiểm tra: chỉ owner hoặc admin xem được

#### TC-PAY-03: Xem lịch sử thanh toán theo booking
- Method: `GET`
- Endpoint: `/api/v1/payments/booking/{bookingId}`
- Kỳ vọng: `200 OK`
- Kiểm tra: lịch sử khớp booking

#### TC-PAY-04: Callback thanh toán từ nhà cung cấp
- Method: `POST`
- Endpoint: `/api/v1/payments/{paymentId}/callback`
- Header: không bắt buộc auth
- Kỳ vọng: `200 OK`
- Kiểm tra: signature, amount, transactionId được xác thực; callback sai bị từ chối xử lý

### 5. Ticket

#### TC-TICKET-01: Xem vé theo mã vé
- Method: `GET`
- Endpoint: `/api/v1/tickets/{ticketNumber}`
- Kỳ vọng: `200 OK`
- Kiểm tra: thông tin vé đúng với booking đã thanh toán

#### TC-TICKET-02: Xem danh sách vé theo booking
- Method: `GET`
- Endpoint: `/api/v1/tickets/booking/{bookingId}`
- Kỳ vọng: `200 OK`
- Kiểm tra: số lượng vé khớp số hành khách

#### TC-TICKET-03: Đổi vé sang chuyến bay khác
- Method: `PUT`
- Endpoint: `/api/v1/tickets/{ticketNumber}/change`
- Kỳ vọng: `200 OK` hoặc `400 Bad Request`
- Kiểm tra: chuyến bay mới hợp lệ, ghế còn trống, giá chênh lệch được tính đúng nếu có

#### TC-TICKET-04: Tải vé
- Method: `GET`
- Endpoint: `/api/v1/tickets/{ticketNumber}/download?format=pdf`
- Kỳ vọng: file PDF hoặc HTML được tải về
- Kiểm tra: file mở được, nội dung đúng thông tin vé

### 6. Refund

#### TC-REFUND-01: Tạo yêu cầu hoàn tiền
- Method: `POST`
- Endpoint: `/api/v1/refunds`
- Header: `Authorization: Bearer <token>`
- Kỳ vọng: `200 OK` hoặc `201 Created` tùy thiết kế service
- Kiểm tra: booking đủ điều kiện hoàn tiền

#### TC-REFUND-02: Xem chi tiết refund
- Method: `GET`
- Endpoint: `/api/v1/refunds/{refundId}`
- Kỳ vọng: `200 OK`
- Kiểm tra: trạng thái refund đúng

#### TC-REFUND-03: Xem refund theo booking
- Method: `GET`
- Endpoint: `/api/v1/refunds/booking/{bookingId}`
- Kỳ vọng: `200 OK`
- Kiểm tra: dữ liệu trả về đúng booking

### 7. Admin

#### TC-ADMIN-01: Xem dashboard metrics
- Method: `GET`
- Endpoint: `/api/v1/admin/dashboard/metrics`
- Header: `Authorization: Bearer <admin_token>`
- Kỳ vọng: `200 OK`
- Kiểm tra: số liệu tổng quan chính xác

#### TC-ADMIN-02: Xem health và activity
- Method: `GET`
- Endpoint: `/api/v1/admin/dashboard/health` và `/activity`
- Kỳ vọng: `200 OK`
- Kiểm tra: trạng thái hệ thống, log hoạt động hiển thị đúng

#### TC-ADMIN-03: Quản lý flight
- Method: `POST`, `PUT`, `DELETE`
- Endpoint: `/api/v1/admin/flights`
- Kỳ vọng: tạo/sửa/xóa thành công với admin
- Kiểm tra: user thường không truy cập được

#### TC-ADMIN-04: Quản lý route
- Method: `POST`, `PUT`, `GET`
- Endpoint: `/api/v1/admin/flights/routes`
- Kỳ vọng: `200 OK` hoặc `201 Created`
- Kiểm tra: route được lưu và tra cứu đúng

#### TC-ADMIN-05: Quản lý booking admin
- Method: `GET`, `DELETE`
- Endpoint: `/api/v1/admin/bookings/{bookingId}`
- Kỳ vọng: admin xem/xử lý booking được

#### TC-ADMIN-06: Danh sách hoàn tiền chờ duyệt
- Method: `GET`
- Endpoint: `/api/v1/admin/bookings/refunds/pending`
- Kỳ vọng: `200 OK`
- Kiểm tra: chỉ admin truy cập được

#### TC-ADMIN-07: Quản lý promotion
- Method: `POST`, `PUT`, `DELETE`, `GET`
- Endpoint: `/api/v1/admin/promotions`
- Kỳ vọng: tạo, cập nhật, xóa, xem khuyến mãi hoạt động đúng

#### TC-ADMIN-08: Quản lý user
- Method: `GET`, `PUT`, `POST`, `DELETE`
- Endpoint: `/api/v1/admin/users`
- Kỳ vọng: admin đổi trạng thái, gán vai trò, xem booking theo user

#### TC-ADMIN-09: Báo cáo
- Method: `POST`, `GET`
- Endpoint: `/api/v1/reports` và các endpoint report con
- Kỳ vọng: tạo report, tải report, xem doanh thu / booking / user report

### 8. Search

#### TC-SEARCH-01: Tìm kiếm tổng quát
- Method: `GET`
- Endpoint: `/api/v1/search/global`
- Kỳ vọng: `200 OK`
- Kiểm tra: tìm được theo keyword qua flights, bookings, users nếu cấu hình hỗ trợ

#### TC-SEARCH-02: Tìm kiếm flights/bookings/users riêng
- Method: `POST`
- Endpoint: `/api/v1/search/flights`, `/api/v1/search/bookings`, `/api/v1/search/users`
- Kỳ vọng: `200 OK`
- Kiểm tra: kết quả lọc đúng theo từ khóa

### 9. Notifications

#### TC-NOTI-01: Xem thông báo
- Method: `GET`
- Endpoint: `/api/v1/notifications`
- Header: `Authorization: Bearer <token>`
- Kỳ vọng: `200 OK`
- Kiểm tra: danh sách thông báo đúng user

#### TC-NOTI-02: Cập nhật cài đặt thông báo
- Method: `PUT`
- Endpoint: `/api/v1/notifications/settings`
- Kỳ vọng: `200 OK`
- Kiểm tra: thay đổi được email, SMS, push nếu có

## Kịch Bản Test Chức Năng End-to-End

### Luồng 1: Đăng ký đến đặt vé
1. Người dùng đăng ký tài khoản mới.
2. Xác minh email bằng code.
3. Đăng nhập lấy token.
4. Tìm chuyến bay theo tuyến và ngày bay.
5. Chọn chuyến bay và tạo booking.
6. Xác nhận booking được tạo thành công.
7. Thanh toán booking.
8. Kiểm tra vé được sinh ra.

### Luồng 2: Hủy vé và hoàn tiền
1. Đăng nhập bằng user đã có booking.
2. Mở chi tiết booking.
3. Hủy booking theo lý do hợp lệ.
4. Kiểm tra booking chuyển trạng thái đúng.
5. Kiểm tra refund được tạo.
6. Admin xem danh sách refund chờ duyệt.

### Luồng 3: Đổi vé
1. Mở vé hợp lệ.
2. Gửi yêu cầu đổi sang chuyến bay khác.
3. Kiểm tra chuyến mới còn ghế.
4. Xác nhận vé được cập nhật.

### Luồng 4: Admin vận hành
1. Admin đăng nhập.
2. Xem dashboard metrics.
3. Tạo hoặc cập nhật flight.
4. Tạo promotion mới.
5. Tìm booking hoặc user.
6. Xuất report doanh thu hoặc booking.

## Kịch Bản Test Lỗi Và Phân Quyền

### Lỗi đầu vào
- Email sai định dạng
- Password quá ngắn
- Thiếu trường bắt buộc
- ID không tồn tại
- Ngày bay không hợp lệ
- passengerCount <= 0

### Phân quyền
- User thường không gọi được endpoint admin
- Thiếu token phải trả `401`
- Token sai hoặc hết hạn phải trả `401`
- Truy cập tài nguyên của người khác phải bị từ chối

### Trạng thái dữ liệu
- Booking đã hủy không được thanh toán tiếp
- Ticket chỉ tồn tại khi booking hợp lệ
- Refund không tạo cho booking không đủ điều kiện
- Callback thanh toán sai chữ ký không được xác nhận

## Mẫu Ghi Kết Quả

| Test Case | Endpoint | Kết quả mong đợi | Kết quả thực tế | Pass/Fail |
|---|---|---:|---:|---|
| TC-AUTH-02 | `/api/v1/users/login` | 200 |  |  |
| TC-FLIGHT-01 | `/api/v1/flights/search` | 200 |  |  |
| TC-BOOK-01 | `/api/v1/bookings` | 201 |  |  |
| TC-PAY-01 | `/api/v1/payments` | 200 |  |  |

## Tiêu Chí Đạt

- Các luồng chính chạy được từ đầu đến cuối
- Không có lỗi `500` ở các case hợp lệ
- Phân quyền đúng với user và admin
- Validate dữ liệu đúng cho case lỗi
- Booking, payment, ticket và refund liên kết đúng nhau

## Gợi Ý Công Cụ Test

- Swagger UI để test nhanh từng endpoint
- Postman để chạy theo collection và lưu token
- cURL hoặc PowerShell để test lặp lại nhanh
