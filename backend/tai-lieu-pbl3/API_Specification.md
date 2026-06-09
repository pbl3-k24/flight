# HƯỚNG DẪN TÍCH HỢP & ĐẶC TẢ API CHI TIẾT (API SPECIFICATION)
## Dự án: Hệ thống Đặt Vé Máy Bay (Flight Booking Web API)

---

## 1. THÔNG TIN CHUNG (GENERAL CONFIGURATION)

*   **Base URL (Môi trường cục bộ):** `http://localhost:5042`
*   **Version API:** `v1`
*   **Định dạng dữ liệu:** `application/json`
*   **Cơ chế xác thực:** `Bearer Token (JWT)` được gửi qua HTTP Header `Authorization`.

---

## 2. NHÓM API XÁC THỰC & TÀI KHOẢN (AUTHENTICATION & USER ENDPOINTS)

### 2.1. Đăng ký tài khoản (Register)
*   **Endpoint:** `/api/v1/auth/register`
*   **Method:** `POST`
*   **Request Body (JSON):**
    ```json
    {
      "fullName": "Nguyen Van A",
      "email": "nguyenvana@gmail.com",
      "password": "Password123@",
      "phone": "0912345678"
    }
    ```
*   **Phản hồi thành công (200 OK):**
    ```json
    {
      "userId": 15,
      "email": "nguyenvana@gmail.com",
      "fullName": "Nguyen Van A",
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "expiresAt": "2026-06-10T16:00:00Z"
    }
    ```

### 2.2. Đăng nhập (Login)
*   **Endpoint:** `/api/v1/auth/login`
*   **Method:** `POST`
*   **Request Body (JSON):**
    ```json
    {
      "email": "nguyenvana@gmail.com",
      "password": "Password123@"
    }
    ```
*   **Phản hồi thành công (200 OK):**
    ```json
    {
      "userId": 15,
      "email": "nguyenvana@gmail.com",
      "fullName": "Nguyen Van A",
      "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "expiresAt": "2026-06-10T16:00:00Z"
    }
    ```

### 2.3. Quên mật khẩu (Forgot Password)
*   **Endpoint:** `/api/v1/auth/forgot-password`
*   **Method:** `POST`
*   **Request Body (JSON):**
    ```json
    {
      "email": "nguyenvana@gmail.com"
    }
    ```
*   **Phản hồi thành công (200 OK):**
    ```json
    {
      "message": "If an account with that email exists, an OTP has been sent"
    }
    ```

### 2.4. Xác nhận OTP khôi phục mật khẩu (Confirm Reset OTP)
*   **Endpoint:** `/api/v1/auth/reset-password`
*   **Method:** `POST`
*   **Request Body (JSON):**
    ```json
    {
      "code": "123456",
      "newPassword": "NewPassword123@"
    }
    ```
*   **Phản hồi thành công (200 OK):**
    ```json
    {
      "success": true,
      "message": "Password reset successfully"
    }
    ```

---

## 3. NHÓM API TRA CỨU CHUYẾN BAY (FLIGHT SEARCH ENDPOINTS)

### 3.1. Tìm kiếm chuyến bay một chiều (Search One-way Flights)
*   **Endpoint:** `/api/v1/flights/search`
*   **Method:** `GET`
*   **Tham số truy vấn (Query Params):**
    *   `departureAirportId` (int): ID sân bay đi (ví dụ: 1)
    *   `arrivalAirportId` (int): ID sân bay đến (ví dụ: 2)
    *   `departureDate` (string, yyyy-MM-dd): Ngày đi (ví dụ: `2026-06-15`)
    *   `passengerCount` (int): Số lượng hành khách (ví dụ: 2)
    *   `seatClassId` (int): ID hạng ghế (1: Economy, 2: Business)
*   **Phản hồi thành công (200 OK):**
    ```json
    [
      {
        "flightId": 234,
        "flightNumber": "VN-123",
        "departureTime": "2026-06-15T08:00:00Z",
        "arrivalTime": "2026-06-15T10:00:00Z",
        "routeCode": "HAN-SGN",
        "aircraftModel": "Airbus A350",
        "availableSeats": 45,
        "price": 1250000.0
      }
    ]
    ```

### 3.2. Lấy thông tin chi tiết sơ đồ ghế (Flight Seat Availability)
*   **Endpoint:** `/api/v1/flights/{flightId}/seats`
*   **Method:** `GET`
*   **Phản hồi thành công (200 OK):**
    ```json
    {
      "flightId": 234,
      "totalSeats": 180,
      "seatClasses": [
        {
          "seatClassId": 1,
          "name": "Economy",
          "availableSeats": 120,
          "price": 1250000.0
        },
        {
          "seatClassId": 2,
          "name": "Business",
          "availableSeats": 15,
          "price": 3200000.0
        }
      ]
    }
    ```

---

## 4. NHÓM API ĐẶT VÉ VÀ THANH TOÁN (BOOKING & PAYMENT ENDPOINTS)

