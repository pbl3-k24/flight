# 🛫 Seed Data - FlightDefinitions & Flights

Hệ thống tạo dữ liệu mẫu tự động cho Flight Booking System.

## 📋 Tổng quan

Hệ thống này tạo dữ liệu mẫu đầy đủ cho 30 ngày tới bao gồm:
- **FlightDefinitions**: ~20 định nghĩa chuyến bay
- **Flights**: ~600 chuyến bay thực tế
- **FlightSeatInventories**: ~1,800 records kho ghế

## 📁 Các file quan trọng

| File | Mô tả |
|------|-------|
| `seed_flight_definitions_and_flights.sql` | File SQL chính - tạo tất cả dữ liệu |
| `create_missing_tables.sql` | Tạo bảng FlightDefinitions nếu chưa có |
| `verify_seeded_data.sql` | Kiểm tra dữ liệu sau khi seed |
| `test_flights_api.http` | Test API endpoints |
| `SEED_FLIGHTS_GUIDE.txt` | Hướng dẫn chi tiết |

## 🚀 Cách sử dụng nhanh

### Bước 1: Tạo bảng (nếu chưa có)

```bash
docker cp create_missing_tables.sql pbl3-postgres:/tmp/
docker exec -i pbl3-postgres psql -U postgres -d flight_booking -f /tmp/create_missing_tables.sql
```

### Bước 2: Seed dữ liệu

```bash
docker cp seed_flight_definitions_and_flights.sql pbl3-postgres:/tmp/
docker exec -i pbl3-postgres psql -U postgres -d flight_booking -f /tmp/seed_flight_definitions_and_flights.sql
```

### Bước 3: Verify

```bash
docker cp verify_seeded_data.sql pbl3-postgres:/tmp/
docker exec -i pbl3-postgres psql -U postgres -d flight_booking -f /tmp/verify_seeded_data.sql
```

## ✈️ Dữ liệu được tạo

### Tuyến HAN → SGN (8 chuyến/ngày)
- **VN201** - 06:00 → 08:15 ⭐ Vietnam Airlines
- **VN203** - 09:00 → 11:15
- **VN205** - 12:00 → 14:15
- **VN207** - 15:00 → 17:15
- **VN209** - 18:00 → 20:15
- **VN211** - 21:00 → 23:15
- **VJ123** - 05:30 → 07:45 💰 VietJet (Budget)
- **VJ125** - 13:30 → 15:45 💰 VietJet (Budget)

### Tuyến SGN → HAN (7 chuyến/ngày)
- **VN202** - 06:00 → 08:15
- **VN204** - 09:00 → 11:15
- **VN206** - 12:00 → 14:15
- **VN208** - 15:00 → 17:15
- **VN210** - 18:00 → 20:15
- **VJ124** - 05:30 → 07:45 💰 VietJet
- **VJ126** - 13:30 → 15:45 💰 VietJet

### Tuyến HAN ⇄ DAD (2 chuyến/ngày mỗi chiều)
- **VN301** - 07:00 → 08:20 (Thứ 2-6)
- **VN303** - 14:00 → 15:20 (Hàng ngày)
- **VN302** - 09:00 → 10:20 (Thứ 2-6)
- **VN304** - 16:00 → 17:20 (Hàng ngày)

### Chuyến bay đặc biệt
- **VN999** - 23:30 → 01:45+1 🌙 (Red-eye flight - qua đêm)

## 📊 Thống kê

```
FlightDefinitions:      ~20 records
Flights:                ~600 records (30 days × ~20 flights/day)
FlightSeatInventories:  ~1,800 records (600 flights × 3 seat classes)
```

## 🔍 Kiểm tra nhanh

### Xem flights hôm nay
```sql
SELECT 
    fd."FlightNumber",
    f."DepartureTime",
    f."ArrivalTime"
FROM "Flights" f
INNER JOIN "FlightDefinitions" fd ON f."FlightDefinitionId" = fd."Id"
WHERE DATE(f."DepartureTime") = CURRENT_DATE
ORDER BY f."DepartureTime";
```

### Đếm số flights
```sql
SELECT COUNT(*) FROM "Flights" WHERE "FlightDefinitionId" IS NOT NULL;
```

## 🧪 Test API

Sử dụng file `test_flights_api.http` với REST Client extension trong VS Code:

```http
### Search flights HAN → SGN today
GET http://localhost:5042/api/v1/flights/search
    ?departureAirportCode=HAN
    &arrivalAirportCode=SGN
    &departureDate=2026-05-07
    &passengers=1
```

## 🔧 Troubleshooting

### ❌ Lỗi: "relation FlightDefinitions does not exist"
**Giải pháp:** Chạy `create_missing_tables.sql` trước

### ❌ Lỗi: "foreign key violation"
**Giải pháp:** Kiểm tra các bảng master data:
```sql
SELECT COUNT(*) FROM "Routes";      -- Phải > 0
SELECT COUNT(*) FROM "Aircraft";    -- Phải > 0
SELECT COUNT(*) FROM "SeatClasses"; -- Phải > 0
SELECT COUNT(*) FROM "AircraftSeatTemplates"; -- Phải > 0
```

### ❌ Không có flights được tạo
**Giải pháp:** Kiểm tra `AircraftSeatTemplates`:
```sql
SELECT * FROM "AircraftSeatTemplates" WHERE "IsDeleted" = FALSE;
```

## 📝 Operating Days

| Value | Days | Description |
|-------|------|-------------|
| 127 | Mon-Sun | Hàng ngày |
| 31 | Mon-Fri | Thứ 2-6 |
| 96 | Sat-Sun | Cuối tuần |

**Công thức:** Mon(1) + Tue(2) + Wed(4) + Thu(8) + Fri(16) + Sat(32) + Sun(64)

## 🔄 Chạy lại

Script an toàn để chạy nhiều lần. Nó sẽ:
1. Xóa dữ liệu cũ
2. Tạo dữ liệu mới
3. Không ảnh hưởng đến bookings hiện có

## 📞 Support

Nếu gặp vấn đề, kiểm tra:
1. ✅ Database connection
2. ✅ Master data (Routes, Aircraft, SeatClasses)
3. ✅ Log output từ script
4. ✅ File `SEED_FLIGHTS_GUIDE.txt` để biết chi tiết

---

**Tạo bởi:** Smart Schema Sync System  
**Phiên bản:** 1.0  
**Ngày:** 2026-05-07
