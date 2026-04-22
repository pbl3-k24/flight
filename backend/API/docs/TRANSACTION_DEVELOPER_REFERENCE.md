# Developer Reference: P0-D Atomic Transactions Implementation

## Quick Start Guide

### Using Transactions in Services

When you need atomic operations across multiple data access steps:

```csharp
public class YourService : IYourService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<YourService> _logger;

    public YourService(IUnitOfWork unitOfWork, ILogger<YourService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> PerformAtomicOperationAsync(Data data)
    {
        try
        {
            // 1. Begin transaction
            await _unitOfWork.BeginTransactionAsync();

            // 2. Perform multiple operations using repositories
            var entity1 = await _unitOfWork.Repository1.GetAsync(...);
            await _unitOfWork.Repository2.CreateAsync(...);
            var entity3 = await _unitOfWork.Repository3.UpdateAsync(...);

            // 3. Commit all changes atomically
            await _unitOfWork.CommitAsync();

            return result;
        }
        catch (Exception ex)
        {
            // 4. Rollback on ANY error
            await _unitOfWork.RollbackAsync();
            _logger.LogError(ex, "Operation failed, rolled back");
            throw;
        }
    }
}
```

## Architecture Overview

```
┌─────────────────────────────────────────┐
│      Controller / Entry Point           │
└──────────────┬──────────────────────────┘
               │ injects
               ▼
┌─────────────────────────────────────────┐
│      Service Layer (BookingService)     │
│  - Business logic                       │
│  - Transaction coordination             │
└──────────────┬──────────────────────────┘
               │ uses
               ▼
┌─────────────────────────────────────────┐
│      IUnitOfWork (Interface)            │
│  - BeginTransactionAsync()              │
│  - CommitAsync()                        │
│  - RollbackAsync()                      │
│  - 16 Repository properties             │
└──────────────┬──────────────────────────┘
               │ implements
               ▼
┌─────────────────────────────────────────┐
│   UnitOfWork (Concrete Implementation)  │
│  - Manages IDbContextTransaction        │
│  - Lazy-loads repositories              │
│  - Coordinates all data access          │
└──────────────┬──────────────────────────┘
               │ uses
               ▼
┌─────────────────────────────────────────┐
│    16 Repositories (Data Access)        │
│  - BookingRepository                    │
│  - BookingPassengerRepository           │
│  - FlightSeatInventoryRepository        │
│  - ... (13 more)                        │
└──────────────┬──────────────────────────┘
               │ accesses
               ▼
┌─────────────────────────────────────────┐
│  DbContext / Entity Framework Core      │
│  - PostgreSQL database                  │
└─────────────────────────────────────────┘
```

## Transaction Flow Diagram

### Success Path
```
Service.BeginTransactionAsync()
        ↓
Step 1: Validate data ✓
        ↓
Step 2: Create entity ✓
        ↓
Step 3: Create related entities ✓
        ↓
Step 4: Update inventory ✓
        ↓
Service.CommitAsync()
        ↓
[DATABASE: All changes persisted atomically]
```

### Failure Path (Any Step Fails)
```
Service.BeginTransactionAsync()
        ↓
Step 1: Validate data ✓
        ↓
Step 2: Create entity ✓
        ↓
Step 3: Create related entities ✗ EXCEPTION
        ↓
Catch block: RollbackAsync()
        ↓
[DATABASE: Unchanged - no partial state]
```

## Repository Access Patterns

### Inside a Transaction
```csharp
// Before: Individual repository injection
private readonly IBookingRepository _bookingRepository;
var booking = await _bookingRepository.GetByIdAsync(id);

// After: Via UnitOfWork (atomic)
private readonly IUnitOfWork _unitOfWork;
var booking = await _unitOfWork.Bookings.GetByIdAsync(id);
```

### Non-Transactional Operations
```csharp
// For read-only or single-operation scenarios (not in transaction)
var flight = await _unitOfWork.Flights.GetByIdAsync(flightId);
```

### Multiple Operations (Must Use Transaction)
```csharp
await _unitOfWork.BeginTransactionAsync();
try
{
    // All operations grouped under single transaction
    var booking = await _unitOfWork.Bookings.CreateAsync(newBooking);
    var passenger = await _unitOfWork.BookingPassengers.CreateAsync(newPassenger);
    var inventory = await _unitOfWork.FlightSeatInventories.GetByIdAsync(inventoryId);
    inventory.HoldSeats(count);
    await _unitOfWork.FlightSeatInventories.UpdateAsync(inventory);

    await _unitOfWork.CommitAsync();
}
catch (Exception ex)
{
    await _unitOfWork.RollbackAsync();
    throw;
}
```

## Available Repositories (via IUnitOfWork)

