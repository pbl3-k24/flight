# 🔐 FIX LOGIN ERROR - STEP BY STEP GUIDE

## ❌ VẤN ĐỀ BẠN GẶP

```
API.Application.Exceptions.ValidationException: Invalid email or password
at API.Application.Services.AuthService.LoginAsync(LoginDto dto)
```

**Nguyên nhân**: Password hash trong database không khớp với password bạn nhập

---

## ✅ GIẢI PHÁP NHANH (5 phút)

### Cách 1: Sử dụng PowerShell Script (KHUYẾN NGHỊ)

1. **Mở PowerShell** trong folder `E:\pbl3\flight\backend\`

2. **Chạy script reset database**:
```powershell
.\reset-db.ps1
```

3. **Khi hoàn thành**, chạy application:
```powershell
dotnet run --project API/API.csproj
```

4. **Đợi để xem test credentials** trong console:
```
════════════════════════════════════════════════════
✅ TEST ACCOUNT CREDENTIALS (Development Only)
════════════════════════════════════════════════════
Admin Account:
  Email: admin@flightbooking.vn
  Password: Admin@123456
...
```

5. **Đăng nhập với**:
   - Email: `admin@flightbooking.vn`
   - Password: `Admin@123456`

---

### Cách 2: Manual Steps (Nếu script không chạy)

#### Step 1: Mở pgAdmin hoặc psql

Xóa database cũ:
```sql
DROP DATABASE IF EXISTS "FlightBookingDB";
```

Tạo database mới:
```sql
CREATE DATABASE "FlightBookingDB";
```

#### Step 2: Chạy migrations

```powershell
cd E:\pbl3\flight\backend
dotnet ef database update --project API/API.csproj
```

#### Step 3: Rebuild và run

```powershell
dotnet build -c Release
dotnet run --project API/API.csproj
```

#### Step 4: Đợi console output hiển thị test credentials

#### Step 5: Sử dụng credentials để đăng nhập

---

## 📝 TEST CREDENTIALS

Sau khi reset database thành công, dùng những account này:

### Admin Account ⭐
```
Email: admin@flightbooking.vn
Password: Admin@123456
```

### User Account 1
```
Email: user1@gmail.com
Password: User1@123456
```

### User Account 2
```
Email: user2@gmail.com
Password: User2@123456
```

---

## 🧪 TEST ĐĂNG NHẬP

### Dùng cURL
```bash
curl -X POST http://localhost:5000/api/v1/Users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@flightbooking.vn",
    "password": "Admin@123456"
  }'
```

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "userId": 1,
    "email": "admin@flightbooking.vn",
    "token": "eyJhbGc...",
    "expiresAt": "2024-XX-XX..."
  }
}
```

### Dùng Postman
1. New request → POST
2. URL: `http://localhost:5000/api/v1/Users/login`
3. Headers: `Content-Type: application/json`
4. Body:
```json
{
  "email": "admin@flightbooking.vn",
  "password": "Admin@123456"
}
```
5. Send → Nhận JWT token ✅

### Dùng Swagger UI
1. Go to `http://localhost:5000/swagger`
2. Find `POST /api/v1/Users/login`
3. Click "Try it out"
4. Nhập email và password
5. Click "Execute"
6. Xem response có token không

---

## ⚠️ NẾU VẪN LỖI

### Problem: "Invalid email or password"

**Kiểm tra:**

1. **Database có users không?**
   ```sql
   SELECT Email, PasswordHash FROM "Users" LIMIT 3;
   ```
   - Nếu trống → chạy lại migration
   - Nếu có nhưng PasswordHash = NULL → xóa database, reset lại

2. **PostgreSQL chạy không?**
   ```powershell
   psql -h localhost -U admin -d FlightBookingDB -c "SELECT COUNT(*) FROM \"Users\";"
   ```

3. **Connection string đúng không?**
   - Check `appsettings.json`:
   ```json
   "DefaultConnection": "Host=localhost;Port=5432;Database=FlightBookingDB;Username=admin;Password=SecretPassword123!"
   ```

4. **PasswordHasher đúng không?**
   - Check `API/Infrastructure/Security/PasswordHasher.cs` có method `VerifyPassword()` không

### Problem: "Database connection failed"

```powershell
# Test PostgreSQL connection
psql -h localhost -p 5432 -U admin -c "SELECT version();"
```

Nếu lỗi → start PostgreSQL:
- Windows: PostgreSQL Services → postgresql-x64-15 → Start
- Or: `net start postgresql-x64-15`

### Problem: Migration errors

```powershell
# Remove migrations
dotnet ef migrations remove --project API/API.csproj -f

# Create new migration
dotnet ef migrations add InitialCreate --project API/API.csproj

# Apply migrations
dotnet ef database update --project API/API.csproj
```

---

## 🔍 DEBUG TIPS

### Xem password hash trong database
```sql
SELECT Email, PasswordHash FROM "Users" WHERE Email = 'admin@flightbooking.vn';
```

Hash phải là **Base64 string dài**, ví dụ:
```
aAbBcCdDeEfFgGhHiIjJkKlLmMnNoOpPqQrSsT...
```

**KHÔNG phải**:
- Rỗng (NULL)
- "placeholder"
- Quá ngắn (< 50 chars)

### Test PasswordHasher trực tiếp

Tạo file test `TestPasswordHasher.cs`:
```csharp
var hasher = new PasswordHasher();
var password = "Admin@123456";
var hash = hasher.HashPassword(password);
Console.WriteLine($"Hash: {hash}");
Console.WriteLine($"Verify: {hasher.VerifyPassword(password, hash)}");
```

### Xem logs chi tiết

Thêm vào `appsettings.json`:
```json
"Logging": {
  "LogLevel": {
    "Default": "Debug",
    "API.Application.Services.AuthService": "Debug"
  }
}
```

---

## ✅ CHECKLIST HOÀN TẤT

- [ ] PostgreSQL chạy
- [ ] Database deleted và created mới
- [ ] Migrations applied
- [ ] Application started thành công
- [ ] Console hiển thị test credentials
- [ ] Có 3 users trong database (admin, user1, user2)
- [ ] Đăng nhập với admin account thành công
- [ ] Nhận JWT token
- [ ] Token có thể dùng cho protected endpoints

---

## 🆘 CẦN GIÚP?

Nếu vẫn không được, hãy kiểm tra:

1. **Logs**: Xem Output window → Output dropdown → Debug
2. **Database**: Dùng pgAdmin hoặc DBeaver để kiểm tra
3. **Connection**: Test kết nối tới PostgreSQL
4. **Code**: Build solution → xem có error không

---

**⏱️ Thời gian**: ~5 phút để fix  
**✅ Status**: Ready to test  
**🚀 Next**: Chạy application + login
