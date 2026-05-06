# VNPay Parameter Verification

## Bảng Tham Số Chính Thức (Từ Tài Liệu VNPay)

| Tham số | Ý nghĩa | Định dạng / Ví dụ | ✅ Status |
|---------|---------|-------------------|-----------|
| vnp_Version | Phiên bản API | 2.1.0 | ✅ Đúng |
| vnp_Command | Lệnh gửi | pay | ✅ Đúng |
| vnp_TmnCode | Mã website (Terminal ID) | DEMOV210 (Sandbox) | ✅ Đúng |
| vnp_Amount | Số tiền (x100) | 120000000 (Số nguyên chuỗi) | ✅ Đúng |
| vnp_CreateDate | Thời gian tạo | 20260503221832 (yyyyMMddHHmmss) | ✅ Đúng |
| vnp_CurrCode | Đơn vị tiền tệ | VND | ✅ Đúng |
| vnp_IpAddr | IP của khách hàng | 113.161.84.26 | ✅ Đã sửa |
| vnp_Locale | Ngôn ngữ hiển thị | vn hoặc en | ✅ Đúng |
| vnp_OrderInfo | Nội dung thanh toán | Thanh toan booking 7OF9MC | ✅ Đã sửa |
| vnp_OrderType | Loại hàng hóa | other | ✅ Đúng |
| vnp_ReturnUrl | URL nhận kết quả | http://localhost:5042/api/v1/payments/vnpay-return | ✅ Đúng |
| vnp_TxnRef | Mã đơn vị (Unique) | 101777821512 | ✅ Đã sửa |
| vnp_SecureHash | Chữ ký kiểm tra | Chuỗi HMAC-SHA512 (64 ký tự) | ✅ Đúng |

## Chi Tiết Implementation

### 1. vnp_Amount ✅
```csharp
var amount = (long)(request.Amount * 100m);  // Convert to long
["vnp_Amount"] = amount.ToString()           // Convert to string
```
**Ví dụ:** 
- Input: `1,200,000 VND`
- Output: `"120000000"` (chuỗi số nguyên)

### 2. vnp_TxnRef ✅
```csharp
var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var transactionRef = $"{request.BookingId}{timestamp}";
```
**Ví dụ:**
- BookingId: `10`
- Timestamp: `1777821512`
- Output: `"101777821512"`

### 3. vnp_OrderInfo ✅
```csharp
// Loại bỏ ký tự đặc biệt, chỉ giữ chữ, số, khoảng trắng, dấu gạch ngang
var orderInfo = Regex.Replace(request.OrderDescription, @"[^a-zA-Z0-9\s\-]", "").Trim();
// Giới hạn 255 ký tự
if (orderInfo.Length > 255) orderInfo = orderInfo.Substring(0, 255);
```
**Ví dụ:**
- Input: `"Thanh toan booking #7OF9MC"`
- Output: `"Thanh toan booking 7OF9MC"` (loại bỏ `#`)

### 4. vnp_IpAddr ✅
```csharp
["vnp_IpAddr"] = "113.161.84.26"  // IP public giả lập
```
**Lưu ý:** VNPay sandbox **KHÔNG** chấp nhận `127.0.0.1` hoặc `localhost`

### 5. vnp_CreateDate ✅
```csharp
var createDate = GetVietnamNow().ToString("yyyyMMddHHmmss");
```
**Ví dụ:** `"20260503221832"`

**Múi giờ:** GMT+7 (Việt Nam)
```csharp
private static DateTime GetVietnamNow()
{
    try
    {
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);
    }
    catch
    {
        return DateTime.UtcNow.AddHours(7);
    }
}
```

### 6. vnp_SecureHash ✅
```csharp
// 1. Build hash data (sorted parameters, không encode)
var hashData = string.Join("&", parameters.Select(p => $"{p.Key}={p.Value}"));

// 2. Tạo HMAC-SHA512
var secureHash = CreateHmacSha512(hashSecret, hashData);

private static string CreateHmacSha512(string secretKey, string data)
{
    using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
    return Convert.ToHexString(hashBytes).ToLowerInvariant();
}
```
**Output:** Chuỗi hex 128 ký tự (64 bytes) lowercase

## Ví Dụ Request Hoàn Chỉnh

### Input
```json
{
  "bookingId": 10,
  "amount": 1200000,
  "orderDescription": "Thanh toan booking 7OF9MC"
}
```

### Parameters (Sorted)
```
vnp_Amount=120000000
vnp_Command=pay
vnp_CreateDate=20260503221832
vnp_CurrCode=VND
vnp_IpAddr=113.161.84.26
vnp_Locale=vn
vnp_OrderInfo=Thanh toan booking 7OF9MC
vnp_OrderType=other
vnp_ReturnUrl=http://localhost:5042/api/v1/payments/vnpay-return
vnp_TmnCode=DEMOV210
vnp_TxnRef=101777821512
vnp_Version=2.1.0
```

