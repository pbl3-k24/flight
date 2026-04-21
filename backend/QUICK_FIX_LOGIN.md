# 🔧 NHANH CHÓNG: FIX LỖI LOGIN

## ✅ CÁC BƯỚC FIX (5-10 PHÚT)

### **Step 1: Mở Terminal PowerShell**
```powershell
cd E:\pbl3\flight\backend
```

### **Step 2: Xóa Database PostgreSQL**

Nếu bạn có psql CLI:
```powershell
$env:PGPASSWORD = "SecretPassword123!"
psql -h localhost -p 5432 -U admin -c "DROP DATABASE IF EXISTS \"FlightBookingDB\";"
```

Hoặc dùng **pgAdmin**: 
- Connect to PostgreSQL server
- Right-click "FlightBookingDB" → Delete

### **Step 3: Clean & Build Solution**

```powershell
# Xóa compiled files
dotnet clean

# Xóa bin/obj folders
Remove-Item -Recurse -Force API/bin, API/obj

# Build project
dotnet build -c Debug
```

### **Step 4: Apply Fresh Migrations**

```powershell
# Remove pending migrations (if any)
dotnet ef migrations list

# If migrations exist, remove them
dotnet ef migrations remove -f

# Create fresh migration
dotnet ef migrations add InitialCreate --project API/API.csproj

# Apply migration (creates database + tables + seed data)
dotnet ef database update --project API/API.csproj
```

### **Step 5: Run Application**

```powershell
dotnet run --project API/API.csproj
```

**Đợi cho đến khi nhìn thấy:**
```
════════════════════════════════════════════════════
✅ TEST ACCOUNT CREDENTIALS (Development Only)
════════════════════════════════════════════════════
Admin Account:
  Email: admin@flightbooking.vn
  Password: Admin@123456
...
```

### **Step 6: Test Login**

Dùng Postman POST:
```
URL: http://localhost:5000/api/v1/Users/login
Headers: Content-Type: application/json
Body:
{
  "email": "admin@flightbooking.vn",
  "password": "Admin@123456"
}
```

Expected Response: ✅ JWT Token returned

---

## 🆘 NẾU VẪN CÓ LỖI BUILD

Nếu bạn thấy lỗi "The type or namespace name 'NotFoundException' could not be found":

### Solution: Thêm using statements

Tạo file `add-usings.ps1`:

```powershell
# Files cần thêm using API.Application.Exceptions
$files = @(
    "API/Application/Services/BookingService.cs",
    "API/Application/Services/PricingService.cs",
    "API/Application/Services/TicketService.cs",
    "API/Application/Services/UserAdminService.cs",
    "API/Application/Services/RefundService.cs",
    "API/Application/Services/BookingAdminService.cs",
    "API/Application/Services/PromotionAdminService.cs",
    "API/Controllers/BookingsController.cs",
    "API/Controllers/FlightsController.cs",
    "API/Controllers/FlightsAdminController.cs",
    "API/Controllers/TicketsController.cs",
    "API/Controllers/PaymentsController.cs"
)

foreach ($file in $files) {
    if (Test-Path $file) {
        $content = Get-Content $file

        # Check if already has the using
        if ($content -notlike "*using API.Application.Exceptions;*") {
            Write-Host "Adding using to $file..."

            # Add after first using statements
            $content = $content -replace '(namespace.*?;)', "`$1`nusing API.Application.Exceptions;"
            Set-Content $file $content
        }
    }
}

Write-Host "✅ Done!"
```

Chạy:
```powershell
.\add-usings.ps1
```

---

## ✨ SUMMARY

**Để sửa lỗi login:**

1. ✅ Delete database
2. ✅ Clean solution (`dotnet clean`)
3. ✅ Apply migrations (`dotnet ef database update`)
4. ✅ Run application
5. ✅ Login with provided credentials

**Test Credentials:**
- Email: `admin@flightbooking.vn`
- Password: `Admin@123456`

---

**⏱️ Time**: 5-10 minutes  
**✅ Status**: Ready to test
