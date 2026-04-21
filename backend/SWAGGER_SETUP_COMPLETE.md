# 📋 Tóm tắt cấu hình Swagger + JWT Authorization

## ✅ Đã hoàn thành

### 1. Cấu hình Swagger Gen
- ✅ Thêm `builder.Services.AddSwaggerGen()`
- ✅ Cấu hình Swagger UI với route root
- ✅ Version: Swashbuckle.AspNetCore 6.5.0

### 2. JWT Authentication
- ✅ Sử dụng custom `JwtAuthenticationHandler`
- ✅ Bearer token trong header `Authorization`
- ✅ HS256 signature (HMAC with SHA-256)
- ✅ Token expiration: 1 giờ (3600 giây)

### 3. Authorization Middleware
- ✅ `app.UseAuthentication()` - xác thực token
- ✅ `app.UseAuthorization()` - kiểm tra quyền
- ✅ Hỗ trợ `[Authorize]` attribute
- ✅ Hỗ trợ `[Authorize(Roles = "Admin")]`

## 🚀 Cách sử dụng

### A. Test bằng Swagger UI

**1. Mở Swagger:**
```
http://localhost:5000
```

**2. Đăng nhập lấy token:**
- Tìm endpoint: `POST /api/v1/Users/login`
- Nhập credentials:
  ```json
  {
    "email": "admin@flightbooking.vn",
    "password": "Admin@123456"
  }
  ```
- Copy token từ response

**3. Authorize Swagger:**
- Nhấp nút "Authorize" (góc trên bên phải)
- Nhập: `Bearer <token>`
- Nhấp "Authorize"

**4. Test API:**
- Gọi endpoint bất kỳ
- Token được gửi tự động

### B. Test bằng Script PowerShell

```powershell
# Chạy script test
./test-api.ps1

# hoặc với tham số tùy chỉnh
./test-api.ps1 -Email "user1@gmail.com" -Password "User1@123456"
```

Script sẽ:
- ✅ Kiểm tra API health
- ✅ Đăng nhập
- ✅ Lấy JWT token
- ✅ Lưu token vào file `jwt_token.txt`
- ✅ Test protected endpoint
- ✅ Hiển thị hướng dẫn tiếp theo

### C. Test bằng cURL

```bash
# 1. Login
curl -X POST "http://localhost:5000/api/v1/Users/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@flightbooking.vn","password":"Admin@123456"}'

# 2. Copy token từ response

# 3. Test protected endpoint
curl -X GET "http://localhost:5000/api/v1/Users/profile" \
  -H "Authorization: Bearer <your-token>"
```

### D. Test bằng Postman

1. **GET TOKEN:**
   - POST: `http://localhost:5000/api/v1/Users/login`
   - Body: JSON
     ```json
     {
       "email": "admin@flightbooking.vn",
       "password": "Admin@123456"
     }
     ```
   - Send → Copy `token` từ response

2. **SET UP AUTH:**
   - Tab "Authorization"
   - Type: "Bearer Token"
   - Token: `<paste here>`

3. **TEST API:**
   - Gọi bất kỳ endpoint nào
   - Token tự động được gửi

## 📚 Tài liệu liên quan

- [`SWAGGER_API_TESTING_GUIDE.md`](./SWAGGER_API_TESTING_GUIDE.md) - Hướng dẫn chi tiết test API
- [`SWAGGER_ADVANCED_CONFIG.md`](./SWAGGER_ADVANCED_CONFIG.md) - Cấu hình nâng cao
- [`test-api.ps1`](./test-api.ps1) - Script PowerShell test nhanh

## 🔐 Test Credentials

### Admin
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

## 📂 File cấu hình chính

| File | Mô tả |
|------|-------|
| `API/Program.cs` | Cấu hình Swagger, JWT, middleware |
| `API/Infrastructure/Security/JwtAuthenticationHandler.cs` | Custom JWT handler |
| `API/Infrastructure/Security/JwtTokenService.cs` | Sinh JWT token |
| `API/Infrastructure/Security/PasswordHasher.cs` | Hash password (PBKDF2-SHA256) |

## 🧪 API Endpoints

### Public (Không cần token)
- `POST /api/v1/Users/register` - Đăng ký
- `POST /api/v1/Users/login` - Đăng nhập
- `GET /health` - Health check

### Protected (Cần token)
- `GET /api/v1/Users/profile` - Profile user
- `POST /api/v1/Users/change-password` - Đổi mật khẩu
- `POST /api/v1/Bookings` - Tạo booking
- `GET /api/v1/Bookings` - Xem booking

### Admin Only (Role = Admin)
- `POST /api/v1/admin/Flights` - Tạo flight
- `GET /api/v1/admin/Dashboard` - Admin dashboard
- `GET /api/v1/admin/Bookings` - Xem tất cả booking

## 🔧 Nếu muốn tùy chỉnh thêm

### Thay đổi JWT expiration time
File: `API/Infrastructure/Security/JwtTokenService.cs`
```csharp
var tokenHandler = new JwtSecurityTokenHandler();
var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
{
    Expires = DateTime.UtcNow.AddHours(2), // Thay đổi thời gian ở đây
    // ...
});
```

### Thêm Refresh Token
Tạo endpoint mới:
```csharp
[HttpPost("refresh-token")]
public async Task<IActionResult> RefreshTokenAsync([FromBody] string refreshToken)
{
    // Implementation...
}
```

### Thêm Custom Claims vào JWT
File: `JwtTokenService.cs`
```csharp
claims.Add(new Claim("custom-claim", "value"));
```

## ✨ Status

- ✅ Swagger cấu hình
- ✅ JWT authentication
- ✅ Authorization middleware
- ✅ Test credentials
- ✅ Hướng dẫn test
- ✅ PowerShell script
- ⏳ Production deployment (sử dụng HTTPS)

## 🚀 Next Steps

1. **Chạy ứng dụng:**
   ```bash
   cd API
   dotnet run
   ```

2. **Mở Swagger:**
   ```
   http://localhost:5000
   ```

3. **Test login:**
   - Endpoint: `POST /api/v1/Users/login`
   - Credentials: `admin@flightbooking.vn / Admin@123456`

4. **Authorize Swagger:**
   - Nút "Authorize"
   - Dán token: `Bearer <token>`

5. **Test protected APIs:**
   - Tất cả token sẽ được gửi tự động

## 📞 Troubleshooting

### Swagger không hiển thị Authorize button
- Kiểm tra: `builder.Services.AddSwaggerGen()` có được gọi
- Kiểm tra middleware: `app.UseAuthentication()` trước `app.UseAuthorization()`

### Token không hoạt động
- Kiểm tra format: `Bearer <token>` (có khoảng trắng)
- Kiểm tra expiration: Token có hết hạn không?
- Kiểm tra signature: Secret key có đúng không?

### API trả về 401 Unauthorized
- Login lại lấy token mới
- Kiểm tra token format: `Authorization: Bearer eyJ...`
- Kiểm tra user status: User phải active

---

**Cấu hình hoàn tất! 🎉**

Bây giờ bạn có thể test API với JWT authorization trong Swagger UI. Hãy bắt đầu bằng cách login và sau đó test các protected endpoints.

Nếu gặp vấn đề, hãy kiểm tra log output hoặc tham khảo tài liệu chi tiết trong `SWAGGER_API_TESTING_GUIDE.md`.
