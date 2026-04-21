# 📊 Dữ Liệu Mẫu & Hướng Dẫn Test - Sample Data & Testing Guide

## 🎯 Dữ Liệu Mẫu Đã Được Tạo - Sample Data Created

### 👥 Users (Người dùng)
```
1. Admin Account (Tài khoản quản trị)
   - Email: admin@flightbooking.vn
   - Password: [Hash] (Cần đặt lại)
   - Role: Admin
   - Status: Active (Hoạt động)

2. Test User 1
   - Email: user1@gmail.com
   - Name: Nguyễn Văn A
   - Phone: 0912345678
   - Role: User

3. Test User 2
   - Email: user2@gmail.com
   - Name: Trần Thị B
   - Phone: 0923456789
   - Role: User
```

### ✈️ Airports (Sân bay)
```
1. SGN - Sân bay Tân Sơn Nhất (TP. Hồ Chí Minh)
2. HAN - Sân bay Nội Bài (Hà Nội)
3. DAD - Sân bay Quốc tế Đà Nẵng (Đà Nẵng)
4. CTS - Sân bay Cần Thơ (Cần Thơ)
```

### 🛫 Routes (Đường bay)
```
1. SGN → HAN (1700 km, ~145 phút)
2. HAN → SGN (1700 km, ~145 phút)
3. SGN → DAD (960 km, ~95 phút)
4. DAD → HAN (760 km, ~75 phút)
```

### ✈️ Aircraft (Máy bay)
```
1. Boeing 737 (VN-ABC123)
   - Total Seats: 180
   - Year: 2020
   - Seat Classes:
     * Economy: 140 seats @ 1,500,000 VND
     * Business: 30 seats @ 3,500,000 VND
     * First: 10 seats @ 5,500,000 VND

2. Airbus A321 (VN-XYZ789)
   - Total Seats: 220
   - Year: 2021
```

### 🛫 Flights (Chuyến bay)
```
1. VN001
   - Route: SGN → HAN
   - Aircraft: Boeing 737
   - Departure: Tomorrow 08:00
   - Arrival: Tomorrow 10:25
   - Status: Active
   - Available Seats (Economy): 135/140

2. VN002
   - Route: HAN → SGN
   - Aircraft: Airbus A321
   - Departure: Tomorrow 14:00
   - Arrival: Tomorrow 16:25
   - Status: Active

3. VN003
   - Route: SGN → DAD
   - Aircraft: Boeing 737
   - Departure: Next week 09:00
   - Arrival: Next week 10:35
   - Status: Active
```

### 🎟️ Promotions (Khuyến mãi)
```
1. SUMMER2024
   - Discount: 10% off
   - Valid: Until 3 months from now
   - Usage: 45/500

2. EARLYBIRD100K
   - Discount: 100,000 VND off
   - Valid: Until 1 month from now
   - Usage: 234/1000

3. NEWUSER20
   - Discount: 20% off (New users)
   - Valid: Until 6 months from now
   - Usage: 567/10000
```

### 💰 Pricing (Giá vé)
```
Economy Class:
- Base Price: 1,500,000 VND
- Current Price (VN001): 1,650,000 VND (Dynamic pricing)

Business Class:
- Base Price: 3,500,000 VND
- Current Price: 3,850,000 VND

First Class:
- Base Price: 5,500,000 VND
- Current Price: 6,000,000 VND
```

---

## 🧪 Hướng Dẫn Test - Testing Guide

### 1️⃣ Test Authentication (Xác thực)

#### Register New User (Đăng ký người dùng mới)
```bash
POST /api/v1/users/register
Content-Type: application/json

{
  "email": "newuser@gmail.com",
  "password": "Test@1234",
  "fullName": "Nguyễn Văn C",
  "phone": "0934567890"
}

Response:
{
  "success": true,
  "message": "Registration successful",
  "data": {
    "userId": 4,
    "email": "newuser@gmail.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2024-01-16T10:00:00Z"
  }
}
```

