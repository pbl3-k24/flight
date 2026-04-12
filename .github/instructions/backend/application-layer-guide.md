# 🔧 Module 2: Application Layer (Business Logic) Guide

## 📋 Mục Đích
Chứa tất cả business logic, orchestration, validation, và service implementations.

---

## 🏗️ Kiến Trúc

```
API Layer (DTOs)
    ↓
[Application Services]
    ├─ Validation
    ├─ Business Logic
    ├─ Orchestration
    └─ Data Mapping
    ↓
[Domain Layer + Infrastructure]
    ├─ Repositories
    ├─ External Services
    └─ Cache
    ↓
[Responses/DTOs]
```

---

## 🎯 Service Layer Pattern

### 1. **Service Responsibilities**
- Implement business rules
- Coordinate between repositories
- Handle transactions
- Validate business constraints
- Call external services
- Manage cache
- Raise domain exceptions

**Service Logic Flow:**
```
Receive Input
    ↓
Validate business rules
    ↓
Check preconditions
    ↓
Call repositories/external services
    ↓
Execute core business logic
    ↓
Update cache if needed
    ↓
Return result or throw exception
```

### 2. **Service Interface Pattern**
- Define contracts clearly
- Group related operations
- Async methods only
- Return DTOs, not entities

**Interface Design:**
```
IFlightService
├─ GetFlightAsync(id) → FlightResponseDto
├─ SearchFlightsAsync(criteria) → List<FlightResponseDto>
├─ CreateFlightAsync(dto) → FlightResponseDto
├─ UpdateFlightAsync(id, dto) → FlightResponseDto
└─ DeleteFlightAsync(id) → void

IBookingService
├─ CreateBookingAsync(dto) → BookingResponseDto
├─ CancelBookingAsync(id) → void
├─ GetUserBookingsAsync(userId) → List<BookingResponseDto>
└─ CheckInBookingAsync(id) → void
```

---

## 💼 Business Logic Algorithms

### Algorithm 1: Flight Search
```
Input: SearchCriteria (departureAirportId, arrivalAirportId, date, ...)
Process:
  1. Validate all input parameters (not null, valid IDs)
  2. Check cache with composite key (depart_arrive_date)
  3. If cached → return cached results
  4. Query database:
     - Filter by departure & arrival airports
     - Filter by departure date
     - Filter by status = Active
     - Sort by departure time (ascending)
  5. Map results to ResponseDTO
  6. Cache results with 1-hour TTL
  7. Return results

Performance Optimization:
  - Use index on (departure_airport_id, arrival_airport_id, departure_time)
  - Cache popular routes
  - Implement pagination for large result sets
```

### Algorithm 2: Booking Creation
```
Input: BookingCreateDto (flightId, userId, passengers[])
Process:
  1. Validate DTO (all required fields present)
  2. Fetch flight:
     - If not exists → throw FlightNotFoundException
     - Check if flight status = Active
  3. Fetch user:
     - If not exists → throw UserNotFoundException
  4. Validate booking eligibility:
     - Check available_seats >= passenger count
     - Check user's booking history (fraud detection)
     - Check passenger details validity
  5. Create booking transaction:
     a. Create booking record (status = Pending)
     b. Generate unique booking reference
     c. Reserve seats (available_seats -= passenger_count)
     d. Create passenger records for each passenger
     e. Commit transaction
  6. Trigger post-booking actions:
     - Send confirmation email
     - Update cache (flight seat count)
     - Log booking event
  7. Return booking confirmation DTO

Error Handling:
  - Insufficient seats → InsufficientSeatsException (400)
  - Flight not found → FlightNotFoundException (404)
  - Invalid passengers → ValidationException (400)
  - Database error → retry or fail gracefully (500)

Concurrency:
  - Use database-level row locking during seat reservation
  - Prevent double booking race condition
```

### Algorithm 3: Booking Cancellation
```
Input: BookingId, UserId (for authorization)
Process:
  1. Fetch booking by ID
  2. Validate:
     - Booking exists
     - User owns booking (userId matches)
     - Status allows cancellation (not already cancelled)
     - Cancel window is still open (e.g., 24 hours before flight)
  3. Calculate refund:
     - Get base fare + taxes
     - Apply cancellation fees (e.g., 20% penalty)
     - Determine refund amount
  4. Update booking:
     a. Change status to Cancelled
     b. Record cancellation timestamp
     c. Update refund amount
  5. Release seats:
     - available_seats += passenger_count
  6. Trigger refund process:
     - Create refund transaction
     - Process via payment gateway
     - Update payment status
  7. Notifications:
     - Send cancellation confirmation
     - Send refund confirmation
  8. Cache invalidation:
     - Remove booking from cache
     - Update flight seat count cache

Constraints:
  - Cancellation only allowed within X hours of flight
  - Refund percentage depends on cancellation time
  - Some flight types may not allow cancellation
```

