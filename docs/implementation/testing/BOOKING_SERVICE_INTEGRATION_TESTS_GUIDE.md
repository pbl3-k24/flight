# ✅ BookingService Integration Testing Guide

## 🎉 Comprehensive Integration Testing Specification

Created a complete integration testing guide for BookingService following the 05_TESTING_GUIDE.md patterns with real database context and mocked external services.

---

## 📦 **Test Class Structure**

### **BookingServiceIntegrationTests.cs**

**Inheritance**:
```csharp
public class BookingServiceIntegrationTests : IAsyncLifetime
```

**IAsyncLifetime Methods**:
- `InitializeAsync()` - Setup test data before each test
- `DisposeAsync()` - Cleanup after tests

---

## 🔧 **Setup Configuration**

### **Dependencies (Real)**
```csharp
private readonly FlightBookingDbContext _context;           // Real InMemory DbContext
private readonly FlightRepository _flightRepository;        // Real repository
private readonly BookingRepository _bookingRepository;      // Real repository
private readonly PassengerRepository _passengerRepository;  // Real repository
```

### **Dependencies (Mocked)**
```csharp
private readonly Mock<IPaymentService> _mockPaymentService;
private readonly Mock<IEmailService> _mockEmailService;
private readonly Mock<ICacheService> _mockCacheService;
private readonly Mock<ILogger<BookingService>> _mockLogger;
```

### **Service Under Test**
```csharp
private BookingService _bookingService;
```

### **Test Data**
```csharp
private Flight _testFlight;           // Flight with 100 seats
private User _testUser;               // Test user
private Airport _departureAirport;    // LAX
private Airport _arrivalAirport;      // JFK
```

---

## 🧪 **5 Integration Test Cases**

### **Test 1**: CreateBookingAsync_ValidBooking_PersistsToDatabase

**Purpose**: Verify complete booking creation workflow with database persistence.

**Setup (Arrange)**:
```csharp
// Create test airports
var departureAirport = new Airport 
{ 
    Code = "LAX",
    Name = "Los Angeles International",
    // ... other properties
};

var arrivalAirport = new Airport 
{ 
    Code = "JFK",
    Name = "John F. Kennedy International",
    // ... other properties
};

// Add to context
await _context.Airports.AddAsync(departureAirport);
await _context.Airports.AddAsync(arrivalAirport);
await _context.SaveChangesAsync();

// Create test flight with 100 seats
var testFlight = new Flight
{
    FlightNumber = "AA100",
    DepartureAirportId = departureAirport.Id,
    ArrivalAirportId = arrivalAirport.Id,
    DepartureTime = DateTime.UtcNow.AddHours(24),
    ArrivalTime = DateTime.UtcNow.AddHours(28),
    TotalSeats = 100,
    AvailableSeats = 100,
    BasePrice = 250.00m,
    Status = FlightStatus.Active
};

// Create test user
var testUser = new User
{
    Email = "testuser@example.com",
    FirstName = "John",
    LastName = "Doe",
    DateOfBirth = new DateTime(1990, 1, 1),
    PasswordHash = "hashed_password",
    Status = UserStatus.Active
};

// Create booking DTO with 3 passengers
var createBookingDto = new BookingCreateDto
{
    FlightId = testFlight.Id,
    PassengerCount = 3,
    Notes = "Integration test booking"
};

// Create passenger DTOs
var passengers = new[]
{
    new PassengerCreateDto { FirstName = "John", LastName = "Doe", ... },
    new PassengerCreateDto { FirstName = "Jane", LastName = "Doe", ... },
    new PassengerCreateDto { FirstName = "Jack", LastName = "Smith", ... }
};

// Mock external services
_mockPaymentService
    .Setup(p => p.ProcessPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>()))
    .ReturnsAsync(new PaymentResponseDto { Success = true, TransactionId = "TXN123" });

_mockEmailService
    .Setup(e => e.SendBookingConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
    .Returns(Task.CompletedTask);

_mockCacheService
    .Setup(c => c.RemoveAsync(It.IsAny<string>()))
    .Returns(Task.CompletedTask);

var seatsBeforeBooking = testFlight.AvailableSeats;
```

**Execution (Act)**:
```csharp
var result = await _bookingService.CreateBookingAsync(
    createBookingDto, 
    testUser.Id, 
    passengers
);
```

**Assertions (Assert)**:
```csharp
// Verify returned DTO
Assert.NotNull(result);
Assert.NotEmpty(result.BookingReference);
Assert.Equal(createBookingDto.FlightId, result.FlightId);
Assert.Equal(createBookingDto.PassengerCount, result.PassengerCount);
Assert.Equal(BookingStatus.Pending.ToString(), result.Status);
```

