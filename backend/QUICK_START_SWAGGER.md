# 🎯 Quick Start - Test API với Swagger

## ⚡ 3 bước nhanh

### 1️⃣ Chạy ứng dụng
```powershell
cd E:\pbl3\flight\backend\API
dotnet run
```

### 2️⃣ Mở Swagger UI
```
http://localhost:5000
```

### 3️⃣ Đăng nhập & Test

**A. Lấy JWT Token:**
- Tìm: `POST /api/v1/Users/login`
- Nhấp: "Try it out"
- Nhập:
  ```json
  {
    "email": "admin@flightbooking.vn",
    "password": "Admin@123456"
  }
  ```
- Nhấp: "Execute"
- Copy: Giá trị `token` từ response

**B. Authorize Swagger:**
- Nhấp: Nút "Authorize" (góc phải)
- Dán: `Bearer <token-của-bạn>`
- Nhấp: "Authorize"

**C. Test Protected API:**
- Chọn bất kỳ endpoint nào
- Nhấp: "Try it out" → "Execute"
- Token tự động được gửi ✅

---

## 📊 Test Credentials

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@flightbooking.vn` | `Admin@123456` |
| User | `user1@gmail.com` | `User1@123456` |
| User | `user2@gmail.com` | `User2@123456` |

---

## 🔥 Test Script (Optional)

```powershell
# Chạy script để tự động test login & lấy token
.\test-api.ps1
```

Script sẽ:
- ✅ Kiểm tra API health
- ✅ Đăng nhập
- ✅ Lấy token
- ✅ Lưu vào file

---

## 📚 Tài liệu

- **Chi tiết:** [`SWAGGER_API_TESTING_GUIDE.md`](./SWAGGER_API_TESTING_GUIDE.md)
- **Nâng cao:** [`SWAGGER_ADVANCED_CONFIG.md`](./SWAGGER_ADVANCED_CONFIG.md)
- **Hoàn tất:** [`SWAGGER_SETUP_COMPLETE.md`](./SWAGGER_SETUP_COMPLETE.md)

---

## 🎉 Done!

Swagger đã cấu hình xong! Bạn có thể test API với JWT authorization ngay bây giờ. 🚀