### Algorithm 4: Seat Availability Check
```
Input: FlightId, PassengerCount
Process:
  1. Get flight from cache or database
  2. Check: available_seats >= passenger_count
  3. Return: boolean (true/false) + available_seats count
  
Optimization:
  - Cache flight seat information
  - Update cache immediately after booking/cancellation
  - Periodic sync with database (every 5 minutes)
```

### Algorithm 5: Dynamic Pricing
```
Input: FlightId, BookingDate
Process:
  1. Get base price from flight
  2. Apply pricing rules:
     a. Calculate days until departure
     b. Get demand level:
        - High demand (many bookings) → multiplier 1.5x
        - Medium demand → multiplier 1.0x
        - Low demand → multiplier 0.8x
     c. Apply seat availability factor:
        - Few seats left → 1.2x multiplier
        - Average availability → 1.0x
     d. Apply booking time factor:
        - Booking far in advance → 0.9x
        - Last minute (< 7 days) → 1.3x
  3. Calculate final price:
     final_price = base_price × demand × availability × booking_time
  4. Apply caps:
     - Max 2x base price
     - Min 0.5x base price
  5. Round to nearest cent
  6. Return calculated price

Complexity:
  - O(1) - direct calculation
  - No database queries needed
```

---

## 🔄 Data Flow

### Get Operation:
```
API Request
    ↓
Service.GetAsync(id)
    ↓
Check Cache
    ├─ Hit → Return cached DTO
    └─ Miss:
        ↓
      Query Repository
        ↓
      Map Entity → DTO
        ↓
      Cache DTO (TTL = 1h)
        ↓
      Return DTO
    ↓
API Response
```

### Create Operation:
```
API Request (CreateDto)
    ↓
Service.CreateAsync(dto)
    ↓
Validate DTO (business rules)
    ↓
Start Transaction
    ├─ Save to Repository
    ├─ Update related entities
    ├─ Commit Transaction
    └─ Update Cache
    ↓
Trigger Events (email, notifications)
    ↓
Map Entity → ResponseDto
    ↓
Return ResponseDto
```

### Update Operation:
```
API Request (UpdateDto)
    ↓
Service.UpdateAsync(id, dto)
    ↓
Fetch existing entity
    ↓
Validate update eligibility
    ↓
Apply changes
    ↓
Start Transaction
    ├─ Save changes
    ├─ Commit
    └─ Invalidate Cache
    ↓
Trigger Events
    ↓
Return updated DTO
```

### Delete Operation:
```
API Request (id)
    ↓
Service.DeleteAsync(id)
    ↓
Fetch entity (verify exists)
    ↓
Check deletion eligibility
    ↓
Start Transaction
    ├─ Soft/Hard delete
    ├─ Update related records
    ├─ Commit
    └─ Invalidate Cache
    ↓
Trigger cleanup events
    ↓
Return success
```

---

## 🎯 Validation Strategy

### Input Validation
```
DTO → FluentValidator
    ├─ Required fields
    ├─ Format validation (email, phone, date)
    ├─ Length constraints
    ├─ Range constraints
    └─ Custom rules

Invalid → 400 Bad Request
Valid → Continue
```

### Business Validation
```
Service Method
    ├─ Resource exists?
    ├─ User authorized?
    ├─ Business rules satisfied?
    ├─ Preconditions met?
    └─ Constraints respected?

Failed → throw DomainException
Passed → execute logic
```

### Example Validations:
```
Booking Creation:
  ✓ Passenger age >= 18 (or with guardian)
  ✓ Available seats >= requested
  ✓ Flight departure > now + 2 hours
  ✓ Valid phone/email format
  ✓ User has valid payment method
  ✓ No duplicate booking same flight/date

Flight Update:
  ✓ Flight status allows updates
  ✓ Can't change past flight details
  ✓ Departure < Arrival time
  ✓ New seat count >= current bookings
```

