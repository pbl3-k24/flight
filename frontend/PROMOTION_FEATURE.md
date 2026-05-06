# Tính năng Tự động Áp dụng Mã Giảm Giá Tốt Nhất

## Mô tả
Hệ thống tự động tìm và áp dụng mã giảm giá tốt nhất cho khách hàng khi đặt vé máy bay, giúp khách hàng tiết kiệm tối đa mà không cần phải tìm kiếm hoặc nhập mã thủ công.

## Cách hoạt động

### 1. Tự động tìm kiếm
- Khi khách hàng chọn chuyến bay, hệ thống tự động gọi API để lấy danh sách các mã giảm giá đang hoạt động
- Hệ thống lọc các mã giảm giá hợp lệ dựa trên:
  - Thời gian hiệu lực (startDate, endDate)
  - Điều kiện tối thiểu (minPurchaseAmount)
  - Trạng thái hoạt động (isActive)

### 2. Tính toán giảm giá
Hệ thống hỗ trợ 2 loại giảm giá:

**Giảm theo phần trăm (Percentage):**
- Tính: `discountAmount = (totalPrice * discountValue) / 100`
- Áp dụng giới hạn tối đa nếu có: `min(discountAmount, maxDiscountAmount)`

**Giảm theo số tiền cố định (FixedAmount):**
- Tính: `discountAmount = discountValue`

### 3. Chọn mã tốt nhất
- Hệ thống so sánh tất cả các mã giảm giá hợp lệ
- Chọn mã có số tiền giảm cao nhất
- Tự động áp dụng vào booking

### 4. Hiển thị cho khách hàng
- **Màn hình thông tin hành khách:**
  - Hiển thị giá gốc
  - Hiển thị mã giảm giá được áp dụng (tên mã, mô tả)
  - Hiển thị số tiền giảm
  - Hiển thị tổng tiền sau giảm

- **Màn hình thanh toán:**
  - Hiển thị chi tiết giá vé
  - Hiển thị mã giảm giá đã áp dụng
  - Hiển thị tổng tiền cuối cùng

## API sử dụng

### 1. Lấy danh sách mã giảm giá đang hoạt động
```javascript
GET /api/v1/admin/PromotionsAdmin/active
```

**Response:**
```json
[
  {
    "promotionId": 1,
    "promoCode": "SUMMER2024",
    "description": "Giảm 20% cho mùa hè",
    "discountType": "Percentage",
    "discountValue": 20,
    "maxDiscountAmount": 500000,
    "minPurchaseAmount": 1000000,
    "startDate": "2024-06-01T00:00:00",
    "endDate": "2024-08-31T23:59:59",
    "isActive": true
  }
]
```

### 2. Tạo booking với mã giảm giá
```javascript
POST /api/v1/Bookings
```

**Request Body:**
```json
{
  "outboundFlightId": 123,
  "passengerCount": 1,
  "seatClassId": 1,
  "passengers": [...],
  "promotionId": 1,  // ID của mã giảm giá tốt nhất
  "contactEmail": "customer@email.com"
}
```

## Code Implementation

### API Functions (src/api.js)

```javascript
// Lấy danh sách promotion đang hoạt động
export const getActivePromotions = () => {
  return makeRequest('/admin/PromotionsAdmin/active')
}

// Tìm promotion tốt nhất cho booking
export const getBestPromotion = async (bookingAmount) => {
  const promotions = await getActivePromotions()
  
  // Lọc promotion hợp lệ
  const validPromotions = promotions.filter(promo => {
    const isActive = checkDateValidity(promo)
    const meetsMinimum = !promo.minPurchaseAmount || 
                         bookingAmount >= promo.minPurchaseAmount
    return isActive && meetsMinimum && promo.isActive
  })
  
  // Tính toán và chọn promotion tốt nhất
  const promotionsWithDiscount = validPromotions.map(promo => ({
    ...promo,
    calculatedDiscount: calculateDiscount(promo, bookingAmount)
  }))
  
  promotionsWithDiscount.sort((a, b) => 
    b.calculatedDiscount - a.calculatedDiscount
  )
  
  return promotionsWithDiscount[0]
}
```

