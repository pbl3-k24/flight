# 🧪 Postman Testing Guide - Hướng Dẫn Test API Bằng Postman

## 📥 Import Postman Collection

### Tạo Collection Mới
1. Mở Postman
2. Click **Collections** → **Create New Collection**
3. Đặt tên: `Flight Booking API`
4. Tạo các folder sau:
   - Authentication
   - Flights
   - Bookings
   - Payments
   - Admin
   - Promotions

---

## 🔐 Authentication Setup

### Variables (Biến Environment)
Tạo một environment với các biến sau:

```json
{
  "base_url": "http://localhost:5000/api/v1",
  "token": "",
  "admin_token": "",
  "user_id": "2",
  "booking_id": "1",
  "flight_id": "1"
}
```

### Cách Tạo Environment:
1. Click **Environments** (góc trái)
2. **New Environment**
3. Thêm variables ở trên
4. **Save**

---

## 👤 Authentication API (Xác Thực)

### 1. Register New User

**Request**:
```
POST {{base_url}}/users/register
Content-Type: application/json

{
  "email": "testuser@example.com",
  "password": "Test@12345",
  "fullName": "Test User",
  "phone": "0987654321"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "message": "Đăng ký thành công",
  "data": {
    "userId": 5,
    "email": "testuser@example.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2024-01-16T10:00:00Z"
  }
}
```

**Save Token** (Postman Script):
```javascript
var jsonData = pm.response.json();
pm.environment.set("token", jsonData.data.token);
pm.environment.set("user_id", jsonData.data.userId);
```

---

### 2. Login

**Request**:
```
POST {{base_url}}/users/login
Content-Type: application/json

{
  "email": "user1@gmail.com",
  "password": "Test@1234"
}
```

**Response** (200 OK):
```json
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

**Save Token** (Postman Script):
```javascript
var jsonData = pm.response.json();
pm.environment.set("token", jsonData.data.token);
pm.environment.set("user_id", jsonData.data.userId);
```

---

### 3. Admin Login

**Request**:
```
POST {{base_url}}/users/login
Content-Type: application/json

{
  "email": "admin@flightbooking.vn",
  "password": "Admin@1234"
}
```

**Save Token** (Postman Script):
```javascript
var jsonData = pm.response.json();
pm.environment.set("admin_token", jsonData.data.token);
```

---

## ✈️ Flight API (Chuyến Bay)

### 1. Search Flights

**Request**:
```
GET {{base_url}}/bookings/search
Content-Type: application/json

?departureAirportId=1&arrivalAirportId=2&departureDate=2024-01-16&passengerCount=2&seatPreference=ECO
```

**Alternative (POST)**:
```
POST {{base_url}}/bookings/search
Content-Type: application/json

{
  "departureAirportId": 1,
  "arrivalAirportId": 2,
  "departureDate": "2024-01-16",
  "passengerCount": 2,
  "seatPreference": "ECO"
}
```

**Response** (200 OK):
```json
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

---

### 2. Get Flight Details

**Request**:
```
GET {{base_url}}/flights/{{flight_id}}
Authorization: Bearer {{token}}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "flightId": 1,
    "flightNumber": "VN001",
    "routeId": 1,
    "aircraftId": 1,
    "departureTime": "2024-01-16T08:00:00Z",
    "arrivalTime": "2024-01-16T10:25:00Z",
    "status": 0
  }
}
```

---

## 🎫 Booking API (Đặt Vé)

### 1. Create Booking

**Request**:
```
POST {{base_url}}/bookings
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "outboundFlightId": 1,
  "returnFlightId": null,
  "passengers": [
    {
      "fullName": "Nguyễn Văn A",
      "dateOfBirth": "1990-05-15",
      "nationalId": "001090123456",
      "passengerType": 0
    }
  ],
  "seatClassId": 1,
  "promotionCode": "SUMMER2024"
}
```

**Response** (201 Created):
```json
{
  "success": true,
  "message": "Đặt vé thành công",
  "data": {
    "bookingId": 2,
    "bookingCode": "ABC123",
    "totalAmount": 1650000,
    "discountAmount": 165000,
    "finalAmount": 1485000,
    "status": "Pending",
    "expiresAt": "2024-01-16T10:15:00Z"
  }
}
```

**Save Booking ID**:
```javascript
var jsonData = pm.response.json();
pm.environment.set("booking_id", jsonData.data.bookingId);
```

---

### 2. Get My Bookings

**Request**:
```
GET {{base_url}}/bookings/my-bookings
Authorization: Bearer {{token}}

?page=1&pageSize=20
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "bookingId": 2,
      "bookingCode": "ABC123",
      "flightNumber": "VN001",
      "departureTime": "2024-01-16T08:00:00Z",
      "passengerCount": 1,
      "totalAmount": 1485000,
      "status": "Pending"
    }
  ]
}
```

---

### 3. Get Booking Details

**Request**:
```
GET {{base_url}}/bookings/{{booking_id}}
Authorization: Bearer {{token}}
```

---

### 4. Cancel Booking

