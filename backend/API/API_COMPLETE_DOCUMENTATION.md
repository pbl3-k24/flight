# 📋 FLIGHT TICKET BOOKING API - COMPLETE DOCUMENTATION

## **1. GENERAL INFORMATION**

- **Project Name**: Flight Ticket Booking API
- **Language**: C# (.NET 10)
- **Architecture**: Clean Architecture (Domain/Application/Infrastructure)
- **Database**: PostgreSQL (Npgsql)
- **Caching**: Redis
- **API Documentation**: Swagger/OpenAPI
- **Version**: v1

---

## **2. TECHNOLOGY STACK**

| Layer | Technology |
|-------|-----------|
| **Web Framework** | ASP.NET Core 10.0 |
| **ORM** | Entity Framework Core 8.0+ |
| **Database** | PostgreSQL |
| **Caching** | Redis (StackExchange.Redis) |
| **API Docs** | Swagger/OpenAPI, Swashbuckle |
| **Logging** | Built-in ILogger |
| **Error Handling** | Custom Exception Hierarchy |

---

## **3. DIRECTORY STRUCTURE**

```
API/
├── Controllers/              # HTTP Controllers (API Endpoints)
│   └── BookingsController.cs
├── Application/              # Business Logic Layer
│   ├── Services/
│   │   ├── FlightService.cs
│   │   └── BookingService.cs
│   ├── Interfaces/
│   │   ├── IFlightService.cs
│   │   ├── IBookingService.cs
│   │   ├── ICacheService.cs
│   │   ├── IFlightRepository.cs
│   │   ├── IBookingRepository.cs
│   │   ├── IUserRepository.cs
│   │   ├── IPassengerRepository.cs
│   │   ├── IPaymentService.cs
│   │   └── IEmailService.cs
│   └── DTOs/                 # Data Transfer Objects
│       ├── FlightCreateDto.cs
│       ├── FlightSearchDto.cs
│       ├── FlightResponseDto.cs
│       ├── FlightUpdateDto.cs
│       ├── BookingCreateDto.cs
│       ├── BookingResponseDto.cs
│       ├── PaginatedBookingsResponseDto.cs
│       ├── PassengerCreateDto.cs
│       └── PassengerResponseDto.cs
├── Domain/                   # Core Business Logic
│   ├── Entities/             # Domain Models
│   │   ├── Flight.cs
│   │   ├── Booking.cs
│   │   ├── User.cs
│   │   ├── Passenger.cs
│   │   ├── Payment.cs
│   │   ├── Airport.cs
│   │   ├── CrewMember.cs
│   │   └── FlightCrew.cs
│   ├── Enums/
│   │   ├── FlightStatus.cs
│   │   ├── BookingStatus.cs
│   │   ├── PaymentStatus.cs
│   │   ├── PaymentMethod.cs
│   │   └── UserStatus.cs
│   └── Exceptions/           # Custom Exceptions
│       ├── DomainException.cs
│       ├── ValidationException.cs
│       ├── FlightNotFoundException.cs
│       ├── BookingNotFoundException.cs
│       ├── InsufficientSeatsException.cs
│       ├── BookingAlreadyCancelledException.cs
│       ├── InvalidBookingStatusException.cs
│       └── NotFoundException.cs
└── Infrastructure/           # Data Access Layer
    ├── Data/
    │   └── FlightBookingDbContext.cs
    ├── Repositories/
    │   ├── BaseRepository.cs
    │   └── FlightRepository.cs
    ├── Configurations/       # EF Core Configurations
    │   ├── FlightConfiguration.cs
    │   ├── BookingConfiguration.cs
    │   ├── PaymentConfiguration.cs
    │   ├── UserConfiguration.cs
    │   ├── AirportAndPassengerConfiguration.cs
    │   └── CrewMemberAndFlightCrewConfiguration.cs
    └── Caching/
        └── InMemoryCacheService.cs
```

---

## **4. DOMAIN ENTITIES & RELATIONSHIPS**

### **Flight Entity (Chuyến Bay)**