### React Component (src/App.jsx)

```javascript
// State management
const [bestPromotion, setBestPromotion] = useState(null)
const [isLoadingPromotion, setIsLoadingPromotion] = useState(false)

// Tự động tìm promotion khi chọn chuyến bay
useEffect(() => {
  if (!selectedFlight || totalPrice === 0) return
  
  const fetchBestPromotion = async () => {
    setIsLoadingPromotion(true)
    const promotion = await getBestPromotion(totalPrice)
    setBestPromotion(promotion)
    setIsLoadingPromotion(false)
  }
  
  fetchBestPromotion()
}, [selectedFlight, totalPrice])

// Tính toán giá cuối cùng
const discountAmount = bestPromotion?.calculatedDiscount || 0
const finalPrice = totalPrice - discountAmount

// Gửi promotionId khi tạo booking
const booking = await createBooking({
  ...bookingData,
  promotionId: bestPromotion?.promotionId || null
})
```

## UI/UX

### Trạng thái loading
```
🔍 Đang tìm mã giảm giá tốt nhất...
```

### Hiển thị mã giảm giá
```
┌─────────────────────────────────────┐
│ 🎁 SUMMER2024                       │
│ Giảm 20% cho mùa hè                 │
│                        -500,000 VND │
└─────────────────────────────────────┘
```

### Tóm tắt giá
```
Giá vé (1 người):           2,500,000 VND
🎁 Giảm giá (SUMMER2024):    -500,000 VND
─────────────────────────────────────────
Tổng cộng:                  2,000,000 VND
```

## Lợi ích

### Cho khách hàng:
- ✅ Không cần tìm kiếm mã giảm giá
- ✅ Không cần nhập mã thủ công
- ✅ Luôn nhận được ưu đãi tốt nhất
- ✅ Tiết kiệm thời gian và tiền bạc

### Cho doanh nghiệp:
- ✅ Tăng tỷ lệ chuyển đổi (conversion rate)
- ✅ Cải thiện trải nghiệm khách hàng
- ✅ Tự động hóa quy trình khuyến mãi
- ✅ Tăng doanh số bán hàng

## Logging & Debug

Hệ thống có logging để dễ dàng debug:

```javascript
console.log('🎁 Áp dụng mã giảm giá:', promotion.promoCode, 
            '- Giảm:', promotion.calculatedDiscount)
```

## Yêu cầu Backend

Backend cần đảm bảo:
1. API `/admin/PromotionsAdmin/active` trả về danh sách promotion đang hoạt động
2. Các trường cần thiết trong response:
   - `promotionId`: ID của promotion
   - `promoCode`: Mã code hiển thị
   - `description`: Mô tả promotion
   - `discountType`: Loại giảm giá (Percentage/FixedAmount)
   - `discountValue`: Giá trị giảm
   - `maxDiscountAmount`: Giới hạn giảm tối đa (cho Percentage)
   - `minPurchaseAmount`: Điều kiện tối thiểu
   - `startDate`, `endDate`: Thời gian hiệu lực
   - `isActive`: Trạng thái

## Testing

### Test cases:
1. ✅ Không có promotion nào → Hiển thị giá gốc
2. ✅ Có 1 promotion hợp lệ → Tự động áp dụng
3. ✅ Có nhiều promotion → Chọn promotion giảm nhiều nhất
4. ✅ Promotion hết hạn → Không áp dụng
5. ✅ Không đủ điều kiện tối thiểu → Không áp dụng
6. ✅ Giảm theo % vượt max → Áp dụng max
7. ✅ Loading state → Hiển thị "Đang tìm..."

## Future Enhancements

- [ ] Cho phép khách hàng nhập mã thủ công
- [ ] Hiển thị danh sách tất cả mã có thể áp dụng
- [ ] Thông báo khi gần đạt điều kiện promotion tốt hơn
- [ ] Lưu lịch sử promotion đã sử dụng
- [ ] Gợi ý thêm vé để đạt promotion tốt hơn