---

## 💾 Cache Strategy

### What to Cache:
- Frequently accessed data (flights, airports)
- Expensive queries (search results)
- Static data (airport info, pricing rules)

### Cache Keys:
```
Pattern: {entity_type}_{identifier}_{variation}
Examples:
  - flight_123
  - flight_search_123_456_2024-04-12
  - airport_100
  - exchange_rates_usd
```

### TTL (Time To Live):
```
- Static data: 1 day
- Flight data: 1 hour
- Search results: 30 minutes
- User data: 30 minutes
- Pricing: 15 minutes
```

### Cache Invalidation:
```
When to invalidate:
  - After create/update/delete
  - Manual cleanup after bulk operations
  - Periodic refresh for stale data
  - On related entity changes

Strategy:
  - Immediate: Invalidate immediately after change
  - Lazy: Remove on next access
  - TTL: Auto-expire after time
  - Event-based: Invalidate on business events
```

---

## 🚨 Exception Handling

### Custom Exceptions:
```
ApplicationException (base)
    ├─ FlightNotFoundException (404)
    ├─ UserNotFoundException (404)
    ├─ InsufficientSeatsException (400)
    ├─ BookingAlreadyCancelledException (400)
    ├─ UnauthorizedException (401)
    ├─ ForbiddenException (403)
    └─ InvalidBookingStateException (400)
```

### Exception Flow:
```
Service throws Exception
    ↓
Controller catches
    ↓
Determine HTTP status
    ↓
Log exception (error level)
    ↓
Return error response
```

---

## 📊 Transaction Management

### When to Use Transactions:
```
✓ Multiple database operations as one unit
✓ Seat reservation + booking creation
✓ Refund + booking cancellation
✓ Payment + order confirmation

Pattern:
  using (var transaction = _context.Database.BeginTransaction())
  {
    try {
      // Multiple operations
      await _bookingRepository.AddAsync(booking);
      await _flightRepository.UpdateAsync(flight);
      await transaction.CommitAsync();
    } catch {
      await transaction.RollbackAsync();
      throw;
    }
  }
```

---

## 🔗 External Service Integration

### Email Service:
```
Trigger: Booking confirmation, cancellation, payment
Pattern: Queue message → Background worker → Send async
Retry: 3 attempts with exponential backoff
Fallback: Log and continue (email failure ≠ booking failure)
```

### Payment Gateway:
```
Trigger: During booking checkout
Pattern: Call async → Wait for response → Update status
Retry: Idempotent operations with transaction ID
Timeout: 30 seconds, then consider failed
Webhook: Listen for payment confirmations
```

### Notification Service:
```
Trigger: Booking events, flight updates
Pattern: Publish event → Subscriber handles
Channels: Email, SMS, Push notification
Rate limit: Respect user preferences
```

---

## 🎪 Service Composition

### Service Dependencies:
```
BookingService
  ├─ IFlightRepository (get flight, update seats)
  ├─ IUserRepository (get user, validate)
  ├─ IBookingRepository (create booking)
  ├─ IPaymentService (process payment)
  ├─ IEmailService (send confirmation)
  ├─ ICacheService (cache operations)
  └─ ILogger (logging)

All injected via constructor
```

---

## 📈 Performance Considerations

1. **Query Optimization**
   - Use indexes on frequently queried columns
   - Avoid N+1 queries
   - Use Include() for related entities
   - Paginate large result sets

2. **Caching Strategy**
   - Cache hot paths (flight search)
   - Use composite keys for complex queries
   - Implement cache warming for critical data

3. **Async/Await**
   - Never block on I/O
   - Use Task.WhenAll() for parallel operations
   - Timeout on external service calls

4. **Batch Operations**
   - Bulk insert/update when possible
   - Combine multiple operations in transactions

---

## ✅ Best Practices

1. **Single Responsibility** - One service = one domain
2. **Dependency Injection** - Always inject dependencies
3. **Async Throughout** - No synchronous I/O
4. **Error Handling** - Meaningful exceptions
5. **Logging** - Log important operations
6. **Validation** - Validate at service level
7. **Transactions** - Use for multi-step operations
8. **Caching** - Cache expensive operations
9. **Testing** - Mock dependencies in tests
10. **Documentation** - Comment complex algorithms

---

**Module**: Application Layer | **Version**: 1.0