### 4.1. Tạo đơn đặt vé mới (Create Booking)
*   **Endpoint:** `/api/v1/bookings`
*   **Method:** `POST`
*   **Yêu cầu xác thực:** Có (Bearer Token)
*   **Request Body (JSON):**
    ```json
    {
      "outboundFlightId": 234,
      "returnFlightId": null,
      "seatClassId": 1,
      "contactEmail": "nguyenvana@gmail.com",
      "contactPhone": "0912345678",
      "promotionCode": "GIAM20K",
      "passengers": [
        {
          "fullName": "NGUYEN VAN A",
          "gender": 1,
          "dateOfBirth": "1995-05-15",
          "nationality": "Vietnamese",
          "passportNumber": "C1234567",
          "nationalId": "123456789012"
        },
        {
          "fullName": "NGUYEN VAN B",
          "gender": 2,
          "dateOfBirth": "2018-10-20",
          "nationality": "Vietnamese",
          "passportNumber": null,
          "nationalId": null
        }
      ]
    }
    ```
*   **Phản hồi thành công (201 Created):**
    ```json
    {
      "bookingId": 4567,
      "bookingCode": "FLY98X",
      "totalAmount": 2500000.0,
      "discountAmount": 20000.0,
      "finalAmount": 2480000.0,
      "status": "Pending",
      "expiresAt": "2026-06-09T23:30:00Z"
    }
    ```

### 4.2. Thanh toán hóa đơn qua VNPAY (Generate VNPAY Payment Link)
*   **Endpoint:** `/api/v1/payments/vnpay/create-link`
*   **Method:** `POST`
*   **Request Body (JSON):**
    ```json
    {
      "bookingId": 4567
    }
    ```
*   **Phản hồi thành công (200 OK):**
    ```json
    {
      "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=248000000&vnp_Command=pay&vnp_CreateDate=20260609223000&vnp_CurrCode=VND&vnp_IpAddr=127.0.0.1&vnp_Locale=vn&vnp_OrderInfo=Thanh+toan+don+ve+FLY98X&vnp_OrderType=other&vnp_ReturnUrl=http%3a%2f%2flocalhost%3a3000%2fpayment-callback&vnp_TmnCode=VNPAY001&vnp_TxnRef=4567&vnp_Version=2.1.0&vnp_SecureHash=abcd1234..."
    }
    ```

### 4.3. Yêu cầu hủy đặt vé (Cancel Booking)
*   **Endpoint:** `/api/v1/bookings/{bookingId}/cancel`
*   **Method:** `POST`
*   **Yêu cầu xác thực:** Có (Bearer Token)
*   **Request Body (JSON):**
    ```json
    {
      "reason": "Thay đổi kế hoạch cá nhân"
    }
    ```
*   **Phản hồi thành công (200 OK):**
    ```json
    {
      "success": true,
      "message": "Booking cancelled successfully. Refund has been initiated."
    }
    ```

---

## 5. NHÓM API QUẢN TRỊ VIÊN (ADMIN ENDPOINTS)

Các API quản lý dữ liệu nền, yêu cầu quyền truy cập của tài khoản **Admin**.

### 5.1. Thêm máy bay mới (Create Aircraft)
*   **Endpoint:** `/api/v1/admin/aircraft`
*   **Method:** `POST`
*   **Yêu cầu xác thực:** Có (Admin Token)
*   **Request Body (JSON):**
    ```json
    {
      "registrationNumber": "VN-A888",
      "model": "Airbus A321neo",
      "totalSeats": 220,
      "isActive": true,
      "seatTemplates": [
        {
          "seatClassId": 1,
          "defaultSeatCount": 12,
          "defaultBasePrice": 3500000
        },
        {
          "seatClassId": 2,
          "defaultSeatCount": 208,
          "defaultBasePrice": 1200000
        }
      ]
    }
    ```
*   **Phản hồi thành công (201 Created):**
    ```json
    {
      "aircraftId": 5,
      "model": "Airbus A321neo",
      "registrationNumber": "VN-A888",
      "totalSeats": 220
    }
    ```

### 5.2. Hủy chuyến bay do sự cố (Admin Cancel Flight)
*   **Endpoint:** `/api/v1/admin/flights/{flightId}/cancel`
*   **Method:** `POST`
*   **Yêu cầu xác thực:** Có (Admin Token)
*   **Request Body (JSON):**
    ```json
    {
      "reason": "Thời tiết xấu bão số 2 tại điểm đến"
    }
    ```
*   **Phản hồi thành công (200 OK):**
    ```json
    {
      "success": true,
      "message": "Flight marked as Canceled. All active bookings notified."
    }
    ```

### 5.3. Xem thống kê doanh thu (Dashboard Report)
*   **Endpoint:** `/api/v1/admin/reports/revenue`
*   **Method:** `GET`
*   **Tham số truy vấn:** `startDate=2026-06-01&endDate=2026-06-30`
*   **Phản hồi thành công (200 OK):**
    ```json
    {
      "totalRevenue": 458900000.0,
      "totalBookings": 182,
      "cancelledBookings": 14,
      "revenueByRoute": [
        {
          "routeCode": "HAN-SGN",
          "revenue": 289000000.0
        },
        {
          "routeCode": "DAD-SGN",
          "revenue": 169900000.0
        }
      ]
    }
    ```
