# Hướng dẫn sử dụng Flight Template Management

## 🎯 Tính năng

Hệ thống quản lý template chuyến bay cho phép admin:
1. **Tạo template theo thứ trong tuần** (Thứ 2 - Chủ nhật) - KHÔNG có ngày cụ thể
2. **Tự động sinh chuyến bay** từ template cho nhiều tuần

## 📋 Cách sử dụng

### Bước 1: Đăng nhập với tài khoản Admin

Đăng nhập với tài khoản có role `admin` để truy cập chức năng quản lý templates.

### Bước 2: Vào màn hình "Quản lý Templates"

Click vào button **"📋 Quản lý Templates"** trên thanh navigation.

### Bước 3: Tạo Template Mới

1. **Nhập thông tin template:**
   - Tên template (VD: "Template Tuần Thường")
   - Mô tả (VD: "Lịch bay cho các ngày thường")
   - Chọn "Kích hoạt template"

2. **Thêm chuyến bay vào template:**
   - Xem danh sách **Flight Definitions** bên trái
   - Chọn flight definition muốn thêm
   - Chọn thứ trong tuần từ dropdown (Thứ 2 - Chủ nhật)
   - Flight sẽ được thêm vào cột tương ứng bên phải

3. **Xem khung template:**
   - 7 cột đại diện cho 7 ngày trong tuần
   - Mỗi cột hiển thị các chuyến bay đã thêm
   - Có thể xóa chuyến bay bằng button "Xóa"

4. **Lưu template:**
   - Click button **"💾 Lưu Template"**
   - Template sẽ được lưu vào database

### Bước 4: Sinh Chuyến Bay từ Template

1. **Chọn template:**
   - Xem danh sách templates đã tạo
   - Click button **"🚀 Sinh chuyến bay"** trên template muốn sử dụng

2. **Cấu hình sinh chuyến bay:**
   - **Ngày bắt đầu tuần:** Chọn ngày Thứ 2 để bắt đầu
   - **Số tuần:** Nhập số tuần muốn sinh (1-52)
   - Hệ thống sẽ hiển thị tổng số chuyến bay sẽ được tạo

3. **Thực hiện sinh chuyến bay:**
   - Click button **"🚀 Sinh X Chuyến Bay"**
   - Hệ thống sẽ tự động tạo tất cả chuyến bay
   - Hiển thị kết quả: số chuyến đã tạo, số chuyến bỏ qua (nếu trùng)

## 🔍 Ví dụ thực tế

### Template "Tuần Thường"

**Cấu hình:**
- Thứ 2: VN101 (SGN → HAN, 08:00)
- Thứ 3: VN102 (HAN → SGN, 14:00)
- Thứ 4: VN103 (SGN → DAD, 09:00)
- Thứ 5: VN104 (DAD → SGN, 15:00)
- Thứ 6: VN105 (SGN → HAN, 08:00)
- Thứ 7: VN106 (HAN → SGN, 16:00)
- Chủ nhật: VN107 (SGN → DAD, 10:00)

**Sinh chuyến bay:**
- Ngày bắt đầu: 01/06/2024 (Thứ 2)
- Số tuần: 4
- **Kết quả:** 28 chuyến bay được tạo tự động (7 chuyến/tuần × 4 tuần)

## ⚠️ Lưu ý quan trọng

### 1. Template chỉ lưu thứ, không lưu ngày
- Template định nghĩa chuyến bay theo **thứ trong tuần**
- Ngày cụ thể chỉ được chọn khi **sinh chuyến bay**
- Một template có thể tái sử dụng cho nhiều kỳ khác nhau

### 2. Không cho phép trùng flight trong cùng một thứ
- Mỗi flight definition chỉ được thêm **1 lần** vào mỗi thứ
- Nếu thêm trùng, hệ thống sẽ báo lỗi

### 3. Ngày bắt đầu phải là Thứ 2
- Khi sinh chuyến bay, phải chọn ngày Thứ 2
- Hệ thống sẽ tự động tính các ngày tiếp theo trong tuần

### 4. Xử lý chuyến bay trùng lặp
- Nếu chuyến bay đã tồn tại, hệ thống sẽ bỏ qua
- Kết quả sẽ hiển thị số chuyến bỏ qua

## 🎨 Giao diện

