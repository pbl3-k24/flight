# 🚀 Flight Template API - Complete Guide

## Tổng quan

Hệ thống Flight Template gồm **2 bước**:

1. **Tạo Template** (lưu vào DB để tái sử dụng) → API CRUD Template
2. **Generate Flights** (dùng template để tạo chuyến bay thực tế) → API Generate

---

## 📋 BƯỚC 1: CRUD Template (Quản lý mẫu lịch bay)

### 1.1. Tạo Template Mới

**Endpoint**: `POST /api/v1/admin/flight-templates`

**Mục đích**: Lưu mẫu lịch bay vào database để tái sử dụng

**Request**:
```json
{
  "name": "Lịch bay mùa hè 2026",
  "description": "Lịch bay HAN-SGN mùa hè",
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
    },
    {
      "dayOfWeek": 1,
      "departureTime": "14:00:00",
      "arrivalTime": "16:30:00",
      "flightNumberPrefix": "VN",
      "flightNumberSuffix": "202",
      "routeId": 1,
      "aircraftId": 2
    }
  ]
}
```

**Response**: `201 Created`
```json
{
  "id": 1,
  "name": "Lịch bay mùa hè 2026",
  "description": "Lịch bay HAN-SGN mùa hè",
  "isActive": true,
  "createdAt": "2026-05-03T10:00:00Z",
  "updatedAt": "2026-05-03T10:00:00Z",
  "details": [...]
}
```

---

### 1.2. Lấy Tất Cả Templates

**Endpoint**: `GET /api/v1/admin/flight-templates`

**Response**:
```json
[
  {
    "id": 1,
    "name": "Lịch bay mùa hè 2026",
    "isActive": true,
    "details": [...]
  }
]
```

---

### 1.3. Lấy Template Theo ID

**Endpoint**: `GET /api/v1/admin/flight-templates/{id}`

---

### 1.4. Cập Nhật Template

**Endpoint**: `PUT /api/v1/admin/flight-templates/{id}`

---

### 1.5. Xóa Template

**Endpoint**: `DELETE /api/v1/admin/flight-templates/{id}`

---

## 🚀 BƯỚC 2: Generate Flights từ Template

**Endpoint**: `POST /api/v1/admin/flight-templates/generate`

**Mục đích**: Dùng template đã lưu để tạo chuyến bay thực tế trong database

**Request**:
```json
{
  "templateId": 1,
  "weekStartDate": "2026-05-04",
  "numberOfWeeks": 4
}
```

**Giải thích**:
- `templateId`: ID của template đã tạo ở bước 1
- `weekStartDate`: Ngày bắt đầu tuần đầu tiên (nên chọn Thứ 2)
- `numberOfWeeks`: Số tuần muốn generate (1-52)

**Response**:
```json
{
  "totalFlightsGenerated": 8,
  "totalFlightsSkipped": 0,
  "warnings": [],
  "errors": []
}
```

---

## 📝 Workflow Hoàn Chỉnh

### Ví dụ: Tạo lịch bay HAN-SGN cho 4 tuần

**Bước 1: Tạo Template**
```bash
POST /api/v1/admin/flight-templates
{
  "name": "Lịch HAN-SGN T2-T6",
  "isActive": true,
  "details": [
    {
      "dayOfWeek": 1,
      "departureTime": "08:00:00",
      "arrivalTime": "10:15:00",
      "flightNumberPrefix": "VN",
      "flightNumberSuffix": "201",
      "routeId": 1,
      "aircraftId": 1
    },
    {
      "dayOfWeek": 2,
      "departureTime": "08:00:00",
      "arrivalTime": "10:15:00",
      "flightNumberPrefix": "VN",
      "flightNumberSuffix": "201",
      "routeId": 1,
      "aircraftId": 1
    }
  ]
}
```

→ Nhận được `templateId: 1`

**Bước 2: Generate Flights**
```bash
POST /api/v1/admin/flight-templates/generate
{
  "templateId": 1,
  "weekStartDate": "2026-05-04",
  "numberOfWeeks": 4
}
```

→ Tạo 8 chuyến bay (2 chuyến/tuần × 4 tuần)

---

## 🆚 So sánh với API Weekly Schedule

