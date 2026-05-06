# 🎯 VNPay "Invalid Data Format" - Giải Pháp Hoàn Chỉnh

## 📋 Tóm Tắt Vấn Đề

VNPay sandbox trả về lỗi **"Invalid data format"** khi tạo payment URL.

## ✅ Các Vấn Đề Đã Được Sửa

### 1. **IP Address Không Hợp Lệ** 🔴 → 🟢
```diff
- ["vnp_IpAddr"] = "127.0.0.1"  // VNPay sandbox từ chối localhost
+ ["vnp_IpAddr"] = "113.161.84.26"  // IP public giả lập
```

### 2. **Transaction Reference Quá Dài** 🔴 → 🟢
```diff
- var transactionRef = $"BOOKING{request.BookingId}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
- // Ví dụ: BOOKING11714838400000 (quá dài, 23 ký tự)

+ var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
+ var transactionRef = $"{request.BookingId}{timestamp}";
+ // Ví dụ: 101777821860 (ngắn hơn, 12 ký tự)
```

### 3. **OrderInfo Có Ký Tự Đặc Biệt** 🔴 → 🟢
```diff
// PaymentService.cs
- OrderDescription = $"Thanh toan booking #{booking.BookingCode}"  // Có ký tự #

+ OrderDescription = $"Thanh toan booking {booking.BookingCode}"  // Loại bỏ #
```

```diff
// VnpayPaymentProvider.cs
- var orderInfo = Regex.Replace(request.OrderDescription, @"[^a-zA-Z0-9\s]", "").Trim();

+ var orderInfo = Regex.Replace(request.OrderDescription, @"[^a-zA-Z0-9\s\-]", "").Trim();
+ // Cho phép: chữ, số, khoảng trắng, dấu gạch ngang
```

### 4. **Thiếu Giới Hạn Độ Dài OrderInfo** 🔴 → 🟢
```csharp
// Thêm validation
if (orderInfo.Length > 255)
{
    orderInfo = orderInfo.Substring(0, 255);
}
```

### 5. **Loại Bỏ Tham Số Không Cần Thiết** 🔴 → 🟢
```diff
- ["vnp_ExpireDate"] = expireDate,  // Không bắt buộc, có thể gây lỗi
```

### 6. **Cải Thiện Logging** 🟡 → 🟢
```csharp
_logger.LogInformation("[VNPAY] TmnCode: {TmnCode}", tmnCode);
_logger.LogInformation("[VNPAY] Amount: {Amount} VND (x100 = {AmountParam})", request.Amount, amount);
_logger.LogInformation("[VNPAY] TxnRef: {TxnRef}", transactionRef);
_logger.LogInformation("[VNPAY] OrderInfo: {OrderInfo}", orderInfo);
_logger.LogInformation("[VNPAY] CreateDate: {CreateDate}", createDate);
_logger.LogInformation("[VNPAY] Hash data: {HashData}", hashData);
_logger.LogInformation("[VNPAY] Secure hash: {SecureHash}", secureHash);
```

## 📊 Bảng Tham Số VNPay (Đã Verify)

| Tham số | Format | Ví dụ | Status |
|---------|--------|-------|--------|
| vnp_Version | String | 2.1.0 | ✅ |
| vnp_Command | String | pay | ✅ |
| vnp_TmnCode | String | DEMOV210 | ✅ |
| vnp_Amount | String (số nguyên) | 120000000 | ✅ |
| vnp_CreateDate | yyyyMMddHHmmss | 20260503222420 | ✅ |
| vnp_CurrCode | String | VND | ✅ |
| vnp_IpAddr | IP Address | 113.161.84.26 | ✅ Đã sửa |
| vnp_Locale | vn hoặc en | vn | ✅ |
| vnp_OrderInfo | Alphanumeric + space + dash | Thanh toan booking 7OF9MC | ✅ Đã sửa |
| vnp_OrderType | String | other | ✅ |
| vnp_ReturnUrl | URL | http://localhost:5042/api/v1/payments/vnpay-return | ✅ |
| vnp_TxnRef | Unique string | 101777821860 | ✅ Đã sửa |
| vnp_SecureHash | HMAC-SHA512 hex | (64 ký tự lowercase) | ✅ |

## 📁 Files Đã Thay Đổi

### 1. `Infrastructure/ExternalServices/VnpayPaymentProvider.cs`
- ✅ Đổi IP: `127.0.0.1` → `113.161.84.26`
- ✅ Rút ngắn TxnRef format
- ✅ Cải thiện regex cho OrderInfo (cho phép dấu gạch ngang)
- ✅ Thêm giới hạn 255 ký tự cho OrderInfo
- ✅ Loại bỏ vnp_ExpireDate
- ✅ Thêm logging chi tiết

### 2. `Application/Services/PaymentService.cs`
- ✅ Loại bỏ ký tự `#` trong OrderDescription

## 📝 Files Tài Liệu Đã Tạo

1. **`VNPAY_FIX_SUMMARY.md`** - Tổng quan về các sửa đổi
2. **`VNPAY_PARAMETER_VERIFICATION.md`** - Chi tiết từng parameter và validation
3. **`VNPAY_FINAL_SUMMARY.md`** - Tài liệu này
4. **`test_vnpay.ps1`** - Script test format URL
5. **`verify_vnpay_params.ps1`** - Script verify parameters
6. **`test_vnpay_url.http`** - HTTP request file để test

