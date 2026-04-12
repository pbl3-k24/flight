# 📡 Module 1: API Layer (Presentation Layer) Guide

## 📋 Mục Đích
Xử lý HTTP requests/responses, input validation, authentication, authorization, routing.

---

## 🏗️ Kiến Trúc

```
API Layer
    ↓ (nhận request)
[Controllers]
    ↓ (gọi service)
[Application Layer Services]
    ↓ (trả về DTO)
[Response Handler]
    ↓ (gửi HTTP response)
[Client]
```

---

## 🎯 Quy Tắc & Pattern

### 1. **Controllers - Workflow**
- Nhận request từ client
- Validate input (model state)
- Gọi Application Service
- Xử lý exceptions từ service
- Return HTTP response với status code phù hợp

**Flow Logic:**
```
Request → Validate → Call Service → Handle Exception → Return Response
```

### 2. **DTO - Data Transfer Objects**
- Tách biệt API contract khỏi domain entities
- 3 loại DTO: CreateDto, UpdateDto, ResponseDto
- Không có business logic trong DTO
- Chỉ chứa properties cần thiết cho communication

**DTO Strategy:**
```
Client Request
    ↓ (CreateDto/UpdateDto)
[API Controller]
    ↓ (validate & map)
[Service Layer]
    ↓ (process business logic)
[Database]
    ↓ (fetch entity)
[Service Layer]
    ↓ (map to ResponseDto)
[API Controller]
    ↓ (ResponseDto)
Client Response
```

### 3. **Validation Strategy**
- API level: Model validation (required, length, format)
- Service level: Business logic validation
- Validation errors → 400 Bad Request
- Validation success → proceed to service

**Validation Flow:**
```
Input DTO
    ↓ [FluentValidation]
Valid? → Yes → Call Service
       ↓ No
    Return 400 Bad Request (validation errors)
```

### 4. **Exception Handling Pattern**
- Specific exceptions → Specific HTTP status codes
- NotFoundException → 404
- InvalidOperationException → 400
- Unauthorized → 401
- Forbidden → 403
- Unexpected errors → 500

**Error Handling Logic:**
```
Service Method Call
    ↓
Exception thrown?
    ├─ Business Exception (NotFoundException) → 404
    ├─ Validation Exception (InvalidOperation) → 400
    ├─ Authorization Exception → 401/403
    └─ Unexpected Exception → 500 + Log
```

### 5. **HTTP Method Semantics**
- **GET** → Fetch resources (no side effects)
- **POST** → Create new resources
- **PUT** → Update entire resource
- **PATCH** → Partial update
- **DELETE** → Remove resource

**Status Code Pattern:**
```
✅ Success:
   200 OK (GET, PUT, PATCH)
   201 Created (POST)
   204 No Content (DELETE)

❌ Failure:
   400 Bad Request (validation error)
   401 Unauthorized (not authenticated)
   403 Forbidden (not authorized)
   404 Not Found (resource not found)
   500 Internal Server Error (unexpected)
```

### 6. **Pagination Strategy**
- Query parameters: `?page=1&pageSize=10`
- Return total count in response header
- Default page size (e.g., 10)
- Max page size limit (e.g., 100)

**Pagination Logic:**
```
Total Records: 150
Page Size: 10
Requested Page: 1

Calculate:
  Skip = (Page - 1) × PageSize
  Take = PageSize
  Total Pages = ceil(Total / PageSize)

Result: Records 1-10, Total Pages: 15
```

### 7. **Response Format**
- Success: `{ data: {...}, statusCode: 200 }`
- Error: `{ message: "...", statusCode: 400 }`
- Consistent format across all endpoints

---

## 💡 Business Logic Scenarios

### Scenario 1: Search Flights
```
Input: DepartureAirportId, ArrivalAirportId, DepartureDate
Algorithm:
1. Validate input params
2. Query database: flights matching criteria
3. Filter by status (Active only)
4. Sort by departure time
5. Map to ResponseDto
6. Return paginated results
```