#### Login (Đăng nhập)
```bash
POST /api/v1/users/login
Content-Type: application/json

{
  "email": "user1@gmail.com",
  "password": "Test@1234"
}

Response:
{
  "success": true,
  "data": {
    "userId": 2,
    "email": "user1@gmail.com",
    "fullName": "Nguyễn Văn A",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2024-01-16T10:00:00Z"
  }
}
```

### 2️⃣ Test Flight Search (Tìm chuyến bay)

#### Search Flights (Tìm chuyến bay)
```bash
POST /api/v1/bookings/search
Content-Type: application/json

{
  "departureAirportId": 1,    // SGN
  "arrivalAirportId": 2,       // HAN
  "departureDate": "2024-01-16",
  "passengerCount": 2,
  "seatPreference": "ECO"
}

Response:
{
  "success": true,
  "data": [
    {
      "flightId": 1,
      "flightNumber": "VN001",
      "departureTime": "2024-01-16T08:00:00Z",
      "arrivalTime": "2024-01-16T10:25:00Z",
      "duration": "2h 25m",
      "availableSeats": {
        "ECO": 135,
        "BUS": 28,
        "FIRST": 10
      },
      "prices": {
        "ECO": 1650000,
        "BUS": 3850000,
        "FIRST": 6000000
      }
    }
  ]
}
```

### 3️⃣ Test Booking (Đặt vé)

#### Create Booking (Tạo đơn đặt vé)
```bash
POST /api/v1/bookings
Authorization: Bearer {TOKEN}
Content-Type: application/json

{
  "outboundFlightId": 1,
  "returnFlightId": null,
  "passengers": [
    {
      "fullName": "Nguyễn Văn A",
      "dateOfBirth": "1990-05-15",
      "passportNumber": "C12345678",
      "nationality": "VN"
    },
    {
      "fullName": "Nguyễn Thị B",
      "dateOfBirth": "1992-08-20",
      "passportNumber": "C87654321",
      "nationality": "VN"
    }
  ],
  "seatClassId": 1,  // Economy
  "promotionCode": "SUMMER2024"
}

Response:
{
  "success": true,
  "message": "Booking created successfully",
  "data": {
    "bookingId": 1,
    "bookingCode": "ABC123",
    "totalAmount": 3300000,
    "discountAmount": 330000,
    "finalAmount": 2970000,
    "status": "Pending",
    "expiresAt": "2024-01-16T10:15:00Z"
  }
}
```

### 4️⃣ Test Promotions (Khuyến mãi)

#### Get Available Promotions (Lấy danh sách khuyến mãi)
```bash
GET /api/v1/promotions/active
Content-Type: application/json

Response:
{
  "success": true,
  "data": [
    {
      "promotionId": 1,
      "code": "SUMMER2024",
      "description": "Khuyến mãi mùa hè 2024",
      "discountType": "Percentage",
      "discountValue": 10,
      "validFrom": "2024-01-15T00:00:00Z",
      "validTo": "2024-04-15T00:00:00Z"
    }
  ]
}
```

#### Apply Promotion Code (Áp dụng mã khuyến mãi)
```bash
POST /api/v1/bookings/apply-promotion
Content-Type: application/json

{
  "bookingId": 1,
  "promotionCode": "SUMMER2024"
}

Response:
{
  "success": true,
  "data": {
    "originalAmount": 3300000,
    "discountAmount": 330000,
    "finalAmount": 2970000,
    "promotionApplied": "SUMMER2024"
  }
}
```

### 5️⃣ Test Admin Features (Tính năng Quản trị)

#### Get All Bookings (Lấy tất cả đơn đặt vé)
```bash
GET /api/v1/admin/bookings
Authorization: Bearer {ADMIN_TOKEN}
Content-Type: application/json

Query Parameters:
- page: 1
- pageSize: 20
- status: 0 (optional)
- bookingCode: (optional)

Response:
{
  "success": true,
  "data": [
    {
      "bookingId": 1,
      "bookingCode": "ABC123",
      "userEmail": "user1@gmail.com",
      "userName": "Nguyễn Văn A",
      "outboundFlight": "VN001",
      "passengerCount": 2,
      "amount": 2970000,
      "bookingStatus": "Pending",
      "createdAt": "2024-01-15T10:00:00Z"
    }
  ]
}
```

