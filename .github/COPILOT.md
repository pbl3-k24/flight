# 🛫 Flight Ticket Booking System - Copilot Instructions

## 📋 Tổng Quan
- **Project**: Flight Ticket Booking System (Dự án bán vé máy bay trực tuyến)
- **Stack**: ASP.NET Core 8.0 Web API | EF Core | PostgreSQL | Redis | Docker
- **Architecture**: Layered Architecture (4 lớp)
- **Language**: C#

---

## 🏗️ Cấu Trúc Dự Án

```
FlightTicketBooking.sln
├── FlightTicketBooking.API              # Presentation Layer
├── FlightTicketBooking.Application      # Business Logic Layer  
├── FlightTicketBooking.Domain           # Entity/Domain Layer
├── FlightTicketBooking.Infrastructure   # Data Access Layer
└── FlightTicketBooking.Tests            # Unit & Integration Tests
```

---

## ⚠️ Ràng Buộc Tối Cao

### 1. **Architecture & Layering**
- Luôn tách biệt 4 lớp: API → Application → Domain → Infrastructure
- Chỉ dùng interface để giao tiếp giữa các lớp
- Không phép Domain/Infrastructure phụ thuộc vào Application/API

### 2. **Dependency Injection**
- Tất cả dependencies phải inject qua constructor
- Đăng ký services trong `Program.cs`
- Không dùng Service Locator pattern

### 3. **Async/Await**
- Tất cả I/O operations (database, cache, HTTP) phải async
- Sử dụng `async Task` / `async Task<T>`
- Không được dùng `.Result` hoặc `.Wait()`

### 4. **Database**
- Sử dụng Entity Framework Core + Migrations
- Tất cả schema changes phải qua migrations
- Luôn commit migration files
- Connection string trong `appsettings.json`

### 5. **DTOs (Data Transfer Objects)**
- API chỉ nhận/trả DTOs, không entities
- Naming: `[Resource]CreateDto`, `[Resource]ResponseDto`, `[Resource]UpdateDto`
- Map entities ↔ DTOs qua AutoMapper

### 6. **Validation**
- Sử dụng FluentValidation cho input validation
- Tạo Validator class cho mỗi DTO
- Không validate trong entities

### 7. **Exception Handling**
- Tạo custom exceptions cho business logic
- Inherit từ `ApplicationException`
- Controllers xử lý exceptions và return HTTP status codes

### 8. **Caching (Redis)**
- Cache frequently accessed data (flights, airports)
- Key format: `{entity_type}_{id}` (e.g., `flight_123`)
- Invalidate cache khi update/delete
- TTL mặc định: 1 hour

### 9. **Logging**
- Inject `ILogger<T>` vào services
- Log INFO khi operations thành công
- Log ERROR với exception details
- Không log sensitive data

### 10. **Testing**
- Unit tests cho tất cả services
- Mock repositories & external services
- Sử dụng xUnit + Moq
- Minimum coverage: 80%

### 11. **Naming Conventions**
```
Classes:           PascalCase (FlightService)
Interfaces:        IPascalCase (IFlightService)
Methods:           PascalCase + Async suffix (GetFlightAsync)
Properties:        PascalCase (FlightId, FlightNumber)
Private fields:    _camelCase (_flightRepository)
Constants:         UPPER_SNAKE_CASE (CACHE_KEY_PREFIX)
```

### 12. **Code Quality**
- Maximum 30 dòng code per method
- One responsibility per class
- Không dùng nested if quá sâu (max 3 levels)
- Luôn sử dụng meaningful names

### 13. **Docker**
- Tất cả services chạy trong Docker containers
- Database: PostgreSQL 16
- Cache: Redis 7
- API: ASP.NET Core 8.0
- docker-compose.yml phải có health checks

### 14. **Git Workflow**
```
Commit format:  <type>: <description>
Types:          feat, fix, refactor, test, docs, chore
Branches:       feature/*, bugfix/*, docs/*
```

### 15. **API Standards**
- RESTful endpoints: `/api/[resource]`
- HTTP methods: GET, POST, PUT, DELETE
- Response format: JSON
- Error response: `{ message: "error details" }`
- Pagination: `?page=1&pageSize=10`
- Return meaningful HTTP status codes (200, 201, 400, 404, 500)

---

## 📁 File Organization Rules

- **Models**: `Domain/Entities/`
- **Interfaces**: `[Layer]/Interfaces/`
- **Services**: `Application/Services/`
- **Repositories**: `Infrastructure/Data/Repositories/`
- **Validators**: `Application/Validators/`
- **DTOs**: `API/DTOs/`
- **Controllers**: `API/Controllers/`
- **Migrations**: `Infrastructure/Data/Migrations/`

---

## 🚫 Forbidden Patterns

❌ Không dùng:
- Service Locator pattern
- Static classes for business logic
- Direct database queries in controllers
- Entities trong API responses
- Synchronous I/O operations
- Try-catch mà không log
- Magic numbers (luôn dùng constants)
- TODO comments mà không có issue tracking

---

## ✅ Pre-Commit Checklist

Trước khi commit:
- [ ] Code builds successfully
- [ ] All tests pass (unit + integration)
- [ ] No breaking changes
- [ ] Follows naming conventions
- [ ] Async/await properly used
- [ ] DTOs mapped correctly
- [ ] Migrations created if needed
- [ ] Logged appropriately
- [ ] No sensitive data exposed

---

## 📞 References

- **Domain**: Entities, ValueObjects, Enums (Domain Layer)
- **Application Services**: Business logic, validation (Application Layer)
- **Repositories**: Data access (Infrastructure Layer)
- **Controllers**: API endpoints (Presentation Layer)
- **AutoMapper Profile**: DTO ↔ Entity mappings

---

**Version**: 1.0 | **Updated**: April 2026