### Scenario 2: Create Booking
```
Input: FlightId, UserId, PassengerCount
Algorithm:
1. Validate flight exists
2. Validate user exists
3. Check available seats >= passenger count
4. Create booking record
5. Reserve seats (decrease available_seats)
6. Generate booking reference
7. Return booking confirmation
```

### Scenario 3: Cancel Booking
```
Input: BookingId, Reason
Algorithm:
1. Fetch booking
2. Validate booking exists & user owns it
3. Check if cancellation is allowed (status, time)
4. Release reserved seats back
5. Update booking status to "Cancelled"
6. Trigger refund process
7. Send cancellation notification
```

---

## 🔄 Request/Response Flow

### Successful Flow:
```
Client Request
    ↓
API Route → Controller Method
    ↓ Inject dependencies
Service is called with DTO
    ↓
Business logic executed
    ↓
Data persisted (if needed)
    ↓
ResponseDto returned from service
    ↓
HTTP 200/201 + ResponseDto
    ↓
Client receives response
```

### Error Flow:
```
Client Request
    ↓
API Route → Validation fails
    ↓
HTTP 400 + Error message
    ↓
Client receives error

OR

Service throws Exception
    ↓
Controller catches exception
    ↓
Determine HTTP status code
    ↓
HTTP 4xx/5xx + Error message
    ↓
Client receives error
```

---

## 🎯 Input Validation Rules

### Level 1: Model Validation (DTO)
- Required fields
- String length limits
- Number ranges
- Email format
- Date format

### Level 2: Business Validation (Service)
- Resource exists
- User owns resource
- Business rule constraints
- Capacity checks
- Time-based rules

**Validation Pyramid:**
```
        Service (Business Logic)
       /                       \
    API (Model State)          Exception Handling
   /           \
Format        Type Checking
```

---

## 🔐 Security Considerations

### Authentication & Authorization
```
Request → Check Auth Header (JWT/Bearer)
          ↓
       Is Authenticated?
       ├─ No → 401 Unauthorized
       └─ Yes → Check Authorization (roles)
                ├─ Not Authorized → 403 Forbidden
                └─ Authorized → Proceed
```

### Sensitive Data Protection
- Don't return passwords, secrets in response
- Don't expose internal IDs where possible
- Validate user owns resource before returning it
- Log without sensitive data

---

## 📊 Response Design

### Success Response Pattern
```
{
  id: 123,
  flightNumber: "AA100",
  ...data...
  // No wrapper needed, just the data
}
```

### Paginated Response Pattern
```
{
  items: [...],
  total: 150,
  page: 1,
  pageSize: 10,
  totalPages: 15
}
```

### Error Response Pattern
```
{
  message: "Readable error message",
  statusCode: 400,
  details: {...} // Optional detailed errors
}
```

---

## 🎪 Controller Responsibilities

✅ **Do:**
- Handle HTTP concerns (status codes, headers)
- Validate ModelState
- Call service layer
- Handle exceptions
- Map DTOs

❌ **Don't:**
- Direct database queries
- Business logic
- Complex calculations
- Data transformations (use service)
- Long methods (>20 lines)

---

## 📝 Common Patterns

### Async/Await Pattern
```
All I/O operations must be async
Controller method → async Task<IActionResult>
Service call → await _service.MethodAsync()
Never use .Result or .Wait()
```

### Dependency Injection Pattern
```
Constructor receives all dependencies
Services are interfaces, not implementations
Dependencies registered in Program.cs
Use them via private readonly fields
```

### Try-Catch Pattern
```
Catch specific exceptions (NotFoundException, etc.)
Log exception with context
Return appropriate HTTP status code
Don't catch and ignore exceptions
```

---

## 🚀 Best Practices

1. **Keep controllers thin** - Logic belongs in services
2. **One responsibility per controller** - One resource type
3. **Use meaningful route names** - RESTful endpoints
4. **Return appropriate status codes** - 200, 201, 400, 404, 500
5. **Validate all inputs** - Both format and business rules
6. **Log important events** - Successful operations and errors
7. **Consistent response format** - Same format for all endpoints
8. **Use DTOs** - Never expose entities directly
9. **Async all the way** - No blocking calls
10. **Secure by default** - Validate authorization before processing

---

**Module**: API Layer | **Version**: 1.0
