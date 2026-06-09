# Tài liệu Ôn tập & Phân tích Toàn diện Backend & Database (Hệ thống Đặt vé máy bay)

Tài liệu này cung cấp một cái nhìn sâu sắc, chi tiết từng dòng code và cấu trúc bảng CSDL của dự án Flight Booking. Đây là cẩm nang giúp bạn tự tin trả lời bất kỳ câu hỏi nào từ giảng viên hướng dẫn hoặc hội đồng bảo vệ PBL3.

---

## PHẦN 1: BẢN ĐỒ KIẾN TRÚC CODE BACKEND (.NET CORE)

Kiến trúc backend của bạn được xây dựng theo mô hình **Clean Architecture**. Dưới đây là cách mã nguồn phân chia trách nhiệm giữa các tầng (Layers):

### 1. Presentation Layer (API Controllers)
Nằm tại mục `API/Controllers/`. Nhiệm vụ duy nhất là đón nhận yêu cầu từ Web Frontend (ReactJS), phân tích URL/Query/Body và xác thực bảo mật JWT Token, sau đó chuyển giao dữ liệu xuống tầng Service.
*   **`UsersController.cs`:** Đăng ký, đăng nhập, đổi mật khẩu, xác thực OTP, quên mật khẩu.
*   **`BookingsController.cs` & `BookingsAdminController.cs`:** Tạo đơn đặt vé, hủy vé, thêm hành lý cho người dùng thường và quản lý đơn đặt vé cho Admin.
*   **`FlightsController.cs` & `FlightsAdminController.cs`:** Người dùng tìm kiếm chuyến bay, Admin thêm chuyến bay mới, sửa giá vé, hủy chuyến bay.
*   **`PromotionsController.cs` & `PromotionsAdminController.cs`:** Người dùng áp mã giảm giá, Admin thêm/sửa/xóa mã code.

### 2. Application Layer (Logic Nghiệp Vụ - Services & Interfaces)
Nằm tại mục `API/Application/Services/` và `API/Application/Interfaces/`. Đây là **nơi chứa toàn bộ logic xử lý chính** của hệ thống (nơi cô giáo sẽ hỏi nhiều nhất về code).
*   **`BookingService.cs` (124 KB - File lớn nhất):** Xử lý quy trình đặt vé máy bay, tính tiền vé dựa vào loại khách (Người lớn/Trẻ em/Em bé), áp dụng mã giảm giá, tạm khóa ghế trong 1 tiếng, đổi vé, xử lý sự cố.
*   **`PricingService.cs`:** Tính giá vé động dựa vào thời gian, ngày bay và ngày khởi hành (Dynamic Pricing).
*   **`PaymentService.cs`:** Tạo URL chuyển hướng sang VNPAY, nhận kết quả callback, xác thực chữ ký SHA512 bảo mật.
*   **`BackgroundJobService.cs`:** Hosted Service chạy ngầm tự động quét và giải phóng các ghế đã đặt nhưng không thanh toán quá 1 tiếng.
*   **`RefundService.cs`:** Xử lý tính toán số tiền hoàn khi khách hàng yêu cầu hủy vé hoặc chuyến bay bị hủy bởi Admin.

### 3. Infrastructure Layer (Truy cập dữ liệu & Liên kết bên ngoài)
Nằm tại mục `API/Infrastructure/`.
*   **`FlightBookingDbContext.cs`:** Trái tim kết nối dữ liệu. Thực hiện ánh xạ các Entity C# sang các bảng PostgreSQL.
*   **`DatabaseSchemaSync.cs`:** File tự viết để đồng bộ cấu trúc bảng tự động khi chạy dự án thay vì dùng EF Migrations truyền thống (tránh lỗi xung đột metadata).
*   **`Repositories/`:** Chứa các hàm CRUD cơ sở dữ liệu của từng đối tượng (User, Flight, Booking...).

