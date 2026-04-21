# 🎯 SAMPLE DATA & TEST GUIDE - SUMMARY

## 📌 Các file mới được tạo:

### 1. **Sample Data Generation** (Tự động khi chạy)
   - `API/Infrastructure/Data/SampleDataForSearching.cs` - Generator dữ liệu test

### 2. **Hướng dẫn Chi Tiết**
   - `API/docs/SAMPLE_DATA_USAGE_GUIDE.md` - Hướng dẫn đầy đủ (Đọc cái này trước!)

### 3. **SQL Inserts**
   - `API/docs/SAMPLE_DATA_FOR_TESTING.sql` - SQL direct insert (nếu muốn dùng SQL)

### 4. **Test Scripts**
   - `API/docs/CURL_TEST_SAMPLES.sh` - Bash script với curl commands
   - `API/docs/TEST_SAMPLES.ps1` - PowerShell script (tương tác)

### 5. **Postman Collection**
   - `API/docs/Flight_Booking_Postman_Collection.json` - Import vào Postman

---

## 🚀 Quick Start (3 bước)

### Bước 1: Chạy ứng dụng
```bash
cd E:\pbl3\flight\backend
dotnet run --project API/API.csproj
```

Chờ logs hiển thị:
```
✅ Sample search data added successfully!
   - Flights: 30+ records
   - Inventories: 60+ records
   - Promotions: 8 records
```

### Bước 2: Lấy JWT Token
```bash
# Đăng nhập
curl -X POST http://localhost:5042/api/v1/Users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@flightbooking.vn",
    "password": "Admin@123456"
  }' | jq '.token' -r
```

Lấy token từ response, copy vào variable `ADMIN_TOKEN`

### Bước 3: Test API
**Option A: PowerShell (Tương tác)**
```powershell
.\API\docs\TEST_SAMPLES.ps1
```

**Option B: Postman**
1. Import file: `API/docs/Flight_Booking_Postman_Collection.json`
2. Set variable `base_url = http://localhost:5042`
3. Set variable `admin_token = <your_token>`
4. Run tests

**Option C: cURL Manual**
```bash
# Test search flights
curl -X GET 'http://localhost:5042/api/v1/flights/search?departureId=1&arrivalId=2&date=2025-01-22'

# Test admin flights
curl -X GET 'http://localhost:5042/api/v1/admin/FlightsAdmin?page=1&pageSize=10' \
  -H "Authorization: Bearer <token>"
```

---

## 📊 Sample Data Summary

| Type | Count | Chi tiết |
|------|-------|---------|
| **Airports** | 6 | SGN, HAN, DAD, CTS, VCA, HUI |
| **Routes** | 7+ | Khác nhau, cả chiều đi và chiều về |
| **Aircraft** | 5 | Boeing/Airbus models, 180-350 seats |
| **Flights** | 30+ | 7 days × 8 flights/day |
| **Inventories** | 60+ | Economy, Business, Premium seats |
| **Promotions** | 8 | % discount, fixed amount, expired codes |
| **Users** | 3 | Admin + 2 users |
| **Bookings** | 2+ | Sample bookings |

---

## ✔️ Test Cases Có Sẵn

### Flights Search
```
✅ Valid route - multiple results
✅ Different dates (7 days)
✅ No results (invalid route)
✅ Pagination (page 1, 2, 3)
```

### Promotions
```
✅ Valid active codes (SUMMER20, SAVE500K, etc)
✅ Expired codes
✅ Full quota codes  
✅ Invalid codes
```

### Admin Endpoints
```
✅ Get flights with pagination
✅ Get flights with seat inventory
✅ Get promotions list
✅ Get active promotions
```

---

## 💡 Lưu Ý Quan Trọng

1. **Auto-add data**: Chỉ chạy lần đầu tiên, lần thứ 2 sẽ skip
2. **Development Mode Only**: Feature chỉ hoạt động khi `IsDevelopment()`
3. **Random Data**: Ghế, giá, status được randomize mỗi lần
4. **Timestamps**: Dùng `DateTime.UtcNow` nên sẽ có current date

---

## 🔍 Kiểm Tra Data đã được add

Chạy queries SQL:

```sql
-- Kiểm tra flights
SELECT COUNT(*) as "Total Flights" FROM "Flights";
-- Expected: 30+

-- Kiểm tra promotions
SELECT COUNT(*) as "Total Promotions" FROM "Promotions";
-- Expected: 8

-- Kiểm tra inventories
SELECT COUNT(*) as "Total Inventories" FROM "FlightSeatInventories";
-- Expected: 60+
```

---

## 🎬 Video Test Flow

1. **Login** → Get token
2. **Search flights** → See available flights
3. **Check admin flights** → See all flights with inventory
4. **List promotions** → See all promo codes
5. **Validate codes** → Test valid/invalid/expired codes
6. **Pagination test** → Test page 1, 2, 3...

---

## 🛠️ Nếu cần thêm dữ liệu khác

Edit `API/Infrastructure/Data/SampleDataForSearching.cs`:

```csharp
// Thêm custom flights
var customFlights = new List<Flight>
{
    new Flight 
    { 
        FlightNumber = "VN999",
        RouteId = routeId,
        // ... more properties
    }
};
context.Flights.AddRange(customFlights);
await context.SaveChangesAsync();
```

Rebuild và rerun application.

---

## 📞 Support

Nếu có lỗi:
1. Kiểm tra logs: Tìm "Adding comprehensive search test data"
2. Check database connection string
3. Ensure database is up to date: `dotnet ef database update`
4. Clear database và restart (nếu cần)

---

**Status: ✅ Complete**
- Sample data generator: Ready
- Test scripts: Ready  
- Documentation: Ready
- Build: Successful

Ready to test! 🚀