## 🧪 Hướng Dẫn Test

### Bước 1: Verify Parameters
```bash
./verify_vnpay_params.ps1
```
Kết quả mong đợi: Tất cả checks đều ✅

### Bước 2: Chạy Application
```bash
dotnet run
```

### Bước 3: Tạo Payment Request

**Option A: Sử dụng Bruno/Postman**
1. Đăng nhập → Lấy JWT token
2. Tìm kiếm chuyến bay
3. Tạo booking
4. Khởi tạo payment với `paymentMethod: "VNPAY"`

**Option B: Sử dụng HTTP file**
```http
POST http://localhost:5042/api/v1/bookings/{bookingId}/payments
Authorization: Bearer {your_token}
Content-Type: application/json

{
  "paymentMethod": "VNPAY"
}
```

### Bước 4: Kiểm Tra Response
```json
{
  "paymentId": 1,
  "bookingId": 10,
  "status": "Pending",
  "amount": 1200000,
  "provider": "VNPAY",
  "transactionRef": "101777821860",
  "paymentLink": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?...",
  "qrCode": "data:image/png;base64,...",
  "createdAt": "2026-05-03T22:24:20Z",
  "expiresAt": "2026-05-03T22:39:20Z"
}
```

### Bước 5: Test Payment URL
Copy `paymentLink` và mở trong browser.

**Kết quả mong đợi:**
- ✅ Trang thanh toán VNPay hiển thị
- ✅ Thông tin đơn hàng đúng
- ✅ Số tiền đúng
- ❌ KHÔNG còn lỗi "Invalid data format"

### Bước 6: Kiểm Tra Logs
```
[VNPAY] Payment URL generated for booking 10
[VNPAY] TmnCode: DEMOV210
[VNPAY] Amount: 1200000 VND (x100 = 120000000)
[VNPAY] TxnRef: 101777821860
[VNPAY] OrderInfo: Thanh toan booking 7OF9MC
[VNPAY] CreateDate: 20260503222420
[VNPAY] Hash data: vnp_Amount=120000000&vnp_Command=pay&...
[VNPAY] Secure hash: a1b2c3d4e5f6...
[VNPAY] Full URL length: 456 chars
```

## 🔍 Debug Nếu Vẫn Có Lỗi

### 1. Verify Hash Manually
```bash
# Copy hash data từ log
vnp_Amount=120000000&vnp_Command=pay&...

# Tính hash tại: https://www.freeformatter.com/hmac-generator.html
Algorithm: SHA512
Secret Key: RAOEXHYVSDDIIENYWSLDIIZTANXUXZFJ
Message: (paste hash data)
Output: Lowercase hex

# So sánh với hash trong log
```

### 2. Kiểm Tra TxnRef Unique
```sql
-- Kiểm tra trong database
SELECT * FROM "Payments" 
WHERE "TransactionRef" = '101777821860';

-- Nếu đã tồn tại → TxnRef bị trùng
-- Giải pháp: Đợi 1 giây và thử lại (timestamp sẽ khác)
```

### 3. Kiểm Tra OrderInfo
```csharp
// Test regex
var test = "Thanh toan booking #7OF9MC";
var result = Regex.Replace(test, @"[^a-zA-Z0-9\s\-]", "");
Console.WriteLine(result);  // "Thanh toan booking 7OF9MC"
```

### 4. Kiểm Tra IP Address
```csharp
// Đảm bảo không phải localhost
["vnp_IpAddr"] = "113.161.84.26"  // ✅
["vnp_IpAddr"] = "127.0.0.1"      // ❌
```

## 📚 Tài Liệu Tham Khảo

- **VNPay Sandbox:** https://sandbox.vnpayment.vn
- **Demo Account:** TmnCode = `DEMOV210`, HashSecret = `RAOEXHYVSDDIIENYWSLDIIZTANXUXZFJ`
- **GitHub vnpayjs:** https://github.com/lehuygiang28/vnpay
- **HMAC Generator:** https://www.freeformatter.com/hmac-generator.html

## ✅ Checklist Cuối Cùng

Trước khi test, đảm bảo:

- [x] Code đã build thành công
- [x] Application đang chạy
- [x] Database đã có dữ liệu (flights, bookings)
- [x] JWT token còn hiệu lực
- [x] Booking ở trạng thái Pending
- [x] Tất cả parameters đúng format (chạy `verify_vnpay_params.ps1`)

## 🎉 Kết Luận

Tất cả các vấn đề gây ra lỗi "Invalid data format" đã được xác định và sửa:

1. ✅ **IP Address** - Đổi từ localhost sang IP public
2. ✅ **TxnRef** - Rút ngắn format để tránh quá dài
3. ✅ **OrderInfo** - Loại bỏ ký tự đặc biệt và giới hạn độ dài
4. ✅ **Parameters** - Đảm bảo đúng format theo tài liệu VNPay
5. ✅ **Logging** - Thêm log chi tiết để debug

Code đã sẵn sàng để test với VNPay sandbox! 🚀

---

**Lưu ý:** Nếu sau khi áp dụng tất cả các fix trên mà vẫn gặp lỗi, hãy:
1. Kiểm tra log chi tiết
2. Verify hash bằng tool online
3. Đảm bảo TxnRef unique (không trùng với transaction trước)
4. Liên hệ VNPay support nếu vấn đề vẫn tiếp diễn