```csharp
Properties:
  - Id: int (Primary Key)
  - FlightNumber: string (e.g., "AA100")
  - DepartureAirportId: int (FK)
  - ArrivalAirportId: int (FK)
  - DepartureTime: DateTime
  - ArrivalTime: DateTime
  - Airline: string
  - AircraftModel: string
  - TotalSeats: int
  - AvailableSeats: int
  - BasePrice: decimal
  - Status: FlightStatus (Active, Cancelled, Delayed, Completed)
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

Relationships:
  - 1:N with Booking
  - N:M with CrewMember (via FlightCrew)
  - N:1 with Airport (Departure)
  - N:1 with Airport (Arrival)

Business Logic Methods:
  - CanBook(seatCount): Check if flight can accommodate booking
  - ReserveSeats(count): Reserve seats for booking
  - ReleaseSeats(count): Release seats (on cancellation)
  - IsDepartureSoon(hours): Check if departure is soon
  - Cancel(): Cancel flight
  - MarkAsDelayed(): Mark flight as delayed
  - MarkAsCompleted(): Mark flight as completed
```

### **Booking Entity (Đặt vé)**

```csharp
Properties:
  - Id: int (Primary Key)
  - FlightId: int (FK)
  - UserId: int (FK)
  - BookingReference: string (Unique - e.g., "ABC123XYZ")
  - PassengerCount: int
  - TotalPrice: decimal
  - Status: BookingStatus (Pending, Confirmed, CheckedIn, Cancelled)
  - CreatedAt: DateTime
  - UpdatedAt: DateTime
  - CancelledAt: DateTime? (null if not cancelled)
  - Notes: string? (Special requests)

Relationships:
  - N:1 with Flight
  - N:1 with User
  - 1:N with Passenger
  - 1:1 with Payment

Business Logic Methods:
  - CanCancel(currentDateTime): Check if booking can be cancelled
    (Must be Confirmed and at least 24h before departure)
```

### **User Entity (Người dùng)**

```csharp
Properties:
  - Id: int (Primary Key)
  - Email: string (Unique)
  - FullName: string
  - Phone: string?
  - Status: UserStatus (Active, Inactive, Suspended)
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

Relationships:
  - 1:N with Booking
```

### **Passenger Entity (Hành khách)**

```csharp
Properties:
  - Id: int (Primary Key)
  - BookingId: int (FK)
  - FirstName: string
  - LastName: string
  - DateOfBirth: DateOnly?
  - Nationality: string?
  - PassportNumber: string?
  - SeatNumber: string?
  - CheckInStatus: bool

Relationships:
  - N:1 with Booking
```

### **Payment Entity (Thanh toán)**

```csharp
Properties:
  - Id: int (Primary Key)
  - BookingId: int (FK)
  - Amount: decimal
  - PaymentMethod: PaymentMethod (Card, Bank, Cash)
  - Status: PaymentStatus (Pending, Completed, Failed, Refunded)
  - TransactionId: string?
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

Relationships:
  - 1:1 with Booking
```

### **Airport Entity (Sân bay)**

```csharp
Properties:
  - Id: int (Primary Key)
  - Code: string (Unique - e.g., "SYD", "LAX")
  - Name: string
  - City: string
  - Country: string
  - Timezone: string?
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

Relationships:
  - 1:N with Flight (as DepartureAirport)
  - 1:N with Flight (as ArrivalAirport)
```

### **CrewMember & FlightCrew Entities**

```csharp
CrewMember:
  - Id: int (Primary Key)
  - FirstName: string
  - LastName: string
  - Role: string (Pilot, Flight Attendant, etc.)
  - EmployeeNumber: string (Unique)
  - Status: UserStatus
  - CreatedAt: DateTime
  - UpdatedAt: DateTime

FlightCrew (Junction Table):
  - FlightId: int (FK, Part of PK)
  - CrewMemberId: int (FK, Part of PK)
  - AssignedAt: DateTime
```

---

## **5. ENUM DEFINITIONS**

### **FlightStatus**
```csharp
Active      = 0,    // Flight is available for booking
Cancelled   = 1,    // Flight has been cancelled
Delayed     = 2,    // Flight is delayed
Completed   = 3     // Flight has completed
```