#### Cancel Booking (Hủy đơn đặt vé)
```bash
POST /api/v1/admin/bookings/{bookingId}/cancel
Authorization: Bearer {ADMIN_TOKEN}
Content-Type: application/json

{
  "reason": "Customer requested cancellation",
  "fullRefund": true
}

Response:
{
  "success": true,
  "message": "Booking cancelled successfully"
}
```

### 6️⃣ Test Payments (Thanh toán)

#### Initiate Payment (Khởi tạo thanh toán)
```bash
POST /api/v1/payments/initiate
Authorization: Bearer {TOKEN}
Content-Type: application/json

{
  "bookingId": 1,
  "paymentMethod": "MOMO",
  "phoneNumber": "0912345678"
}

Response:
{
  "success": true,
  "data": {
    "paymentId": 1,
    "paymentLink": "https://momo.vn/pay/...",
    "qrCode": "data:image/png;base64,...",
    "expiresAt": "2024-01-15T11:00:00Z"
  }
}
```

### 7️⃣ Test Tickets (Vé máy bay)

#### Get Tickets (Lấy vé)
```bash
GET /api/v1/tickets/{bookingId}
Authorization: Bearer {TOKEN}
Content-Type: application/json

Response:
{
  "success": true,
  "data": [
    {
      "ticketId": 1,
      "ticketNumber": "VN001-ABC123-001",
      "passengerName": "Nguyễn Văn A",
      "flightNumber": "VN001",
      "seatNumber": "12A",
      "seatClass": "Economy",
      "departureTime": "2024-01-16T08:00:00Z",
      "arrivalTime": "2024-01-16T10:25:00Z",
      "status": "Issued"
    }
  ]
}
```

---

## 🔐 Authentication Tokens (Token)

### Cách lấy Token:
```bash
# Login để lấy token
POST /api/v1/users/login

# Token sẽ được trả về trong response, sử dụng trong header:
Authorization: Bearer {TOKEN}
```

### Test Token:
```bash
# Header:
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 🧮 Test Scenarios (Kịch bản test)

### Scenario 1: Complete Booking Flow (Luồng đặt vé hoàn chỉnh)
1. ✅ Register new user
2. ✅ Login
3. ✅ Search flights
4. ✅ Apply promotion code
5. ✅ Create booking
6. ✅ Initiate payment
7. ✅ Confirm payment
8. ✅ Get tickets

### Scenario 2: Admin Management (Quản lý Admin)
1. ✅ Login as admin
2. ✅ View all bookings
3. ✅ View pending refunds
4. ✅ Cancel booking
5. ✅ Approve refund

### Scenario 3: Search & Filter (Tìm kiếm & lọc)
1. ✅ Search flights by route
2. ✅ Search flights by date
3. ✅ Filter by seat class
4. ✅ Filter by price range

---

## 📝 Notes (Ghi chú)

- ⚠️ **Mật khẩu**: Dữ liệu seed có chứa hash password mẫu. Cần đặt lại mật khẩu thực tế.
- ⚠️ **Giá vé**: Giá được tính toán động theo tỷ lệ chiếm chỗ và thời gian.
- ✅ **Chuyến bay**: Tất cả chuyến bay được tạo với ngày/giờ trong tương lai để test.
- ✅ **Khuyến mãi**: 3 mã khuyến mãi đã được tạo, có thể áp dụng ngay.

---

## 🚀 Chạy Seed Data

Khi khởi động ứng dụng, dữ liệu seed sẽ tự động được áp dụng nếu database trống:

```bash
# Chạy ứng dụng
dotnet run

# Output:
# ✅ Database migrations applied successfully.
# ✅ Sample data seeding completed.
```

---

**Status**: ✅ Ready for Testing
**Data Created**: ✅ Complete
**Can Start Testing**: ✅ Yes
