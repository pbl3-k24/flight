# CHECK Constraints - Tóm tắt

## 📋 Tất cả CHECK Constraints đã thêm vào database

### 1. **Routes Table**
```sql
CK_Route_DistanceKm_Positive
    DistanceKm > 0
    → Khoảng cách phải dương

CK_Route_EstimatedDurationMinutes_Positive
    EstimatedDurationMinutes > 0
    → Thời gian ước tính phải dương

CK_Route_DifferentAirports
    DepartureAirportId != ArrivalAirportId
    → Sân bay khởi hành phải khác sân bay đến
```

### 2. **Aircraft Table**
```sql
CK_Aircraft_TotalSeats_Positive
    TotalSeats > 0
    → Tổng số ghế phải dương
```

### 3. **AircraftSeatTemplates Table**
```sql
CK_AircraftSeatTemplate_DefaultSeatCount_Positive
    DefaultSeatCount > 0
    → Số ghế mặc định phải dương

CK_AircraftSeatTemplate_DefaultBasePrice_Positive
    DefaultBasePrice > 0
    → Giá cơ bản mặc định phải dương
```

### 4. **SeatClasses Table**
```sql
CK_SeatClass_RefundPercent_Valid
    RefundPercent >= 0 AND RefundPercent <= 100
    → Phần trăm hoàn tiền phải từ 0-100%

CK_SeatClass_ChangeFee_NonNegative
    ChangeFee >= 0
    → Phí đổi chuyến không được âm
```

### 5. **FlightSeatInventories Table**
```sql
CK_FlightSeatInventory_TotalSeats_Positive
    TotalSeats > 0
    → Tổng số ghế phải dương

CK_FlightSeatInventory_AvailableSeats_Valid
    AvailableSeats >= 0 AND AvailableSeats <= TotalSeats
    → Ghế khả dụng phải từ 0 đến tổng số

CK_FlightSeatInventory_HeldSeats_NonNegative
    HeldSeats >= 0
    → Ghế tạm giữ không được âm

CK_FlightSeatInventory_SoldSeats_NonNegative
    SoldSeats >= 0
    → Ghế đã bán không được âm

CK_FlightSeatInventory_Seats_Total
    AvailableSeats + HeldSeats + SoldSeats <= TotalSeats
    → Tổng ghế sử dụng không vượt quá tổng

CK_FlightSeatInventory_BasePrice_Positive
    BasePrice > 0
    → Giá cơ bản phải dương

CK_FlightSeatInventory_CurrentPrice_Positive
    CurrentPrice > 0
    → Giá hiện tại phải dương
```

### 6. **Bookings Table**
```sql
CK_Booking_TotalAmount_Positive
    TotalAmount > 0
    → Tổng tiền phải dương

CK_Booking_DiscountAmount_NonNegative
    DiscountAmount >= 0
    → Tiền giảm giá không được âm

CK_Booking_FinalAmount_Positive
    FinalAmount > 0
    → Tiền cuối cùng phải dương

CK_Booking_FinalAmount_LessThanTotal
    FinalAmount <= TotalAmount
    → Tiền cuối cùng không vượt quá tổng tiền
```

### 7. **Promotions Table**
```sql
CK_Promotion_DiscountValue_Positive
    DiscountValue > 0
    → Giá trị giảm giá phải dương

CK_Promotion_UsedCount_NonNegative
    UsedCount >= 0
    → Số lần sử dụng không được âm
```

### 8. **Payments Table**
```sql
CK_Payment_Amount_Positive
    Amount > 0
    → Số tiền thanh toán phải dương
```

### 9. **RefundPolicies Table**
```sql
CK_RefundPolicy_HoursBeforeDeparture_Positive
    HoursBeforeDeparture > 0
    → Số giờ trước cất cánh phải dương

CK_RefundPolicy_RefundPercent_Valid
    RefundPercent >= 0 AND RefundPercent <= 100
    → Phần trăm hoàn tiền phải từ 0-100%

CK_RefundPolicy_PenaltyFee_NonNegative
    PenaltyFee >= 0
    → Phí phạt không được âm
```

### 10. **RefundRequests Table**
```sql
CK_RefundRequest_RefundAmount_Positive
    RefundAmount > 0
    → Số tiền hoàn lại phải dương
```

---

## 📊 Tổng số CHECK Constraints: **22 constraints**

## ✅ Lợi ích của các CHECK constraints:

1. **Đảm bảo tính toàn vẹn dữ liệu** - Database sẽ reject dữ liệu không hợp lệ
2. **Ngăn chặn dữ liệu âm** - Các trường số lượng, giá tiền, phần trăm không thể âm
3. **Ràng buộc logic** - Đảm bảo các mối quan hệ giữa các trường
4. **Hiệu suất** - Kiểm tra ở mức database, nhanh hơn kiểm tra ở application layer
5. **Bảo mật** - Ngăn chặn việc insert/update dữ liệu không hợp lệ trực tiếp vào database

## 🔄 Hướng dẫn thêm constraints vào entity khác:

1. Thêm CHECK constraints vào configuration file của entity:
```csharp
builder.HasCheckConstraint(
    "CK_TableName_ColumnName_Rule",
    "\"ColumnName\" > 0");
```

2. Tạo migration mới:
```powershell
dotnet ef migrations add AddCheckConstraintFor[EntityName]
```

3. Apply migration:
```powershell
dotnet ef database update
```

---

## 🎯 Kiểm tra constraints trong pgAdmin:

1. Mở pgAdmin: http://localhost:5050
2. Đăng nhập: pgadmin@example.com / pgadmin123
3. Chọn database: FlightBookingDB
4. Mở table bất kỳ
5. Chọn tab "Constraints" để xem tất cả CHECK constraints