### **BookingStatus**
```csharp
Pending     = 0,    // Booking created but not confirmed
Confirmed   = 1,    // Booking confirmed and payment successful
CheckedIn   = 2,    // Passengers have checked in
Cancelled   = 3     // Booking has been cancelled
```

### **PaymentStatus**
```csharp
Pending     = 0,    // Payment not yet processed
Completed   = 1,    // Payment successful
Failed      = 2,    // Payment failed
Refunded    = 3     // Payment refunded (on booking cancellation)
```

### **PaymentMethod**
```csharp
Card        = 0,    // Credit/Debit Card
Bank        = 1,    // Bank Transfer
Cash        = 2     // Cash Payment
```

### **UserStatus**
```csharp
Active      = 0,    // User account is active
Inactive    = 1,    // User account is inactive
Suspended   = 2     // User account is suspended
```

---

## **6. API ENDPOINTS**

### **Base URL**: `http://localhost:5000/api/v1`

---

### **🎫 Bookings Controller** (`/api/v1/bookings`)

#### **1. GET /api/v1/bookings**
Get all bookings with pagination.

**Query Parameters:**
- `page` (int): Page number (default: 1, min: 1)
- `pageSize` (int): Items per page (default: 10, max: 100)

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": 1,
      "flightId": 5,
      "userId": 2,
      "bookingReference": "ABC123XYZ",
      "passengerCount": 2,
      "totalPrice": 500.00,
      "status": "Confirmed",
      "createdAt": "2024-01-15T10:30:00Z",
      "passengers": [
        {
          "id": 1,
          "firstName": "John",
          "lastName": "Doe",
          "seatNumber": "12A"
        }
      ]
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "total": 50,
  "hasNextPage": true
}
```

**Error Responses:**
- `400`: Invalid page or pageSize
- `500`: Internal server error

---

#### **2. GET /api/v1/bookings/{id}**
Get a specific booking by ID.

**Path Parameters:**
- `id` (int): Booking ID

**Response (200 OK):**
```json
{
  "id": 1,
  "flightId": 5,
  "userId": 2,
  "bookingReference": "ABC123XYZ",
  "passengerCount": 2,
  "totalPrice": 500.00,
  "status": "Confirmed",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T12:00:00Z",
  "flight": {
    "id": 5,
    "flightNumber": "AA100",
    "departureTime": "2024-02-15T14:00:00Z",
    "arrivalTime": "2024-02-15T18:00:00Z"
  },
  "passengers": [
    {
      "id": 1,
      "firstName": "John",
      "lastName": "Doe",
      "dateOfBirth": "1990-01-15",
      "nationality": "Vietnamese",
      "passportNumber": "AB123456",
      "seatNumber": "12A",
      "checkInStatus": true
    }
  ]
}
```

**Error Responses:**
- `400`: Invalid booking ID
- `404`: Booking not found
- `500`: Internal server error

---

#### **3. POST /api/v1/bookings**
Create a new booking.

**Request Body:**
```json
{
  "flightId": 5,
  "passengerCount": 2,
  "passengers": [
    {
      "firstName": "John",
      "lastName": "Doe",
      "dateOfBirth": "1990-01-15",
      "nationality": "Vietnamese",
      "passportNumber": "AB123456"
    },
    {
      "firstName": "Jane",
      "lastName": "Doe",
      "dateOfBirth": "1992-03-20",
      "nationality": "Vietnamese",
      "passportNumber": "AB789012"
    }
  ]
}
```

**Response (201 Created):**
```json
{
  "id": 1,
  "bookingReference": "ABC123XYZ",
  "flightId": 5,
  "userId": 1,
  "passengerCount": 2,
  "totalPrice": 500.00,
  "status": "Pending",
  "createdAt": "2024-01-15T10:30:00Z",
  "passengers": [
    {
      "id": 1,
      "firstName": "John",
      "lastName": "Doe",
      "seatNumber": null
    }
  ]
}
```

**Error Responses:**
- `400`: Invalid data, insufficient seats, or flight ID is invalid
- `404`: Flight not found
- `422`: Validation error
- `500`: Internal server error

**Validation Rules:**
- flightId must be > 0
- passengerCount must be > 0 and >= passengers.Count
- At least one passenger is required
- Passenger count must match provided passengers list

---

#### **4. DELETE /api/v1/bookings/{id}**
Cancel an existing booking.

**Path Parameters:**
- `id` (int): Booking ID

**Response (200 OK):**
```json
{
  "id": 1,
  "bookingReference": "ABC123XYZ",
  "flightId": 5,
  "userId": 1,
  "status": "Cancelled",
  "cancelledAt": "2024-01-16T11:00:00Z"
}
```

**Error Responses:**
- `400`: Invalid booking ID
- `404`: Booking not found
- `409`: Booking already cancelled or cannot be cancelled
- `500`: Internal server error

**Cancellation Rules:**
- Booking must be in "Confirmed" status
- Must be at least 24 hours before flight departure
- Seats will be released back to the flight
- Refund process will be initiated

---

### **✈️ Health Check**

#### **GET /health**
Check API health status.

**Response (200 OK):**
```json
{
  "status": "Healthy"
}
```

---

### **📖 Swagger UI**

**Development Mode:**
- Swagger UI: `http://localhost:5000/` (at root)
- API JSON: `http://localhost:5000/swagger/v1/swagger.json`