```csharp
_unitOfWork.Users                    // IUserRepository
_unitOfWork.Roles                    // IRoleRepository
_unitOfWork.Airports                 // IAirportRepository
_unitOfWork.Aircraft                 // IAircraftRepository
_unitOfWork.Routes                   // IRouteRepository
_unitOfWork.SeatClasses              // ISeatClassRepository
_unitOfWork.Flights                  // IFlightRepository
_unitOfWork.FlightSeatInventories    // IFlightSeatInventoryRepository
_unitOfWork.Bookings                 // IBookingRepository
_unitOfWork.BookingPassengers        // IBookingPassengerRepository
_unitOfWork.Payments                 // IPaymentRepository
_unitOfWork.RefundRequests           // IRefundRequestRepository
_unitOfWork.Promotions               // IPromotionRepository
_unitOfWork.Tickets                  // ITicketRepository
_unitOfWork.NotificationLogs         // INotificationLogRepository
_unitOfWork.AuditLogs                // IAuditLogRepository
```

## Common Patterns

### Pattern 1: Simple Transaction
```csharp
public async Task<BookingResult> CreateBookingAsync(CreateBookingDto dto)
{
    try
    {
        await _unitOfWork.BeginTransactionAsync();

        // All database operations
        var booking = await _unitOfWork.Bookings.CreateAsync(newBooking);

        await _unitOfWork.CommitAsync();
        return new BookingResult { Success = true, BookingId = booking.Id };
    }
    catch (Exception ex)
    {
        await _unitOfWork.RollbackAsync();
        _logger.LogError(ex, "Failed to create booking");
        throw;
    }
}
```

### Pattern 2: Validation Before Transaction
```csharp
public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
{
    // Validation (NO transaction needed - read-only)
    var booking = await _unitOfWork.Bookings.GetByIdAsync(request.BookingId);
    if (booking == null) throw new NotFoundException();
    if (booking.Status != 0) throw new ValidationException();

    try
    {
        // Transaction (only for persistent changes)
        await _unitOfWork.BeginTransactionAsync();

        booking.Status = 1; // Confirmed
        await _unitOfWork.Bookings.UpdateAsync(booking);

        var payment = new Payment { ... };
        await _unitOfWork.Payments.CreateAsync(payment);

        await _unitOfWork.CommitAsync();
        return new PaymentResult { Success = true };
    }
    catch (Exception ex)
    {
        await _unitOfWork.RollbackAsync();
        throw;
    }
}
```

### Pattern 3: Conditional Operations
```csharp
public async Task<bool> CancelBookingAsync(int bookingId, int userId)
{
    try
    {
        await _unitOfWork.BeginTransactionAsync();

        var booking = await _unitOfWork.Bookings.GetByIdAsync(bookingId);
        if (booking == null) throw new NotFoundException();
        if (booking.UserId != userId) throw new UnauthorizedException();

        // Get related data
        var passengers = await _unitOfWork.BookingPassengers
            .GetByBookingIdAsync(bookingId);
        var inventory = await _unitOfWork.FlightSeatInventories
            .GetByIdAsync(passengers.First().FlightSeatInventoryId);

        // Update inventory (release held seats)
        if (inventory != null && booking.Status == 0) // Pending
        {
            inventory.ReleaseHeldSeats(passengers.Count);
            await _unitOfWork.FlightSeatInventories.UpdateAsync(inventory);
        }

        // Update booking
        booking.Status = 3; // Cancelled
        await _unitOfWork.Bookings.UpdateAsync(booking);

        await _unitOfWork.CommitAsync();
        return true;
    }
    catch (Exception ex)
    {
        await _unitOfWork.RollbackAsync();
        _logger.LogError(ex, "Failed to cancel booking {BookingId}", bookingId);
        throw;
    }
}
```

## Error Handling

### Exception Flow
```
Service Method
    ↓
try {
    BeginTransactionAsync()
    [Multiple operations]
    CommitAsync()
}
catch (ValidationException ex) {
    RollbackAsync() ← Transaction discarded
    LogError()
    throw;           ← Propagate to controller/global handler
}
```

### Common Exceptions
```csharp
// Validation errors (expected, rolled back)
throw new ValidationException("Insufficient seats available");

// Not found errors (expected, rolled back)
throw new NotFoundException("Flight not found");

// Authorization errors (expected, rolled back)
throw new UnauthorizedException("Cannot access this booking");

// Unexpected errors (caught, rolled back, logged)
throw new InvalidOperationException("Database transaction failed");
```

## Performance Considerations

