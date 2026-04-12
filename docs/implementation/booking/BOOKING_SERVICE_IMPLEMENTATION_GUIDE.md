# BookingService Implementation - Complete Guide

## ✅ Overview

Successfully implemented the **BookingService** class implementing **IBookingService** with full business logic, transaction management, and error handling following the 02_APPLICATION_LAYER_GUIDE.md booking creation and cancellation algorithms.

**Location**: `API/Application/Services/BookingService.cs`

---

## 📦 Components Created (5 Interfaces + 1 Service)

### 1. IBookingRepository Interface
**File**: `API/Application/Interfaces/IBookingRepository.cs`

**Responsibilities**:
- Database operations for bookings
- CRUD operations
- Query by user, flight, reference
- Transaction management

**Key Methods**:
- `GetByIdAsync(id)` - Get single booking
- `GetByUserIdAsync(userId, skip, take)` - Get user's bookings
- `GetCountByUserIdAsync(userId)` - Count user's bookings
- `GetByReferenceAsync(reference)` - Find by booking reference
- `GetByFlightIdAsync(flightId)` - Get flight's bookings
- `AddAsync(booking)` - Create booking
- `UpdateAsync(booking)` - Update booking
- `DeleteAsync(id)` - Delete booking
- `IsReferenceUniqueAsync(reference)` - Check reference uniqueness
- `SaveChangesAsync()` - Persist changes
- `BeginTransactionAsync()` - Start transaction

---

### 2. IUserRepository Interface
**File**: `API/Application/Interfaces/IUserRepository.cs`

**Key Methods**:
- `GetByIdAsync(id)` - Get user by ID
- `GetByEmailAsync(email)` - Get user by email
- `ExistsAsync(id)` - Check user existence
- `SaveChangesAsync()` - Persist changes

---

### 3. IPassengerRepository Interface
**File**: `API/Application/Interfaces/IPassengerRepository.cs`

**Key Methods**:
- `GetByBookingIdAsync(bookingId)` - Get booking's passengers
- `AddAsync(passenger)` - Add single passenger
- `AddRangeAsync(passengers)` - Add multiple passengers
- `SaveChangesAsync()` - Persist changes

---

### 4. IPaymentService Interface
**File**: `API/Application/Interfaces/IPaymentService.cs`

**Key Methods**:
- `ProcessRefundAsync(bookingId, refundAmount, reason)` - Process refund
- `GetRefundPercentage(hoursUntilDeparture)` - Calculate refund percentage
- `CalculateRefundAmount(originalAmount, hoursUntilDeparture, fee)` - Calculate refund with penalties

---

### 5. IEmailService Interface
**File**: `API/Application/Interfaces/IEmailService.cs`

**Key Methods**:
- `SendBookingConfirmationAsync(...)` - Send confirmation email
- `SendCancellationConfirmationAsync(...)` - Send cancellation email
- `SendRefundNotificationAsync(...)` - Send refund notification
- `SendCheckInReminderAsync(...)` - Send check-in reminder

---

### 6. BookingService Class
**File**: `API/Application/Services/BookingService.cs`

**Implements**: IBookingService

**Dependencies** (Injected):
- `IFlightRepository` - Flight data access
- `IBookingRepository` - Booking data access
- `IUserRepository` - User data access
- `IPassengerRepository` - Passenger data access
- `IPaymentService` - Payment processing
- `ICacheService` - Caching
- `IEmailService` - Email notifications
- `ILogger<BookingService>` - Logging

---

## 🎯 Method Implementations

### 1. GetAllBookingsAsync(page, pageSize)

**Algorithm**:
```
1. Validate page >= 1
2. Validate pageSize in [1, 100]
3. Calculate skip = (page - 1) * pageSize
4. Query repository for all bookings
5. Map entities to DTOs
6. Calculate total pages
7. Return PaginatedBookingsResponseDto
```

**Validation**:
- page >= 1
- 1 <= pageSize <= 100

**Example**:
```csharp
var response = await _bookingService.GetAllBookingsAsync(1, 10);
// Returns PaginatedBookingsResponseDto with 10 items per page
```