---

## **7. APPLICATION SERVICES**

### **IFlightService**

**Interface Methods:**
```csharp
Task<FlightResponseDto> GetFlightAsync(int id);
Task<IEnumerable<FlightResponseDto>> SearchFlightsAsync(FlightSearchDto criteria);
Task<FlightResponseDto> CreateFlightAsync(FlightCreateDto dto);
Task<FlightResponseDto> UpdateFlightAsync(int id, FlightUpdateDto dto);
Task CancelFlightAsync(int id);
```

**Features:**
- Automatic caching
- Business rules validation
- Seat availability management
- Flight status transitions

---

### **IBookingService**

**Interface Methods:**
```csharp
Task<PaginatedBookingsResponseDto> GetAllBookingsAsync(int page, int pageSize);
Task<BookingResponseDto> GetBookingByIdAsync(int bookingId);
Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto dto, int userId);
Task<BookingResponseDto> CancelBookingAsync(int bookingId, int userId);
```

**Features:**
- Flight availability validation
- User existence validation
- Unique booking reference generation
- Seat reservation and release
- Refund processing
- Authorization checks

---

### **ICacheService**

**Interface Methods:**
```csharp
Task<T?> GetAsync<T>(string key);
Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
Task RemoveAsync(string key);
Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
```

**Features:**
- In-memory caching (development)
- Redis caching (production)
- Cache invalidation
- Expiry management

---

## **8. REPOSITORY PATTERN**

### **IRepository<T> (Generic Interface)**

**Methods:**
```csharp
Task<T> GetByIdAsync(int id);
Task<IEnumerable<T>> GetAllAsync();
Task<T> CreateAsync(T entity);
Task<T> UpdateAsync(T entity);
Task<bool> DeleteAsync(int id);
```

---

### **IFlightRepository**

**Additional Methods:**
```csharp
Task<Flight?> GetFlightWithSeatsAsync(int id);
Task<IEnumerable<Flight>> SearchFlightsAsync(FlightSearchDto criteria);
Task<IEnumerable<Flight>> GetFlightsByStatusAsync(FlightStatus status);
```

---

## **9. DATABASE SCHEMA (PostgreSQL)**

### **Tables Structure**

