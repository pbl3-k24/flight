# VNPay "Invalid Data Format" - Bản Sửa Lỗi

## Vấn Đề
VNPay sandbox trả về lỗi **"Invalid data format"** khi tạo payment URL.

## Nguyên Nhân Đã Xác Định

### 1. **IP Address Không Hợp Lệ**
- **Trước**: `vnp_IpAddr = "127.0.0.1"` (localhost)
- **Vấn đề**: VNPay sandbox không chấp nhận IP localhost
- **Sau**: `vnp_IpAddr = "113.161.84.26"` (IP public giả lập)

### 2. **TxnRef Quá Dài**
- **Trước**: `BOOKING{bookingId}{milliseconds}` → Ví dụ: `BOOKING11714838400000` (quá dài)
- **Vấn đề**: VNPay giới hạn độ dài TxnRef
- **Sau**: `{bookingId}{seconds}` → Ví dụ: `11714838400` (ngắn hơn)

### 3. **OrderInfo Có Ký Tự Đặc Biệt**
- **Trước**: `"Thanh toan booking #123"` (có ký tự `#`)
- **Vấn đề**: VNPay chỉ chấp nhận chữ, số, khoảng trắng, dấu gạch ngang
- **Sau**: `"Thanh toan booking 123"` (loại bỏ `#`)

### 4. **Thiếu Giới Hạn Độ Dài OrderInfo**
- **Thêm**: Giới hạn OrderInfo tối đa 255 ký tự theo yêu cầu VNPay

### 5. **Loại Bỏ vnp_ExpireDate**
- **Trước**: Có tham số `vnp_ExpireDate`
- **Vấn đề**: Tham số này không bắt buộc và có thể gây lỗi nếu format sai
- **Sau**: Loại bỏ để đơn giản hóa

## Các File Đã Sửa

### 1. `Infrastructure/ExternalServices/VnpayPaymentProvider.cs`
```csharp
// Thay đổi chính:
- Đổi IP từ 127.0.0.1 → 113.161.84.26
- Rút ngắn TxnRef: {bookingId}{seconds} thay vì BOOKING{bookingId}{milliseconds}
- Cải thiện regex cho OrderInfo: cho phép dấu gạch ngang
- Thêm giới hạn độ dài OrderInfo (max 255 chars)
- Loại bỏ vnp_ExpireDate
- Thêm log chi tiết hơn (TmnCode, CreateDate)
```

### 2. `Application/Services/PaymentService.cs`
```csharp
// Loại bỏ ký tự # trong OrderDescription
- Trước: $"Thanh toan booking #{booking.BookingCode}"
+ Sau:  $"Thanh toan booking {booking.BookingCode}"
```

## Cách Kiểm Tra

### Bước 1: Build lại project
```bash
dotnet build
```

### Bước 2: Chạy application
```bash
dotnet run
```

### Bước 3: Tạo booking và payment
Sử dụng Bruno hoặc Postman để:
1. Đăng nhập và lấy JWT token
2. Tìm kiếm chuyến bay
3. Tạo booking
4. Khởi tạo payment với VNPay

### Bước 4: Kiểm tra log
Tìm các dòng log sau:
```
[VNPAY] Payment URL generated for booking {BookingId}
[VNPAY] TmnCode: DEMOV210
[VNPAY] Amount: {Amount} VND (x100 = {AmountParam})
[VNPAY] TxnRef: {TxnRef}
[VNPAY] OrderInfo: {OrderInfo}
[VNPAY] CreateDate: {CreateDate}
[VNPAY] Hash data: {HashData}
[VNPAY] Secure hash: {SecureHash}
```

### Bước 5: Test payment URL
Copy payment URL từ response và mở trong browser. Nếu không còn lỗi "Invalid data format", nghĩa là đã fix thành công!

## Tham Số VNPay Quan Trọng

| Tham số | Bắt buộc | Format | Ví dụ |
|---------|----------|--------|-------|
| vnp_TmnCode | ✅ | String | DEMOV210 |
| vnp_Amount | ✅ | Number (VND × 100) | 10000000 (= 100,000 VND) |
| vnp_TxnRef | ✅ | Alphanumeric, max 100 chars | 11714838400 |
| vnp_OrderInfo | ✅ | Alphanumeric + space + dash, max 255 | Thanh toan booking TEST001 |
| vnp_IpAddr | ✅ | Valid IP (not localhost) | 113.161.84.26 |
| vnp_CreateDate | ✅ | yyyyMMddHHmmss | 20260503221339 |
| vnp_Version | ✅ | String | 2.1.0 |
| vnp_Command | ✅ | String | pay |
| vnp_CurrCode | ✅ | String | VND |
| vnp_Locale | ✅ | vn or en | vn |
| vnp_ReturnUrl | ✅ | Valid URL | http://localhost:5042/api/v1/payments/vnpay-return |
| vnp_OrderType | ✅ | String | other |
| vnp_SecureHash | ✅ | HMAC-SHA512 hex | (auto generated) |

## Lưu Ý Quan Trọng

1. **Múi giờ**: VNPay yêu cầu thời gian theo múi giờ Việt Nam (GMT+7)
2. **Hash Algorithm**: Phải dùng HMAC-SHA512 và convert sang lowercase hex
3. **Thứ tự tham số**: Phải sort theo alphabet khi tạo hash
4. **URL Encoding**: Chỉ encode values trong query string, KHÔNG encode vnp_SecureHash
5. **IP Address**: Không được dùng 127.0.0.1 hoặc localhost

## Nếu Vẫn Còn Lỗi

### Kiểm tra lại:
1. ✅ TmnCode và HashSecret có đúng không?
2. ✅ Thời gian tạo có đúng múi giờ GMT+7 không?
3. ✅ TxnRef có unique không? (không trùng với transaction trước)
4. ✅ Amount có đúng format (VND × 100) không?
5. ✅ OrderInfo có chứa ký tự đặc biệt không?

### Debug bằng log:
```csharp
_logger.LogInformation("[VNPAY] Hash data: {HashData}", hashData);
_logger.LogInformation("[VNPAY] Secure hash: {SecureHash}", secureHash);
_logger.LogInformation("[VNPAY] Full URL: {URL}", paymentUrl);
```

Copy hash data và tự tính hash bằng tool online để so sánh:
- https://www.freeformatter.com/hmac-generator.html
- Algorithm: SHA512
- Secret Key: RAOEXHYVSDDIIENYWSLDIIZTANXUXZFJ
- Message: (paste hash data)
- Output: Lowercase hex

## Tài Liệu Tham Khảo
- VNPay Sandbox: https://sandbox.vnpayment.vn
- VNPay Demo Account: TmnCode = DEMOV210
- GitHub vnpayjs: https://github.com/lehuygiang28/vnpay
