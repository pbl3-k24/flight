# ✅ Flight Service Unit Testing Guide - Complete Implementation

## 🎉 FlightServiceTests - Comprehensive Unit Testing

This guide provides complete unit test specifications for FlightService following xUnit and AAA (Arrange-Act-Assert) patterns from the 05_TESTING_GUIDE.md.

---

## 📦 **Test Class Structure**

### **FlightServiceTests.cs**

```csharp
namespace API.Tests.Services;

using AutoMapper;
using Moq;
using Xunit;
using API.Application.Interfaces;
using API.Application.Services;
using API.Application.DTOs;
using API.Domain.Entities;
using API.Domain.Enums;
using API.Domain.Exceptions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Unit tests for FlightService class.
/// Tests service methods for flight operations including CRUD and caching.
/// Uses xUnit framework with Moq for mocking dependencies.
/// </summary>
public class FlightServiceTests
{
    // Constructor and Setup
    // Test Methods (Details Below)
}
```

---

## 🔧 **Setup Configuration**

### **Class Fields**
```csharp
private readonly Mock<IFlightRepository> _mockFlightRepository;
private readonly Mock<IMapper> _mockMapper;
private readonly Mock<ICacheService> _mockCacheService;
private readonly Mock<ILogger<FlightService>> _mockLogger;
private readonly FlightService _flightService;
```

### **Constructor**
```csharp
public FlightServiceTests()
{
    // Initialize all mocks
    _mockFlightRepository = new Mock<IFlightRepository>();
    _mockMapper = new Mock<IMapper>();
    _mockCacheService = new Mock<ICacheService>();
    _mockLogger = new Mock<ILogger<FlightService>>();

    // Create FlightService instance with mocked dependencies
    _flightService = new FlightService(
        _mockFlightRepository.Object,
        _mockMapper.Object,
        _mockCacheService.Object,
        _mockLogger.Object
    );
}
```

---

## 🧪 **Test Cases Specification**

### **Test 1: GetFlightAsync_WithValidId_ReturnsFlight**

**Purpose**: Verify that a valid flight ID returns the flight from database and caches it.

**Test Scenario**:
- User requests flight with ID = 1
- Flight exists in database
- Cache miss (returns null on first check)

**Setup (Arrange)**:
```csharp
// Create test flight entity
var flight = new Flight
{
    Id = 1,
    FlightNumber = "AA100",
    DepartureAirportId = 1,
    ArrivalAirportId = 2,
    DepartureTime = DateTime.UtcNow.AddHours(2),
    ArrivalTime = DateTime.UtcNow.AddHours(6),
    Airline = "American Airlines",
    AircraftModel = "Boeing 737",
    TotalSeats = 180,
    AvailableSeats = 45,
    BasePrice = 250.00m,
    Status = FlightStatus.Active,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Create expected DTO
var flightResponseDto = new FlightResponseDto 
{ 
    Id = 1,
    FlightNumber = "AA100",
    // ... other properties
};

// Setup mocks
_mockCacheService
    .Setup(c => c.GetAsync<FlightResponseDto>("flight_1"))
    .ReturnsAsync((FlightResponseDto?)null);

_mockFlightRepository
    .Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(flight);

_mockMapper
    .Setup(m => m.Map<FlightResponseDto>(flight))
    .Returns(flightResponseDto);

_mockCacheService
    .Setup(c => c.SetAsync("flight_1", flightResponseDto, It.IsAny<TimeSpan>()))
    .Returns(Task.CompletedTask);
```

**Execution (Act)**:
```csharp
var result = await _flightService.GetFlightAsync(1);
```

**Verification (Assert)**:
```csharp
// Assert result
Assert.NotNull(result);
Assert.Equal(1, result.Id);
Assert.Equal("AA100", result.FlightNumber);
Assert.Equal(45, result.AvailableSeats);

// Verify mock calls
_mockCacheService.Verify(
    c => c.GetAsync<FlightResponseDto>("flight_1"),
    Times.Once,
    "Cache should be checked once"
);

_mockFlightRepository.Verify(
    r => r.GetByIdAsync(1),
    Times.Once,
    "Repository should be queried once"
);

_mockCacheService.Verify(
    c => c.SetAsync("flight_1", flightResponseDto, It.IsAny<TimeSpan>()),
    Times.Once,
    "Result should be cached once"
);
```