### Hash Data (Để tạo vnp_SecureHash)
```
vnp_Amount=120000000&vnp_Command=pay&vnp_CreateDate=20260503221832&vnp_CurrCode=VND&vnp_IpAddr=113.161.84.26&vnp_Locale=vn&vnp_OrderInfo=Thanh toan booking 7OF9MC&vnp_OrderType=other&vnp_ReturnUrl=http://localhost:5042/api/v1/payments/vnpay-return&vnp_TmnCode=DEMOV210&vnp_TxnRef=101777821512&vnp_Version=2.1.0
```

### Final URL
```
https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=120000000&vnp_Command=pay&vnp_CreateDate=20260503221832&vnp_CurrCode=VND&vnp_IpAddr=113.161.84.26&vnp_Locale=vn&vnp_OrderInfo=Thanh%20toan%20booking%207OF9MC&vnp_OrderType=other&vnp_ReturnUrl=http%3A%2F%2Flocalhost%3A5042%2Fapi%2Fv1%2Fpayments%2Fvnpay-return&vnp_TmnCode=DEMOV210&vnp_TxnRef=101777821512&vnp_Version=2.1.0&vnp_SecureHash=<64_char_hex_string>
```

## Các Lỗi Thường Gặp

### ❌ "Invalid data format"

**Nguyên nhân có thể:**

1. **vnp_Amount không phải chuỗi số nguyên**
   - ❌ Sai: `"120000000.00"` (có dấu thập phân)
   - ✅ Đúng: `"120000000"` (số nguyên)

2. **vnp_TxnRef trùng lặp**
   - Mỗi transaction phải có TxnRef unique
   - Sử dụng timestamp để đảm bảo unique

3. **vnp_OrderInfo có ký tự đặc biệt**
   - ❌ Sai: `"Thanh toán #123"` (có `#`)
   - ✅ Đúng: `"Thanh toan 123"` (chỉ chữ, số, khoảng trắng)

4. **vnp_IpAddr = 127.0.0.1**
   - VNPay sandbox không chấp nhận localhost IP
   - Dùng IP public giả lập: `113.161.84.26`

5. **vnp_CreateDate sai múi giờ**
   - Phải dùng múi giờ Việt Nam (GMT+7)
   - ❌ Sai: UTC time
   - ✅ Đúng: Vietnam time

6. **vnp_SecureHash sai**
   - Phải sort parameters theo alphabet
   - Không encode values khi tạo hash data
   - Dùng HMAC-SHA512, output lowercase hex

## Checklist Trước Khi Test

- [ ] vnp_Amount là chuỗi số nguyên (không có dấu thập phân)
- [ ] vnp_TxnRef unique (không trùng với transaction trước)
- [ ] vnp_OrderInfo không có ký tự đặc biệt (`#`, `@`, `!`, etc.)
- [ ] vnp_OrderInfo không quá 255 ký tự
- [ ] vnp_IpAddr không phải 127.0.0.1
- [ ] vnp_CreateDate theo múi giờ GMT+7
- [ ] vnp_SecureHash được tạo từ hash data đã sort
- [ ] Parameters trong URL được encode (trừ vnp_SecureHash)
- [ ] vnp_TmnCode = "DEMOV210" (sandbox)
- [ ] vnp_HashSecret = "RAOEXHYVSDDIIENYWSLDIIZTANXUXZFJ" (sandbox)

## Test Manual

### Bước 1: Tạo Hash Data
```bash
# Copy hash data từ log
vnp_Amount=120000000&vnp_Command=pay&...
```

### Bước 2: Tính HMAC-SHA512
Sử dụng tool online: https://www.freeformatter.com/hmac-generator.html
- **Algorithm:** SHA512
- **Secret Key:** `RAOEXHYVSDDIIENYWSLDIIZTANXUXZFJ`
- **Message:** (paste hash data)
- **Output:** Lowercase hex

### Bước 3: So Sánh
So sánh hash từ tool với hash trong log:
```
[VNPAY] Secure hash: <your_hash>
```

Nếu khớp → Hash đúng ✅  
Nếu không khớp → Kiểm tra lại hash data và secret key ❌

## Kết Luận

Tất cả parameters đã được implement đúng theo tài liệu VNPay chính thức. Các thay đổi chính:

1. ✅ vnp_IpAddr: `127.0.0.1` → `113.161.84.26`
2. ✅ vnp_TxnRef: Rút ngắn format để tránh quá dài
3. ✅ vnp_OrderInfo: Loại bỏ ký tự `#` và các ký tự đặc biệt khác
4. ✅ Thêm giới hạn 255 ký tự cho OrderInfo
5. ✅ Đảm bảo Amount là chuỗi số nguyên

Nếu vẫn gặp lỗi "Invalid data format", hãy:
1. Kiểm tra log để xem hash data
2. Verify hash bằng tool online
3. Đảm bảo TxnRef unique (không trùng với transaction trước)
