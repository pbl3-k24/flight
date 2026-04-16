# 🧪 API Testing Plan - Flight Booking System

## 📋 Overview

Complete testing strategy for the Flight Booking API with 7 phases, including authentication, bookings, payments, admin functions, and more.

---

## 🎯 Testing Strategy

### Test Environment
- **Base URL**: `http://localhost:5000/api/v1`
- **Swagger UI**: `http://localhost:5000`
- **Database**: PostgreSQL (auto-seeded)
- **Sample Data**: Pre-loaded users, flights, airports

### Tools Required
- ✅ Postman (recommended) or cURL
- ✅ PostgreSQL client (optional)
- ✅ Browser (for Swagger UI)

---

## ✅ Phase 1: Pre-Testing Setup

### Step 1: Start the Application
```bash
cd API
dotnet run
```

### Expected Output
```
✅ Database migrations applied successfully.
✅ Seeding sample data...
✅ Sample data seeding completed.
Listening on: http://localhost:5000
```

### Step 2: Verify Database
- Database should be created with 25+ tables
- Sample data should include 3 users, 4 airports, 3 flights, 3 promotions

### Step 3: Access Swagger UI
```
http://localhost:5000
```

Should see all endpoints documented and grouped by controller

---

## 🔐 Phase 2: Authentication Testing

### Test 2.1: Login (Get JWT Token)
**Endpoint**: `POST /users/login`

**Request**:
```json
{
  "email": "user1@gmail.com",
  "password": "Test@1234"
}
```

**Expected Response**: ✅ 200 OK
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

**✅ Success Criteria**:
- Status: 200
- Token is returned
- Token is valid JWT format
- ExpiresAt is in the future

---

### Test 2.2: Use Token on Protected Endpoint
**Endpoint**: `GET /bookings` (requires auth)

**Request Headers**:
```
Authorization: Bearer <TOKEN_FROM_TEST_2.1>
```

**Expected Response**: ✅ 200 OK or empty list
```json
{
  "success": true,
  "data": []
}
```

**✅ Success Criteria**:
- Status: 200 (not 401)
- Token is accepted
- Can access protected endpoint

---

### Test 2.3: Invalid Token Handling
**Endpoint**: `GET /bookings`

**Request Headers**:
```
Authorization: Bearer invalid.token.here
```

**Expected Response**: ❌ 401 Unauthorized
```json
{
  "error": "Invalid or expired token"
}
```

**✅ Success Criteria**:
- Status: 401
- Proper error message
- No data returned

---

### Test 2.4: Missing Token
**Endpoint**: `GET /bookings` (no auth header)

**Expected Response**: ❌ 401 Unauthorized

**✅ Success Criteria**:
- Status: 401
- Authentication required message

---

## ✈️ Phase 3: Flight Operations Testing

### Test 3.1: Search Flights (Public - No Auth)
**Endpoint**: `GET /bookings/search`

**Query Parameters**:
```
?departureAirportId=1
&arrivalAirportId=2
&departureDate=2024-01-16
&passengerCount=2
&seatPreference=ECO
```

**Expected Response**: ✅ 200 OK
```json
{
  "success": true,
  "data": [
    {
      "flightId": 1,
      "flightNumber": "VN001",
      "departureTime": "2024-01-16T08:00:00Z",
      "arrivalTime": "2024-01-16T10:25:00Z",
      "availableSeats": {
        "ECO": 135,
        "BUS": 28,
        "FIRST": 10
      },
      "prices": {
        "ECO": 1650000,
        "BUS": 3850000
      }
    }
  ]
}
```

**✅ Success Criteria**:
- Status: 200
- Results contain flights
- Prices match sample data
- Available seats > 0

---

### Test 3.2: Get Flight Details (With Auth)
**Endpoint**: `GET /flights/{flightId}`

**Headers**:
```
Authorization: Bearer <TOKEN>
```

