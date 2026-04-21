# 📊 Sample Data Seeding - Tóm Tắt

## ✅ Dữ Liệu Mẫu Đã Tạo

Chúng tôi đã tạo ra một hệ thống seed data hoàn chỉnh cho ứng dụng quản lý đặt vé máy bay. Dữ liệu sẽ tự động được thêm vào database khi ứng dụng khởi động.

### 📝 Dữ Liệu Mẫu Bao Gồm:

| Category | Count | Details |
|----------|-------|---------|
| **Users** | 3 | 1 Admin + 2 Regular Users |
| **Airports** | 4 | SGN, HAN, DAD, CTS |
| **Routes** | 4 | SG-HN, HN-SG, SG-DN, DN-HN |
| **Aircraft** | 2 | Boeing 737, Airbus A321 |
| **Seat Classes** | 3 | Economy, Business, First |
| **Seat Templates** | 3 | Mapping Aircraft to Seat Classes |
| **Flights** | 3 | VN001, VN002, VN003 |
| **Seat Inventory** | 2 | Inventory for Flight 1 |
| **Promotions** | 3 | SUMMER2024, EARLYBIRD100K, NEWUSER20 |
| **Refund Policies** | 1 | Economy Class refund policy |
| **Sample Booking** | 1 | Sample booking with passenger |

---

## 🚀 Cách Sử Dụng

### 1️⃣ Khởi Động Ứng Dụng
```bash
dotnet run
```

### 2️⃣ Xem Kết Quả
```
✅ Database migrations applied successfully.
✅ Seeding sample data...
✅ Sample data seeding completed.
```

### 3️⃣ Kiểm Tra Database
- Database sẽ tự động được tạo nếu chưa tồn tại
- Dữ liệu mẫu sẽ được thêm vào (chỉ lần đầu tiên)
- Lần chạy tiếp theo sẽ bỏ qua seeding nếu database đã có dữ liệu

---

## 👥 Tài Khoản Test

### Admin Account
```
Email: admin@flightbooking.vn
Password: [Cần đặt lại - hiện tại là hash mẫu]
Role: Admin
```

### Regular Users
```
Email: user1@gmail.com / user2@gmail.com
Password: [Cần đặt lại - hiện tại là hash mẫu]
Role: User
```

**⚠️ Lưu ý**: Hash password hiện tại là mẫu. Trước khi deploy production, cần:
1. Tạo migration để cập nhật password thực tế
2. Hash password bằng bcrypt hoặc phương pháp bảo mật khác
3. Cập nhật giá trị hash trong `DbInitializer.cs`

---

## ✈️ Thông Tin Chuyến Bay

### Các Sân Bay
- **SGN**: Tân Sơn Nhất (TP.HCM)
- **HAN**: Nội Bài (Hà Nội)
- **DAD**: Quốc tế Đà Nẵng
- **CTS**: Cần Thơ

### Các Chuyến Bay (Chạy Tomorrow)
1. **VN001**: SGN → HAN, 08:00 - 10:25
2. **VN002**: HAN → SGN, 14:00 - 16:25
3. **VN003**: SGN → DAD, 09:00 (week after) - 10:35

---

## 💰 Giá Vé & Khuyến Mãi

### Giá Cơ Bản
- **Economy**: 1,500,000 VND
- **Business**: 3,500,000 VND
- **First**: 5,500,000 VND

### Khuyến Mãi Có Sẵn
1. **SUMMER2024**: Giảm 10%
2. **EARLYBIRD100K**: Giảm 100,000 VND
3. **NEWUSER20**: Giảm 20% (người dùng mới)

---

## 📂 Files Liên Quan

- **File Seeding**: `API/Infrastructure/Data/DbInitializer.cs`
- **Program.cs**: Gọi `DbInitializer.InitializeDatabaseAsync()`
- **Test Guide**: `API/SAMPLE_DATA_TESTING_GUIDE.md`
- **Documentation**: `API/REPOSITORY_IMPLEMENTATION_GUIDE.md`

---

## 🔧 Tùy Chỉnh Dữ Liệu

### Để Thay Đổi Dữ Liệu Mẫu:

1. Chỉnh sửa `DbInitializer.cs`
2. **Xóa database** để seed lại từ đầu
3. Chạy lại ứng dụng

### Để Bỏ Qua Seeding:

Comment out phần gọi DbInitializer trong `Program.cs`:
```csharp
// await DbInitializer.InitializeDatabaseAsync(dbContext);
```

---

## ✨ Tính Năng

### ✅ Seed Automátically
- Tự động tạo dữ liệu khi database trống
- Không seed lại nếu database có dữ liệu

### ✅ Transaction Safe
- Sử dụng `await context.SaveChangesAsync()` 
- Đảm bảo tính toàn vẹn dữ liệu

### ✅ Comprehensive Data
- Tất cả các entities cần thiết để test
- Relationships được set up đúng

### ✅ Realistic Data
- Giá vé thực tế VND
- Tên sân bay, máy bay thực tế
- Codes khuyến mãi hợp lý

---

## 🐛 Troubleshooting

### Database không được seed
**Giải pháp**: 
- Kiểm tra connection string trong `appsettings.json`
- Xóa database cũ nếu có
- Chạy lại ứng dụng

### Lỗi duplicate key
**Giải pháp**:
- Xóa database: `drop database flight_booking;`
- Chạy lại ứng dụng

### Password hash không hoạt động
**Giải pháp**:
- Hash password trước trong code
- Hoặc sử dụng API register để tạo user mới

---

## 📖 Hướng Dẫn Test Chi Tiết

Xem file: `API/SAMPLE_DATA_TESTING_GUIDE.md`

Nó bao gồm:
- Cách gọi từng API endpoint
- Dữ liệu mẫu input/output
- Test scenarios hoàn chỉnh
- cURL/Postman examples

---

## 🎯 Next Steps

1. ✅ **Chạy ứng dụng**: `dotnet run`
2. ✅ **Kiểm tra database**: Dữ liệu sẽ được thêm vào
3. ✅ **Test API**: Sử dụng dữ liệu mẫu để test
4. ✅ **Đặt password thực tế**: Cập nhật hash password
5. ✅ **Deploy**: Đẩy code lên server

---

**Status**: ✅ Seed Data Ready  
**Build**: ✅ Passing  
**Database**: ✅ Auto-initialized  
**Testing**: ✅ Ready to go
