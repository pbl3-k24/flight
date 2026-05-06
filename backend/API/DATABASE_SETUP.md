# Database Setup Guide

## Tự động khởi tạo database (Khuyến nghị)

Ứng dụng sẽ **TỰ ĐỘNG** tạo và seed database khi chạy lần đầu tiên trên thiết bị mới.

### Bước 1: Cài đặt PostgreSQL

```bash
# Windows: Download từ https://www.postgresql.org/download/windows/
# hoặc dùng Docker:
docker run --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -d postgres
```

### Bước 2: Tạo database

```sql
CREATE DATABASE FlightBookingDB;
```

### Bước 3: Cấu hình connection string

Sửa file `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FlightBookingDB;Username=postgres;Password=postgres"
  }
}
```

### Bước 4: Chạy ứng dụng

```bash
dotnet run
```

**Ứng dụng sẽ tự động:**
1. ✅ Apply tất cả EF migrations
2. ✅ Tạo master data (Roles, Airports, Aircraft, Routes, SeatClasses)
3. ✅ Tạo AircraftSeatTemplates
4. ✅ Tạo FlightDefinitions (20 flight patterns)
5. ✅ Generate Flights cho 30 ngày tới
6. ✅ Generate FlightSeatInventories
7. ✅ Tạo admin user: `admin@flightbooking.vn` / `Admin@123456`

## Kiểm tra kết quả

Sau khi chạy, kiểm tra database:

```sql
-- Kiểm tra số lượng records
SELECT 'Roles' as table_name, COUNT(*) as count FROM "Roles"
UNION ALL SELECT 'Airports', COUNT(*) FROM "Airports"
UNION ALL SELECT 'Aircraft', COUNT(*) FROM "Aircraft"
UNION ALL SELECT 'Routes', COUNT(*) FROM "Routes"
UNION ALL SELECT 'SeatClasses', COUNT(*) FROM "SeatClasses"
UNION ALL SELECT 'AircraftSeatTemplates', COUNT(*) FROM "AircraftSeatTemplates"
UNION ALL SELECT 'FlightDefinitions', COUNT(*) FROM "FlightDefinitions"
UNION ALL SELECT 'Flights', COUNT(*) FROM "Flights"
UNION ALL SELECT 'FlightSeatInventories', COUNT(*) FROM "FlightSeatInventories"
UNION ALL SELECT 'Users', COUNT(*) FROM "Users";
```

Kết quả mong đợi:
- Roles: 2
- Airports: 5
- Aircraft: 5
- Routes: 6
- SeatClasses: 3
- AircraftSeatTemplates: 13
- FlightDefinitions: 20
- Flights: ~600 (30 ngày × ~20 flights/ngày)
- FlightSeatInventories: ~1500 (flights × seat classes)
- Users: 1 (admin)

## Test API

```bash
# Login admin
curl -X POST http://localhost:5042/api/v1/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@flightbooking.vn","password":"Admin@123456"}'

# Search flights
curl -X POST http://localhost:5042/api/v1/flights/search \
  -H "Content-Type: application/json" \
  -d '{
    "departureAirportId": 1,
    "arrivalAirportId": 2,
    "departureDate": "2026-05-10T00:00:00Z",
    "passengerCount": 1,
    "seatPreference": 1
  }'
```

## Troubleshooting

### Lỗi: "Database already exists"
- Database đã có dữ liệu, ứng dụng sẽ bỏ qua seed
- Nếu muốn reset: Drop database và tạo lại

### Lỗi: "Connection refused"
- Kiểm tra PostgreSQL đang chạy
- Kiểm tra connection string trong appsettings.json

### Lỗi: "Migration failed"
- Xóa folder `Migrations/` và chạy lại:
  ```bash
  dotnet ef migrations add InitialCreate
  dotnet run
  ```

## Reset database (Development only)

```sql
-- Drop và tạo lại database
DROP DATABASE IF EXISTS FlightBookingDB;
CREATE DATABASE FlightBookingDB;
```

Sau đó chạy lại ứng dụng, nó sẽ tự động seed lại.

## Production deployment

Trên production, đảm bảo:
1. Connection string được cấu hình qua environment variables
2. Database backup được thiết lập
3. Migration được test trước khi deploy

```bash
# Set connection string via environment
export ConnectionStrings__DefaultConnection="Host=prod-db;Port=5432;Database=FlightBookingDB;Username=app_user;Password=secure_password"
dotnet run
```