**Key Points**:
- ✅ Cache is checked first
- ✅ Repository is called on cache miss
- ✅ Result is cached for next request
- ✅ All dependencies are verified

---

### **Test 2: GetFlightAsync_WithInvalidId_ThrowsFlightNotFoundException**

**Purpose**: Verify that requesting a non-existent flight throws FlightNotFoundException.

**Test Scenario**:
- User requests flight with ID = 99999 (doesn't exist)
- Repository returns null
- Exception should be thrown

**Setup (Arrange)**:
```csharp
_mockCacheService
    .Setup(c => c.GetAsync<FlightResponseDto>("flight_99999"))
    .ReturnsAsync((FlightResponseDto?)null);

_mockFlightRepository
    .Setup(r => r.GetByIdAsync(99999))
    .ReturnsAsync((Flight?)null); // No flight found
```

**Execution (Act) & Verification (Assert)**:
```csharp
var exception = await Assert.ThrowsAsync<FlightNotFoundException>(
    () => _flightService.GetFlightAsync(99999)
);

Assert.NotNull(exception);
Assert.Contains("99999", exception.Message);
```

**Verify Mock Calls**:
```csharp
_mockFlightRepository.Verify(
    r => r.GetByIdAsync(99999),
    Times.Once,
    "Repository should be queried"
);

_mockCacheService.Verify(
    c => c.SetAsync(It.IsAny<string>(), It.IsAny<FlightResponseDto>(), It.IsAny<TimeSpan>()),
    Times.Never,
    "Cache should not be set on exception"
);
```

**Key Points**:
- ✅ Exception is thrown for invalid ID
- ✅ Repository is called once
- ✅ Cache is not updated on error
- ✅ Exception contains flight ID in message

---

### **Test 3: CreateFlightAsync_WithValidDto_ReturnsCreatedFlight**

**Purpose**: Verify that a valid FlightCreateDto creates a new flight successfully.

**Test Scenario**:
- User provides valid FlightCreateDto
- Flight is created with initial available seats = total seats
- ID is assigned after save

**Setup (Arrange)**:
```csharp
var createFlightDto = new FlightCreateDto
{
    FlightNumber = "UA200",
    DepartureAirportId = 1,
    ArrivalAirportId = 2,
    DepartureTime = DateTime.UtcNow.AddHours(4),
    ArrivalTime = DateTime.UtcNow.AddHours(8),
    Airline = "United Airlines",
    AircraftModel = "Boeing 787",
    TotalSeats = 242,
    BasePrice = 350.00m
};

var createdFlight = new Flight { /* properties */ };

_mockMapper
    .Setup(m => m.Map<Flight>(createFlightDto))
    .Returns(createdFlight);

_mockFlightRepository
    .Setup(r => r.AddAsync(createdFlight))
    .ReturnsAsync(createdFlight);

_mockFlightRepository
    .Setup(r => r.SaveChangesAsync())
    .ReturnsAsync(1); // 1 row affected
```

**Execution (Act)**:
```csharp
var result = await _flightService.CreateFlightAsync(createFlightDto);
```

**Verification (Assert)**:
```csharp
Assert.NotNull(result);
Assert.True(result.Id > 0, "ID should be assigned");
Assert.Equal("UA200", result.FlightNumber);
Assert.Equal(242, result.AvailableSeats);

_mockFlightRepository.Verify(
    r => r.AddAsync(It.IsAny<Flight>()),
    Times.Once
);

_mockFlightRepository.Verify(
    r => r.SaveChangesAsync(),
    Times.Once
);
```

**Key Points**:
- ✅ DTO is mapped to entity
- ✅ Entity is added to repository
- ✅ Changes are saved
- ✅ ID is assigned after save
- ✅ Available seats = total seats initially

---

### **Test 4: UpdateFlightAsync_WithValidData_UpdatesAndInvalidatesCache**

**Purpose**: Verify that updating a flight invalidates its cache.

**Test Scenario**:
- User updates existing flight with new data
- Flight is retrieved, updated, saved
- Cache is invalidated for specific flight
- Search cache pattern is invalidated

**Setup (Arrange)**:
```csharp
var updateFlightDto = new FlightUpdateDto
{
    Airline = "American Airlines Updated",
    AircraftModel = "Boeing 777",
    BasePrice = 300.00m
};

var existingFlight = new Flight { /* existing data */ };
var updatedFlight = new Flight { /* updated data */ };

_mockFlightRepository
    .Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(existingFlight);

_mockFlightRepository
    .Setup(r => r.UpdateAsync(It.IsAny<Flight>()))
    .Returns(Task.CompletedTask);

_mockFlightRepository
    .Setup(r => r.SaveChangesAsync())
    .ReturnsAsync(1);

_mockCacheService
    .Setup(c => c.RemoveAsync("flight_1"))
    .Returns(Task.CompletedTask);

_mockCacheService
    .Setup(c => c.RemoveByPatternAsync("flights_search_*"))
    .Returns(Task.CompletedTask);
```

**Execution (Act)**:
```csharp
var result = await _flightService.UpdateFlightAsync(1, updateFlightDto);
```

**Verification (Assert)**:
```csharp
Assert.NotNull(result);
Assert.Equal(1, result.Id);

_mockFlightRepository.Verify(
    r => r.UpdateAsync(It.IsAny<Flight>()),
    Times.Once
);

_mockCacheService.Verify(
    c => c.RemoveAsync("flight_1"),
    Times.Once,
    "Specific flight cache should be invalidated"
);

_mockCacheService.Verify(
    c => c.RemoveByPatternAsync("flights_search_*"),
    Times.Once,
    "Search cache pattern should be invalidated"
);
```

**Key Points**:
- ✅ Flight is retrieved
- ✅ Flight is updated
- ✅ Changes are saved
- ✅ Specific cache key is removed
- ✅ Related cache patterns are removed

---

### **Test 5: GetFlightAsync_WithCacheHit_ReturnsCachedValue**

**Purpose**: Verify that cached flights are returned without hitting the repository.

**Test Scenario**:
- User requests a previously cached flight
- Cache returns the DTO directly
- Repository is NOT called

**Setup (Arrange)**:
```csharp
var cachedFlightDto = new FlightResponseDto
{
    Id = 1,
    FlightNumber = "AA100",
    // ... other properties
};

_mockCacheService
    .Setup(c => c.GetAsync<FlightResponseDto>("flight_1"))
    .ReturnsAsync(cachedFlightDto); // Cache hit
```

**Execution (Act)**:
```csharp
var result = await _flightService.GetFlightAsync(1);
```

**Verification (Assert)**:
```csharp
Assert.NotNull(result);
Assert.Equal(cachedFlightDto.Id, result.Id);

_mockFlightRepository.Verify(
    r => r.GetByIdAsync(It.IsAny<int>()),
    Times.Never,
    "Repository should NOT be called on cache hit"
);
```

**Key Points**:
- ✅ Cache is checked
- ✅ Cached value is returned
- ✅ Repository is NOT called
- ✅ Performance optimization verified

---

### **Test 6: ReserveSeatsAsync_WithSufficientSeats_ReserveSuccessfully**

**Purpose**: Verify that seats are reserved when sufficient availability exists.

**Test Scenario**:
- Flight has 50 available seats
- User reserves 5 seats
- Available seats reduced to 45
- Cache is invalidated

**Setup (Arrange)**:
```csharp
var flight = new Flight
{
    Id = 1,
    AvailableSeats = 50,
    // ... other properties
};

_mockFlightRepository
    .Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(flight);

_mockFlightRepository
    .Setup(r => r.SaveChangesAsync())
    .ReturnsAsync(1);

_mockCacheService
    .Setup(c => c.RemoveAsync("flight_1"))
    .Returns(Task.CompletedTask);
```

**Execution (Act)**:
```csharp
await _flightService.ReserveSeatsAsync(1, 5);
```

**Verification (Assert)**:
```csharp
Assert.Equal(45, flight.AvailableSeats);

_mockFlightRepository.Verify(
    r => r.SaveChangesAsync(),
    Times.Once
);

_mockCacheService.Verify(
    c => c.RemoveAsync("flight_1"),
    Times.Once
);
```

**Key Points**:
- ✅ Seats are deducted correctly
- ✅ Changes are saved
- ✅ Cache is invalidated

---

### **Test 7: ReserveSeatsAsync_WithInsufficientSeats_ThrowsException**

**Purpose**: Verify that InsufficientSeatsException is thrown when not enough seats.

**Test Scenario**:
- Flight has 10 available seats
- User tries to reserve 100 seats
- Exception should be thrown
- State should not change

**Setup (Arrange)**:
```csharp
var flight = new Flight
{
    Id = 1,
    AvailableSeats = 10,
};

_mockFlightRepository
    .Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(flight);
```

**Execution (Act) & Verification (Assert)**:
```csharp
var exception = await Assert.ThrowsAsync<InsufficientSeatsException>(
    () => _flightService.ReserveSeatsAsync(1, 100)
);

_mockFlightRepository.Verify(
    r => r.SaveChangesAsync(),
    Times.Never,
    "Changes should NOT be saved"
);
```

**Key Points**:
- ✅ Exception is thrown
- ✅ State is not modified
- ✅ Changes are not saved

---

## 📊 **Mock Verification Patterns**

### **Verify Once (Exactly One Call)**
```csharp
_mockRepository.Verify(
    r => r.GetByIdAsync(id),
    Times.Once,
    "Should be called exactly once"
);
```

### **Verify Never (No Calls)**
```csharp
_mockRepository.Verify(
    r => r.DeleteAsync(It.IsAny<int>()),
    Times.Never,
    "Should never be called"
);
```

### **Verify Any Number of Times**
```csharp
_mockRepository.Verify(
    r => r.GetByIdAsync(It.IsAny<int>()),
    Times.AtLeastOnce,
    "Should be called at least once"
);
```

### **Verify with Argument Matching**
```csharp
_mockRepository.Verify(
    r => r.AddAsync(It.Is<Flight>(f => f.Id == 1)),
    Times.Once,
    "Should add flight with ID 1"
);
```

---

## ✅ **AAA Pattern Summary**

### **Arrange Phase**
- Setup mock return values
- Create test data
- Configure expected behavior

### **Act Phase**
- Call service method
- Execute test scenario

### **Assert Phase**
- Verify result matches expected
- Verify exceptions thrown
- Verify mock calls

---

## 📚 **Testing Best Practices**

✅ **One Assertion Focus**
- Each test verifies one behavior
- Clear test name reflects what's tested

✅ **Descriptive Names**
- `GetFlightAsync_WithValidId_ReturnsFlight`
- Format: `MethodName_Scenario_ExpectedResult`

✅ **Isolated Tests**
- No dependencies between tests
- Each test sets up its own data

✅ **Mock Verification**
- Verify dependencies were called correctly
- Verify correct number of calls
- Verify correct arguments

✅ **Test Data**
- Use realistic test values
- Create minimal test entities
- Focus on relevant properties

---

## 🔍 **Common Testing Scenarios**

| Scenario | Pattern | Verification |
|----------|---------|--------------|
| Happy Path | Valid input → Success | Assert result + verify calls |
| Exception | Invalid input → Exception | Assert exception + verify no state change |
| Cache Hit | Cached data → Return cached | Verify repository not called |
| Cache Miss | No cache → Query DB → Cache | Verify repository called + cache set |
| Update | Modify entity → Save → Invalidate | Verify update + verify cache removal |

---

## 🚀 **Running Tests**

### **Run All Tests**
```bash
dotnet test
```

### **Run Specific Test Class**
```bash
dotnet test --filter "ClassName=FlightServiceTests"
```

### **Run Specific Test**
```bash
dotnet test --filter "Name~GetFlightAsync_WithValidId"
```

### **With Code Coverage**
```bash
dotnet test /p:CollectCoverage=true
```

---

## 📦 **Required NuGet Packages**

```xml
<PackageReference Include="xunit" Version="2.6.0" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.0" />
<PackageReference Include="Moq" Version="4.20.0" />
<PackageReference Include="AutoMapper" Version="12.0.1" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
```

---

**Status**: ✅ **UNIT TEST SPECIFICATION COMPLETE**  
**Framework**: xUnit  
**Mocking**: Moq  
**Pattern**: AAA (Arrange-Act-Assert)  
**Coverage**: 7 Test Cases

---

**Ready to implement unit tests! 🚀**
