# 📊 HƯỚNG DẪN DÙNG SAMPLE DATA ĐỂ TEST TÌMKIẾM

## ✅ Cách sử dụng

### Option 1: Tự động (Khuyên dùng)
Khi chạy ứng dụng trong **Development mode**, sample data sẽ tự động được thêm vào:

```bash
cd E:\pbl3\flight\backend
dotnet run --project API/API.csproj
```

Xem logs:
```
Adding comprehensive search test data...
  - Flights: 30+ records
  - Inventories: 60+ records  
  - Promotions: 8 records
✅ Sample search data added successfully!
```

### Option 2: SQL trực tiếp
Dùng file `SAMPLE_DATA_FOR_TESTING.sql` để insert vào PostgreSQL:

```bash
psql -U postgres -d flight_booking_db -f API/docs/SAMPLE_DATA_FOR_TESTING.sql
```

---

## 📋 SAMPLE DATA ĐƯỢC THÊM

### 1️⃣ AIRPORTS (Sân Bay)
```
SGN - Tân Sơn Nhất      (HCM)
HAN - Nội Bài          (Hà Nội)
DAD - Quốc tế Đà Nẵng   (Đà Nẵng)
CTS - Cần Thơ           (Cần Thơ)
VCA - Buôn Mê Thuộc     (Đắk Lắk)
HUI - Phú Bài           (Huế)
```

### 2️⃣ ROUTES (Tuyến Bay)
```
SGN ↔ HAN   (1700 km, 145 phút)
SGN ↔ DAD   (960 km, 100 phút)
HAN ↔ DAD   (500 km, 75 phút)
SGN ↔ CTS   (330 km, 55 phút)
HAN ↔ HUI   (tùy chỉnh)
```

### 3️⃣ AIRCRAFT (Máy Bay)
```
Boeing 737       - VN-ABC123   (180 ghế)
Airbus A320      - VN-XYZ789   (220 ghế)
Boeing 787       - VN-DEF456   (242 ghế)
Airbus A321      - VN-GHI789   (236 ghế)
Boeing 777       - VN-JKL012   (350 ghế)
```

### 4️⃣ SEAT CLASSES (Hạng Ghế)
```
ECO    - Economy          (Priority: 3)
BUS    - Business         (Priority: 2)
PRM    - Premium          (Priority: 1)
```

### 5️⃣ FLIGHTS (Chuyến Bay)
**30+ chuyến bay** được tạo với:
- **7 ngày** tiếp theo (từ hôm nay)
- **8 chuyến bay/ngày** vào các khung giờ: 6, 8, 10, 12, 14, 16, 18, 20
- Mix các tuyến bay khác nhau
- Status ngẫu nhiên (Active/Cancelled)

**Ví dụ:**
```
VN001: HCM → Hà Nội   (8:00 AM)   - Boeing 737
VN002: Hà Nội → HCM   (2:00 PM)   - Airbus A320
VN003: HCM → Đà Nẵng  (5:00 PM)   - Boeing 787
...
```

### 6️⃣ FLIGHT SEAT INVENTORIES (Hàng Ghế Máy Bay)
Cho mỗi chuyến bay:
- **Economy**: 150 ghế (giá 1.2M - 1.4M VND)
- **Business**: 40 ghế (giá 2.8M - 3.0M VND)
- **Premium**: 15 ghế (một số chuyến) (giá 4.5M VND)

Với trạng thái ngẫu nhiên:
- Ghế sẵn có: 50-100%
- Ghế đã bán: 0-50%
- Ghế giữ chờ: 0-10%

### 7️⃣ PROMOTIONS (Mã Khuyến Mại)
```
SUMMER20        - Giảm 20%               (hết hạn: 90 ngày)
SAVE500K        - Giảm cố định 500K     (hết hạn: 30 ngày)
EARLYBIRD15     - Giảm 15% Early Bird   (hết hạn: 60 ngày)
VIP10           - Giảm 10% VIP          (đã full 50/50)
NEWYEAR2025     - Giảm 30% Tết          (hết hạn: 120 ngày)
BUSINESS25      - Giảm 25% Business     (hết hạn: 40 ngày)
WEEKEND         - Giảm 300K Weekend     (hết hạn: 180 ngày)
STUDENT15       - Giảm 15% Sinh viên    (hết hạn: 100 ngày)
```

---

## 🧪 TEST CASES VỚI API

### 1. Tìm kiếm chuyến bay (Search Flights)
```bash
GET /api/v1/flights/search?departureId=1&arrivalId=2&date=2025-01-22
```

**Kỳ vọng:** Trả về tất cả chuyến bay từ SGN → HAN vào ngày 22/1

---

### 2. Lấy danh sách chuyến bay admin (Get Flights List)
```bash
GET /api/v1/admin/FlightsAdmin?page=1&pageSize=20
Authorization: Bearer <admin_token>
```

**Kỳ vọng:** Trả về 20 chuyến bay đầu tiên với đầy đủ thông tin

---

### 3. Lấy danh sách khuyến mại (Get Promotions)
```bash
GET /api/v1/admin/PromotionsAdmin?page=1&pageSize=10
Authorization: Bearer <admin_token>
```

**Kỳ vọng:** Trả về 10 mã khuyến mại đầu tiên

