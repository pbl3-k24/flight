# ✈️ Flight Booking System

Hệ thống đặt vé máy bay trực tuyến, được xây dựng theo kiến trúc **Clean Architecture** với backend ASP.NET Core 8 và frontend React 19.

---

## 📖 Giới Thiệu

**Flight Booking System** là ứng dụng web cho phép người dùng tìm kiếm, đặt vé, và quản lý các chuyến bay nội địa. Hệ thống bao gồm đầy đủ luồng từ tìm kiếm chuyến bay → đặt chỗ → thanh toán (qua cổng VNPAY) → xuất vé điện tử.

### Tính Năng Chính

**Dành cho Người dùng:**
- 🔍 Tìm kiếm chuyến bay 1 chiều / khứ hồi theo sân bay, ngày bay, số hành khách
- 💺 Chọn hạng ghế (Phổ thông / Thương gia) với **giá động** theo thời điểm đặt
- 👥 Quản lý thông tin hành khách, lưu hành khách thường xuyên
- 🎁 Áp dụng mã giảm giá (khuyến mãi)
- 💳 Thanh toán trực tuyến qua **cổng VNPAY**
- 🎫 Xem và tải vé điện tử (QR code)
- 📋 Lịch sử đặt vé, hủy vé, hoàn tiền
- 🔄 Đổi chuyến bay, nâng hạng ghế
- 🔔 Thông báo trạng thái chuyến bay

**Dành cho Quản trị viên:**
- 📊 Dashboard tổng quan & thống kê real-time
- ✈️ Quản lý chuyến bay, tuyến bay, máy bay
- 📅 Tạo lịch bay tự động từ template tuần
- 👤 Quản lý người dùng, đặt vé
- 🏷️ Quản lý chương trình khuyến mãi
- 📈 Báo cáo doanh thu