```sql
Flights:
  - id (PK)
  - flight_number (UNIQUE)
  - departure_airport_id (FK)
  - arrival_airport_id (FK)
  - departure_time
  - arrival_time
  - airline
  - aircraft_model
  - total_seats
  - available_seats
  - base_price
  - status
  - created_at
  - updated_at

Bookings:
  - id (PK)
  - flight_id (FK)
  - user_id (FK)
  - booking_reference (UNIQUE)
  - passenger_count
  - total_price
  - status
  - created_at
  - updated_at
  - cancelled_at (nullable)
  - notes (nullable)

Users:
  - id (PK)
  - email (UNIQUE)
  - full_name
  - phone (nullable)
  - status
  - created_at
  - updated_at

Passengers:
  - id (PK)
  - booking_id (FK)
  - first_name
  - last_name
  - date_of_birth (nullable)
  - nationality (nullable)
  - passport_number (nullable)
  - seat_number (nullable)
  - check_in_status

Payments:
  - id (PK)
  - booking_id (FK, UNIQUE)
  - amount
  - payment_method
  - status
  - transaction_id (nullable, UNIQUE)
  - created_at
  - updated_at

Airports:
  - id (PK)
  - code (UNIQUE)
  - name
  - city
  - country
  - timezone (nullable)
  - created_at
  - updated_at

CrewMembers:
  - id (PK)
  - first_name
  - last_name
  - role
  - employee_number (UNIQUE)
  - status
  - created_at
  - updated_at

FlightCrews:
  - flight_id (FK, PK)
  - crew_member_id (FK, PK)
  - assigned_at
```

### **Indexes**
```sql
- flights.flight_number (UNIQUE)
- bookings.flight_id
- bookings.user_id
- bookings.booking_reference (UNIQUE)
- users.email (UNIQUE)
- passengers.booking_id
- payments.booking_id (UNIQUE)
- payments.transaction_id (UNIQUE)
- airports.code (UNIQUE)
- crew_members.employee_number (UNIQUE)
```

---

## **10. CONFIGURATION & SETUP**

### **appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=flight_booking;Username=postgres;Password=your_password"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "Jwt": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "flight-booking-api",
    "Audience": "flight-booking-clients",
    "ExpirationMinutes": 60
  }
}
```

### **Program.cs Configuration**

**Services Registered:**
- DbContext (EF Core + Npgsql with retry policy)
- Redis Caching
- CORS (AllowAll - requires adjustment for production)
- Swagger/OpenAPI
- Controllers
- Custom Services (Scoped DI)

**Middleware Pipeline:**
- Swagger/SwaggerUI (Development only)
- CORS
- HTTPS Redirection
- Controller Mapping
- Health Check Endpoint

**Initialization:**
- Auto database migration on startup
- Error handling with logging

---

## **11. EXCEPTION HANDLING**

### **Custom Exception Hierarchy**

```csharp
DomainException (Base)
  ├── ValidationException
  ├── FlightNotFoundException
  ├── BookingNotFoundException
  ├── InsufficientSeatsException
  ├── BookingAlreadyCancelledException
  ├── InvalidBookingStatusException
  └── NotFoundException
```

### **HTTP Error Response Format**

```json
// 400 Bad Request
{
  "message": "Invalid booking data.",
  "errors": {
    "field": ["error message"]
  }
}

// 404 Not Found
{
  "message": "Flight not found."
}

// 409 Conflict
{
  "message": "Booking has already been cancelled."
}

// 422 Unprocessable Entity
{
  "message": "Validation failed.",
  "errors": {
    "passengerCount": ["must be greater than 0"]
  }
}

// 500 Internal Server Error
{
  "message": "An error occurred while processing your request."
}
```

---

## **12. CORS CONFIGURATION**

**Current Policy: AllowAll**
- Allows any origin
- Allows any HTTP method
- Allows any header

⚠️ **⚠️ WARNING**: This is NOT suitable for production.

**Recommended for Production:**
```csharp
options.AddPolicy("Production", policy =>
{
    policy.WithOrigins("https://yourdomain.com", "https://app.yourdomain.com")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
});
```

---

## **13. LOGGING STRATEGY**

**Log Levels:**
- **Information**: Normal operations (bookings, flights)
- **Warning**: Expected errors (404, validation failures)
- **Error**: Critical errors (database failures, exceptions)

**Logged Operations:**
- Booking creation, retrieval, cancellation
- Flight operations
- Database migration status
- Request/response data
- Error stack traces
- Authorization/Authentication events

---

## **14. CACHING STRATEGY**

**Cache Configuration:**
- **Flight lists**: TTL 5-15 minutes
- **Flight details**: TTL 5-15 minutes
- **Booking data**: NO CACHE (real-time)
- **Seat availability**: NO CACHE (real-time)

**Cache Key Format:**
```
flight-booking:{entity}:{id}
Example: flight-booking:flight:5
```

**Cache Invalidation Triggers:**
- Flight creation/update/cancellation
- Booking creation/cancellation (invalidates flight cache)

---

## **15. SECURITY NOTES**

### **Currently Implemented**
- ✅ HTTPS Redirection
- ✅ CORS Configuration
- ✅ Input validation

### **Not Yet Implemented**
- ❌ Authentication (JWT/OAuth)
- ❌ Authorization (Role-based access control)
- ❌ Rate limiting
- ❌ API key validation
- ❌ Advanced input sanitization

### **TODO for Production**
```csharp
// Add JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* config */ });