---

### 4. Validate mã khuyến mại (Validate Promotion Code)
```bash
POST /api/v1/promotions/validate
Content-Type: application/json

{
  "code": "SUMMER20"
}
```

**Kỳ vọng:** Trả về:
```json
{
  "isValid": true,
  "discountType": 0,
  "discountValue": 20,
  "remainingUsage": 455
}
```

---

## 🔐 Tài khoản Test

```
Admin:
  Email: admin@flightbooking.vn
  Password: Admin@123456
  Role: Admin

User1:
  Email: user1@gmail.com
  Password: User1@123456
  Role: User

User2:
  Email: user2@gmail.com
  Password: User2@123456
  Role: User
```

### Lấy JWT Token:
```bash
POST /api/v1/Users/login
Content-Type: application/json

{
  "email": "admin@flightbooking.vn",
  "password": "Admin@123456"
}
```

---

## 📊 Thống kê Dữ Liệu

| Entity | Số Lượng | Mục Đích |
|--------|----------|---------|
| Airports | 6 | Test tìm kiếm theo sân bay |
| Routes | 7+ | Test tìm kiếm theo tuyến bay |
| Aircraft | 5 | Test thông tin máy bay |
| Flights | 30+ | Test search, filter, pagination |
| Inventories | 60+ | Test ghế và giá |
| Promotions | 8 | Test validate, search, filter |
| Users | 3 | Test authentication |
| Bookings | 2+ | Test booking history |

---

## 💡 Tips Test Hiệu Quả

### 1. Test Pagination
```bash
# Trang 1 - 20 items
GET /api/v1/admin/FlightsAdmin?page=1&pageSize=20

# Trang 2 - 20 items
GET /api/v1/admin/FlightsAdmin?page=2&pageSize=20

# 10 items per page
GET /api/v1/admin/FlightsAdmin?page=1&pageSize=10
```

### 2. Test Date Range
```bash
# Chuyến bay hôm nay
GET /api/v1/flights/search?departureId=1&arrivalId=2&date=2025-01-21

# Chuyến bay ngày mai
GET /api/v1/flights/search?departureId=1&arrivalId=2&date=2025-01-22

# Chuyến bay 7 ngày sau
GET /api/v1/flights/search?departureId=1&arrivalId=2&date=2025-01-28
```

### 3. Test Không Có Dữ Liệu
```bash
# Sân bay không tồn tại
GET /api/v1/flights/search?departureId=999&arrivalId=1&date=2025-01-21

# Ngày quá khứ
GET /api/v1/flights/search?departureId=1&arrivalId=2&date=2024-01-01
```

### 4. Test Mã Khuyến Mại
```bash
# Mã hợp lệ đang hoạt động
GET /api/v1/promotions/validate?code=SUMMER20

# Mã hết hạn
GET /api/v1/promotions/validate?code=EXPIRED2024

# Mã đã full quota
GET /api/v1/promotions/validate?code=VIP10

# Mã không tồn tại
GET /api/v1/promotions/validate?code=INVALID999
```

---

## 🛠️ Cách Thêm Dữ Liệu Tùy Chỉnh

Nếu muốn thêm dữ liệu riêng, edit file:
```
API/Infrastructure/Data/SampleDataForSearching.cs
```

Ví dụ thêm chuyến bay custom:
```csharp
var customFlight = new Flight
{
    FlightNumber = "VN999",
    RouteId = routeId,
    AircraftId = aircraftId,
    DepartureTime = DateTime.UtcNow.AddDays(5).AddHours(14),
    ArrivalTime = DateTime.UtcNow.AddDays(5).AddHours(15).AddMinutes(45),
    Status = 0,
    CreatedAt = DateTime.UtcNow
};

context.Flights.Add(customFlight);
await context.SaveChangesAsync();
```

---

## ⚠️ Lưu Ý

1. **Development Mode Only**: Dữ liệu này chỉ tự động thêm khi chạy `Development`
2. **Idempotent**: Nếu chạy lần thứ 2, sẽ skip vì check `if (existingFlights < 30)`
3. **Random Data**: Ghế available, giá, status được sinh ngẫu nhiên
4. **Timestamps**: Tất cả timestamps lấy từ `DateTime.UtcNow`

---

## 🚀 Các Endpoint Để Test

### Flights
- `GET /api/v1/flights/search` - Tìm kiếm chuyến bay
- `GET /api/v1/admin/FlightsAdmin` - DS chuyến bay (Admin)
- `POST /api/v1/admin/FlightsAdmin` - Tạo chuyến bay (Admin)
- `PUT /api/v1/admin/FlightsAdmin/{id}` - Cập nhật (Admin)

### Promotions
- `GET /api/v1/admin/PromotionsAdmin` - DS khuyến mại (Admin)
- `POST /api/v1/admin/PromotionsAdmin` - Tạo khuyến mại (Admin)
- `GET /api/v1/promotions/validate?code=XXX` - Validate code
- `PUT /api/v1/admin/PromotionsAdmin/{id}` - Cập nhật (Admin)

### Bookings
- `GET /api/v1/bookings/my` - Lịch sử booking của user
- `POST /api/v1/bookings` - Tạo booking mới

---

Chúc bạn test vui vẻ! 🎉