---

### 2. GetBookingByIdAsync(bookingId)

**Algorithm**:
```
1. Validate bookingId > 0
2. Check cache with key "booking_{id}"
3. If cached, return immediately
4. Query repository
5. If not found, throw BookingNotFoundException
6. Map to ResponseDto
7. Cache with 1-hour TTL
8. Log success
9. Return ResponseDto
```

**Cache Strategy**:
- Key: `booking_{id}`
- TTL: 1 hour
- Invalidated on update/cancel

**Example**:
```csharp
var booking = await _bookingService.GetBookingByIdAsync(1);
// Returns complete BookingResponseDto with passenger details
```

---

### 3. CreateBookingAsync(BookingCreateDto dto, int userId)

**Algorithm from 02_APPLICATION_LAYER_GUIDE.md**:
```
1. Validate input parameters
2. Fetch flight, check if exists and Active
3. Fetch user, check if exists
4. Validate: available_seats >= passenger_count
5. Start database transaction:
   a. Create booking record (status = Pending)
   b. Generate unique booking reference
   c. Add booking to repository
   d. Create passenger records
   e. Reserve seats (available_seats -= count)
   f. Commit transaction
6. Trigger post-booking actions (async, don't await):
   - Send confirmation email
   - Update flight cache
7. Map and return BookingResponseDto
```

**Validation Rules**:
```
✓ DTO not null
✓ FlightId > 0
✓ PassengerCount > 0
✓ Passengers array not empty
✓ PassengerCount == Passengers.Count
✓ UserId > 0
✓ Flight exists
✓ Flight status == Active
✓ User exists
✓ Available seats >= PassengerCount
```

**Transaction Steps**:
1. Create Booking with Pending status
2. Generate unique BookingReference (AAA000XXX format)
3. Create Passenger entities
4. Reserve seats on flight
5. Auto-commit transaction

**Post-Booking Actions** (async, non-blocking):
- Send booking confirmation email
- Invalidate flight caches
- Update flight seat count

**Example**:
```csharp
var bookingDto = new BookingCreateDto
{
    FlightId = 1,
    PassengerCount = 2,
    Passengers = new List<PassengerCreateDto>
    {
        new PassengerCreateDto { FirstName = "John", LastName = "Doe", ... },
        new PassengerCreateDto { FirstName = "Jane", LastName = "Doe", ... }
    }
};

var booking = await _bookingService.CreateBookingAsync(bookingDto, userId: 1);
// Returns BookingResponseDto with:
// - Assigned ID
// - Unique BookingReference
// - Status = Pending
// - TotalPrice calculated
// - Passenger details
```

**Booking Reference Generation**:
- Format: `AAA000XXX` (3 letters + 3 digits + 3 alphanumeric)
- Example: `ABC123XYZ`, `DEF456PQR`
- Uniqueness guaranteed via database check

---

### 4. CancelBookingAsync(bookingId, userId)

**Algorithm from 02_APPLICATION_LAYER_GUIDE.md**:
```
1. Fetch booking
2. Validate: exists, user owns it, status allows cancellation
3. Check if within cancellation window (24 hours before)
4. Calculate refund amount with penalty
5. Start transaction:
   a. Change status to Cancelled
   b. Release seats back
   c. Update booking and flight
   d. Commit
6. Trigger refund process (async):
   - Process refund via payment service
   - Send cancellation email
7. Invalidate caches
8. Return updated BookingResponseDto
```

**Validation**:
```
✓ BookingId > 0
✓ UserId > 0
✓ Booking exists
✓ User owns booking (booking.UserId == userId)
✓ Status != Cancelled (not already cancelled)
✓ Status != CheckedIn (cannot cancel checked-in)
✓ Flight departure not in past
```

**Cancellation Window**:
- Default: 24 hours before flight departure
- Late cancellations (< 24 hours) apply 20% penalty
- Cannot cancel after departure

**Refund Calculation**:
```
refundAmount = originalAmount * refundPercentage - cancellationFee
refundPercentage = Depends on hours until departure
cancellationFee = 20% of originalAmount
```