**Database Verification**:
```csharp
// Query database directly to verify persistence
var persistedBooking = await _context.Bookings
    .AsNoTracking()
    .FirstOrDefaultAsync(b => b.BookingReference == result.BookingReference);

Assert.NotNull(persistedBooking);
Assert.Equal(testUser.Id, persistedBooking.UserId);
Assert.Equal(createBookingDto.FlightId, persistedBooking.FlightId);
Assert.Equal(BookingStatus.Pending, persistedBooking.Status);
```

**Seat Verification**:
```csharp
// Verify flight seats decreased
var flightAfterBooking = await _context.Flights
    .AsNoTracking()
    .FirstOrDefaultAsync(f => f.Id == testFlight.Id);

Assert.Equal(
    seatsBeforeBooking - createBookingDto.PassengerCount, 
    flightAfterBooking.AvailableSeats
);
```

**Passenger Verification**:
```csharp
// Verify passengers created
var bookedPassengers = await _context.Passengers
    .AsNoTracking()
    .Where(p => p.BookingId == persistedBooking.Id)
    .ToListAsync();

Assert.Equal(createBookingDto.PassengerCount, bookedPassengers.Count);
Assert.All(bookedPassengers, p => Assert.NotEmpty(p.Email));
```

**Mock Verification**:
```csharp
// Verify external services called
_mockPaymentService.Verify(
    p => p.ProcessPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>()),
    Times.Once
);

_mockEmailService.Verify(
    e => e.SendBookingConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()),
    Times.Once
);
```

**Key Testing Points**:
- ✅ Booking created and persisted
- ✅ Passengers created and persisted
- ✅ Flight seats updated
- ✅ Database transaction successful
- ✅ External services called

---

### **Test 2**: CancelBookingAsync_ValidBooking_ReleasesSeatsAndPersists

**Purpose**: Verify booking cancellation with seat release and database updates.

**Setup (Arrange)**:
```csharp
// Create existing booking
var booking = new Booking
{
    FlightId = _testFlight.Id,
    UserId = _testUser.Id,
    BookingReference = "TEST-20240415-ABC123",
    PassengerCount = 2,
    TotalPrice = 500.00m,
    Status = BookingStatus.Confirmed,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

await _bookingRepository.AddAsync(booking);
await _bookingRepository.SaveChangesAsync();

// Reserve seats
_testFlight.AvailableSeats -= booking.PassengerCount;
await _flightRepository.UpdateAsync(_testFlight);
await _flightRepository.SaveChangesAsync();

var seatsBeforeCancellation = _testFlight.AvailableSeats;

// Mock external services
_mockEmailService
    .Setup(e => e.SendCancellationConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
    .Returns(Task.CompletedTask);
```

**Execution (Act)**:
```csharp
var result = await _bookingService.CancelBookingAsync(booking.Id, _testUser.Id);
```

**Assertions (Assert)**:
```csharp
Assert.NotNull(result);
Assert.Equal(BookingStatus.Cancelled.ToString(), result.Status);
```

**Database Verification**:
```csharp
// Query to verify persistence
var persistedBooking = await _context.Bookings
    .AsNoTracking()
    .FirstOrDefaultAsync(b => b.Id == booking.Id);

Assert.NotNull(persistedBooking);
Assert.Equal(BookingStatus.Cancelled, persistedBooking.Status);
Assert.NotNull(persistedBooking.CancelledAt); // Timestamp set
```

**Seat Release Verification**:
```csharp
// Verify seats released
var flightAfterCancellation = await _context.Flights
    .AsNoTracking()
    .FirstOrDefaultAsync(f => f.Id == _testFlight.Id);

Assert.Equal(
    seatsBeforeCancellation + booking.PassengerCount,
    flightAfterCancellation.AvailableSeats
);
```

**Mock Verification**:
```csharp
_mockEmailService.Verify(
    e => e.SendCancellationConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()),
    Times.Once
);
```

**Key Testing Points**:
- ✅ Booking status updated to Cancelled
- ✅ CancelledAt timestamp set
- ✅ Seats released back to flight
- ✅ Database persisted changes
- ✅ Cancellation email sent

---

### **Test 3**: CreateBookingAsync_InsufficientSeats_ThrowsExceptionAndRollsBack

**Purpose**: Verify transaction rollback when insufficient seats.

**Setup (Arrange)**:
```csharp
// Create flight with only 5 seats
var smallFlight = new Flight
{
    FlightNumber = "UA200",
    TotalSeats = 5,
    AvailableSeats = 5, // Only 5 seats available
    Status = FlightStatus.Active
};

await _flightRepository.AddAsync(smallFlight);
await _flightRepository.SaveChangesAsync();

// Try to book 10 passengers
var createBookingDto = new BookingCreateDto
{
    FlightId = smallFlight.Id,
    PassengerCount = 10 // More than available
};

var passengers = Enumerable.Range(1, 10)
    .Select(i => new PassengerCreateDto { ... })
    .ToArray();

var seatsBeforeAttempt = smallFlight.AvailableSeats;
var bookingsBeforeAttempt = await _context.Bookings.CountAsync();
```