| Feature | Flight Template API | Weekly Schedule API |
|---------|-------------------|-------------------|
| **Endpoint** | `/api/v1/admin/flight-templates` | `/api/v1/admin/FlightsAdmin/weekly-schedule` |
| **Lưu template** | ✅ Có (lưu vào DB) | ❌ Không |
| **Tái sử dụng** | ✅ Có (dùng lại nhiều lần) | ❌ Không |
| **CRUD** | ✅ Có (Get/Create/Update/Delete) | ❌ Không |
| **Generate** | ✅ Có (từ template đã lưu) | ✅ Có (trực tiếp) |
| **Use case** | Lịch bay cố định, tái sử dụng | Tạo nhanh 1 lần |

**Kết luận**: 
- Dùng **Flight Template API** khi cần lưu và tái sử dụng mẫu lịch bay
- Dùng **Weekly Schedule API** khi chỉ cần tạo chuyến bay 1 lần không cần lưu template

---

## 📊 Các trường dữ liệu

### Template Level
- `name` (string, required): Tên template
- `description` (string, optional): Mô tả
- `isActive` (bool): Template có active không

### Detail Level
- `dayOfWeek` (int, 0-6): 0=CN, 1=T2, ..., 6=T7
- `departureTime` (string, HH:mm:ss): Giờ khởi hành
- `arrivalTime` (string, HH:mm:ss): Giờ đến
- `flightNumberPrefix` (string): Tiền tố số hiệu (VD: "VN")
- `flightNumberSuffix` (string): Hậu tố số hiệu (VD: "201")
- `routeId` (int): ID tuyến bay
- `aircraftId` (int): ID máy bay

---

## ⚠️ Lưu ý quan trọng

### Overnight Flights
Nếu `arrivalTime < departureTime` → tự động +1 ngày

### Duplicate Check
- ✅ Kiểm tra trùng flight number trong cùng ngày
- ✅ Kiểm tra xung đột máy bay (1 máy bay không thể bay 2 chuyến cùng lúc)

### Seat Inventory
Hệ thống tự động tạo seat inventory cho mỗi chuyến bay dựa trên `AircraftSeatTemplate`

---

## 🔧 Migration

Cần chạy migration để tạo 2 bảng mới:
- `FlightScheduleTemplates`
- `FlightTemplateDetails`

```bash
dotnet ef migrations add AddFlightTemplates
dotnet ef database update
```

---

## API Endpoint

### 🚀 Create Weekly Schedule (API TẠO TEMPLATE TUẦN)

**Endpoint**: `POST /api/v1/admin/FlightsAdmin/weekly-schedule`

**Authentication**: Yêu cầu role `Admin`

**Mô tả**: Tạo lịch bay tuần và tự động generate cho nhiều tuần tiếp theo

---

