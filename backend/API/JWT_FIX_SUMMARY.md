# JWT Authentication Fix Summary

## 🔧 Vấn đề đã fix

### 1. ✅ Token không chứa Role claims
**Vấn đề:** Token được tạo ra không chứa role information, nên `[Authorize(Roles="Admin")]` luôn fail
**Fix:** Cập nhật `JwtTokenService.GenerateToken()` để include role claims từ user.Roles

### 2. ✅ Roles không được load khi login
**Vấn đề:** AuthService dùng `GetByEmailAsync()` (không load roles) thay vì `GetWithRolesAsync()`
**Fix:** 
- Tạo method mới `GetByEmailWithRolesAsync()` trong UserRepository
- Cập nhật AuthService.LoginAsync() để dùng method mới này

### 3. ✅ Admin user không có role assignment
**Vấn đề:** DbInitializer không tạo UserRole entries
**Fix:** Thêm code để tạo UserRole entries cho admin user với Admin role

### 4. ✅ ContactEmail missing trong Booking seed
**Vấn đề:** Booking entity yêu cầu ContactEmail nhưng seeding không cung cấp
**Fix:** Thêm `ContactEmail = testUser1.Email` khi tạo Booking

### 5. ✅ Swagger UI không tự động gửi token
**Vấn đề:** Swagger UI tạo authorize button nhưng không tự động áp dụng cho tất cả endpoints
**Fix:** Tạo `AuthorizeOperationFilter` để tự động add security requirement cho tất cả endpoints

### 6. ✅ Thiếu logging để debug
**Vấn đề:** Không có chi tiết logs khi authentication fail
**Fix:** Thêm detailed logging vào:
- JwtAuthenticationHandler
- JwtTokenService

---

## 📝 Các file đã thay đổi

### Core Authentication Files
1. `API/Infrastructure/Security/JwtAuthenticationHandler.cs`
   - Thêm detailed logging

2. `API/Application/Services/JwtTokenService.cs`
   - Thêm role claims vào token
   - Thêm detailed logging validation

3. `API/Application/Services/AuthService.cs`
   - Cập nhật LoginAsync() dùng GetByEmailWithRolesAsync()

### Repository Files
4. `API/Infrastructure/Repositories/UserRepository.cs`
   - Thêm GetByEmailWithRolesAsync() method

5. `API/Application/Interfaces/IUserRepository.cs`
   - Thêm GetByEmailWithRolesAsync() interface method

### Database Seeding
6. `API/Infrastructure/Data/DbInitializer.cs`
   - Thêm UserRole assignment cho users
   - Fix ContactEmail trong Booking

### Swagger Configuration
7. `API/Program.cs`
   - Thêm AuthorizeOperationFilter

8. `API/Infrastructure/Swagger/AuthorizeOperationFilter.cs` (NEW)
   - Tự động add security requirement cho tất cả endpoints

### Testing & Debugging
9. `API/Controllers/DebugController.cs` (NEW)
   - 5 debug endpoints để test authentication flow

10. `API/JWT_DEBUGGING_GUIDE.md` (NEW)
    - Chi tiết guide để test JWT authentication

11. `jwt-test.ps1` (NEW)
    - PowerShell script tự động test tất cả endpoints

---

## 🧪 Cách test ngay bây giờ

### Option 1: Manual test trong Swagger UI
```
1. Start server: dotnet run
2. Vào http://localhost:5042
3. POST /api/v1/Users/login (lấy token)
4. Click "Authorize" button, paste token
5. GET /api/v1/Debug/with-auth (test JWT)
6. GET /api/v1/Debug/admin-only (test role)
7. GET /api/v1/admin/FlightsAdmin (real endpoint)
```

### Option 2: Automated test script
```powershell
cd E:\pbl3\flight\backend
powershell .\jwt-test.ps1
```

---

## 🎯 Expected Results

### Successful JWT Flow
```
✅ Login → Token generated with roles
✅ Token sent in Authorization header
✅ JwtAuthenticationHandler validates token
✅ Role claims extracted correctly
✅ [Authorize(Roles="Admin")] check passes
✅ Admin endpoint returns 200 OK
```

### Log Output (successful case)
```
[INF] Authorization Header: Bearer eyJhbGci...
[INF] Extracted token (first 50 chars): eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
[INF] Validating JWT token (first 50 chars): eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
[INF] JWT token validated successfully. Claims: ...sub=1, email=admin@flightbooking.vn, ..., role=Admin
[INF] Token validated successfully. Principal claims: ...
[INF] WithAuth endpoint called. UserId: 1, Email: admin@flightbooking.vn, Roles: Admin
```

---

## 📚 Key Classes

### JwtAuthenticationHandler
- Extracts Bearer token từ Authorization header
- Gọi JwtTokenService.ValidateToken()
- Tạo AuthenticationTicket nếu valid
- Returns 401 với message nếu invalid

### JwtTokenService
- GenerateToken(): Tạo JWT token với role claims
- ValidateToken(): Validate token signature, issuer, audience, lifetime

### AuthService.LoginAsync()
- Fetch user WITH roles (quan trọng!)
- Verify password
- Gọi GenerateToken() (token sẽ chứa roles)
- Return token trong response

### Program.cs Middleware Order
```csharp
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthentication();   // ← JwtAuthenticationHandler runs here
app.UseAuthorization();    // ← Role check happens here
app.MapControllers();
```

---

## 🚀 Next Steps (nếu vẫn có vấn đề)

1. Check logs chi tiết từ dotnet run output
2. Xem Authorization header có được gửi không
3. Verify token format đúng
4. Check JWT signature key match
5. Verify Issuer/Audience config
6. Kiểm tra token expiration time

---

**Status:** ✅ Ready to test
**Last Updated:** 2026-04-18