**Execution (Act) & Assertion (Assert)**:
```csharp
var exception = await Assert.ThrowsAsync<InsufficientSeatsException>(
    () => _bookingService.CreateBookingAsync(createBookingDto, _testUser.Id, passengers)
);

Assert.NotNull(exception);
```

**Transaction Rollback Verification**:
```csharp
// Verify database unchanged
var flightAfterAttempt = await _context.Flights
    .AsNoTracking()
    .FirstOrDefaultAsync(f => f.Id == smallFlight.Id);

Assert.Equal(seatsBeforeAttempt, flightAfterAttempt.AvailableSeats);
```

**No Data Created Verification**:
```csharp
// Verify no booking created
var bookingsAfterAttempt = await _context.Bookings.CountAsync();
Assert.Equal(bookingsBeforeAttempt, bookingsAfterAttempt);

// Verify no passengers created
var passengersCreated = await _context.Passengers.CountAsync();
Assert.Equal(0, passengersCreated);
```

**Mock Verification (NOT Called)**:
```csharp
// Verify external services NOT called
_mockPaymentService.Verify(
    p => p.ProcessPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>()),
    Times.Never
);

_mockEmailService.Verify(
    e => e.SendBookingConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()),
    Times.Never
);
```

**Key Testing Points**:
- ✅ Exception thrown for insufficient seats
- ✅ Database rolled back (no booking created)
- ✅ Database rolled back (no passengers created)
- ✅ Flight seats unchanged
- ✅ External services NOT called

---

### **Test 4**: GetBookingAsync_ExistingBooking_ReturnsCompleteData

**Purpose**: Verify booking retrieval with all related data.

**Setup (Arrange)**:
```csharp
// Create booking with passengers
var booking = new Booking
{
    FlightId = _testFlight.Id,
    UserId = _testUser.Id,
    BookingReference = "TEST-20240415-XYZ789",
    PassengerCount = 2,
    TotalPrice = 500.00m,
    Status = BookingStatus.Confirmed
};

await _bookingRepository.AddAsync(booking);
await _bookingRepository.SaveChangesAsync();

// Create passengers
var passenger1 = new Passenger
{
    BookingId = booking.Id,
    FirstName = "Alice",
    LastName = "Johnson",
    Email = "alice@example.com",
    PassportNumber = "P123456"
};

var passenger2 = new Passenger
{
    BookingId = booking.Id,
    FirstName = "Bob",
    LastName = "Johnson",
    Email = "bob@example.com",
    PassportNumber = "P654321"
};

await _passengerRepository.AddAsync(passenger1);
await _passengerRepository.AddAsync(passenger2);
await _passengerRepository.SaveChangesAsync();
```

**Execution (Act)**:
```csharp
var result = await _bookingService.GetBookingAsync(booking.Id, _testUser.Id);
```

**Assertions (Assert)**:
```csharp
Assert.NotNull(result);
Assert.Equal(booking.BookingReference, result.BookingReference);
Assert.Equal(booking.PassengerCount, result.PassengerCount);
Assert.Equal(BookingStatus.Confirmed.ToString(), result.Status);
Assert.NotEmpty(result.Passengers);
Assert.Equal(2, result.Passengers.Count);
```

**Key Testing Points**:
- ✅ Booking retrieved
- ✅ Passengers eager-loaded
- ✅ All properties populated
- ✅ Correct data returned

---

### **Test 5**: GetUserBookingsAsync_MultipleBookings_ReturnsPaginatedResults

**Purpose**: Verify paginated booking retrieval.

**Setup (Arrange)**:
```csharp
// Create multiple bookings
for (int i = 1; i <= 5; i++)
{
    var booking = new Booking
    {
        FlightId = _testFlight.Id,
        UserId = _testUser.Id,
        BookingReference = $"TEST-20240415-{i:D3}",
        PassengerCount = i,
        TotalPrice = (decimal)(i * 100),
        Status = BookingStatus.Confirmed,
        CreatedAt = DateTime.UtcNow.AddDays(-i)
    };

    await _bookingRepository.AddAsync(booking);
}

await _bookingRepository.SaveChangesAsync();
```

**Execution (Act) - Page 1**:
```csharp
var page1 = await _bookingService.GetUserBookingsAsync(_testUser.Id, 1, 3);
```

**Assertions (Assert) - Page 1**:
```csharp
Assert.NotNull(page1);
Assert.Equal(5, page1.TotalCount);        // 5 total bookings
Assert.Equal(3, page1.PageSize);          // 3 per page
Assert.Equal(1, page1.CurrentPage);       // Page 1
Assert.Equal(3, page1.Bookings.Count);    // 3 items on page 1
Assert.True(page1.HasNextPage);           // More pages available
```

