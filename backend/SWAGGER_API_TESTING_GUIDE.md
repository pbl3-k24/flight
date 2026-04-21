# 🚀 Hướng dẫn Test API với Swagger (JWT Authorization)

## 📋 Mục lục
1. [Truy cập Swagger](#truy-cập-swagger)
2. [Đăng nhập và lấy JWT Token](#đăng-nhập-và-lấy-jwt-token)
3. [Test API có Authorization](#test-api-có-authorization)
4. [Danh sách các API chính](#danh-sách-các-api-chính)

---

## 🌐 Truy cập Swagger

Khi ứng dụng đang chạy, hãy truy cập:

```
http://localhost:5000
```

Bạn sẽ thấy giao diện Swagger UI với đầy đủ API documentation.

---

## 🔐 Đăng nhập và lấy JWT Token

### Bước 1: Gọi API Login

1. **Mở Swagger UI** → Tìm endpoint `POST /api/v1/Users/login`
2. **Nhấp "Try it out"**
3. **Nhập credentials:**
   ```json
   {
     "email": "admin@flightbooking.vn",
     "password": "Admin@123456"
   }
   ```
4. **Nhấp "Execute"**

### Bước 2: Lấy JWT Token từ Response

Phản hồi sẽ trông như thế này:

```json
{
  "success": true,
  "message": "Login successful",
  "userId": "uuid-here",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
  "expiresIn": 3600
}
```

**Sao chép giá trị `token`** từ response.

### Bước 3: Authorize trong Swagger

1. **Nhấp nút "Authorize"** (góc trên bên phải Swagger UI)
2. **Dán JWT token vào:**
   ```
   Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c
   ```

   **LƯU Ý:** Thêm `Bearer ` trước token (có khoảng cách)

3. **Nhấp "Authorize"** để lưu token
4. **Nhấp "Close"**

Giờ bạn đã có quyền truy cập các API được bảo vệ bằng `[Authorize]`!

---

## ✅ Test API có Authorization

Sau khi authorized:

1. **Mở bất kỳ endpoint nào có `[Authorize]`**
   - Ví dụ: `GET /api/v1/Users/profile` hoặc `POST /api/v1/Bookings`
2. **Nhấp "Try it out"**
3. **Nhập dữ liệu cần thiết** (nếu có)
4. **Nhấp "Execute"**

Token sẽ được tự động gửi trong header `Authorization: Bearer <your-token>`

---

## 📚 Danh sách các API chính

### 🔓 Không cần Authorization (Public)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/v1/Users/register` | Đăng ký tài khoản mới |
| POST | `/api/v1/Users/login` | Đăng nhập và lấy JWT token |
| POST | `/api/v1/Users/verify-email` | Xác minh email |
| GET | `/api/v1/Flights/search` | Tìm kiếm chuyến bay |
| GET | `/health` | Kiểm tra trạng thái API |

### 🔒 Cần Authorization (User)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/v1/Bookings` | Tạo đơn đặt chỗ |
| GET | `/api/v1/Bookings` | Lấy danh sách đặt chỗ |
| POST | `/api/v1/Users/change-password` | Thay đổi mật khẩu |
| POST | `/api/v1/Payments` | Thực hiện thanh toán |
| POST | `/api/v1/Refunds` | Yêu cầu hoàn tiền |

### 👨‍💼 Cần Authorization (Admin)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/api/v1/admin/Flights` | Tạo chuyến bay mới |
| PUT | `/api/v1/admin/Flights/{id}` | Cập nhật thông tin chuyến bay |
| DELETE | `/api/v1/admin/Flights/{id}` | Xóa chuyến bay |
| GET | `/api/v1/admin/Dashboard` | Xem bảng điều khiển admin |

---

## 📝 Test Credentials

### Admin Account
```
Email: admin@flightbooking.vn
Password: Admin@123456
```

### Regular Users
```
Email: user1@gmail.com
Password: User1@123456

Email: user2@gmail.com
Password: User2@123456
```

---

## 🐛 Troubleshooting

### ❌ Lỗi: "401 Unauthorized"

**Nguyên nhân:** Token không được gửi hoặc đã hết hạn

**Cách khắc phục:**
1. Đăng nhập lại để lấy token mới
2. Kiểm tra token format: `Bearer <token>`
3. Nhấp "Authorize" trong Swagger UI để gửi lại

### ❌ Lỗi: "400 Invalid credentials"

**Nguyên nhân:** Email hoặc mật khẩu sai

**Cách khắc phục:**
1. Kiểm tra credentials: `admin@flightbooking.vn / Admin@123456`
2. Chắc chắn không có khoảng trắng thừa
3. Kiểm tra viết hoa/thường đúng

### ❌ Lỗi: "403 Forbidden"

**Nguyên nhân:** Bạn không có quyền truy cập resource này

**Cách khắc phục:**
1. Nếu cần Admin: Sử dụng tài khoản Admin
2. Kiểm tra role của user trong database
3. Đảm bảo user không bị vô hiệu hóa

---

## 🎯 Flow ví dụ: Tạo Booking

1. **Đăng nhập** → Lấy JWT token
2. **Authorize Swagger** → Dán token
3. **Tìm kiếm chuyến bay** → `GET /api/v1/Flights/search`
4. **Tạo booking** → `POST /api/v1/Bookings`
   ```json
   {
     "flightId": "flight-uuid",
     "passengers": [
       {
         "firstName": "John",
         "lastName": "Doe",
         "email": "john@example.com",
         "passportNumber": "AB123456"
       }
     ]
   }
   ```
5. **Thực hiện thanh toán** → `POST /api/v1/Payments`

---

## 💾 Lưu ý quan trọng

- **Token có thời hạn:** Thường 1 giờ, sau đó cần đăng nhập lại
- **Refresh Token:** Nếu cần duy trì session dài, sử dụng refresh token
- **Secure Token:** Không chia sẻ token với người khác
- **HTTPS:** Trong production, luôn sử dụng HTTPS để bảo vệ token

---

## 🔄 Quy trình chi tiết lấy Token

```mermaid
graph TD
    A["Truy cập Swagger UI"] -->|http://localhost:5000| B["Tìm endpoint /api/v1/Users/login"]
    B --> C["Nhấp Try it out"]
    C --> D["Nhập credentials"]
    D --> E["Nhấp Execute"]
    E --> F{"Response?"}
    F -->|Thành công| G["Copy token"]
    F -->|Lỗi| H["Kiểm tra credentials"]
    G --> I["Nhấp Authorize"]
    I --> J["Dán token với Bearer"]
    J --> K["Nhấp Authorize"]
    K --> L["✅ Authorized!"]
```

---

Chúc bạn test API thành công! 🚀