### Công Nghệ Sử Dụng

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core 8 (C#) — Clean Architecture |
| Database | PostgreSQL 17 |
| Cache | Redis 7 |
| Frontend | React 19 + Vite 8 + TailwindCSS |
| Thanh toán | VNPAY Sandbox |
| Xác thực | JWT Bearer |
| Email | SMTP Gmail |
| Container | Docker & Docker Compose |

### Sân Bay Được Hỗ Trợ

| Mã | Tên sân bay | Thành phố |
|---|---|---|
| SGN | Tân Sơn Nhất | TP. Hồ Chí Minh |
| HAN | Nội Bài | Hà Nội |
| DAD | Quốc tế Đà Nẵng | Đà Nẵng |
| CTS | Sân bay Cần Thơ | Cần Thơ |
| VCA | Sân bay Buôn Mê Thuột | Buôn Mê Thuột |
| HUI | Sân bay Phú Bài | Huế |

---

## 🖥️ Yêu Cầu Hệ Thống

Trước khi cài đặt, hãy đảm bảo máy tính đã có:

| Phần mềm | Phiên bản | Mục đích |
|---|---|---|
| [Git](https://git-scm.com/) | Bất kỳ | Clone source code |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | 4.x trở lên | Chạy PostgreSQL, Redis, pgAdmin |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | **8.0** | Chạy backend API |
| [Node.js](https://nodejs.org/) | **18+** (khuyến nghị 20 LTS) | Chạy frontend React |

> **Lưu ý cho Windows:** Nên dùng **PowerShell** hoặc **Windows Terminal** để thực hiện các lệnh bên dưới.

---

## 🚀 Hướng Dẫn Cài Đặt & Khởi Chạy

### Bước 1: Clone Source Code

```bash
git clone <đường-dẫn-repository>
cd flight
```

---

### Bước 2: Khởi Động Database & Redis (Docker)

Backend cần **PostgreSQL** và **Redis**. Tất cả được cấu hình sẵn trong `docker-compose.yml`.

```bash
# Di chuyển vào thư mục chứa docker-compose
cd backend/API

# Khởi động tất cả dịch vụ ở chế độ nền
docker-compose up -d
```

Kiểm tra các container đã chạy thành công:

```bash
docker ps
```

Bạn sẽ thấy các container: `flight_postgres_db`, `flight_redis`, `flight_pgadmin`.

> ⏳ Lần đầu chạy, Docker cần tải các image xuống (~vài phút tùy tốc độ mạng).

**Thông tin kết nối database:**
- Host: `localhost` | Port: **`5433`** *(chú ý: 5433 không phải 5432)*
- Database: `FlightBookingDB`
- Username: `admin`
- Password: `SecretPassword123!`

---

### Bước 3: Chạy Backend (ASP.NET Core API)

> Đảm bảo Docker đang chạy trước khi thực hiện bước này.

```bash
# Vẫn ở trong thư mục backend/API
dotnet run
```

Hoặc nếu muốn tự động reload khi sửa code:

```bash
dotnet watch run
```

✅ Backend khởi động thành công khi thấy dòng:
```
Now listening on: http://localhost:5000
```

**Các địa chỉ quan trọng sau khi backend chạy:**

| Địa chỉ | Mô tả |
|---|---|
| http://localhost:5000 | Swagger UI — Tài liệu & kiểm thử API |
| http://localhost:5000/health | Health check endpoint |

> **Lưu ý về Schema Sync:** Lần đầu khởi chạy, backend sẽ **không** tự động tạo bảng (vì `DatabaseStartup.Enabled = false` trong `appsettings.json`). Xem mục [Seed Dữ liệu Mẫu](#️-seed-dữ-liệu-mẫu-tùy-chọn) bên dưới nếu cần.

---

### Bước 4: Chạy Frontend (React + Vite)

Mở **cửa sổ terminal mới** (giữ terminal backend đang chạy), sau đó:

```bash
# Từ thư mục gốc của dự án (flight/)
cd frontend

# Cài đặt các thư viện (chỉ cần làm lần đầu)
npm install

# Khởi chạy ứng dụng
npm run dev
```

✅ Frontend khởi động thành công khi thấy:
```
  VITE v8.x  ready in xxx ms
  ➜  Local:   http://localhost:5173/
```

Mở trình duyệt và truy cập: **http://localhost:5173**

---

## 🗄️ Seed Dữ Liệu Mẫu (Tùy Chọn)

Để hệ thống có dữ liệu chuyến bay để tìm kiếm, bạn cần bật chức năng seed dữ liệu.

### Cách 1: Bật tự động qua appsettings (khuyến nghị)

Chỉnh sửa file `backend/API/appsettings.json`, thay đổi các giá trị sau thành `true`:

```json
"DatabaseStartup": {
  "Enabled": true,
  "SchemaSync": true,
  "InitialSeed": true
},
"CsvSeed": {
  "Enabled": true,
  "Directory": "seed-data"
}
```

Sau đó restart backend:

```bash
# Nhấn Ctrl+C để dừng, rồi chạy lại
dotnet run
```

Backend sẽ tự động:
1. Tạo tất cả các bảng (Schema Sync)
2. Seed dữ liệu cơ bản (sân bay, máy bay, tuyến bay, hạng ghế)
3. Seed CSV data (flight templates, lịch bay)

> ⚠️ Sau khi seed xong, hãy **tắt lại** các flag trên (`false`) để tránh chạy lại mỗi lần khởi động.

### Cách 2: Chạy toàn bộ qua Docker Compose (Full Stack)

Nếu muốn chạy cả backend trong Docker (không cần cài .NET):

```bash
cd backend/API
docker-compose up -d
```

Docker Compose sẽ tự động khởi động API container với `DatabaseStartup__Enabled=true`.

---

## 👤 Tài Khoản Mặc Định

Sau khi seed dữ liệu, hệ thống tạo sẵn các tài khoản:

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@flightbooking.com` | `Admin@123456` |
| User (test) | `user@example.com` | `User@123456` |

> Tài khoản admin có thể đăng nhập tại giao diện web và truy cập tab **Quản trị** để quản lý toàn hệ thống.

---

## 🌐 Tổng Hợp Các Địa Chỉ Truy Cập

| Dịch vụ | Địa chỉ | Thông tin đăng nhập |
|---|---|---|
| 🌍 Frontend (Web App) | http://localhost:5173 | — |
| 📘 Swagger API Docs | http://localhost:5000 | — |
| 🗄️ pgAdmin (Quản lý DB) | http://localhost:5050 | Email: `pgadmin@example.com` / Pass: `pgadmin123` |
| 🔴 Redis | localhost:6379 | — |
| 🐘 PostgreSQL | localhost:**5433** | user: `admin` / pass: `SecretPassword123!` |

---

## 📁 Cấu Trúc Thư Mục

```
flight/
├── backend/
│   └── API/                      ← ASP.NET Core 8 Web API
│       ├── Controllers/           ← 25 API Controllers
│       ├── Application/
│       │   ├── Services/          ← 34 Business Logic Services
│       │   └── Interfaces/        ← 54 Interface contracts
│       ├── Domain/Entities/       ← 36 Domain Entities
│       ├── Infrastructure/
│       │   ├── Data/              ← DbContext, Schema Sync
│       │   └── Repositories/      ← 23 Repositories
│       ├── seed-data/             ← File CSV dữ liệu mẫu
│       ├── appsettings.json       ← Cấu hình chính
│       └── docker-compose.yml     ← Cấu hình Docker
├── frontend/
│   ├── src/
│   │   ├── App.jsx                ← Toàn bộ giao diện (React SPA)
│   │   └── api.js                 ← API client layer
│   ├── .env                       ← Cấu hình URL backend
│   └── package.json
└── README.md
```

---

## ⚙️ Cấu Hình Nâng Cao

### Thay Đổi URL Backend (Frontend)

Nếu backend chạy ở cổng khác, chỉnh sửa file `frontend/.env`:

```env
VITE_API_BASE_URL=http://localhost:5000/api/v1
```

### Cấu Hình Email (SMTP)

Để tính năng gửi email hoạt động (xác thực email, đặt lại mật khẩu), cập nhật trong `appsettings.json`:

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "your-email@gmail.com",
  "Password": "your-app-password",
  "FromEmail": "your-email@gmail.com"
}
```

> Với Gmail, cần tạo **App Password** tại: https://myaccount.google.com/apppasswords

### Cấu Hình VNPAY (Thanh Toán)

Hệ thống đang dùng **VNPAY Sandbox** (môi trường test). Để kiểm thử thanh toán, dùng thẻ test:
- Ngân hàng: NCB
- Số thẻ: `9704198526191432198`
- Tên chủ thẻ: `NGUYEN VAN A`
- Ngày phát hành: `07/15`
- OTP: `123456`

---

## 🛠️ Xử Lý Sự Cố Thường Gặp

### Backend không kết nối được database
```
❌ Lỗi: "Cannot connect to database"
```
**Giải pháp:** Kiểm tra Docker đang chạy và container postgres healthy:
```bash
docker ps
docker logs flight_postgres_db
```

### Frontend không kết nối được API
```
❌ Lỗi: "API Error" hoặc CORS error
```
**Giải pháp:** Kiểm tra backend đang chạy tại `http://localhost:5000` và file `frontend/.env` đúng địa chỉ.

### Port 5433 bị chiếm
```
❌ Lỗi: "Port 5433 already in use"
```
**Giải pháp:** Tắt service PostgreSQL đang chạy trên máy hoặc thay port trong `docker-compose.yml`.

### Lỗi `dotnet: command not found`
**Giải pháp:** Cài đặt [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) và khởi động lại terminal.

---

## 📝 Ghi Chú Kỹ Thuật

- **EF Core Migrations bị vô hiệu hóa.** Hệ thống dùng `DatabaseSchemaSync` tùy chỉnh để tạo/đồng bộ schema. Không cần chạy `dotnet ef database update`.
- **Múi giờ:** Toàn bộ dữ liệu thời gian được lưu dạng **UTC** trong database và chuyển đổi sang **giờ Việt Nam (UTC+7)** khi hiển thị.
- **Giá vé động:** Giá được tính theo 3 yếu tố: thời điểm so với giờ khởi hành, giờ bay trong ngày, và thứ trong tuần.
- **Redis:** Được dùng để cache thông tin chuyến bay (TTL 1 giờ). Nếu Redis không chạy, hệ thống vẫn hoạt động bình thường (không cache).