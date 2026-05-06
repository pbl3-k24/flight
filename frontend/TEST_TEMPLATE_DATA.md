# Test Template Data Format

## ✅ Dữ liệu gửi đi ĐÚNG format API yêu cầu

### Format API yêu cầu:
```json
{
  "name": "string",
  "description": "string",
  "isActive": true,
  "details": [
    {
      "routeId": 0,
      "aircraftId": 0,
      "dayOfWeek": 0,
      "departureTime": "string",
      "arrivalTime": "string",
      "flightNumberPrefix": "string",
      "flightNumberSuffix": "string"
    }
  ]
}
```

### Dữ liệu thực tế gửi đi:
```javascript
{
  name: "Template Tuần Thường",           // ✅ string
  description: "Lịch bay các ngày thường", // ✅ string
  isActive: true,                          // ✅ boolean
  details: [                               // ✅ array
    {
      routeId: 5,                          // ✅ number (từ flightDefinition.routeId)
      aircraftId: 2,                       // ✅ number (từ flightDefinition.defaultAircraftId)
      dayOfWeek: 0,                        // ✅ number (0=Thứ 2, 6=Chủ nhật)
      departureTime: "08:00:00",           // ✅ string (HH:mm:ss)
      arrivalTime: "10:00:00",             // ✅ string (HH:mm:ss)
      flightNumberPrefix: "VN",            // ✅ string (extracted từ flightNumber)
      flightNumberSuffix: ""               // ✅ string (empty)
    }
  ]
}
```

## 🔍 Chi tiết mapping

### 1. Template Level
| Field | Type | Source | Example |
|-------|------|--------|---------|
| `name` | string | `templateFormData.name` | "Template Tuần Thường" |
| `description` | string | `templateFormData.description` | "Lịch bay các ngày thường" |
| `isActive` | boolean | `templateFormData.isActive` | true |
| `details` | array | `templateSlots.map(...)` | [...] |

### 2. Detail Level
| Field | Type | Source | Example | Note |
|-------|------|--------|---------|------|
| `routeId` | number | `slot.flightDefinition.routeId` | 5 | ID của route |
| `aircraftId` | number | `slot.flightDefinition.defaultAircraftId` | 2 | ID của aircraft |
| `dayOfWeek` | number | `slot.dayOfWeek` | 0 | 0=Thứ 2, 6=CN |
| `departureTime` | string | `slot.flightDefinition.departureTime` | "08:00:00" | Format HH:mm:ss |
| `arrivalTime` | string | `slot.flightDefinition.arrivalTime` | "10:00:00" | Format HH:mm:ss |
| `flightNumberPrefix` | string | Extracted từ `flightNumber` | "VN" | Regex: /^([A-Z]+)/ |
| `flightNumberSuffix` | string | Empty string | "" | Luôn là "" |

## 🎯 Logic xử lý

### 1. Extract Flight Number Prefix
```javascript
let prefix = 'FL'  // Default
if (slot.flightDefinition.flightNumber) {
  const match = slot.flightDefinition.flightNumber.match(/^([A-Z]+)/)
  if (match) {
    prefix = match[1]  // VD: "VN" từ "VN123"
  }
}
```

**Ví dụ:**
- `flightNumber: "VN123"` → `prefix: "VN"`
- `flightNumber: "VJ456"` → `prefix: "VJ"`
- `flightNumber: "QH789"` → `prefix: "QH"`
- `flightNumber: null` → `prefix: "FL"` (default)

### 2. Format Time
```javascript
const formatTime = (time) => {
  if (!time) return '08:00:00'
  // Nếu có format HH:mm:ss.sssssss, chỉ lấy HH:mm:ss
  return time.substring(0, 8)
}
```

**Ví dụ:**
- `"08:00:00"` → `"08:00:00"` ✅
- `"08:00:00.0000000"` → `"08:00:00"` ✅
- `null` → `"08:00:00"` ✅ (default)