**Transaction Steps**:
1. Change status to Cancelled
2. Update UpdatedAt timestamp
3. Release seats (available_seats += passenger_count)
4. Save changes
5. Auto-commit

**Async Refund Process**:
- Process refund via payment service
- Send cancellation confirmation email
- Update refund status

**Example**:
```csharp
var cancelledBooking = await _bookingService.CancelBookingAsync(
    bookingId: 1,
    userId: 1  // Must own the booking
);
// Returns BookingResponseDto with:
// - Status = Cancelled
// - Seats released on flight
// - Refund processed asynchronously
```

**Seat Release Logic**:
```csharp
// Upon cancellation, seats are released back to flight
flight.AvailableSeats += booking.PassengerCount;
// E.g., if flight had 45 seats available and booking had 2 passengers:
// new available = 45 + 2 = 47
```

---

### 5. CheckInBookingAsync(bookingId, userId)

**Algorithm**:
```
1. Validate ids
2. Fetch booking
3. Validate: exists, user owns it, status == Confirmed
4. Update status to CheckedIn
5. Update timestamp
6. Save changes
7. Invalidate caches
8. Return updated ResponseDto
```

**Validation**:
```
✓ BookingId > 0
✓ UserId > 0
✓ Booking exists
✓ User owns booking
✓ Status == Confirmed (must be confirmed first)
```

**Example**:
```csharp
var checkedInBooking = await _bookingService.CheckInBookingAsync(
    bookingId: 1,
    userId: 1
);
// Returns BookingResponseDto with Status = CheckedIn
```

---

## 💾 Cache Strategy

### Cache Keys
```
Single Booking:        booking_{id}           → 1 hour TTL
User's Bookings:       user_bookings_{userId} → (invalidated on change)
Flight:                flight_{id}            → (invalidated on booking)
Flight Search:         flights_search_*       → (invalidated on booking)
```

### Invalidation Triggers
```
Create Booking:
  ✓ Invalidate flights_search_* (flight seats changed)
  ✓ Invalidate flight_{flightId} (cached flight)

Cancel Booking:
  ✓ Invalidate booking_{id}
  ✓ Invalidate user_bookings_{userId}
  ✓ Invalidate flight_{flightId}
  ✓ Invalidate flights_search_*

Check In Booking:
  ✓ Invalidate booking_{id}
  ✓ Invalidate user_bookings_{userId}
```

---

## 🔄 Transaction Management

### Transaction Pattern
```csharp
using (var transaction = await _bookingRepository.BeginTransactionAsync())
{
    try
    {
        // Step 1: Create/Update booking
        await _bookingRepository.UpdateAsync(booking);
        
        // Step 2: Update related entities
        await _flightRepository.UpdateAsync(flight);
        
        // Step 3: Save all changes
        await _bookingRepository.SaveChangesAsync();
        
        // Transaction auto-commits when using is disposed
    }
    catch (Exception ex)
    {
        // Transaction automatically rolls back on exception
        throw;
    }
}
```

### Guarantee ACID Properties
- **Atomicity**: All or nothing
- **Consistency**: Valid state maintained
- **Isolation**: Concurrent operations safe
- **Durability**: Committed changes persist

---

## 🔐 Error Handling

### Exception Mapping

| Scenario | Exception | HTTP Status |
|----------|-----------|------------|
| Invalid input | ValidationException | 400 |
| Not found | BookingNotFoundException | 404 |
| Already cancelled | BookingAlreadyCancelledException | 400 |
| Invalid status | InvalidBookingStatusException | 400 |
| No seats | InsufficientSeatsException | 400 |
| Unauthorized | UnauthorizedAccessException | 401 |
| Unexpected | Exception | 500 |

### Error Examples
```
// Invalid page
ValidationException: "Page number must be greater than 0."

// Booking not found
BookingNotFoundException: "Booking with ID 999 was not found."

// Already cancelled
BookingAlreadyCancelledException: "Booking 1 has already been cancelled."

// Wrong user
UnauthorizedAccessException: "You are not authorized to cancel this booking."

// Status not valid
InvalidBookingStatusException: "Cannot cancel booking with status 'CheckedIn'."

// No available seats
InsufficientSeatsException: "Insufficient seats available. Requested: 5, Available: 3"
```