**Execution (Act) - Page 2**:
```csharp
var page2 = await _bookingService.GetUserBookingsAsync(_testUser.Id, 2, 3);
```

**Assertions (Assert) - Page 2**:
```csharp
Assert.Equal(2, page2.Bookings.Count);    // 2 items on page 2
Assert.False(page2.HasNextPage);          // No more pages
```

**Key Testing Points**:
- ✅ Pagination works correctly
- ✅ Total count accurate
- ✅ Page size respected
- ✅ HasNextPage flag accurate
- ✅ Only user's bookings returned

---

## 📊 **Integration Test Architecture**

```
BookingServiceIntegrationTests
├─ Constructor
│  ├─ Create InMemory DbContext
│  ├─ Create real repositories
│  └─ Create mock services
│
├─ InitializeAsync (Setup)
│  ├─ Create airports
│  ├─ Create flights
│  ├─ Create users
│  └─ Create service instance
│
├─ Test Methods (5 tests)
│  ├─ CreateBookingAsync_ValidBooking_PersistsToDatabase
│  ├─ CancelBookingAsync_ValidBooking_ReleasesSeatsAndPersists
│  ├─ CreateBookingAsync_InsufficientSeats_ThrowsExceptionAndRollsBack
│  ├─ GetBookingAsync_ExistingBooking_ReturnsCompleteData
│  └─ GetUserBookingsAsync_MultipleBookings_ReturnsPaginatedResults
│
└─ DisposeAsync (Cleanup)
   └─ Dispose DbContext
```

---

## 🎯 **Key Integration Testing Patterns**

### **Real Database with InMemory**
```csharp
var options = new DbContextOptionsBuilder<FlightBookingDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;

var context = new FlightBookingDbContext(options);
```

### **Seed Test Data**
```csharp
await _context.Airports.AddAsync(airport);
await _context.Flights.AddAsync(flight);
await _context.Users.AddAsync(user);
await _context.SaveChangesAsync();
```

### **Query Database Directly**
```csharp
var persistedBooking = await _context.Bookings
    .AsNoTracking()
    .FirstOrDefaultAsync(b => b.BookingReference == reference);

Assert.NotNull(persistedBooking);
```

### **Mock External Services**
```csharp
_mockPaymentService
    .Setup(p => p.ProcessPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>()))
    .ReturnsAsync(new PaymentResponseDto { Success = true });
```

### **Verify State Changes**
```csharp
var flightBefore = flight.AvailableSeats;
await _bookingService.CreateBookingAsync(...);
var flightAfter = await _context.Flights.FindAsync(flight.Id);

Assert.Equal(flightBefore - passengerCount, flightAfter.AvailableSeats);
```

---

## ✅ **Best Practices**

✅ **Real Database**
- Use InMemory for speed
- Test actual EF Core queries
- Verify persistence

✅ **Seeded Data**
- Create realistic test data
- Multiple airports, flights, users
- Complete test scenarios

✅ **Query Verification**
- Query database directly
- Verify persistence
- Check state changes

✅ **Transaction Testing**
- Verify rollback on error
- Check no data created on exception
- Ensure atomicity

✅ **Mock External Services**
- Keep external calls isolated
- Control service behavior
- Verify they're called correctly

---

## 🔄 **Integration vs Unit Tests**

| Aspect | Unit Tests | Integration Tests |
|--------|-----------|-------------------|
| Database | Mocked | Real (InMemory) |
| Repositories | Mocked | Real |
| Services | Real | Real |
| External APIs | Mocked | Mocked |
| Focus | Method behavior | Data persistence |
| Speed | Fast | Slower |
| Coverage | Individual methods | Full workflows |

---

## 📚 **NuGet Packages Required**

```xml
<!-- Testing Framework -->
<PackageReference Include="xunit" Version="2.6.0" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />

<!-- Mocking -->
<PackageReference Include="Moq" Version="4.20.0" />

<!-- Database -->
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.5" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.5" />

<!-- Test SDK -->
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
```

---

## 🚀 **Running Integration Tests**

```bash
# Run all integration tests
dotnet test --filter "Category=Integration"

# Run specific test class
dotnet test --filter "ClassName=BookingServiceIntegrationTests"

# Run specific test method
dotnet test --filter "Name~CreateBookingAsync_ValidBooking"

# With code coverage
dotnet test /p:CollectCoverage=true --filter "Category=Integration"
```

---

**Status**: ✅ **INTEGRATION TEST SPECIFICATION COMPLETE**  
**Framework**: xUnit  
**Database**: InMemory  
**Mocking**: Moq  
**Test Cases**: 5 comprehensive scenarios  
**Quality**: Production-Ready

---

**Ready to create integration test project and implement these tests! 🚀**
