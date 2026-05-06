# Hướng dẫn fix lỗi "Selected flight has already departed"

## Nguyên nhân
Lỗi này xảy ra khi:
1. Dữ liệu test trong database đã cũ (các chuyến bay được tạo từ lâu)
2. Thời gian khởi hành của chuyến bay đã qua so với thời gian hiện tại

## Giải pháp

### Cách 1: Sử dụng API endpoint (Khuyến nghị - Nhanh nhất)

1. **Kiểm tra các chuyến bay đã qua:**
```bash
curl http://localhost:5042/api/test-data/check-past-flights
```

2. **Cập nhật tự động các chuyến bay về tương lai:**
```bash
curl -X POST http://localhost:5042/api/test-data/refresh-flight-dates
```

Endpoint này sẽ:
- Tìm tất cả chuyến bay có `DepartureTime` trong quá khứ
- Cập nhật chúng sang ngày mai, giữ nguyên giờ trong ngày
- Tự động tính lại `ArrivalTime` dựa trên duration

### Cách 2: Chạy SQL Script

Kết nối vào PostgreSQL và chạy file `update_flight_dates.sql`:

```bash
psql -h localhost -p 5432 -U admin -d FlightBookingDB -f update_flight_dates.sql
```

Hoặc sử dụng pgAdmin/DBeaver để chạy script.

### Cách 3: Reset toàn bộ database (Mất dữ liệu)

**Cảnh báo:** Cách này sẽ xóa toàn bộ dữ liệu!

```bash
cd backend/API
dotnet ef database drop --force
dotnet ef database update
```

## Thay đổi đã thực hiện

### 1. BookingService.cs
Đã thay đổi validation từ:
```csharp
if (outboundFlight.DepartureTime <= DateTime.UtcNow)
{
    throw new ValidationException("Selected flight has already departed");
}
```

Thành:
```csharp
// Allow booking up to 2 hours before departure
var minimumBookingTime = DateTime.UtcNow.AddHours(2);
if (outboundFlight.DepartureTime <= minimumBookingTime)
{
    throw new ValidationException("Cannot book flights departing within 2 hours");
}
```

**Lợi ích:** 
- Thực tế hơn (không cho phép đặt vé quá gần giờ bay)
- Tránh lỗi do chênh lệch múi giờ nhỏ

### 2. TestDataController.cs (Mới)
Thêm 2 endpoints để quản lý dữ liệu test:
- `GET /api/test-data/check-past-flights` - Kiểm tra chuyến bay đã qua
- `POST /api/test-data/refresh-flight-dates` - Cập nhật chuyến bay về tương lai

### 3. RoutesDebugController.cs
Fix lỗi compile do sai tên properties:
- `Distance` → `DistanceKm`
- `EstimatedDuration` → `EstimatedDurationMinutes`

## Kiểm tra sau khi fix

1. Gọi API refresh:
```bash
curl -X POST http://localhost:5042/api/test-data/refresh-flight-dates
```

2. Thử tạo booking lại từ frontend

3. Kiểm tra log để đảm bảo không còn lỗi

## Lưu ý cho Production

- **Xóa hoặc bảo vệ** `TestDataController` trước khi deploy production
- Thêm `[Authorize(Roles = "Admin")]` nếu muốn giữ lại
- Cân nhắc thêm validation múi giờ nếu hệ thống phục vụ nhiều quốc gia