### 3. Day of Week Mapping
```javascript
0 = Thứ 2 (Monday)
1 = Thứ 3 (Tuesday)
2 = Thứ 4 (Wednesday)
3 = Thứ 5 (Thursday)
4 = Thứ 6 (Friday)
5 = Thứ 7 (Saturday)
6 = Chủ nhật (Sunday)
```

## 📋 Ví dụ đầy đủ

### Input (từ UI):
```javascript
templateFormData = {
  name: "Template Tuần Thường",
  description: "Lịch bay cho các ngày thường",
  isActive: true
}

templateSlots = [
  {
    id: "1-0-1234567890",
    dayOfWeek: 0,  // Thứ 2
    flightDefinition: {
      id: 1,
      flightNumber: "VN101",
      routeId: 5,
      defaultAircraftId: 2,
      departureTime: "08:00:00",
      arrivalTime: "10:00:00",
      departureAirportCode: "SGN",
      arrivalAirportCode: "HAN"
    }
  },
  {
    id: "2-1-1234567891",
    dayOfWeek: 1,  // Thứ 3
    flightDefinition: {
      id: 2,
      flightNumber: "VN102",
      routeId: 6,
      defaultAircraftId: 2,
      departureTime: "14:00:00",
      arrivalTime: "16:00:00",
      departureAirportCode: "HAN",
      arrivalAirportCode: "SGN"
    }
  }
]
```

### Output (gửi đến API):
```json
{
  "name": "Template Tuần Thường",
  "description": "Lịch bay cho các ngày thường",
  "isActive": true,
  "details": [
    {
      "routeId": 5,
      "aircraftId": 2,
      "dayOfWeek": 0,
      "departureTime": "08:00:00",
      "arrivalTime": "10:00:00",
      "flightNumberPrefix": "VN",
      "flightNumberSuffix": ""
    },
    {
      "routeId": 6,
      "aircraftId": 2,
      "dayOfWeek": 1,
      "departureTime": "14:00:00",
      "arrivalTime": "16:00:00",
      "flightNumberPrefix": "VN",
      "flightNumberSuffix": ""
    }
  ]
}
```

## ✅ Validation

### Frontend Validation:
1. ✅ `name` không được rỗng
2. ✅ `details` phải có ít nhất 1 item
3. ✅ Không cho phép trùng flight trong cùng một thứ

### Backend Validation (expected):
1. ✅ `routeId` phải tồn tại trong database
2. ✅ `aircraftId` phải tồn tại trong database
3. ✅ `dayOfWeek` phải trong khoảng 0-6
4. ✅ `departureTime` và `arrivalTime` phải có format HH:mm:ss
5. ✅ `flightNumberPrefix` không được null/empty

## 🔧 Debug

### Console Log
Khi tạo template, check console để xem dữ liệu gửi đi:
```javascript
console.log('📤 Creating template:', templateData)
```

### Expected Output:
```
📤 Creating template: {
  name: "Template Tuần Thường",
  description: "Lịch bay cho các ngày thường",
  isActive: true,
  details: [
    {
      routeId: 5,
      aircraftId: 2,
      dayOfWeek: 0,
      departureTime: "08:00:00",
      arrivalTime: "10:00:00",
      flightNumberPrefix: "VN",
      flightNumberSuffix: ""
    }
  ]
}
```

## 🎯 Kết luận

**Dữ liệu gửi đi HOÀN TOÀN ĐÚNG format API yêu cầu!**

Tất cả các field đều có:
- ✅ Đúng tên field
- ✅ Đúng kiểu dữ liệu
- ✅ Đúng format
- ✅ Đúng cấu trúc

Nếu API vẫn báo lỗi, có thể do:
1. Backend validation rules khác với documentation
2. Database constraints (foreign key, unique, etc.)
3. Authorization issues
4. Network/CORS issues

Hãy check console log và network tab để xem chi tiết lỗi từ backend.
