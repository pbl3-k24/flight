# 🧪 Module 5: Testing Guide

## 📋 Mục Đích
Unit tests, integration tests, test strategies, mocking patterns, test data.

---

## 🏗️ Testing Pyramid

```
                    [E2E Tests]
                   (API endpoints)
                    < 10% of tests
                          |
                    [Integration Tests]
                   (Services + Database)
                    ~ 20% of tests
                          |
                    [Unit Tests]
                   (Individual services)
                    ~ 70% of tests
```

---

## 🧬 Unit Testing Strategy

### 1. **Unit Test Purpose**
```
Test single unit in isolation:
  - Service methods
  - Business logic
  - Validators
  - Value objects

Not testing:
  - Database
  - External services (mocked)
  - Network calls (mocked)
  - File system (mocked)
```

### 2. **Unit Test Pattern (AAA)**
```
Arrange: Setup test data and mocks
Act:     Call method under test
Assert:  Verify results and mock calls

Example Structure:
  // Arrange
  var mockRepository = new Mock<IRepository>();
  var service = new Service(mockRepository.Object);
  mockRepository.Setup(r => r.GetAsync(1)).ReturnsAsync(entity);

  // Act
  var result = await service.GetAsync(1);

  // Assert
  Assert.NotNull(result);
  Assert.Equal(expected, result);
  mockRepository.Verify(r => r.GetAsync(1), Times.Once);
```

### 3. **Mocking Strategy**
```
What to Mock:
  ✓ Repositories (IRepository<T>)
  ✓ External services (IEmailService, IPaymentGateway)
  ✓ Cache service (ICacheService)
  ✓ Logger (ILogger<T>)

What NOT to Mock:
  ✗ Value objects
  ✗ DTOs
  ✗ Entities (use real or minimal setup)
  ✗ Core business logic

Setup Patterns:
  1. Default behavior: Returns null/empty
  2. Specific returns: ReturnsAsync(value)
  3. Multiple calls: SetupSequence
  4. Throw exceptions: Throws(ex)

Verification Patterns:
  - Times.Once: Called exactly once
  - Times.Never: Never called
  - Times.Exactly(2): Called exactly twice
  - Times.AtLeast(3): Called 3+ times
```

### 4. **Test Naming Convention**
```
Pattern: MethodName_Scenario_ExpectedResult

Examples:
  GetFlightAsync_WithValidId_ReturnsFlightResponseDto()
  GetFlightAsync_WithInvalidId_ThrowsFlightNotFoundException()
  CreateBookingAsync_WithInsufficientSeats_ThrowsException()
  ReserveSeats_WhenAvailable_ReducesAvailableCount()
  CalculatePrice_WithDemandFactor_AppliesMultiplier()

Benefits:
  - Clear test intent
  - Easy to find related tests
  - Readable test names = better documentation
```

### 5. **Common Unit Test Scenarios**

**Success Path:**
```
Test successful operation
- Mock dependencies to return valid data
- Call method
- Assert correct result returned
- Assert dependencies called correctly

Example:
  GetFlightAsync(1) → Returns FlightResponseDto
  Verify: Repository.GetByIdAsync called once
```

**Failure Path:**
```
Test business rule violations
- Condition is invalid
- Method throws domain exception
- Service handles gracefully

Example:
  ReserveSeats(100) with only 50 available
  → Throws InsufficientSeatsException
  → Available seats unchanged
```

**Edge Cases:**
```
Boundary conditions:
  - Null inputs
  - Empty collections
  - Zero/negative values
  - Maximum values
  - Special characters in strings

Example:
  ReserveSeats(0) → throws ArgumentException
  ReserveSeats(-1) → throws ArgumentException
  FlightNumber = "" → throws ArgumentException
```

**Async/Await:**
```
Test async methods
- Use async Task test methods
- Use await for service calls
- Don't use .Result or .Wait()

Example:
  [Fact]
  public async Task GetFlightAsync_ReturnsFlight()
  {
    var result = await service.GetFlightAsync(1);
    Assert.NotNull(result);
  }
```

---

## 🔗 Integration Testing Strategy

### 1. **Integration Test Purpose**
```
Test multiple components together:
  - Service + Repository
  - Service + Database
  - Service + Cache
  - Service + External Service calls (with test doubles)

NOT a full E2E test:
  - Still mocking external APIs
  - Using test database
  - No real user interaction
```