### Khung Template (7 cột)
```
┌─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┐
│ Thứ 2   │ Thứ 3   │ Thứ 4   │ Thứ 5   │ Thứ 6   │ Thứ 7   │ CN      │
├─────────┼─────────┼─────────┼─────────┼─────────┼─────────┼─────────┤
│ VN101   │ VN102   │ VN103   │ VN104   │ VN105   │ VN106   │ VN107   │
│ SGN→HAN │ HAN→SGN │ SGN→DAD │ DAD→SGN │ SGN→HAN │ HAN→SGN │ SGN→DAD │
│ 08:00   │ 14:00   │ 09:00   │ 15:00   │ 08:00   │ 16:00   │ 10:00   │
│ [Xóa]   │ [Xóa]   │ [Xóa]   │ [Xóa]   │ [Xóa]   │ [Xóa]   │ [Xóa]   │
└─────────┴─────────┴─────────┴─────────┴─────────┴─────────┴─────────┘
```

### Form Sinh Chuyến Bay
```
┌────────────────────────────────────────────────────────────┐
│ 🚀 Sinh Chuyến Bay từ Template: Template Tuần Thường      │
├────────────────────────────────────────────────────────────┤
│ Template này sẽ tạo chuyến bay theo thứ:                  │
│ [Thứ 2: 1 chuyến] [Thứ 3: 1 chuyến] ... [CN: 1 chuyến]   │
│                                                            │
│ Ngày bắt đầu tuần: [01/06/2024]                           │
│ Số tuần: [4]                                               │
│ Tổng: 28 chuyến bay                                        │
│                                                            │
│ [🚀 Sinh 28 Chuyến Bay]                                    │
└────────────────────────────────────────────────────────────┘
```

## 🔧 API Endpoints

### 1. Lấy danh sách flight definitions
```
GET /api/v1/admin/flight-definitions?activeOnly=true
```

### 2. Lấy danh sách templates
```
GET /api/v1/admin/flight-templates
```

### 3. Tạo template mới
```
POST /api/v1/admin/flight-templates
Body: {
  name: string,
  description: string,
  isActive: boolean,
  details: [{
    routeId: number,
    aircraftId: number,
    dayOfWeek: number (0-6),
    departureTime: string (HH:mm:ss),
    arrivalTime: string (HH:mm:ss),
    flightNumberPrefix: string,
    flightNumberSuffix: string
  }]
}
```

### 4. Sinh chuyến bay từ template
```
POST /api/v1/admin/flight-templates/generate
Body: {
  templateId: number,
  weekStartDate: string (ISO date),
  numberOfWeeks: number
}
```

### 5. Xóa template
```
DELETE /api/v1/admin/flight-templates/{id}
```

## ✅ Lợi ích

1. **Tiết kiệm thời gian:** Tạo hàng trăm chuyến bay chỉ trong vài click
2. **Tính nhất quán:** Đảm bảo lịch bay đồng nhất theo tuần
3. **Dễ quản lý:** Template có thể tái sử dụng nhiều lần
4. **Linh hoạt:** Có thể tạo nhiều template cho các mục đích khác nhau
5. **An toàn:** Tự động kiểm tra trùng lặp, không tạo chuyến bay trùng

## 🚀 Workflow hoàn chỉnh

```
1. Admin đăng nhập
   ↓
2. Vào "Quản lý Templates"
   ↓
3. Tạo template mới
   ├─ Nhập tên, mô tả
   ├─ Chọn flight definitions
   └─ Thêm vào các thứ (Thứ 2 - CN)
   ↓
4. Lưu template
   ↓
5. Chọn template để sinh chuyến bay
   ├─ Chọn ngày bắt đầu (Thứ 2)
   └─ Chọn số tuần
   ↓
6. Sinh chuyến bay
   ↓
7. Hệ thống tự động tạo tất cả chuyến bay
   ↓
8. Hiển thị kết quả
```

## 📊 Thống kê

- **Thời gian tạo template:** ~2-3 phút
- **Thời gian sinh chuyến bay:** ~5-10 giây cho 100 chuyến
- **Số template tối đa:** Không giới hạn
- **Số tuần tối đa:** 52 tuần (1 năm)
- **Số chuyến bay/template:** Không giới hạn

## 🎓 Best Practices

1. **Đặt tên template rõ ràng:** "Template Tuần Thường", "Template Cuối Tuần", "Template Lễ Tết"
2. **Thêm mô tả chi tiết:** Giúp dễ nhớ mục đích sử dụng
3. **Test với 1 tuần trước:** Sinh 1 tuần để kiểm tra trước khi sinh nhiều tuần
4. **Backup template:** Có thể tạo nhiều phiên bản của cùng một template
5. **Xóa template cũ:** Xóa các template không còn sử dụng để dễ quản lý