**Expected Response**: ✅ 200 OK
```json
{
  "success": true,
  "data": {
    "flightId": 1,
    "flightNumber": "VN001",
    "aircraftModel": "Boeing 737",
    "departureTime": "2024-01-16T08:00:00Z"
  }
}
```

**✅ Success Criteria**:
- Status: 200
- Contains flight details
- Authentication works

---

## 🎫 Phase 4: Booking Operations

### Test 4.1: Create Booking (With Auth)
**Endpoint**: `POST /bookings`

**Headers**:
```
Authorization: Bearer <TOKEN_FROM_USER1>
```

**Request Body**:
```json
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

**Expected Response**: ✅ 201 Created
```json
{
  "success": true,
  "message": "Booking created successfully",
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

**✅ Success Criteria**:
- Status: 201
- Booking created with ID
- Promotion discount applied
- Final amount = total - discount
- Token expires in 15 minutes

**Save this for next tests**:
- `BOOKING_ID` = 2
- `BOOKING_CODE` = ABC123

---

### Test 4.2: Get My Bookings
**Endpoint**: `GET /bookings/my-bookings`

**Headers**:
```
Authorization: Bearer <TOKEN>
```

**Expected Response**: ✅ 200 OK
```json
{
  "success": true,
  "data": [
    {
      "bookingId": 2,
      "bookingCode": "ABC123",
      "flightNumber": "VN001",
      "status": "Pending",
      "totalAmount": 1485000
    }
  ]
}
```

**✅ Success Criteria**:
- Status: 200
- Contains booking from Test 4.1
- Status is "Pending"

---

### Test 4.3: Get Booking Details
**Endpoint**: `GET /bookings/{bookingId}`

**URL**: `/bookings/2`

**Headers**:
```
Authorization: Bearer <TOKEN>
```

**Expected Response**: ✅ 200 OK
```json
{
  "success": true,
  "data": {
    "bookingId": 2,
    "bookingCode": "ABC123",
    "passengers": [...],
    "status": "Pending",
    "finalAmount": 1485000
  }
}
```

**✅ Success Criteria**:
- Status: 200
- Contains full booking details
- Shows all passengers

---

## 💳 Phase 5: Payment Testing

### Test 5.1: Initiate Payment
**Endpoint**: `POST /payments/initiate`

**Headers**:
```
Authorization: Bearer <TOKEN>
```

**Request**:
```json
{
  "bookingId": 2,
  "paymentMethod": "MOMO",
  "phoneNumber": "0912345678"
}
```

**Expected Response**: ✅ 200 OK
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

**✅ Success Criteria**:
- Status: 200
- Payment ID returned
- Payment link provided
- QR code generated

---

### Test 5.2: Confirm Payment (Mock)
**Endpoint**: `POST /payments/{bookingId}/confirm`

**Headers**:
```
Authorization: Bearer <TOKEN>
```

**Request**:
```json
{
  "transactionRef": "MOMO_123456789",
  "paymentMethod": "MOMO"
}
```

**Expected Response**: ✅ 200 OK

**✅ Success Criteria**:
- Status: 200
- Booking status changes to "Confirmed"

---

## 🎟️ Phase 6: Tickets Testing

### Test 6.1: Get Tickets
**Endpoint**: `GET /tickets/{bookingId}`

**URL**: `/tickets/2`

**Headers**:
```
Authorization: Bearer <TOKEN>
```

**Expected Response**: ✅ 200 OK
```json
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
      "status": "Issued"
    }
  ]
}
```

**✅ Success Criteria**:
- Status: 200
- Ticket generated
- Contains all flight info
- Seat assigned

---

## 👨‍💼 Phase 7: Admin Operations

### Test 7.1: Admin Login
**Endpoint**: `POST /users/login`

**Request**:
```json
{
  "email": "admin@flightbooking.vn",
  "password": "Test@1234"
}
```

**Expected Response**: ✅ 200 OK

**Save this for admin tests**:
- `ADMIN_TOKEN` = token value

---

### Test 7.2: Get All Bookings (Admin)
**Endpoint**: `GET /admin/bookings`

**Headers**:
```
Authorization: Bearer <ADMIN_TOKEN>
```

**Query Parameters**:
```
?page=1&pageSize=20
```

**Expected Response**: ✅ 200 OK
```json
{
  "success": true,
  "data": [
    {
      "bookingId": 1,
      "bookingCode": "ABC123",
      "userEmail": "user1@gmail.com",
      "userName": "Nguyễn Văn A",
      "status": "Pending",
      "amount": 1485000
    }
  ]
}
```

**✅ Success Criteria**:
- Status: 200
- Lists all bookings
- Shows user and amount info

---

### Test 7.3: Get Pending Refunds (Admin)
**Endpoint**: `GET /admin/refunds/pending`

**Headers**:
```
Authorization: Bearer <ADMIN_TOKEN>
```

**Expected Response**: ✅ 200 OK

**✅ Success Criteria**:
- Status: 200
- Returns pending refunds list

---

### Test 7.4: Cancel Booking (Admin)
**Endpoint**: `POST /admin/bookings/{bookingId}/cancel`

**URL**: `/admin/bookings/2/cancel`

**Headers**:
```
Authorization: Bearer <ADMIN_TOKEN>
```

**Request**:
```json
{
  "reason": "Customer requested cancellation",
  "fullRefund": true
}
```

**Expected Response**: ✅ 200 OK

**✅ Success Criteria**:
- Status: 200
- Booking status changes to "Cancelled"
- Refund created if requested

---

## 🎁 Phase 8: Promotions Testing

### Test 8.1: Get Active Promotions (Public)
**Endpoint**: `GET /promotions/active`

**Expected Response**: ✅ 200 OK
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

**✅ Success Criteria**:
- Status: 200
- Lists all active promotions
- Shows discount info
- 3 promotions visible (SUMMER2024, EARLYBIRD100K, NEWUSER20)

---

## 🚨 Phase 9: Error Handling Testing

### Test 9.1: Invalid Email Format
**Endpoint**: `POST /users/login`

**Request**:
```json
{
  "email": "invalid-email",
  "password": "Test@1234"
}
```

**Expected Response**: ❌ 400 Bad Request

**✅ Success Criteria**:
- Status: 400
- Error message about invalid email

---

### Test 9.2: Wrong Password
**Endpoint**: `POST /users/login`

**Request**:
```json
{
  "email": "user1@gmail.com",
  "password": "WrongPassword"
}
```

**Expected Response**: ❌ 401 Unauthorized

**✅ Success Criteria**:
- Status: 401
- Generic error (no user exists message)

---

### Test 9.3: Non-existent Booking
**Endpoint**: `GET /bookings/999999`

**Headers**:
```
Authorization: Bearer <TOKEN>
```

**Expected Response**: ❌ 404 Not Found

**✅ Success Criteria**:
- Status: 404
- Error message about booking not found

---

### Test 9.4: Insufficient Seats
**Endpoint**: `POST /bookings`

**Request**: (for a flight with no seats)
```json
{
  "outboundFlightId": 999,
  "passengers": [{"fullName": "...", ...}],
  "seatClassId": 1
}
```

**Expected Response**: ❌ 400 Bad Request

**✅ Success Criteria**:
- Status: 400
- Error about insufficient seats

---

## 📊 Phase 10: Data Validation Testing

### Test 10.1: Missing Required Fields
**Endpoint**: `POST /bookings`

**Request** (missing passengers):
```json
{
  "outboundFlightId": 1,
  "seatClassId": 1
}
```

**Expected Response**: ❌ 400 Bad Request

**✅ Success Criteria**:
- Status: 400
- Validation error message

---

### Test 10.2: Invalid Date Format
**Endpoint**: `GET /bookings/search`

**Query**:
```
?departureDate=invalid-date
```

**Expected Response**: ❌ 400 Bad Request

**✅ Success Criteria**:
- Status: 400
- Error about invalid date format

---

## 🔒 Phase 11: Authorization Testing

### Test 11.1: User Cannot Access Other User's Booking
**Endpoint**: `GET /bookings/{bookingId_of_other_user}`

**Headers**:
```
Authorization: Bearer <TOKEN_USER1>
```

**Expected Response**: ❌ 403 Forbidden (or 404)

**✅ Success Criteria**:
- Status: 403 or 404
- User cannot access other's data

---

### Test 11.2: Non-Admin Cannot Access Admin Endpoints
**Endpoint**: `GET /admin/bookings`

**Headers**:
```
Authorization: Bearer <TOKEN_REGULAR_USER>
```

**Expected Response**: ❌ 403 Forbidden

**✅ Success Criteria**:
- Status: 403
- Admin-only error message

---

## 📈 Summary Checklist

### Authentication (6 tests)
- [ ] Login successful
- [ ] Token works on protected endpoint
- [ ] Invalid token rejected
- [ ] Missing token rejected
- [ ] Token validation works
- [ ] Admin login works

### Flights (2 tests)
- [ ] Search flights works
- [ ] Flight details accessible

### Bookings (3 tests)
- [ ] Create booking works
- [ ] Get my bookings works
- [ ] Get booking details works

### Payments (2 tests)
- [ ] Initiate payment works
- [ ] Confirm payment works

### Tickets (1 test)
- [ ] Get tickets works

### Admin (4 tests)
- [ ] Get all bookings works
- [ ] Get pending refunds works
- [ ] Cancel booking works
- [ ] Non-admin cannot access

### Promotions (1 test)
- [ ] Get active promotions works

### Error Handling (4 tests)
- [ ] Invalid email rejected
- [ ] Wrong password rejected
- [ ] Non-existent booking returns 404
- [ ] Validation errors returned

### Authorization (2 tests)
- [ ] Cannot access other's booking
- [ ] Non-admin cannot access admin endpoints

**Total: 25+ tests**

---

## 🚀 Running Tests in Postman

### Quick Setup
1. Open Postman
2. Create new Collection: "Flight Booking API"
3. Create Environment with variables:
   - `base_url` = `http://localhost:5000/api/v1`
   - `token` = (empty, will be filled)
   - `admin_token` = (empty, will be filled)
   - `booking_id` = (empty, will be filled)

### Pre-request Script (for saving token)
```javascript
var jsonData = pm.response.json();
if (jsonData.data && jsonData.data.token) {
    pm.environment.set("token", jsonData.data.token);
}
if (jsonData.data && jsonData.data.bookingId) {
    pm.environment.set("booking_id", jsonData.data.bookingId);
}
```

### See POSTMAN_TESTING_GUIDE.md for full setup

---

## 📝 Test Report Template

```
Test Plan Execution Report
==========================

Date: ___________
Tester: ________

Total Tests: 25
Passed: ___
Failed: ___
Skipped: ___

Authentication Tests: _/6
Flight Tests: _/2
Booking Tests: _/3
Payment Tests: _/2
Ticket Tests: _/1
Admin Tests: _/4
Promotion Tests: _/1
Error Handling: _/4
Authorization: _/2

Failed Tests:
1. ___________
2. ___________

Notes:
_________________________
_________________________
```

---

## ✅ Success Criteria

**API is production-ready when**:
- ✅ All 25+ tests pass
- ✅ No unexpected errors
- ✅ Response times < 1 second
- ✅ Data validation works
- ✅ Authorization enforced
- ✅ Error messages clear
- ✅ Sample data loads correctly

---

**Happy Testing!** 🎉

For detailed Postman guide: `POSTMAN_TESTING_GUIDE.md`  
For sample data details: `SAMPLE_DATA_TESTING_GUIDE.md`  
For troubleshooting: `TESTING_QUICKSTART.md`
