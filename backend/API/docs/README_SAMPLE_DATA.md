# 🎯 SAMPLE DATA FOR TESTING - README

## 📌 Mục đích

Cung cấp **30+ chuyến bay**, **8 mã khuyến mại**, và **60+ hàng ghế** tự động để test tính năng tìm kiếm.

---

## 🚀 Quick Start (3 dòng)

```bash
cd E:\pbl3\flight\backend
dotnet run --project API/API.csproj
# Xong! Data được thêm tự động 🎉
```

---

## 📖 Tài Liệu

| File | Dùng cho |
|------|----------|
| **SAMPLE_DATA_QUICK_REFERENCE.md** | 👈 **Bắt đầu ở đây!** (3 bước) |
| **SAMPLE_DATA_USAGE_GUIDE.md** | Chi tiết đầy đủ (test cases) |
| **TEST_SAMPLES.ps1** | PowerShell script (tương tác) |
| **CURL_TEST_SAMPLES.sh** | Bash script |
| **Flight_Booking_Postman_Collection.json** | Postman import |
| **SAMPLE_DATA_FOR_TESTING.sql** | SQL direct insert |

---

## ✅ Dữ liệu được thêm

```
✅ 6 Sân bay (SGN, HAN, DAD, CTS, VCA, HUI)
✅ 7+ Tuyến bay (khác nhau)
✅ 5 Máy bay (Boeing, Airbus)
✅ 30+ Chuyến bay (7 ngày, 8 chuyến/ngày)
✅ 60+ Hàng ghế (Eco, Business, Premium)
✅ 8 Mã khuyến mại (active, expired)
✅ 3 Tài khoản test (admin, user1, user2)
```

---

## 🧪 Test luôn ngay

### Cách 1: PowerShell (Tương tác)
```powershell
.\API\docs\TEST_SAMPLES.ps1
```

### Cách 2: Postman
Import: `API/docs/Flight_Booking_Postman_Collection.json`

### Cách 3: cURL Manual
```bash
curl http://localhost:5042/api/v1/flights/search?departureId=1&arrivalId=2&date=2025-01-22
```

---

## 🔐 Tài khoản Test

```
Admin:  admin@flightbooking.vn / Admin@123456
User1:  user1@gmail.com / User1@123456
User2:  user2@gmail.com / User2@123456
```

---

## 💡 Một số mã khuyến mại

```
✅ SUMMER20         - Giảm 20%
✅ SAVE500K         - Giảm 500K cố định
✅ EARLYBIRD15      - Giảm 15%
✅ NEWYEAR2025      - Giảm 30%
❌ EXPIRED2024      - Đã hết hạn
❌ VIP10            - Đã full quota
```

---

## 📊 Test Cases Sẵn Sàng

| Test | File |
|------|------|
| Search flights (HCM→HN) | ✅ |
| Pagination (page 1, 2) | ✅ |
| Different routes | ✅ |
| Valid promo codes | ✅ |
| Expired codes | ✅ |
| Invalid codes | ✅ |
| Admin endpoints | ✅ |
| Flight with seats | ✅ |

---

## ❓ FAQ

**Q: Data tự động được add bao giờ?**  
A: Lần đầu tiên khi chạy app (development mode)

**Q: Lần 2 chạy có bị duplicate không?**  
A: Không, code check `if (existingFlights < 30)`

**Q: Ở đâu để xem data được add?**  
A: Logs hiển thị "✅ Sample search data added successfully!"

**Q: Cần làm gì để add thêm dữ liệu?**  
A: Edit `API/Infrastructure/Data/SampleDataForSearching.cs`

---

## 🎯 Đọc Tiếp

1. **Nhanh (3 min)**: SAMPLE_DATA_QUICK_REFERENCE.md
2. **Chi tiết (15 min)**: SAMPLE_DATA_USAGE_GUIDE.md  
3. **Code (1 min)**: API/Infrastructure/Data/SampleDataForSearching.cs

---

**Status: ✅ Ready to Use**

🚀 Happy Testing!
