# JWT Authentication Debugging Guide

## Các bước test

### 1. Start Server
```powershell
cd E:\pbl3\flight\backend\API
dotnet run
```

Server sẽ chạy trên `http://localhost:5042`

---

### 2. Test Login (lấy token)
```
POST /api/v1/Users/login
Content-Type: application/json

{
  "email": "admin@flightbooking.vn",
  "password": "Admin@123456"
}
```

**Response:**
```json
{
  "userId": 1,
  "email": "admin@flightbooking.vn",
  "fullName": "Quản trị viên",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-04-18T..."
}
```

**Lưu token để dùng trong các request tiếp theo**

---

### 3. Test Endpoints (trong Swagger UI)

#### Step 1: Click "Authorize" button
- Nút xanh ở trên cùng của Swagger UI

#### Step 2: Paste token (không cần "Bearer " prefix)
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

#### Step 3: Test các endpoint trong thứ tự này:

**A. Debug endpoint - No Auth (để chắc chắn server chạy)**
```
GET /api/v1/Debug/no-auth
```

Expected: 200 OK

---

**B. Debug endpoint - With Auth (test JWT validation)**
```
GET /api/v1/Debug/with-auth
```

Expected: 200 OK
Response sẽ show:
```json
{
  "message": "Authentication successful",
  "userId": "1",
  "email": "admin@flightbooking.vn",
  "roles": ["Admin"],
  "timestamp": "..."
}
```

---

**C. Debug endpoint - Admin Only (test role-based auth)**
```
GET /api/v1/Debug/admin-only
```

Expected: 200 OK (vì user có Admin role)

---

**D. Debug endpoint - Show Claims (xem tất cả claims trong token)**
```
GET /api/v1/Debug/claims
```

Expected: 200 OK với chi tiết tất cả claims

---

**E. Real admin endpoint**
```
GET /api/v1/admin/FlightsAdmin
```

Expected: 200 OK (nếu tất cả đều hoạt động)

---

### 4. Check Logs (nếu test fails)

Hãy xem output của dotnet run để kiểm tra logs:

```
[INF] Authorization Header: Bearer eyJhbGci...
[INF] Extracted token (first 50 chars): eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
[INF] Validating JWT token (first 50 chars): eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
[INF] JWT token validated successfully. Claims: ...
[INF] WithAuth endpoint called. UserId: 1, Email: admin@flightbooking.vn, Roles: Admin
```

---

## Các lỗi có thể gặp & cách fix

### 1. 401 Unauthorized: Valid JWT token required
**Nguyên nhân:** Token không được gửi hoặc không hợp lệ
**Cách fix:**
- Kiểm tra token có được paste vào Authorize không (không cần "Bearer ")
- Kiểm tra token chưa hết hạn
- Kiểm tra logs xem Authorization header có được nhận không

### 2. Token format invalid
**Nguyên nhân:** Token bị cắt hoặc sai định dạng
**Cách fix:**
- Copy đúng toàn bộ token từ response
- Không thêm "Bearer " khi paste vào Swagger (Swagger tự thêm)
- Không có khoảng trắng ở đầu hoặc cuối

### 3. 403 Forbidden
**Nguyên nhân:** User không có đủ quyền (role)
**Cách fix:**
- Kiểm tra user có role "Admin" không
- Xem endpoint yêu cầu role nào
- Check logs xem claims có chứa role không

---

## Workflow kiểm tra từng bước

```
1. Start server
   ↓
2. GET /api/v1/Debug/no-auth  (200 OK) → Server chạy
   ↓
3. POST /api/v1/Users/login  (200 OK) → Lấy token
   ↓
4. Paste token vào Authorize
   ↓
5. GET /api/v1/Debug/with-auth  (200 OK) → JWT validation hoạt động
   ↓
6. GET /api/v1/Debug/claims  (200 OK) → Xem claims chi tiết
   ↓
7. GET /api/v1/Debug/admin-only  (200 OK) → Role-based auth hoạt động
   ↓
8. GET /api/v1/admin/FlightsAdmin  (200 OK) → Real admin endpoint hoạt động
```

---

## File hữu ích để check

- `API/Infrastructure/Security/JwtAuthenticationHandler.cs` - Xử lý JWT validation
- `API/Application/Services/JwtTokenService.cs` - Tạo & validate token
- `API/Program.cs` - Middleware configuration
- Logs từ dotnet run - Chi tiết điều gì xảy ra

---

## Test Data

**Admin Account:**
- Email: `admin@flightbooking.vn`
- Password: `Admin@123456`
- Role: `Admin`

**Regular User:**
- Email: `user1@gmail.com`
- Password: `User1@123456`
- Role: `User`

---

Hãy thực hiện test workflow này và check logs. Token authorization phải hoạt động ở step 5-6!
