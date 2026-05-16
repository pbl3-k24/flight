# Contract Test Checklist (Phase D)

## Mục tiêu
- Xác nhận route chuẩn `api/v1` hoạt động đúng.
- Xác nhận route legacy trả deprecation headers đúng chính sách.
- Ngăn client mới sử dụng route cũ/debug/test.

## Preconditions
- API chạy thành công tại `http://localhost:5042`.
- Có token admin nếu test endpoint admin.

## 1) Additional Services route normalization

### 1.1 Route mới phải hoạt động
- `GET /api/v1/additional-services`
- Expect:
  - Status `200`
  - Body là danh sách services

### 1.2 Route cũ phải bị loại bỏ
- `GET /api/AdditionalServices`
- Expect:
  - Status `404`
    - `Warning` chứa nội dung deprecated

### 1.3 By-seat-class route (new)
- `GET /api/v1/additional-services/by-seat-class/{seatClassId}`
- Expect:
  - Status `200` hoặc `404` tùy dữ liệu
  - Schema response đúng `SeatClassServiceConfigDto`

### 1.4 By-seat-class route (legacy) phải bị loại bỏ
- `GET /api/AdditionalServices/by-seat-class/{seatClassId}`
- Expect:
  - Status `404`

## 2) Debug/Test/Migration routes removed
- `GET /api/v1/debug/no-auth`
- Expect:
  - Status `404`

## 3) Swagger contract
- Swagger không hiển thị các controller:
  - `DebugController`
  - `DatabaseFixController`
  - `DatabaseResetController`
  - `MigrationController`
  - `RoutesDebugController`
  - `TestDataController`
  - `TestVnpayController`
- Swagger hiển thị route mới:
  - `/api/v1/additional-services`

## 4) Regression smoke (không đổi hành vi)
- `Flights search`: `POST /api/v1/Flights/search`
- `Bookings create`: `POST /api/v1/Bookings`
- `Payments create`: `POST /api/v1/Payments`
- `FlightsAdmin routes`: `GET /api/v1/admin/FlightsAdmin/routes`
- Expect: status code và schema không đổi so với trước khi normalize route.

## 5) Exit criteria
- Tất cả mục 1, 2, 3 pass.
- Không còn route legacy hoạt động trong runtime chính.