**Request**:
```
POST {{base_url}}/bookings/{{booking_id}}/cancel
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "reason": "Change of plans"
}
```

---

## 💳 Payment API (Thanh Toán)

### 1. Initiate Payment

**Request**:
```
POST {{base_url}}/payments/initiate
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "bookingId": {{booking_id}},
  "paymentMethod": "MOMO",
  "phoneNumber": "0912345678"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "paymentId": 1,
    "paymentLink": "https://momo.vn/pay/...",
    "qrCode": "data:image/png;base64,...",
    "expiresAt": "2024-01-16T11:00:00Z"
  }
}
```

---

### 2. Confirm Payment (Mock)

**Request**:
```
POST {{base_url}}/payments/{{booking_id}}/confirm
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "transactionRef": "MOMO_123456",
  "paymentMethod": "MOMO"
}
```

---

## 🎁 Promotions API (Khuyến Mãi)

### 1. Get Available Promotions

**Request**:
```
GET {{base_url}}/promotions/active
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "promotionId": 1,
      "code": "SUMMER2024",
      "discountType": "Percentage",
      "discountValue": 10,
      "validFrom": "2024-01-15T00:00:00Z",
      "validTo": "2024-04-15T00:00:00Z"
    }
  ]
}
```

---

### 2. Apply Promotion Code

**Request**:
```
POST {{base_url}}/bookings/apply-promotion
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "bookingId": {{booking_id}},
  "promotionCode": "SUMMER2024"
}
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": {
    "originalAmount": 1650000,
    "discountAmount": 165000,
    "finalAmount": 1485000,
    "promotionApplied": "SUMMER2024"
  }
}
```

---

## 👨‍💼 Admin API (Quản Trị)

### 1. Get All Bookings (Admin)

**Request**:
```
GET {{base_url}}/admin/bookings
Authorization: Bearer {{admin_token}}

?page=1&pageSize=20&status=0
```

**Response** (200 OK):
```json
{
  "success": true,
  "data": [
    {
      "bookingId": 1,
      "bookingCode": "ABC123",
      "userEmail": "user1@gmail.com",
      "userName": "Nguyễn Văn A",
      "outboundFlight": "VN001",
      "passengerCount": 1,
      "amount": 1485000,
      "bookingStatus": "Pending",
      "createdAt": "2024-01-16T10:00:00Z"
    }
  ]
}
```

---

### 2. Get Pending Refunds

**Request**:
```
GET {{base_url}}/admin/refunds/pending
Authorization: Bearer {{admin_token}}

?page=1&pageSize=20
```

---

### 3. Cancel Booking (Admin)

**Request**:
```
POST {{base_url}}/admin/bookings/{{booking_id}}/cancel
Authorization: Bearer {{admin_token}}
Content-Type: application/json

{
  "reason": "Admin cancellation",
  "fullRefund": true
}
```

---

### 4. Approve Refund

**Request**:
```
POST {{base_url}}/admin/refunds/{{refund_id}}/approve
Authorization: Bearer {{admin_token}}
Content-Type: application/json

{
  "approved": true,
  "note": "Refund approved"
}
```

---

## 🎯 Test Scenarios (Kịch Bản Test)

### Scenario 1: Complete Booking Workflow
1. ✅ Register/Login
2. ✅ Search Flights
3. ✅ Create Booking
4. ✅ Apply Promotion
5. ✅ Initiate Payment
6. ✅ Get Booking Details

### Scenario 2: Admin Operations
1. ✅ Admin Login
2. ✅ View All Bookings
3. ✅ View Pending Refunds
4. ✅ Approve Refund
5. ✅ Cancel Booking

---

## 📝 Postman Tips

### Auto-save Token
Thêm script sau vào **Post-response**:
```javascript
var jsonData = pm.response.json();
if (jsonData.data && jsonData.data.token) {
    pm.environment.set("token", jsonData.data.token);
}
```

### Test Status Code
```javascript
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});
```

### Validate Response Body
```javascript
pm.test("Response has required fields", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.success).to.be.true;
    pm.expect(jsonData.data).to.exist;
});
```

---

## 🔗 Useful Links

- **API Base URL**: `http://localhost:5000/api/v1`
- **Swagger UI**: `http://localhost:5000` (khi Environment = Development)
- **Sample Data Guide**: `API/SAMPLE_DATA_TESTING_GUIDE.md`

---

## ⚠️ Common Issues

### 401 Unauthorized
- **Giải pháp**: Kiểm tra token có hợp lệ không
- Login lại để lấy token mới
- Kiểm tra Authorization header: `Bearer {TOKEN}`

### 404 Not Found
- **Giải pháp**: Kiểm tra URL có đúng không
- Kiểm tra ID có tồn tại trong database không

### 400 Bad Request
- **Giải pháp**: Kiểm tra JSON body có đúng format không
- Kiểm tra required fields

---

**Status**: ✅ Ready for Testing
**Base URL**: http://localhost:5000/api/v1
**Documentation**: API/SAMPLE_DATA_TESTING_GUIDE.md
