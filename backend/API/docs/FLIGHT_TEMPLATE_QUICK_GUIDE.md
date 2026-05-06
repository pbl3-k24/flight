# 🚀 Quick Guide: Flight Template System

## 2 Bước Sử Dụng

### BƯỚC 1: Tạo Template (Lưu vào DB)

**API**: `POST /api/v1/admin/flight-templates`

```json
{
  "name": "Lịch bay mùa hè",
  "isActive": true,
  "details": [
    {
      "dayOfWeek": 1,
      "departureTime": "08:00:00",
      "arrivalTime": "10:30:00",
      "flightNumberPrefix": "VN",
      "flightNumberSuffix": "201",
      "routeId": 1,
      "aircraftId": 1
    }
  ]
}
```

→ Nhận `templateId: 1`

---

### BƯỚC 2: Generate Flights từ Template

**API**: `POST /api/v1/admin/flight-templates/generate`

```json
{
  "templateId": 1,
  "weekStartDate": "2026-05-04",
  "numberOfWeeks": 4
}
```

→ Tạo chuyến bay thực tế trong DB

---

## API Endpoints

| Method | Endpoint | Mục đích |
|--------|----------|----------|
| POST | `/api/v1/admin/flight-templates` | Tạo template mới |
| GET | `/api/v1/admin/flight-templates` | Lấy tất cả templates |
| GET | `/api/v1/admin/flight-templates/{id}` | Lấy template theo ID |
| PUT | `/api/v1/admin/flight-templates/{id}` | Cập nhật template |
| DELETE | `/api/v1/admin/flight-templates/{id}` | Xóa template |
| POST | `/api/v1/admin/flight-templates/generate` | **Generate flights** |

---

## So sánh 2 API

### Flight Template API (MỚI)
- ✅ Lưu template vào DB
- ✅ Tái sử dụng nhiều lần
- ✅ CRUD đầy đủ
- 🎯 **Use case**: Lịch bay cố định, dùng lại nhiều lần

### Weekly Schedule API (CŨ)
- ❌ Không lưu template
- ❌ Tạo 1 lần rồi thôi
- 🎯 **Use case**: Tạo nhanh không cần lưu

---

## Migration

```bash
# Chạy migration SQL
psql -U postgres -d flight_booking -f docs/FLIGHT_TEMPLATE_MIGRATION.sql

# Hoặc dùng EF Core
dotnet ef migrations add AddFlightTemplates
dotnet ef database update
```

---

## Xem thêm

📄 Chi tiết: `docs/FLIGHT_TEMPLATE_API.md`

---

## Request Mẫu

```json
{
  "weekStartDate": "2026-05-04",
  "autoGenerateWeeks": 12,
  "flights": [
    {
      "flightNumberPrefix": "VN",
      "routeId": 1,
      "aircraftId": 1,
      "dayOfWeek": 1,
      "departureTimeOfDay": "08:00:00",
      "arrivalTimeOfDay": "10:30:00",
      "isActive": true
    }
  ]
}
```

---

## Các trường quan trọng

| Trường | Giá trị | Mô tả |
|--------|---------|-------|
| `weekStartDate` | `"2026-05-04"` | Ngày bắt đầu (nên chọn Thứ 2) |
| `autoGenerateWeeks` | `12` | Số tuần tự động tạo thêm (1-52) |
| `dayOfWeek` | `0-6` | 0=CN, 1=T2, 2=T3, 3=T4, 4=T5, 5=T6, 6=T7 |
| `departureTimeOfDay` | `"08:00:00"` | Giờ khởi hành (HH:mm:ss) |
| `arrivalTimeOfDay` | `"10:30:00"` | Giờ đến (HH:mm:ss) |

---

## Ví dụ: Tạo lịch bay 4 tuần

```bash
curl -X POST "http://localhost:5042/api/v1/admin/FlightsAdmin/weekly-schedule" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "weekStartDate": "2026-05-04",
    "autoGenerateWeeks": 3,
    "flights": [
      {
        "flightNumberPrefix": "VN",
        "routeId": 1,
        "aircraftId": 1,
        "dayOfWeek": 1,
        "departureTimeOfDay": "08:00:00",
        "arrivalTimeOfDay": "10:30:00",
        "isActive": true
      }
    ]
  }'
```

**Kết quả**: Tạo 4 chuyến bay (1 chuyến/tuần × 4 tuần)

---

## Lưu ý

✅ **Overnight flights**: Nếu `arrivalTime < departureTime` → tự động +1 ngày

✅ **Duplicate check**: Hệ thống tự động skip chuyến bay trùng

✅ **Tổng tuần**: `1 + autoGenerateWeeks` (VD: `autoGenerateWeeks: 12` → 13 tuần)

---

## Xem thêm

Chi tiết đầy đủ: `docs/FLIGHT_TEMPLATE_API.md`
