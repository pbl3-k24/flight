# BookingsController - Implementation Guide

## ✅ Overview

The `BookingsController` implements REST endpoints for managing flight bookings, following the API Layer patterns from `01_API_LAYER_GUIDE.md`.

**Location**: `API/Controllers/BookingsController.cs`  
**Route Base**: `api/v1/bookings`  
**Framework**: ASP.NET Core 10 with Dependency Injection

---

## 📋 Endpoints

### 1. GET /api/v1/bookings
**Get All Bookings (Paginated)**

- **Method**: `GetAll(page, pageSize)`
- **Status Codes**:
  - `200 OK` - Bookings retrieved successfully
  - `400 Bad Request` - Invalid pagination parameters
  - `500 Internal Server Error` - Unexpected error

- **Parameters**:
  - `page` (int, query): Page number (1-based). Default: 1
  - `pageSize` (int, query): Items per page (1-100). Default: 10

- **Response**:
```json
{
  "items": [
    {
      "id": 1,
      "bookingReference": "ABC123XYZ",
      "flightId": 5,
      "flightNumber": "AA100",
      "userId": 1,
      "passengerCount": 2,
      "totalPrice": 599.98,
      "status": "Confirmed",
      "createdAt": "2026-04-11T10:30:00Z",
      "updatedAt": "2026-04-11T10:35:00Z",
      "passengers": [...],
      "notes": "Window seats preferred"
    }
  ],
  "total": 150,
  "page": 1,
  "pageSize": 10,
  "totalPages": 15
}
```

- **Example Requests**:
```bash
# Get first page with default size
GET /api/v1/bookings

# Get page 3 with custom size
GET /api/v1/bookings?page=3&pageSize=20

# Invalid: page < 1
GET /api/v1/bookings?page=0&pageSize=10
# Response 400: "Page number must be greater than 0."

# Invalid: pageSize > 100
GET /api/v1/bookings?page=1&pageSize=150
# Response 400: "Page size must be between 1 and 100."
```

---

### 2. GET /api/v1/bookings/{id}
**Get Booking by ID**

- **Method**: `GetById(id)`
- **Status Codes**:
  - `200 OK` - Booking found
  - `400 Bad Request` - Invalid booking ID
  - `404 Not Found` - Booking not found
  - `500 Internal Server Error` - Unexpected error

- **Parameters**:
  - `id` (int, path): The booking ID to retrieve

- **Response** (200 OK):
```json
{
  "id": 1,
  "bookingReference": "ABC123XYZ",
  "flightId": 5,
  "flightNumber": "AA100",
  "userId": 1,
  "passengerCount": 2,
  "totalPrice": 599.98,
  "status": "Confirmed",
  "createdAt": "2026-04-11T10:30:00Z",
  "updatedAt": "2026-04-11T10:35:00Z",
  "passengers": [
    {
      "id": 1,
      "firstName": "John",
      "lastName": "Doe",
      "dateOfBirth": "1985-06-15T00:00:00Z",
      "email": "john@example.com",
      "phoneNumber": "+1-555-0123"
    },
    {
      "id": 2,
      "firstName": "Jane",
      "lastName": "Doe",
      "dateOfBirth": "1988-03-22T00:00:00Z",
      "email": "jane@example.com",
      "phoneNumber": "+1-555-0124"
    }
  ],
  "notes": "Window seats preferred"
}
```

- **Error Response** (404 Not Found):
```json
{
  "message": "Booking with ID 999 was not found."
}
```

- **Example Requests**:
```bash
# Valid booking
GET /api/v1/bookings/1
# Response 200: {...booking details...}

# Booking not found
GET /api/v1/bookings/999
# Response 404: {"message": "Booking with ID 999 was not found."}

# Invalid ID
GET /api/v1/bookings/0
# Response 400: {"message": "Booking ID must be greater than 0."}
```

---

### 3. POST /api/v1/bookings
**Create New Booking**

- **Method**: `Create(dto)`
- **Status Codes**:
  - `201 Created` - Booking created successfully (includes Location header)
  - `400 Bad Request` - Invalid input or validation error
  - `404 Not Found` - Flight not found
  - `500 Internal Server Error` - Unexpected error

- **Request Body**:
```json
{
  "flightId": 5,
  "passengerCount": 2,
  "notes": "Window seats preferred",
  "passengers": [
    {
      "firstName": "John",
      "lastName": "Doe",
      "dateOfBirth": "1985-06-15",
      "passportNumber": "AB123456789",
      "nationality": "US",
      "email": "john@example.com",
      "phoneNumber": "+1-555-0123"
    },
    {
      "firstName": "Jane",
      "lastName": "Doe",
      "dateOfBirth": "1988-03-22",
      "passportNumber": "CD987654321",
      "nationality": "US",
      "email": "jane@example.com",
      "phoneNumber": "+1-555-0124"
    }
  ]
}
```