### 2. **Test Database Setup**
```
Options:
  1. In-memory database (EF Core InMemory)
     - Fast, isolated
     - Not real database behavior
     - Migrations not tested

  2. Real PostgreSQL in Docker
     - Realistic
     - Slower
     - Requires Docker
     - Tests migrations

  3. SQL Lite in memory
     - Portable
     - Close to real behavior
     - Migrations tested

Recommendation:
  - Unit tests: Use mocks
  - Integration tests: Use real database (PostgreSQL in Docker)
  - Local development: Use in-memory for speed
  - CI/CD pipeline: Use real database
```

### 3. **Test Fixtures**
```
Setup reusable test data:

FlightFixture:
  - Creates test flights
  - Creates test users
  - Inserts into database
  - Cleanup after test

BookingFixture:
  - Extends FlightFixture
  - Creates test bookings
  - Manages passengers

IAsyncLifetime Pattern:
  InitializeAsync() → Setup before test
  DisposeAsync() → Cleanup after test
```

### 4. **Integration Test Pattern**

```
Setup:
  1. Create test database
  2. Run migrations
  3. Seed test data
  4. Create real repositories

Act:
  1. Call service with real repository
  2. Service uses real database
  3. Mock external services only

Assert:
  1. Verify database state changed correctly
  2. Query database to confirm persistence
  3. Verify repository returned correct data

Cleanup:
  1. Rollback transaction or
  2. Delete test data or
  3. Restore backup
```

### 5. **Common Integration Test Scenarios**

**Create and Retrieve:**
```
1. Service.CreateFlightAsync(dto)
2. Verify flight inserted in database
3. Query database directly: Assert.NotNull(flight)
4. Call Service.GetFlightAsync(id)
5. Assert returned flight matches what's in database
```

**Update and Verify:**
```
1. Insert test flight
2. Service.UpdateFlightAsync(id, updateDto)
3. Query database
4. Assert changes persisted
5. Assert related entities updated (if necessary)
```

**Delete and Cascade:**
```
1. Insert flight with bookings
2. Service.DeleteFlightAsync(id)
3. Verify flight deleted
4. Verify bookings handled (deleted or marked cancelled)
5. Verify cache cleared
```

**Transaction Rollback:**
```
1. Start transaction
2. Perform operations
3. Simulate error
4. Verify transaction rolled back
5. Verify database unchanged
```

---

## 🎯 Test Coverage

### 1. **Coverage Goals**
```
Minimum Targets:
  - Services: 80%+ coverage
  - Repositories: 70%+ coverage
  - Validators: 90%+ coverage
  - Controllers: 60%+ (mainly error cases)
  - Value Objects: 95%+ coverage

Not every line needs test:
  - Auto-generated code (migrations)
  - Trivial getters/setters
  - Third-party libraries
  - Infrastructure setup
```

### 2. **Coverage Measurement**
```
Tools:
  - Coverlet (for .NET)
  - OpenCover (alternative)
  - ReportGenerator (visualize coverage)

Run coverage:
  dotnet test /p:CollectCoverageMetrics=true
  
View report:
  - HTML report generated
  - Identify uncovered lines
  - Prioritize testing gaps
```

### 3. **What to Test**
```
✓ Happy paths (success cases)
✓ Error paths (exceptions)
✓ Edge cases (boundaries)
✓ Validations (business rules)
✓ Calculations (complex logic)
✓ State changes (entity updates)
✓ Side effects (cache, logging)

✗ Trivial methods (auto-properties)
✗ Generated code
✗ Third-party libraries
✗ Database behavior (EF Core tested by Microsoft)
```

---

## 🔄 Test Data Management

### 1. **Builder Pattern for Test Data**
```
Fluent builders for complex objects:

var flight = new FlightBuilder()
  .WithFlightNumber("AA100")
  .WithDepartureTime(DateTime.Now.AddDays(1))
  .WithAvailableSeats(50)
  .Build();

Benefits:
  - Readable test setup
  - Reusable across tests
  - Easy to modify for specific test cases
  - Defaults for common scenarios
```

### 2. **Factory Pattern for Fixtures**
```
Centralized test data creation:

FlightFixtures:
  GetActiveFlightWithBookings() → Flight + Bookings
  GetCancelledFlight() → Cancelled flight
  GetFlightNearDeparture() → Flight departing in 2 hours
  GetOverbookedFlight() → Flight with no seats

Benefits:
  - Consistent test data
  - Reuse across tests
  - Easy to update test scenarios
```

### 3. **Seed Test Database**
```
Initialize with common data:

async Task SeedDatabase(DbContext context)
{
  await context.Airports.AddRangeAsync(GetAirports());
  await context.Airlines.AddRangeAsync(GetAirlines());
  await context.SaveChangesAsync();
}

Run before tests:
  - Same for all tests
  - Known starting state
  - Faster than creating per-test
```

---

## ⚙️ Mocking External Services