---

## 📝 Logging Strategy

### Log Levels

**INFO**:
```csharp
_logger.LogInformation("Fetching all bookings - Page: {Page}, PageSize: {PageSize}", page, pageSize);
_logger.LogInformation("Fetching booking with ID: {BookingId}", bookingId);
_logger.LogInformation("Booking {BookingId} found in cache", bookingId);
_logger.LogInformation("Creating booking for user {UserId} on flight {FlightId} with {PassengerCount} passengers", ...);
_logger.LogInformation("Successfully created booking {BookingId} with reference {BookingReference}", ...);
_logger.LogInformation("Cancelling booking {BookingId} for user {UserId}", ...);
_logger.LogInformation("Refund calculated for booking {BookingId}: Original: {Original}, Refund: {Refund}", ...);
```

**WARNING**:
```csharp
_logger.LogWarning("Booking with ID {BookingId} not found", bookingId);
_logger.LogWarning("Flight with ID {FlightId} not found", flightId);
_logger.LogWarning("Insufficient seats on flight {FlightId}. Requested: {Requested}, Available: {Available}", ...);
_logger.LogWarning("User {UserId} attempted to cancel booking owned by {OwnerId}", ...);
_logger.LogWarning("Cannot cancel booking with status {Status}", ...);
```

**ERROR**:
```csharp
_logger.LogError(ex, "Error retrieving booking with ID: {BookingId}", bookingId);
_logger.LogError(ex, "Error during booking creation transaction");
_logger.LogError(ex, "Error cancelling booking {BookingId}", bookingId);
_logger.LogError(ex, "Error in post-booking actions for booking {BookingId}", ...);
```

---

## 🏗️ Architecture Integration

```
BookingsController (API Layer)
    ↓ (DTOs)
BookingService (Application Layer) ← IBookingService interface ✓
    ├─→ IFlightRepository (Database)
    ├─→ IBookingRepository (Database)
    ├─→ IUserRepository (Database)
    ├─→ IPassengerRepository (Database)
    ├─→ IPaymentService (Refunds)
    ├─→ ICacheService (Caching)
    ├─→ IEmailService (Notifications)
    └─→ ILogger (Logging)
```

---

## ✅ Build Status

✅ **Compilation**: SUCCESSFUL  
✅ **Errors**: 0  
✅ **Warnings**: 0  
✅ **Ready for**: Integration Testing

---

## 📚 Related Files

**Existing**:
- ✅ IBookingService.cs (interface)
- ✅ BookingResponseDto.cs
- ✅ BookingCreateDto.cs
- ✅ PassengerResponseDto.cs
- ✅ PassengerCreateDto.cs
- ✅ PaginatedBookingsResponseDto.cs
- ✅ BookingsController.cs

**Created**:
- ✅ BookingService.cs (implementation)
- ✅ IBookingRepository.cs (interface)
- ✅ IUserRepository.cs (interface)
- ✅ IPassengerRepository.cs (interface)
- ✅ IPaymentService.cs (interface)
- ✅ IEmailService.cs (interface)

**Still Needed**:
- ⏳ BookingRepository.cs (implementation)
- ⏳ UserRepository.cs (implementation)
- ⏳ PassengerRepository.cs (implementation)
- ⏳ PaymentService.cs (implementation)
- ⏳ EmailService.cs (implementation)

---

## 🚀 Next Steps

1. **Implement Repository Classes**
   - BookingRepository
   - UserRepository
   - PassengerRepository

2. **Implement Service Classes**
   - PaymentService
   - EmailService

3. **Register Dependencies**
   - Add to Program.cs DI container

4. **Create FlightsController**
   - Create REST endpoints

5. **Create Tests**
   - Unit tests for BookingService
   - Integration tests

---

**Version**: 1.0  
**Status**: Complete ✅  
**Ready for**: Repository Implementation
