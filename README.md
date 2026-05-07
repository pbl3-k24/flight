# Flight Booking System

Dự án này là hệ thống đặt vé máy bay (Flight Booking System). Dưới đây là hướng dẫn chi tiết cách để khởi chạy dự án trên máy mới sau khi clone về.

## Yêu cầu hệ thống (Prerequisites)

Trước khi chạy dự án, hãy đảm bảo máy tính của bạn đã cài đặt các phần mềm sau:
1. **Docker & Docker Compose**: Dùng để chạy database (PostgreSQL) và cache (Redis).
2. **.NET 8.0 SDK**: Dùng để chạy backend API.
3. **Node.js (phiên bản 18+ khuyến nghị)**: Dùng để chạy frontend (Vite + React).
4. **Git**: Để clone source code.

---

## Các bước khởi chạy dự án

### 1. Clone source code
Mở terminal và chạy lệnh sau để lấy code về:
```bash
git clone <đường_dẫn_git_của_bạn>
cd flight
```

### 2. Chạy Database & Redis (Docker)
Backend yêu cầu database PostgreSQL và Redis để hoạt động. Các dịch vụ này được cấu hình sẵn trong file `docker-compose.yml`.

1. Di chuyển vào thư mục chứa Docker Compose:
   ```bash
   cd backend/API
   ```
2. Khởi chạy các dịch vụ ở chế độ background (`-d`):
   ```bash
   docker-compose up -d
   ```
   *(Đợi một lát để Docker pull các image PostgreSQL, Redis, pgAdmin và khởi động container)*

### 3. Chạy Backend (ASP.NET Core API)
Hệ thống backend được cấu hình tự động tạo database (`EnsureCreated`) và tự động seed dữ liệu mẫu (sample data) trong lần khởi chạy đầu tiên.

1. Đảm bảo bạn vẫn đang ở trong thư mục `backend/API`.
2. Khởi chạy Backend bằng .NET CLI:
   ```bash
   dotnet run
   ```
   *(Hoặc nếu bạn muốn bật chế độ tự reload code khi chỉnh sửa, hãy dùng `dotnet watch run`)*
3. Sau khi backend chạy lên thành công, bạn có thể truy cập API Swagger UI tại địa chỉ:
   👉 **http://localhost:5000**

### 4. Chạy Frontend (React + Vite)
Frontend được xây dựng bằng React và Vite, kết nối trực tiếp với Backend API.

1. Mở một cửa sổ Terminal mới (giữ Terminal của backend đang chạy).
2. Di chuyển vào thư mục `frontend`:
   ```bash
   cd frontend
   ```
3. Cài đặt các gói thư viện phụ thuộc (dependencies):
   ```bash
   npm install
   ```
4. Khởi chạy ứng dụng Frontend:
   ```bash
   npm run dev
   ```
5. Vite sẽ cung cấp cho bạn một đường dẫn (thường là http://localhost:5173). Mở trình duyệt và truy cập vào đường dẫn đó để sử dụng ứng dụng.

---

## Thông tin thêm

- **Cơ sở dữ liệu (Database)**: Được lưu mặc định ở cổng `5432` với user `admin` và password `SecretPassword123!`.
- **Trang quản lý Database (pgAdmin)**: Bạn có thể truy cập trang pgAdmin tại địa chỉ http://localhost:5050 để xem dữ liệu bằng giao diện (thông tin đăng nhập cấu hình tại `docker-compose.yml`).
- Hệ thống hỗ trợ xử lý việc khởi tạo bảng (Schema Sync) tự động khi backend chạy. Không cần phải chạy Entity Framework Migrations thủ công.