### Transaction Duration
**Keep transactions as short as possible**:
```csharp
// ✅ GOOD: Validation before transaction
var flight = await _unitOfWork.Flights.GetByIdAsync(flightId);
if (flight == null) throw new NotFoundException(); // Before transaction

await _unitOfWork.BeginTransactionAsync();
// Only persistent operations here
await _unitOfWork.CommitAsync();

// ❌ BAD: Long-running operations in transaction
await _unitOfWork.BeginTransactionAsync();
var flight = await _unitOfWork.Flights.GetByIdAsync(flightId);
if (flight == null) throw new NotFoundException(); // In transaction
var externalData = await _externalService.FetchAsync(...); // Slow!
// ... more operations
await _unitOfWork.CommitAsync();
```

### Read-Only Operations
```csharp
// ✅ GOOD: No transaction needed for reads
var bookings = await _unitOfWork.Bookings.GetByUserIdAsync(userId);
var flights = await _unitOfWork.Flights.GetAllAsync();

// ❌ BAD: Unnecessary transaction for reads
await _unitOfWork.BeginTransactionAsync();
var booking = await _unitOfWork.Bookings.GetByIdAsync(id);
await _unitOfWork.CommitAsync(); // Pointless for read-only
```

## Logging

### Transaction Logging
UnitOfWork automatically logs:
```
[Information] Transaction started
[Information] Transaction committed successfully
[Warning] Transaction rolled back
[Error] Error committing transaction: {exception}
```

### Service Logging
Add logging for business logic:
```csharp
_logger.LogInformation("Creating booking with {PassengerCount} passengers", dto.PassengerCount);
// ... transaction ...
_logger.LogInformation("Booking created: {BookingCode}", booking.BookingCode);
```

## Testing with Transactions

### Unit Test Pattern
```csharp
[Fact]
public async Task CreateBookingAsync_WhenValid_ShouldCommit()
{
    // Arrange
    var dto = new CreateBookingDto { ... };
    var initialCount = _context.Bookings.Count();

    // Act
    await _service.CreateBookingAsync(userId, dto);

    // Assert
    Assert.Equal(initialCount + 1, _context.Bookings.Count());
}

[Fact]
public async Task CreateBookingAsync_WhenInvalid_ShouldRollback()
{
    // Arrange
    var dto = new CreateBookingDto { ... }; // Invalid
    var initialCount = _context.Bookings.Count();

    // Act & Assert
    await Assert.ThrowsAsync<ValidationException>(() => 
        _service.CreateBookingAsync(userId, dto));

    // Database unchanged
    Assert.Equal(initialCount, _context.Bookings.Count());
}
```

## Troubleshooting

### Issue: "No active transaction to commit"
**Cause**: Called `CommitAsync()` without `BeginTransactionAsync()`
**Fix**: Always call BeginTransactionAsync first
```csharp
await _unitOfWork.BeginTransactionAsync(); // Add this
// ... operations ...
await _unitOfWork.CommitAsync();
```

### Issue: Data still persists after exception
**Cause**: Exception caught elsewhere or not properly propagated
**Fix**: Ensure RollbackAsync called in catch block
```csharp
catch (Exception ex)
{
    await _unitOfWork.RollbackAsync(); // Add if missing
    throw;
}
```

### Issue: Performance degradation
**Cause**: Long-running operations in transaction
**Fix**: Move validation/slow operations before transaction
```csharp
// Validate first (outside transaction)
var flight = await _unitOfWork.Flights.GetByIdAsync(id);
if (flight == null) throw new NotFoundException();

// Then transaction (only persistent operations)
await _unitOfWork.BeginTransactionAsync();
// ... create/update ...
await _unitOfWork.CommitAsync();
```

## Migration Guide: Adding Transactions to Existing Services

### Before (No Transaction)
```csharp
public async Task<Result> DoSomethingAsync()
{
    var entity1 = await _bookingRepository.GetAsync(...);
    var entity2 = await _passengerRepository.CreateAsync(...);
    var entity3 = await _inventoryRepository.UpdateAsync(...);
    return result;
}
```

### After (With Transaction)
```csharp
// 1. Change constructor
public MyService(IUnitOfWork unitOfWork, ILogger<MyService> logger)
{
    _unitOfWork = unitOfWork;
    _logger = logger;
}

// 2. Wrap method in transaction
public async Task<Result> DoSomethingAsync()
{
    try
    {
        await _unitOfWork.BeginTransactionAsync();

        var entity1 = await _unitOfWork.Bookings.GetAsync(...);
        var entity2 = await _unitOfWork.BookingPassengers.CreateAsync(...);
        var entity3 = await _unitOfWork.FlightSeatInventories.UpdateAsync(...);

        await _unitOfWork.CommitAsync();
        return result;
    }
    catch (Exception ex)
    {
        await _unitOfWork.RollbackAsync();
        _logger.LogError(ex, "Operation failed");
        throw;
    }
}
```

---

**For questions or issues**: Refer to the actual implementation in `BookingService.cs` for working examples.