// Add authorization policies
builder.Services.AddAuthorization();

// Add rate limiting
builder.Services.AddRateLimiting();
```

---

## **16. DEVELOPMENT WORKFLOW**

### **Database Setup**
```bash
# Create migrations
dotnet ef migrations add InitialCreate

# Apply migrations
dotnet ef database update
```

### **Run Application**
```bash
# Development mode
dotnet run

# Production mode
dotnet publish -c Release
```

### **Testing Endpoints**

**Health Check:**
```bash
curl http://localhost:5000/health
```

**Get All Bookings:**
```bash
curl http://localhost:5000/api/v1/bookings?page=1&pageSize=10
```

**Get Single Booking:**
```bash
curl http://localhost:5000/api/v1/bookings/1
```

**Create Booking:**
```bash
curl -X POST http://localhost:5000/api/v1/bookings \
  -H "Content-Type: application/json" \
  -d '{
    "flightId": 1,
    "passengerCount": 2,
    "passengers": [
      {"firstName": "John", "lastName": "Doe"},
      {"firstName": "Jane", "lastName": "Doe"}
    ]
  }'
```

**Cancel Booking:**
```bash
curl -X DELETE http://localhost:5000/api/v1/bookings/1
```

---

## **17. KEY FILES LOCATION**

| File | Path |
|------|------|
| **Entry Point** | `API/Program.cs` |
| **Main Controller** | `API/Controllers/BookingsController.cs` |
| **Flight Service** | `API/Application/Services/FlightService.cs` |
| **Booking Service** | `API/Application/Services/BookingService.cs` |
| **DbContext** | `API/Infrastructure/Data/FlightBookingDbContext.cs` |
| **Flight Repository** | `API/Infrastructure/Repositories/FlightRepository.cs` |
| **Base Repository** | `API/Infrastructure/Repositories/BaseRepository.cs` |
| **Cache Service** | `API/Infrastructure/Caching/InMemoryCacheService.cs` |
| **Domain Entities** | `API/Domain/Entities/` |
| **DTOs** | `API/Application/DTOs/` |
| **Exceptions** | `API/Domain/Exceptions/` |
| **Enums** | `API/Domain/Enums/` |
| **EF Configurations** | `API/Infrastructure/Configurations/` |
| **Configuration** | `API/appsettings.json` |

---

## **18. DATA FLOW DIAGRAM**

```
Client Request
    ↓
HTTP Controller (BookingsController)
    ↓
Application Service (IBookingService)
    ├─→ Repository (IBookingRepository) → Database
    ├─→ Repository (IFlightRepository) → Database
    ├─→ Cache Service (ICacheService)
    └─→ Domain Logic (Booking.CanCancel, Flight.ReserveSeats)
    ↓
DTOs (BookingResponseDto, etc.)
    ↓
HTTP Response (JSON)
    ↓
