# 🔧 Cấu hình JWT Authorization trong Swagger (Advanced)

## 📌 Giới thiệu

Hiện tại, Swagger UI của bạn sử dụng Swashbuckle version 6.5.0, hỗ trợ JWT Bearer authentication.

Tài liệu này hướng dẫn cách nâng cao cấu hình JWT nếu bạn muốn tùy chỉnh giao diện hoặc thêm tính năng.

---

## 🛠️ Cấu hình Hiện tại

Trong `Program.cs`, Swagger đã được cấu hình với hỗ trợ JWT:

```csharp
builder.Services.AddSwaggerGen();
```

Cấu hình mặc định này cho phép bạn:
- ✅ Nhập JWT token thủ công
- ✅ Gửi token tự động trong header `Authorization: Bearer <token>`
- ✅ Test tất cả endpoint có `[Authorize]`

---

## 🚀 Nâng Cao: Cấu hình tùy chỉnh

Nếu bạn muốn cấu hình chi tiết hơn (ví dụ: thêm description, tùy chỉnh UI, v.v.), hãy cập nhật `Program.cs`:

### Option 1: Cấu hình OpenAPI (Swagger)

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // Thêm Security Definition cho Bearer token
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Nhập JWT token của bạn. Ví dụ: Bearer eyJhbGciOiJIUzI1NiIs...",
    });

    // Yêu cầu token cho tất cả endpoint có [Authorize]
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });

    // Các cấu hình khác (title, version, description, v.v.)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});
```

### Option 2: Tùy chỉnh Swagger UI

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Flight API v1");

        // Tùy chỉnh giao diện
        options.RoutePrefix = string.Empty; // Swagger UI ở root
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.DefaultModelsExpandDepth(0);

        // Theme tùy chỉnh (nếu muốn)
        options.InjectStylesheet("https://cdnjs.cloudflare.com/ajax/libs/swagger-ui/4.15.5/swagger-ui.css");
    });
}
```

---

## 🔑 JWT Token Format

Ứng dụng sử dụng **HS256 (HMAC with SHA-256)** để ký token.

Token structure (JWT):
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI1YjIyYzlhYy1lYWY4LTQ2NTAtYjNkYy1hNGM2OGI5MjQ2ZjEiLCJlbWFpbCI6ImFkbWluQGZsaWdodGJvb2tpbmcudm4iLCJuYmYiOjE3MTI4NDAyNDAsImV4cCI6MTcxMjg0Mzg0MCwiaWF0IjoxNzEyODQwMjQwfQ.abc123...
```

### Decode Token (online tools):
- https://jwt.io
- https://jwtdebugger.com

Giải mã sẽ thấy payload:
```json
{
  "sub": "uuid-of-user",
  "email": "admin@flightbooking.vn",
  "nbf": 1712840240,
  "exp": 1712843840,
  "iat": 1712840240
}
```

---

## 🔄 Custom Authentication Handler

Ứng dụng sử dụng custom JWT handler: `JwtAuthenticationHandler`

File: `API/Infrastructure/Security/JwtAuthenticationHandler.cs`

Nó:
1. Đọc token từ header `Authorization: Bearer <token>`
2. Xác nhận signature và expiration
3. Trích xuất claims (user ID, email, roles)
4. Tạo `ClaimsPrincipal` cho `User` context

---

## 📊 Authorization Policies

### Policy: Chỉ Admin
```csharp
[Authorize(Roles = "Admin")]
public async Task<IActionResult> AdminOnly()
{
    return Ok("Only admin can access");
}
```

### Policy: Chỉ User đã xác minh email
```csharp
[Authorize]
[Authorize(Policy = "EmailVerified")]
public async Task<IActionResult> RequireVerification()
{
    return Ok("Email must be verified");
}
```

---

## 🧪 Test Token Manually

### Sử dụng cURL:

```bash
# 1. Đăng nhập
curl -X POST "http://localhost:5000/api/v1/Users/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@flightbooking.vn","password":"Admin@123456"}'

# Response:
# {
#   "token": "eyJhbGciOiJIUzI1NiIs...",
#   "expiresIn": 3600,
#   "userId": "uuid"
# }

# 2. Sử dụng token
curl -X GET "http://localhost:5000/api/v1/Users/profile" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

### Sử dụng Postman:

1. **GET TOKEN:**
   - Method: POST
   - URL: `http://localhost:5000/api/v1/Users/login`
   - Body (raw JSON):
     ```json
     {
       "email": "admin@flightbooking.vn",
       "password": "Admin@123456"
     }
     ```
   - Sao chép giá trị `token` từ response

2. **SET UP AUTHORIZATION:**
   - Tab: "Authorization"
   - Type: "Bearer Token"
   - Token: `<paste token here>`

3. **TEST API:**
   - Gọi bất kỳ endpoint nào
   - Token sẽ tự động được gửi

---

## 🛡️ Security Best Practices

### ✅ Luôn làm:
- Sử dụng HTTPS trong production
- Lưu token trong `localStorage` hoặc `sessionStorage` (client-side)
- Refresh token khi hết hạn
- Gửi token trong header (không trong URL)
- Xóa token khi user logout

### ❌ Không bao giờ làm:
- Hiển thị token trong logs
- Commit token vào source code
- Gửi token qua URL query parameters
- Chia sẻ token công khai
- Lưu token trong plaintext

---

## 🐛 Debug JWT Issues

### Issue: Token không hoạt động

**Kiểm tra:**
1. Token format: `Bearer <token>` (với khoảng trắng)
2. Token expiration: Kiểm tra `exp` claim
3. Signature: Đảm bảo secret key đúng
4. User status: User phải active (`Status == 0`)

### Logs:
Xem logs trong Visual Studio Output window để debug:
```
JWT validation failed: Token expired
JWT validation failed: Invalid signature
JWT validation failed: User not found
```

---

## 📚 Tài liệu tham khảo

- [Swagger/Swashbuckle Documentation](https://swagger.io/tools/swagger-ui/)
- [JWT.io](https://jwt.io) - Tìm hiểu JWT
- [ASP.NET Core Authentication](https://docs.microsoft.com/aspnet/core/security/authentication)
- [OpenAPI Specification](https://spec.openapis.org/oas/v3.0.3)

---

## ✨ Tiếp theo

Nếu bạn muốn:
- **Thêm Refresh Token:** Tạo endpoint `/api/v1/Users/refresh-token`
- **Multi-tenant Support:** Thêm `TenantId` vào JWT claims
- **Role-based Access Control (RBAC):** Tạo custom Authorization Policies
- **API Versioning:** Sử dụng `[ApiVersion]` attribute

Hãy tạo issue hoặc yêu cầu để thêm các tính năng này! 🚀