- **Success Response** (201 Created):
```
Location: /api/v1/bookings/1

{
  "id": 1,
  "bookingReference": "ABC123XYZ",
  "flightId": 5,
  "flightNumber": "AA100",
  "userId": 1,
  "passengerCount": 2,
  "totalPrice": 599.98,
  "status": "Pending",
  "createdAt": "2026-04-11T10:30:00Z",
  "updatedAt": "2026-04-11T10:30:00Z",
  "passengers": [...],
  "notes": "Window seats preferred"
}
```

- **Error Examples**:
```json
// Invalid flight ID
{
  "message": "Flight ID must be greater than 0."
}

// No passengers
{
  "message": "At least one passenger is required."
}

// Passenger count mismatch
{
  "message": "Passenger count must match the number of passengers provided."
}

// Flight not found
{
  "message": "Flight with ID 999 was not found."
}

// Insufficient seats
{
  "message": "Insufficient seats available. Requested: 5, Available: 3"
}
```

- **Validation Rules**:
  - ✓ Flight ID must be > 0
  - ✓ Passenger count must be >= 1
  - ✓ At least 1 passenger required
  - ✓ Passenger count must match passengers array length
  - ✓ Flight must exist and be active
  - ✓ Flight must have sufficient available seats
  - ✓ Each passenger must have valid details

---

### 4. DELETE /api/v1/bookings/{id}
**Cancel Booking**

- **Method**: `Cancel(id)`
- **Status Codes**:
  - `200 OK` - Booking cancelled successfully
  - `400 Bad Request` - Invalid booking state
  - `401 Unauthorized` - User not authorized
  - `404 Not Found` - Booking not found
  - `500 Internal Server Error` - Unexpected error

- **Parameters**:
  - `id` (int, path): The booking ID to cancel

- **Response** (200 OK):
```json
{
  "id": 1,
  "bookingReference": "ABC123XYZ",
  "flightId": 5,
  "flightNumber": "AA100",
  "userId": 1,
  "passengerCount": 2,
  "totalPrice": 599.98,
  "status": "Cancelled",
  "createdAt": "2026-04-11T10:30:00Z",
  "updatedAt": "2026-04-11T11:00:00Z",
  "passengers": [...],
  "notes": "Window seats preferred"
}
```

- **Error Scenarios**:
```json
// Booking already cancelled
{
  "message": "Booking 1 has already been cancelled."
}

// Already checked in (cannot cancel)
{
  "message": "Cannot cancel booking with status 'CheckedIn'."
}

// User not authorized
{
  "message": "You are not authorized to cancel this booking."
}

// Booking not found
{
  "message": "Booking with ID 999 was not found."
}
```

- **Business Logic**:
  1. Validates booking exists
  2. Checks user authorization
  3. Validates booking status allows cancellation
  4. Cancels the booking
  5. Releases reserved seats
  6. Initiates refund process
  7. Returns updated booking

---

### 5. PUT /api/v1/bookings/{id}/check-in
**Check In Passenger**

- **Method**: `CheckIn(id)`
- **Status Codes**:
  - `200 OK` - Check-in successful
  - `400 Bad Request` - Invalid booking state
  - `401 Unauthorized` - User not authorized
  - `404 Not Found` - Booking not found
  - `500 Internal Server Error` - Unexpected error

- **Parameters**:
  - `id` (int, path): The booking ID to check in

- **Response** (200 OK):
```json
{
  "id": 1,
  "bookingReference": "ABC123XYZ",
  "flightId": 5,
  "flightNumber": "AA100",
  "userId": 1,
  "passengerCount": 2,
  "totalPrice": 599.98,
  "status": "CheckedIn",
  "createdAt": "2026-04-11T10:30:00Z",
  "updatedAt": "2026-04-11T11:30:00Z",
  "passengers": [...],
  "notes": "Window seats preferred"
}
```

- **Error Scenarios**:
```json
// Booking not confirmed (must be Confirmed to check in)
{
  "message": "Cannot checkin booking with status 'Pending'."
}

// User not authorized
{
  "message": "You are not authorized to check in this booking."
}

// Booking already checked in
{
  "message": "Cannot checkin booking with status 'CheckedIn'."
}

// Booking cancelled
{
  "message": "Cannot checkin booking with status 'Cancelled'."
}
```

- **Business Logic**:
  1. Validates booking exists
  2. Checks user authorization
  3. Validates booking status is "Confirmed"
  4. Updates booking status to "CheckedIn"
  5. Returns updated booking

---

## 🔧 Implementation Details

### Error Handling Strategy

Following the `01_API_LAYER_GUIDE.md` patterns:

```csharp
try
{
    // 1. Input validation
    if (!ModelState.IsValid)
        return BadRequest(...);
    
    // 2. Call service
    var result = await _service.MethodAsync(...);
    
    // 3. Return success
    return Ok(result);
}
catch (NotFoundException ex)
{
    _logger.LogWarning(ex, "Resource not found");
    return NotFound(...);
}
catch (ValidationException ex)
{
    _logger.LogWarning(ex, "Validation error");
    return BadRequest(...);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return StatusCode(500, ...);
}
```

### Exception Mapping

| Exception | HTTP Status | Message |
|-----------|-------------|---------|
| `BookingNotFoundException` | 404 | Booking with ID {id} was not found. |
| `FlightNotFoundException` | 404 | Flight with ID {id} was not found. |
| `InsufficientSeatsException` | 400 | Insufficient seats available... |
| `InvalidBookingStatusException` | 400 | Cannot {operation} booking with status... |
| `BookingAlreadyCancelledException` | 400 | Booking {id} has already been cancelled. |
| `ValidationException` | 400 | Validation error in {field}... |
| `UnauthorizedAccessException` | 401 | You are not authorized... |
| Other Exceptions | 500 | An error occurred while... |

### Dependency Injection

```csharp
public BookingsController(
    IBookingService bookingService,
    ILogger<BookingsController> logger)
{
    _bookingService = bookingService;
    _logger = logger;
}
```

### Logging Strategy

- **INFO**: Important operations (GetAll, GetById, Create, Cancel, CheckIn)
- **WARNING**: Validation errors, not found errors, authorization failures
- **ERROR**: Unexpected exceptions with full exception details

```csharp
_logger.LogInformation("Creating booking for user {UserId} on flight {FlightId}", userId, flightId);
_logger.LogWarning(ex, "Booking not found with ID: {BookingId}", bookingId);
_logger.LogError(ex, "Error creating booking");
```

---

## 📝 Validation Layers

### Level 1: Input Validation (Controller)
- `page > 0` and `pageSize 1-100`
- `id > 0`
- `ModelState.IsValid`
- Required fields present
- Passenger count > 0
- Passenger count matches array length

### Level 2: Business Validation (Service)
- Flight exists and is active
- User exists
- Flight has sufficient seats
- Booking status allows operation
- User owns the booking (authorization)
- Passenger details valid

---

## 🔐 Authorization

**Current Implementation**: Placeholder (userId = 1)

**TODO**: Extract from JWT token claims
```csharp
var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
```

All operations should validate the authenticated user owns/can access the resource.

---

## 📊 Response Format

### Success (2xx)
```json
{
  "id": 1,
  "bookingReference": "ABC123XYZ",
  ...data fields...
}
```

### Pagination Success (2xx)
```json
{
  "items": [...],
  "total": 150,
  "page": 1,
  "pageSize": 10,
  "totalPages": 15
}
```

### Error (4xx/5xx)
```json
{
  "message": "Human-readable error description",
  "errors": { ... } // Optional detailed errors
}
```

---

## 🚀 Usage Examples

### Example 1: Create Booking
```bash
POST /api/v1/bookings
Content-Type: application/json

{
  "flightId": 5,
  "passengerCount": 2,
  "passengers": [
    {
      "firstName": "John",
      "lastName": "Doe",
      "dateOfBirth": "1985-06-15",
      "email": "john@example.com"
    },
    {
      "firstName": "Jane",
      "lastName": "Doe",
      "dateOfBirth": "1988-03-22",
      "email": "jane@example.com"
    }
  ]
}
```

### Example 2: Get All Bookings (Page 2)
```bash
GET /api/v1/bookings?page=2&pageSize=20
```

### Example 3: Check In
```bash
PUT /api/v1/bookings/1/check-in
```

### Example 4: Cancel Booking
```bash
DELETE /api/v1/bookings/1
```

---

## 🔄 Request Flow

```
┌─ Client Request
│
├─ Route Matching → BookingsController
│
├─ Dependency Injection
│   ├─ IBookingService
│   └─ ILogger<BookingsController>
│
├─ Input Validation
│   ├─ Parameter validation
│   └─ ModelState validation
│
├─ Try Block
│   ├─ Call Service Method
│   ├─ Log success
│   └─ Return Response
│
└─ Catch Block
    ├─ Catch specific exceptions
    ├─ Log error with context
    ├─ Determine HTTP status code
    └─ Return error response
```

---

## ✅ Build Status

✅ **Compilation**: Successful  
✅ **Dependencies**: All resolved  
✅ **Ready**: For integration testing

---

## 📚 Related Files

- **Service Interface**: `Application/Interfaces/IBookingService.cs`
- **DTOs**: `Application/DTOs/Booking*Dto.cs`
- **Exceptions**: `Domain/Exceptions/`
- **Guide**: `.github/backend/01_API_LAYER_GUIDE.md`

---

**Version**: 1.0 | **Status**: Complete ✅