### 1. **Email Service Mock**
```
Mock Setup:
  _emailServiceMock.Setup(e => e.SendAsync(
    It.IsAny<string>(), 
    It.IsAny<string>()))
  .ReturnsAsync(true);

Verify:
  _emailServiceMock.Verify(
    e => e.SendAsync(
      It.Is<string>(s => s.Contains("booking")),
      It.IsAny<string>()),
    Times.Once);
```

### 2. **Payment Gateway Mock**
```
Mock Setup - Success:
  _paymentMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
  .ReturnsAsync(new PaymentResponse { IsSuccessful = true, TransactionId = "123" });

Mock Setup - Failure:
  _paymentMock.Setup(p => p.ProcessPaymentAsync(It.IsAny<PaymentRequest>()))
  .ReturnsAsync(new PaymentResponse { IsSuccessful = false, ErrorCode = "INSUFFICIENT_FUNDS" });

Test both paths:
  - Success scenario
  - Failure scenarios (all error codes)
```

### 3. **Cache Service Mock**
```
Mock Cache Hit:
  _cacheMock.Setup(c => c.GetAsync<FlightResponseDto>("flight_1"))
  .ReturnsAsync(new FlightResponseDto { Id = 1 });

Mock Cache Miss:
  _cacheMock.Setup(c => c.GetAsync<FlightResponseDto>("flight_999"))
  .ReturnsAsync((FlightResponseDto)null);

Verify Cache Operations:
  _cacheMock.Verify(c => c.SetAsync("flight_1", It.IsAny<FlightResponseDto>(), It.IsAny<TimeSpan?>()), Times.Once);
```

---

## 🚀 Continuous Testing

### 1. **Test Execution**
```
Local:
  dotnet test                    // Run all tests
  dotnet test --filter "Category=Unit"  // Run specific category
  dotnet test --no-build         // Skip build
  dotnet test --verbosity detailed  // See details

CI/CD Pipeline:
  - Run on every commit
  - Fail build if tests fail
  - Generate test reports
  - Track coverage trends
```

### 2. **Test Organization**
```
Tests should be:
  ✓ Fast (< 1 second per unit test)
  ✓ Independent (no order dependencies)
  ✓ Repeatable (same result every run)
  ✓ Clear (obvious intent)
  ✓ Isolated (mock external dependencies)
```

### 3. **Test Categories**
```
[Trait("Category", "Unit")]
public class ServiceUnitTests { }

[Trait("Category", "Integration")]
public class ServiceIntegrationTests { }

[Trait("Category", "Slow")]
public class PerformanceTests { }

Usage:
  dotnet test --filter "Category=Unit"  // Only unit tests
  dotnet test --filter "Category!=Slow" // Exclude slow tests
```

---

## 📊 Test Reporting

### 1. **Test Report**
```
Success:
  ✓ 250 tests passed in 45s
  ✓ 85% code coverage
  ✓ No skipped tests

Failure:
  ✗ 5 tests failed
  ! 2 tests skipped (known issues)
  ⚠ Coverage dropped to 82%

Display:
  - Test count and duration
  - Pass/fail ratio
  - Coverage percentage
  - Trend (vs previous run)
```

### 2. **Failure Analysis**
```
When test fails:
  1. Read error message clearly
  2. Check assertion (what expected vs actual)
  3. Look at test name (should describe intent)
  4. Review test data setup (Arrange section)
  5. Check mocks are configured correctly
  6. Run in debugger to step through
```

---

## ✅ Best Practices

1. **Test One Thing** - One assertion per test (or closely related)
2. **Clear Naming** - Test name explains what it tests
3. **No Test Dependencies** - Tests run independently
4. **Fast Execution** - Unit tests < 1s, integration < 5s
5. **Deterministic** - Same result every run
6. **No Flaky Tests** - Timing-dependent tests = bad
7. **Mock External Deps** - No real API/DB calls
8. **Arrange-Act-Assert** - Clear test structure
9. **Test Edge Cases** - Null, empty, boundaries
10. **Maintain Tests** - Keep tests updated with code

---

## 🚫 Anti-Patterns

❌ **Test All Implementation Details**
- Test behavior, not implementation
- Implementation can change, behavior stays same

❌ **Share Test Data Across Tests**
- Each test sets up its own data
- Order independence
- Failure isolation

❌ **Test External Systems**
- Mock external APIs/databases
- Test your code, not their code

❌ **Assertion in Arrange**
- Arrange: Setup
- Act: Execute
- Assert: Verify results

❌ **Long, Complex Tests**
- Keep tests small and focused
- Easy to understand and maintain

---

**Module**: Testing | **Version**: 1.0