### 4. Domain Layer (Thực thể lõi - Entities & Enums)
Nằm tại mục `API/Domain/`. Chứa các lớp thực thể thuần (User, Flight, Ticket, Booking...) tương ứng với các bảng trong DB và các Enum quy định trạng thái (ví dụ: `BookingStatus.Pending = 0`, `BookingStatus.Confirmed = 1`, `BookingStatus.Cancelled = 2`).

---

## PHẦN 2: PHÂN TÍCH DATABASE SCHEMA & CÁC MỐI QUAN HỆ (RELATIONS)

Hội đồng chấm thi rất chú trọng vào sơ đồ cơ sở dữ liệu. Dưới đây là phân tích chi tiết từng mối quan hệ để bạn giải trình:

```
[Airports] (Sân bay) < 1 --- N > [Routes] (Tuyến bay)
                                     |
                                     1
                                     |
                                     N
                             [FlightDefinitions] (Khung giờ bay cố định)
                                     |
                                     1
                                     |
                                     N
                                 [Flights] (Chuyến bay cụ thể theo ngày)
```

### 1. Mối quan hệ phân quyền người dùng
*   `Users` (1) ---- (N) `UserRoles` (N) ---- (1) `Roles`
    *   *Mô tả:* Mối quan hệ nhiều - nhiều giữa Người dùng và Quyền (User / Admin). Được giải quyết bằng bảng trung gian `UserRoles` chứa khóa ngoại `UserId` và `RoleId`.

### 2. Mối quan hệ cấu trúc chuyến bay (Dễ bị hỏi nhất)
*   `Airports` (1) ---- (N) `Routes`
    *   *Mô tả:* Một sân bay có thể nằm trong nhiều tuyến bay (vừa là nơi đi, vừa là nơi đến). Bảng `Routes` chứa 2 khóa ngoại là `DepartureAirportId` và `ArrivalAirportId` nối về bảng `Airports`.
*   `Routes` (1) ---- (N) `FlightDefinitions`
    *   *Mô tả:* Một chặng bay (ví dụ: HAN -> SGN) có thể có nhiều khung giờ bay cố định trong ngày (VN123 bay lúc 8:00, VN456 bay lúc 15:00).
*   `FlightDefinitions` (1) ---- (N) `Flights`
    *   *Mô tả:* Một khung giờ cố định sẽ được áp dụng để tạo ra hàng loạt chuyến bay thực tế trong các ngày khác nhau (VN123 ngày 10/06, VN123 ngày 11/06...).
*   `Flights` (1) ---- (N) `FlightSeatInventories`
    *   *Mô tả:* Một chuyến bay cụ thể sẽ có bảng thống kê số lượng ghế trống và giá vé của từng hạng ghế (Eco, Biz). Bảng này giúp cập nhật giá vé động theo thời gian thực mà không làm ảnh hưởng đến giá gốc ban đầu.

### 3. Mối quan hệ đặt vé và xuất vé
*   `Bookings` (1) ---- (N) `BookingPassengers`
    *   *Mô tả:* Một đơn đặt vé có thể mua cho nhiều hành khách đi cùng nhau (Họ tên, ngày sinh, loại khách: Người lớn/Trẻ em/Em bé).
*   `BookingPassengers` (1) ---- (N) `Tickets`
    *   *Mô tả:* Với mỗi hành khách trong đơn đặt vé, sau khi thanh toán thành công, hệ thống sẽ xuất ra một vé máy bay (`Ticket`) riêng biệt, gắn với một số ghế cụ thể (`SeatNumber`) và mã code vé riêng.

---

## PHẦN 3: REVIEW SÂU CHI TIẾT TỪNG SERVICE CỐT LÕI (CODE WALKTHROUGH)

Dưới đây là phân tích chi tiết logic chạy trong các Service quan trọng nhất của hệ thống:

### 1. Phân tích `PricingService.cs` (Logic Tính Giá Vé Động - Dynamic Pricing)
Hệ thống của bạn tính toán giá vé động tự động theo hệ số nhân nhân với giá gốc:
`Price_Current = Price_Base * Multiplier_Time * Multiplier_Hour * Multiplier_DayOfWeek`

#### a. Hệ số nhân thời gian cận kề giờ bay:
*   Càng sát giờ bay giá càng đắt (do nhu cầu mua vé gấp cao):
    *   **Dưới 6 tiếng:** Nhân **1.80** (Tăng 80% giá vé).
    *   **Dưới 24 tiếng:** Nhân **1.50** (Tăng 50%).
    *   **Từ 1 đến 2 ngày:** Nhân **1.30** (Tăng 30%).
    *   **Từ 2 đến 6 ngày:** Nhân **1.15** (Tăng 15%).
    *   **Từ 7 đến 14 ngày:** Giữ nguyên giá gốc (Nhân **1.00**).
    *   **Từ 15 đến 30 ngày:** Giảm nhẹ (Nhân **0.95** - giảm 5%).
    *   **Đặt sớm trên 30 ngày:** Ưu đãi đặt sớm (Nhân **0.85** - giảm 15%).

#### b. Hệ số nhân khung giờ bay trong ngày:
*   Các khung giờ đẹp (sáng sớm hoặc chiều tối) giá sẽ đắt hơn các chuyến bay đêm muộn:
    *   **0h - 6h sáng:** Nhân **0.85** (Giảm 15% vì giờ bay xấu, ít người đi).
    *   **6h - 9h sáng:** Nhân **1.20** (Giờ vàng, nhiều người bay đi công tác/du lịch).
    *   **9h - 16h chiều:** Giữ nguyên giá (Nhân **1.00**).
    *   **16h - 20h tối:** Nhân **1.25** (Giờ vàng cao điểm nhất).
    *   **20h - 24h đêm:** Nhân **1.05**.

#### c. Hệ số nhân ngày trong tuần:
*   Bay cuối tuần (thứ 6, thứ 7, chủ nhật) giá cao hơn ngày thường:
    *   Thứ 2, 3, 4: Giảm nhẹ hoặc giữ nguyên (Nhân **0.90 - 1.00**).
    *   Thứ 6, Chủ nhật: Nhân **1.15** (Tăng 15%).
    *   Thứ 7: Nhân **1.20** (Tăng 20% - ngày cao điểm đi du lịch).

#### d. Tự động làm tròn tiền:
*   Giá vé sau khi nhân các hệ số sẽ lẻ. Hệ thống sử dụng hàm làm tròn đến **10.000đ** gần nhất.

---

### 2. Phân tích `BookingService.cs` (Quy trình đặt vé và giữ ghế)
Hàm `CreateBookingAsync` và `CancelBookingAsync` thực hiện các nghiệp vụ vô cùng chặt chẽ:

#### a. Ràng buộc nghiệp vụ (Validation Rules):
*   Chuyến bay phải khởi hành cách thời điểm đặt ít nhất **2 tiếng** (tránh đặt vé khi máy bay sắp cất cánh).
*   Kiểm tra số ghế trống trong bảng `FlightSeatInventories` của hạng ghế đã chọn. Nếu không đủ ghế, báo lỗi ngay lập tức.
*   **Ràng buộc độ tuổi hành khách:** Trong danh sách hành khách, hệ thống kiểm tra ngày sinh và bắt buộc phải có **ít nhất 1 người lớn trên 14 tuổi** đi kèm.

#### b. Cơ chế khóa tạm thời (Seat Holding):
*   Khi người dùng bấm đặt vé, hệ thống thực hiện trừ số ghế trống và tăng số ghế giữ trong Database:
    ```csharp
    inventory.AvailableSeats -= passengerCount;
    inventory.HeldSeats += passengerCount;
    ```
*   Tạo bản ghi đơn đặt vé với trạng thái `BookingStatus.Pending` (Chờ thanh toán) và thiết lập trường `ExpiresAt = DateTime.UtcNow.AddHours(1)` (Đơn hàng chỉ có hiệu lực thanh toán trong 1 giờ).

