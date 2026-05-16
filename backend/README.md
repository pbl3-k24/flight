# Flight Booking Backend

Backend API cho hệ thống bán vé máy bay, xây dựng bằng ASP.NET Core (.NET 10).

## 1) Yêu cầu môi trường

- .NET SDK 10.x
- Docker Desktop (nếu chạy bằng Docker)
- PostgreSQL 14+ (nếu chạy local không Docker)
- Git

## 2) Clone repo

```bash
git clone https://github.com/pbl3-k24/flight.git
cd flight/backend
```

## 3) Cấu trúc quan trọng

- API project: `API/API.csproj`
- Dockerfile: `API/Dockerfile`
- Docker compose: `API/docker-compose.yml`
- Test project: `API.Tests/API.Tests.csproj`

## 4) Chạy cách 1: Local bằng dotnet

### 4.1 Cấu hình

Chỉnh file `API/appsettings.json` (hoặc dùng biến môi trường) với tối thiểu:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpireMinutes`
- `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`, `Smtp:FromEmail`
- `VNPAY:TmnCode`, `VNPAY:HashSecret`, `VNPAY:BaseUrl`, `VNPAY:PaymentPath`, `VNPAY:RefundPath`

Ví dụ connection string PostgreSQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=flight_booking;Username=postgres;Password=postgres"
  }
}
```

Lưu ý:
- `Jwt:Key` phải đủ mạnh (>= 128 bits cho HS256).
- Không dùng key cấu hình cũ kiểu `SmtpSettings` hoặc `Payment:*`.

### 4.2 Restore + Build

```bash
dotnet restore API/API.csproj
dotnet build API/API.csproj
```

### 4.3 Migrate database

```bash
dotnet ef database update --project API/API.csproj
```

Nếu thiếu `dotnet-ef`:

```bash
dotnet tool install --global dotnet-ef
```

### 4.4 Run API

```bash
dotnet run --project API/API.csproj
```

Mặc định:
- Base URL: `http://localhost:5042`
- Swagger: `http://localhost:5042/swagger`
- Health: `http://localhost:5042/health`

## 5) Chạy cách 2: Docker Compose

Chạy từ thư mục `API`:

```bash
cd API
docker compose up -d --build
```

Xem log API:

```bash
docker compose logs -f api
```

Dừng:

```bash
docker compose down
```

Dừng + xóa volume DB:

```bash
docker compose down -v
```

Khi chạy Docker:
- API: `http://localhost:5000`
- Swagger: `http://localhost:5000/swagger`
- Health: `http://localhost:5000/health`
- PostgreSQL: `localhost:5433`
- pgAdmin: `http://localhost:5050`

## 6) Test nhanh các luồng chính

### 6.1 User/Auth
- `POST /api/v1/Users/login`

### 6.2 Flight search + booking
- `POST /api/v1/Flights/search`
- `POST /api/v1/Bookings`

### 6.3 Payment
- `POST /api/v1/Payments`
- `POST /api/v1/Payments/{paymentId}/callback`

### 6.4 Cancel booking
- `POST /api/v1/Bookings/{id}/cancel`

### 6.5 Admin flight management
- `GET /api/v1/admin/FlightsAdmin/routes`
- `POST /api/v1/admin/FlightsAdmin`
- `POST /api/v1/admin/FlightsAdmin/{flightId}/cancel`

`{flightId}/cancel` sẽ:
- hủy chuyến bay,
- hủy booking liên quan,
- queue hoàn tiền cho booking đã thanh toán,
- gửi email thông báo.

## 7) Chạy test tự động trong repo

```bash
dotnet run --project API.Tests/API.Tests.csproj -p RestoreIgnoreFailedSources=true
```

Kỳ vọng:
- In ra `PASS`.

## 8) Các route cũ đã loại bỏ

Không còn dùng trong runtime chính:

- `/api/admin/database/*`
- `/api/admin/migration/*`
- `/api/debug/*`
- `/api/test-data/*`
- `/api/test/vnpay/*`

Frontend/clients cần dùng chuẩn route `api/v1/...`.