Client
```

---

## **19. IMPORTANT BUSINESS RULES**

### **Flight Rules**
- A flight must have a departure time before arrival time
- Departure airport must be different from arrival airport
- Available seats cannot exceed total seats
- Only Active flights can accept new bookings
- Flight status can only transition in valid ways:
  - Active → Cancelled, Delayed, Completed
  - Delayed → Active, Completed
  - Cancelled → (no transitions)
  - Completed → (no transitions)

### **Booking Rules**
- Booking reference must be unique
- Passenger count must match the number of provided passengers
- At least one passenger is required
- Only Confirmed bookings can be checked in
- Booking can only be cancelled if:
  - Status is Confirmed
  - At least 24 hours before flight departure
- User must own the booking to cancel it

### **Seat Management**
- When booking is created: seats are reserved
- When booking is cancelled: seats are released
- Available seats cannot go below 0 or above total seats

---

## **20. API RESPONSE TIME & PERFORMANCE**

**Expected Response Times:**
- GET requests: < 100ms (with cache: < 10ms)
- POST requests: < 500ms
- DELETE requests: < 500ms

**Optimization Techniques:**
- Caching frequently accessed data
- Pagination for large datasets
- EF Core query optimization
- Database indexing on foreign keys and unique fields
- Connection pooling via Entity Framework Core

---

## **21. DEPLOYMENT CONSIDERATIONS**

### **Environment-Specific Configuration**

**Development:**
- Swagger enabled
- In-memory caching
- Detailed logging
- CORS: AllowAll

**Production:**
- Swagger disabled
- Redis caching
- Minimal logging
- CORS: Specific origins only
- HTTPS enforced
- Authentication/Authorization enabled
- Rate limiting enabled

---

## **22. MONITORING & LOGGING**

**Metrics to Monitor:**
- API response time
- Database query performance
- Cache hit/miss rates
- Error rates by endpoint
- Authentication failures
- Database migration status

**Log Locations:**
- Console (during development)
- Application Insights (recommended for production)
- File logging (optional)

---

## **23. TROUBLESHOOTING GUIDE**

### **Common Issues & Solutions**

**Issue: "Connection string 'DefaultConnection' is missing"**
- Solution: Ensure `appsettings.json` has correct connection string

**Issue: Redis connection fails**
- Solution: Check Redis is running on localhost:6379
- Alternative: Ensure Redis ConnectionString in config

**Issue: Database migration fails**
- Solution: Check PostgreSQL is running
- Solution: Verify database name and credentials
- Solution: Check migrations folder exists

**Issue: Bookings not being created**
- Solution: Check flight exists and is Active
- Solution: Check flight has available seats
- Solution: Check passenger count matches provided passengers

**Issue: Booking cancellation fails**
- Solution: Check booking is in Confirmed status
- Solution: Check flight departure is > 24 hours away
- Solution: Check user authorization (owns booking)

---

## **24. FUTURE ENHANCEMENTS**

1. **Authentication & Authorization**
   - JWT token-based authentication
   - Role-based access control (Admin, Customer, Staff)
   - OAuth2/OpenID Connect integration

2. **Payment Integration**
   - Stripe/PayPal integration
   - Payment processing webhook handlers
   - Refund automation

3. **Email Notifications**
   - Booking confirmation emails
   - Cancellation confirmation emails
   - Check-in reminders

4. **Advanced Reporting**
   - Revenue reports
   - Booking analytics
   - Flight occupancy reports

5. **Seat Selection**
   - Detailed seat map
   - Seat selection during booking
   - Seat change after booking

6. **Multi-language Support**
   - i18n implementation
   - Localized error messages
   - Multiple currency support

7. **Mobile App Support**
   - Push notifications
   - Offline functionality
   - QR code check-in

---

## **25. GLOSSARY**

| Term | Definition |
|------|-----------|
| **DTO** | Data Transfer Object - used for API request/response |
| **Entity** | Domain model representing a business concept |
| **Repository** | Data access abstraction layer |
| **Service** | Business logic layer |
| **DbContext** | EF Core context managing database operations |
| **Migration** | Database schema version control |
| **CORS** | Cross-Origin Resource Sharing |
| **JWT** | JSON Web Token for authentication |
| **TTL** | Time To Live for cache expiry |
| **PK** | Primary Key |
| **FK** | Foreign Key |
| **Aggregate** | Domain-driven design pattern for entity relationships |

---

## **26. CONTACT & SUPPORT**

**Project Repository:** https://github.com/pbl3-k24/flight

**For questions or issues:** Create an issue in the GitHub repository.

---

**Document Version:** 1.0  
**Last Updated:** January 2024  
**Document Status:** Complete