#### c. Áp dụng mã giảm giá (Promotion Verification):
*   Hệ thống kiểm tra mã giảm giá: Có tồn tại? Còn hạn sử dụng? Có đạt mức chi tiêu tối thiểu của đơn hàng không?
*   Nếu hợp lệ, trừ tiền giảm giá vào tổng hóa đơn đặt vé và ghi nhận tạm thời 1 lượt sử dụng của mã giảm giá.

---

### 3. Phân tích `PaymentService.cs` (Tích hợp thanh toán VNPAY & Bảo mật)
Quy trình thanh toán hoạt động thông qua cơ chế chữ ký điện tử an toàn:

#### a. Tạo yêu cầu thanh toán (Redirect URL):
*   Khi người dùng chọn thanh toán, Backend gom các thông tin gồm: Mã đơn vé, Số tiền, Thời gian tạo, Địa chỉ IP của client, và Mã định danh VNPAY.
*   Backend sắp xếp các tham số này theo thứ tự alphabet (quy định bắt buộc của VNPAY).
*   Sử dụng thuật toán **HMACSHA512** với khóa bí mật (Secret Key) để mã hóa toàn bộ chuỗi tham số thành một chuỗi **chữ ký bảo mật (vnp_SecureHash)**.
*   Tạo URL chuyển hướng chứa các thông tin đơn hàng và mã Hash này gửi về cho Frontend hiển thị trang VNPAY.

#### b. Nhận kết quả và xác thực (Callback/IPN):
*   Khi người dùng thanh toán xong tại cổng ngân hàng, VNPAY gửi request callback chứa kết quả giao dịch về API của Backend.
*   Backend thực hiện trích xuất toàn bộ tham số nhận được (ngoại trừ tham số `vnp_SecureHash`), sắp xếp lại và tự mã hóa HMACSHA512 để tạo ra mã Hash đối chứng.
*   **Kiểm tra tính toàn vẹn dữ liệu:**
    *   Mã Hash tự tính toán phải trùng khớp 100% với mã Hash VNPAY gửi về.
    *   Số tiền thanh toán thực tế phải trùng khớp với số tiền của đơn vé lưu trong DB.
*   Nếu khớp:
    1.  Cập nhật trạng thái đơn vé từ `Pending` -> `Confirmed`.
    2.  Chuyển ghế từ trạng thái tạm giữ sang đã bán:
        ```csharp
        inventory.HeldSeats -= passengerCount;
        inventory.SoldSeats += passengerCount;
        ```
    3.  Tạo bản ghi dữ liệu vé (`Ticket`) chính thức và số ghế cho khách hàng.
    4.  Gửi email xác nhận kèm vé điện tử.

---

### 4. Phân tích `BackgroundJobService.cs` (Tác vụ chạy ngầm giải phóng ghế)
Hệ thống sử dụng **Hosted Service** chạy nền của .NET (kế thừa `BackgroundService`).

*   Cứ mỗi **1 phút**, tác vụ này sẽ tự động thức dậy.
*   Quét trong bảng `Bookings` tìm các đơn hàng ở trạng thái `Pending` (Chờ thanh toán) có thời gian hiện tại vượt quá thời gian hết hạn (`ExpiresAt` - tức là quá 1 tiếng mà khách hàng không thanh toán).
*   Nếu phát hiện đơn quá hạn, hệ thống bắt đầu mở một Database Transaction để hủy đơn an toàn:
    1.  Cập nhật trạng thái Booking từ `Pending` -> `Cancelled` (Đã hủy).
    2.  Tìm số lượng ghế tương ứng của đơn hàng này và giải phóng chúng:
        ```csharp
        inventory.HeldSeats -= passengerCount;
        inventory.AvailableSeats += passengerCount;
        ```
    3.  Nếu đơn vé có áp dụng mã giảm giá, hệ thống cộng trả lại 1 lượt sử dụng cho mã giảm giá đó.
    4.  Lưu toàn bộ thay đổi và commit Transaction.