## Request Format

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
    },
    {
      "flightNumberPrefix": "VN",
      "routeId": 1,
      "aircraftId": 2,
      "dayOfWeek": 1,
      "departureTimeOfDay": "14:00:00",
      "arrivalTimeOfDay": "16:30:00",
      "isActive": true
    },
    {
      "flightNumberPrefix": "VN",
      "routeId": 2,
      "aircraftId": 1,
      "dayOfWeek": 2,
      "departureTimeOfDay": "09:00:00",
      "arrivalTimeOfDay": "11:30:00",
      "isActive": true
    }
  ]
}
```

---

## Giải thích các trường

### Root Level
- **`weekStartDate`** (string, required): Ngày bắt đầu tuần đầu tiên (format: `yyyy-MM-dd`)
  - Nên chọn ngày Thứ 2 để dễ quản lý
  - VD: `"2026-05-04"` (Thứ 2)

- **`autoGenerateWeeks`** (int, optional): Số tuần tự động generate thêm
  - Default: `12` tuần
  - Min: `1`, Max: `52`
  - VD: `12` → tạo 13 tuần (tuần gốc + 12 tuần tiếp theo)

- **`flights`** (array, required): Mảng các chuyến bay trong tuần mẫu

### Flight Pattern Object
- **`flightNumberPrefix`** (string, required): Tiền tố số hiệu chuyến bay
  - VD: `"VN"`, `"VJ"`, `"QH"`
  
- **`flightNumber`** (string, optional): Số hiệu chuyến bay đầy đủ (nếu muốn custom)
  - Nếu không có, hệ thống dùng `flightNumberPrefix`

- **`routeId`** (int, required): ID của route (tuyến bay)
  - Phải tồn tại trong database

- **`aircraftId`** (int, required): ID của máy bay
  - Phải tồn tại trong database

- **`dayOfWeek`** (int, required): Thứ trong tuần
  - `0` = Chủ nhật (Sunday)
  - `1` = Thứ 2 (Monday)
  - `2` = Thứ 3 (Tuesday)
  - `3` = Thứ 4 (Wednesday)
  - `4` = Thứ 5 (Thursday)
  - `5` = Thứ 6 (Friday)
  - `6` = Thứ 7 (Saturday)

- **`departureTimeOfDay`** (string, required): Giờ khởi hành (format: `HH:mm:ss`)
  - VD: `"08:00:00"`, `"14:30:00"`

- **`arrivalTimeOfDay`** (string, required): Giờ đến (format: `HH:mm:ss`)
  - VD: `"10:30:00"`, `"16:45:00"`
  - ⚠️ Nếu `arrivalTime < departureTime` → hệ thống tự động cộng 1 ngày (chuyến bay qua đêm)

- **`isActive`** (bool, optional): Trạng thái active
  - Default: `true`

---

## Response Format

```json
[
  {
    "flightId": 1,
    "flightNumber": "VN",
    "routeCode": "HAN-SGN",
    "aircraftModel": "Boeing 787",
    "departureTime": "2026-05-05T08:00:00Z",
    "arrivalTime": "2026-05-05T10:30:00Z",
    "totalSeats": 250,
    "availableSeats": 250,
    "bookedSeats": 0,
    "isActive": true,
    "createdAt": "2026-05-03T10:00:00Z"
  },
  {
    "flightId": 2,
    "flightNumber": "VN",
    "routeCode": "HAN-SGN",
    "aircraftModel": "Airbus A321",
    "departureTime": "2026-05-05T14:00:00Z",
    "arrivalTime": "2026-05-05T16:30:00Z",
    "totalSeats": 200,
    "availableSeats": 200,
    "bookedSeats": 0,
    "isActive": true,
    "createdAt": "2026-05-03T10:00:00Z"
  }
]
```

---

## Ví dụ sử dụng

### Ví dụ 1: Tạo lịch bay HAN-SGN cho 4 tuần

```bash
POST /api/v1/admin/FlightsAdmin/weekly-schedule
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "weekStartDate": "2026-05-04",
  "autoGenerateWeeks": 3,
  "flights": [
    {
      "flightNumberPrefix": "VN",
      "routeId": 1,
      "aircraftId": 1,
      "dayOfWeek": 1,
      "departureTimeOfDay": "06:00:00",
      "arrivalTimeOfDay": "08:15:00",
      "isActive": true
    },
    {
      "flightNumberPrefix": "VN",
      "routeId": 1,
      "aircraftId": 2,
      "dayOfWeek": 1,
      "departureTimeOfDay": "12:00:00",
      "arrivalTimeOfDay": "14:15:00",
      "isActive": true
    },
    {
      "flightNumberPrefix": "VN",
      "routeId": 1,
      "aircraftId": 3,
      "dayOfWeek": 1,
      "departureTimeOfDay": "18:00:00",
      "arrivalTimeOfDay": "20:15:00",
      "isActive": true
    }
  ]
}
```

**Kết quả**: Tạo 12 chuyến bay (3 chuyến/tuần × 4 tuần)

---

### Ví dụ 2: Lịch bay cả tuần (Thứ 2 - Chủ nhật)

```json
{
  "weekStartDate": "2026-05-04",
  "autoGenerateWeeks": 12,
  "flights": [
    {
      "flightNumberPrefix": "VN201",
      "routeId": 1,
      "aircraftId": 1,
      "dayOfWeek": 1,
      "departureTimeOfDay": "08:00:00",
      "arrivalTimeOfDay": "10:15:00",
      "isActive": true
    },
    {
      "flightNumberPrefix": "VN202",
      "routeId": 1,
      "aircraftId": 1,
      "dayOfWeek": 2,
      "departureTimeOfDay": "08:00:00",
      "arrivalTimeOfDay": "10:15:00",
      "isActive": true
    },
    {
      "flightNumberPrefix": "VN203",
      "routeId": 1,
      "aircraftId": 1,
      "dayOfWeek": 3,
      "departureTimeOfDay": "08:00:00",
      "arrivalTimeOfDay": "10:15:00",
      "isActive": true
    },
    {
      "flightNumberPrefix": "VN204",
      "routeId": 1,
      "aircraftId": 1,
      "dayOfWeek": 4,
      "departureTimeOfDay": "08:00:00",
      "arrivalTimeOfDay": "10:15:00",
      "isActive": true
    },
    {
      "flightNumberPrefix": "VN205",
      "routeId": 1,
      "aircraftId": 1,
      "dayOfWeek": 5,
      "departureTimeOfDay": "08:00:00",
      "arrivalTimeOfDay": "10:15:00",
      "isActive": true
    },
    {
      "flightNumberPrefix": "VN206",
      "routeId": 1,
      "aircraftId": 1,
      "dayOfWeek": 6,
      "departureTimeOfDay": "08:00:00",
      "arrivalTimeOfDay": "10:15:00",
      "isActive": true
    },
    {
      "flightNumberPrefix": "VN207",
      "routeId": 1,
      "aircraftId": 1,
      "dayOfWeek": 0,
      "departureTimeOfDay": "08:00:00",
      "arrivalTimeOfDay": "10:15:00",
      "isActive": true
    }
  ]
}
```

**Kết quả**: Tạo 91 chuyến bay (7 chuyến/tuần × 13 tuần)

---

### Ví dụ 3: Chuyến bay qua đêm

```json
{
  "weekStartDate": "2026-05-04",
  "autoGenerateWeeks": 4,
  "flights": [
    {
      "flightNumberPrefix": "VN",
      "routeId": 5,
      "aircraftId": 2,
      "dayOfWeek": 1,
      "departureTimeOfDay": "23:00:00",
      "arrivalTimeOfDay": "01:00:00",
      "isActive": true
    }
  ]
}
```

**Giải thích**: 
- Bay 23:00 Thứ 2 → Đến 01:00 Thứ 3 (hôm sau)
- Hệ thống tự động xử lý overnight flight

---

## Lưu ý quan trọng

### ⚠️ Xử lý chuyến bay qua đêm
Nếu `arrivalTimeOfDay < departureTimeOfDay`, hệ thống tự động cộng thêm 1 ngày cho arrival time.

**Code logic**:
```csharp
if (arrivalTime <= departureTime)
{
    arrivalTime = arrivalTime.AddDays(1);
}
```

### ⚠️ Kiểm tra trùng lặp
Hệ thống kiểm tra trùng lặp bằng method:
```csharp
await _flightRepository.ExistsAsync(flightNumber, departureTime, routeId, aircraftId)
```

Nếu chuyến bay đã tồn tại → **Skip** (không tạo duplicate)

### ⚠️ Tính toán số chuyến bay
- **Tuần gốc**: 1 tuần
- **Auto generate**: `autoGenerateWeeks` tuần
- **Tổng**: `1 + autoGenerateWeeks` tuần

**Ví dụ**:
- `autoGenerateWeeks: 12` → Tạo 13 tuần
- 3 chuyến/tuần → 3 × 13 = **39 chuyến bay**

---

## Error Responses

### 400 Bad Request - Validation Error
```json
{
  "message": "AutoGenerateWeeks must be between 1 and 52"
}
```

```json
{
  "message": "Schedule template must contain at least one flight"
}
```

```json
{
  "message": "DayOfWeek must be between 0 and 6"
}
```

### 404 Not Found - Resource Not Found
```json
{
  "message": "Route not found: 999"
}
```

```json
{
  "message": "Aircraft not found: 999"
}
```

### 500 Internal Server Error
```json
{
  "message": "Error creating weekly schedule"
}
```

---

## So sánh với yêu cầu ban đầu

### ✅ Đã có sẵn
- ✅ Tạo lịch bay tuần từ template
- ✅ Auto-generate nhiều tuần
- ✅ Xử lý overnight flights
- ✅ Kiểm tra duplicate flights
- ✅ Hỗ trợ nhiều chuyến bay/ngày

### ❌ Chưa có (nếu cần)
- ❌ Lưu template vào database (hiện tại chỉ generate trực tiếp)
- ❌ CRUD operations cho template (Get/Update/Delete template)
- ❌ Kiểm tra aircraft conflict (máy bay bay 2 chuyến cùng lúc)

**Kết luận**: API hiện tại đủ để tạo lịch bay tuần. Nếu cần lưu template để tái sử dụng, có thể mở rộng sau.