---

## PHẦN 4: BỘ CÂU HỎI PHẢN BIỆN MỞ RỘNG (DÀNH CHO ĐIỂM 9 - 10)

Hãy chuẩn bị kỹ các câu trả lời cho những câu hỏi nâng cao sau để chinh phục hoàn toàn hội đồng phản biện:

### 💬 Câu 1: Làm thế nào hệ thống đảm bảo tính nhất quán (Consistency) khi thanh toán VNPAY thành công nhưng Server gửi email xác nhận bị lỗi?
*   **Cách trả lời:**
    *   Hệ thống phân tách rõ ràng giữa **giao dịch cốt lõi** (thay đổi trạng thái đơn hàng, xuất vé trên DB) và **tác vụ thông báo** (gửi email).
    *   Logic xác nhận thanh toán thành công được bọc trong một **Database Transaction**. Nếu việc ghi nhận đơn hàng hoặc xuất vé vào DB thất bại (ví dụ: mất kết nối mạng DB), hệ thống sẽ Rollback (hủy bỏ) giao dịch để đảm bảo dữ liệu nhất quán, VNPAY sẽ nhận được phản hồi lỗi để hoàn tiền cho khách.
    *   Việc gửi email được thực hiện bên ngoài Transaction sau khi DB đã commit thành công. Nếu gửi email bị lỗi (ví dụ: SMTP Server bị nghẽn), hệ thống sẽ ghi log lỗi gửi mail nhưng **đơn vé vẫn được xác nhận thành công** để khách hàng vẫn có vé bay. Khách hàng vẫn có thể xem vé trực tiếp trên giao diện lịch sử đặt vé của Website mà không bị mất vé.

### 💬 Câu 2: Trong code DbContext có xử lý `ApplyEntityAuditAndNormalizeTimes`, đây là cơ chế gì và tại sao lại cần nó?
*   **Cách trả lời:**
    *   Đây là cơ chế **Tự động ghi nhật ký chỉnh sửa (Audit Log) và chuẩn hóa thời gian** tích hợp trực tiếp khi lưu dữ liệu.
    *   **Tự động cập nhật thời gian sửa đổi:** Mỗi khi có hành động cập nhật (Update) dữ liệu của bất kỳ bảng nào, DbContext sẽ tự động tìm trường `UpdatedAt` của thực thể đó và gán bằng thời gian hiện tại (`DateTime.UtcNow`).
    *   **Tự động tăng phiên bản dữ liệu (Version):** Hỗ trợ cơ chế **Khóa lạc quan (Optimistic Concurrency Control)**. Mỗi lần thực thể bị sửa đổi, trường `Version` tự động cộng thêm 1.
    *   **Chuẩn hóa múi giờ:** Toàn bộ dữ liệu kiểu thời gian (`DateTime`) trước khi ghi vào Database đều được hàm `NormalizeDateTimePropertiesToUtc` tự động chuyển đổi từ giờ Việt Nam (GMT+7) sang giờ chuẩn quốc tế **UTC (GMT+0)**.

### 💬 Câu 3: Dữ liệu của hệ thống được tạo mẫu ban đầu (Seed Data) như thế nào?
*   **Cách trả lời:**
    *   Hệ thống sử dụng cơ chế tự động đọc dữ liệu từ các file cấu hình định dạng **CSV** (đặt trong thư mục seed-data) để khởi tạo dữ liệu mẫu cho Database khi dự án khởi chạy lần đầu.
    *   Cơ chế này đọc danh sách các sân bay lớn của Việt Nam, thông tin các hãng bay, thông số máy bay mặc định và sơ đồ ghế mẫu để nạp tự động vào hệ thống, giúp dự án ngay lập tức hoạt động được sau khi cài đặt mà không cần nhập thủ công bằng tay.
