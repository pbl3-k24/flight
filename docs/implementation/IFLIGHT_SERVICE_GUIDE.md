# IFlightService - Service Interface Guide

## ✅ Overview

The `IFlightService` interface defines the contract for flight-related business operations following the 02_APPLICATION_LAYER_GUIDE.md patterns.

**Location**: `API/Application/Interfaces/IFlightService.cs`

---

## 📋 Service Interface Specification

### Service Responsibilities

The `IFlightService` implements:
- ✅ Business logic coordination
- ✅ Data validation and constraints
- ✅ DTO mapping and transformation
- ✅ Repository orchestration
- ✅ Domain entity management
- ✅ Exception handling and translation

### Methods Overview

| Method | Input | Output | Purpose |
|--------|-------|--------|---------|
| `GetFlightAsync` | Flight ID | FlightResponseDto | Retrieve single flight |
| `SearchFlightsAsync` | FlightSearchDto | IEnumerable<FlightResponseDto> | Search with criteria |
| `CreateFlightAsync` | FlightCreateDto | FlightResponseDto | Create new flight |
| `UpdateFlightAsync` | ID + FlightUpdateDto | FlightResponseDto | Update existing flight |
| `DeleteFlightAsync` | Flight ID | Task | Delete flight |

---

## 🔄 Method Details

### 1. GetFlightAsync(int id)

**Signature**:
```csharp
Task<FlightResponseDto> GetFlightAsync(int id);
```

**Purpose**: Retrieve a single flight by ID with all details.

**Input**:
- `id` (int): The flight ID to retrieve

**Output**:
- `FlightResponseDto`: Complete flight information including:
  - Flight details (number, airline, aircraft)
  - Airport information (departure and arrival)
  - Seat information (total and available)
  - Pricing (base price)
  - Status (Active, Cancelled, Delayed, Completed)
  - Timestamps (created, updated)

**Exceptions**:
- `FlightNotFoundException` (404): Flight with given ID not found

**Business Logic**:
```
1. Validate input (id > 0)
2. Check cache for flight
3. If cached, return cached FlightResponseDto
4. Query repository for flight
5. If not found, throw FlightNotFoundException
6. Load related airport data
7. Map Flight entity → FlightResponseDto
8. Cache the result (TTL: 1 hour)
9. Return FlightResponseDto
```

**Usage**:
```csharp
// In controller
var flight = await _flightService.GetFlightAsync(1);

// Returns FlightResponseDto with all details
{
  "id": 1,
  "flightNumber": "AA100",
  "departureAirportId": 1,
  "departureAirportName": "Los Angeles International",
  "departureAirportCode": "LAX",
  ...
}
```

---

### 2. SearchFlightsAsync(FlightSearchDto criteria)

**Signature**:
```csharp
Task<IEnumerable<FlightResponseDto>> SearchFlightsAsync(FlightSearchDto criteria);
```

**Purpose**: Search for flights matching specific criteria (route, date, seat class, passenger count).

**Input**:
- `criteria` (FlightSearchDto):
  - `DepartureAirportId` (int): Required - departure airport
  - `ArrivalAirportId` (int): Required - arrival airport
  - `DepartureDate` (DateTime): Required - departure date
  - `SeatClass` (string, nullable): Optional - seat class filter
  - `PassengerCount` (int): Required - number of passengers

**Output**:
- `IEnumerable<FlightResponseDto>`: Collection of matching flights sorted by departure time

**Exceptions**:
- `ValidationException` (400): Invalid search criteria

**Business Logic**:
```
1. Validate search criteria:
   - DepartureAirportId > 0
   - ArrivalAirportId > 0
   - ArrivalAirportId != DepartureAirportId
   - DepartureDate is not in past
   - PassengerCount > 0

2. Query database:
   - Filter by departure airport
   - Filter by arrival airport
   - Filter by departure date (date only, not time)
   - Filter by status = Active only
   - Filter by seat class if provided
   
3. Filter results:
   - Keep only flights with available_seats >= passenger_count
   
4. Sort:
   - By departure time (ascending)
   
5. Map entities → FlightResponseDto
   
6. Cache results (TTL: 30 minutes)
   
7. Return collection
```

**Usage Example**:
```csharp
// Search flights from LAX to JFK on April 15 for 2 passengers
var criteria = new FlightSearchDto
{
    DepartureAirportId = 1,      // LAX
    ArrivalAirportId = 2,        // JFK
    DepartureDate = new DateTime(2026, 4, 15),
    SeatClass = "Economy",
    PassengerCount = 2
};

var flights = await _flightService.SearchFlightsAsync(criteria);

// Returns:
[
  {
    "id": 1,
    "flightNumber": "AA100",
    "departureTime": "2026-04-15T08:00:00Z",
    "availableSeats": 45,
    ...
  },
  {
    "id": 5,
    "flightNumber": "AA200",
    "departureTime": "2026-04-15T14:00:00Z",
    "availableSeats": 12,
    ...
  }
]
```

**Optimization**:
- Cache popular routes
- Index on (departure_airport_id, arrival_airport_id, departure_time)
- Pagination for large result sets

---

### 3. CreateFlightAsync(FlightCreateDto dto)

**Signature**:
```csharp
Task<FlightResponseDto> CreateFlightAsync(FlightCreateDto dto);
```

**Purpose**: Create a new flight with initial seat availability.

**Input**:
- `dto` (FlightCreateDto):
  - `FlightNumber` (string): Flight designation
  - `DepartureAirportId` (int): Departure airport
  - `ArrivalAirportId` (int): Arrival airport
  - `DepartureTime` (DateTime): Scheduled departure
  - `ArrivalTime` (DateTime): Scheduled arrival
  - `TotalSeats` (int): Total seat capacity
  - `BasePrice` (decimal): Base price per seat

**Output**:
- `FlightResponseDto`: Newly created flight with ID and default status

**Exceptions**:
- `FlightNotFoundException`: Referenced airport not found
- `ValidationException`: Invalid flight data or business rule violation

**Business Logic**:
```
1. Validate DTO:
   - FlightNumber not empty
   - DepartureAirportId > 0
   - ArrivalAirportId > 0
   - TotalSeats > 0
   - BasePrice >= 0

2. Validate business rules:
   - DepartureAirportId != ArrivalAirportId
   - DepartureTime < ArrivalTime
   - DepartureTime is in future
   - Airports exist
   - Flight number is unique (or allowed to be duplicated)

3. Start transaction

4. Create Flight entity:
   - Set properties from DTO
   - Set Status = Active
   - Set AvailableSeats = TotalSeats
   - Set CreatedAt = DateTime.UtcNow
   - Set UpdatedAt = DateTime.UtcNow

5. Save to repository:
   - _flightRepository.AddAsync(flight)
   - _unitOfWork.SaveChangesAsync()

6. Commit transaction

7. Invalidate cache:
   - Remove any cached flight lists
   - Clear search results cache

8. Trigger events:
   - FlightCreatedEvent

9. Map Flight → FlightResponseDto

10. Return response
```

**Usage Example**:
```csharp
var createDto = new FlightCreateDto
{
    FlightNumber = "AA100",
    DepartureAirportId = 1,
    ArrivalAirportId = 2,
    DepartureTime = new DateTime(2026, 4, 15, 8, 0, 0),
    ArrivalTime = new DateTime(2026, 4, 15, 13, 0, 0),
    TotalSeats = 150,
    BasePrice = 299.99m
};

var flight = await _flightService.CreateFlightAsync(createDto);

// Returns FlightResponseDto with:
// - Assigned ID
// - Status = "Active"
// - AvailableSeats = 150
// - CreatedAt = now
```

---

### 4. UpdateFlightAsync(int id, FlightUpdateDto dto)

**Signature**:
```csharp
Task<FlightResponseDto> UpdateFlightAsync(int id, FlightUpdateDto dto);
```

**Purpose**: Update an existing flight's information.

**Input**:
- `id` (int): Flight ID to update
- `dto` (FlightUpdateDto):
  - `FlightNumber` (string, nullable): Updated flight number
  - `DepartureAirportId` (int, nullable): Updated departure airport
  - `ArrivalAirportId` (int, nullable): Updated arrival airport
  - `DepartureTime` (DateTime, nullable): Updated departure time
  - `ArrivalTime` (DateTime, nullable): Updated arrival time
  - `Airline` (string, nullable): Updated airline
  - `AircraftModel` (string, nullable): Updated aircraft model
  - `TotalSeats` (int, nullable): Updated total seats
  - `BasePrice` (decimal, nullable): Updated base price

**Output**:
- `FlightResponseDto`: Updated flight details

**Exceptions**:
- `FlightNotFoundException`: Flight not found
- `ValidationException`: Invalid update data or business rule violation

**Business Logic**:
```
1. Validate id > 0

2. Fetch existing flight:
   - If not found, throw FlightNotFoundException

3. Validate update eligibility:
   - If flight status is Completed/Cancelled:
     - Only allow limited updates (price, aircraft model)
   - If flights have active bookings:
     - Cannot change airport or time significantly

4. Apply changes (only if provided in DTO):
   - If FlightNumber provided: validate unique, update
   - If DepartureAirportId provided: validate exists, update
   - If ArrivalAirportId provided: validate exists, update
   - If DepartureTime provided: validate constraints, update
   - If ArrivalTime provided: validate constraints, update
   - If TotalSeats provided: 
     - Validate >= current bookings
     - Update and recalculate available seats
   - If BasePrice provided: validate >= 0, update
   - If Airline provided: update
   - If AircraftModel provided: update

5. Validate updated constraints:
   - DepartureTime < ArrivalTime
   - DepartureAirportId != ArrivalAirportId
   - New TotalSeats >= passenger count of existing bookings

6. Start transaction

7. Update flight:
   - Set UpdatedAt = DateTime.UtcNow
   - Save changes
   - Commit transaction

8. Invalidate cache:
   - Remove flight from cache
   - Clear search results
   - Clear flight list cache

9. Trigger events:
   - FlightUpdatedEvent

10. Map updated entity → FlightResponseDto

11. Return response
```

**Design - Nullable Properties**:
Only properties included in the DTO (non-null) will be updated. This allows partial updates:

```csharp
// Update only the price and aircraft model
var updateDto = new FlightUpdateDto
{
    BasePrice = 349.99m,
    AircraftModel = "Boeing 777"
    // All other properties are null (not updated)
};

var updatedFlight = await _flightService.UpdateFlightAsync(1, updateDto);
```

**Usage Example**:
```csharp
// Partial update - only change the price
var dto = new FlightUpdateDto { BasePrice = 349.99m };
var flight = await _flightService.UpdateFlightAsync(1, dto);

// Full update - change multiple fields
var dto = new FlightUpdateDto
{
    DepartureTime = new DateTime(2026, 4, 16, 9, 0, 0),
    ArrivalTime = new DateTime(2026, 4, 16, 14, 0, 0),
    BasePrice = 329.99m
};
var flight = await _flightService.UpdateFlightAsync(1, dto);
```

---

### 5. DeleteFlightAsync(int id)

**Signature**:
```csharp
Task DeleteFlightAsync(int id);
```

**Purpose**: Delete a flight from the system.

**Input**:
- `id` (int): Flight ID to delete

**Output**:
- `Task`: Completed task (no return value)

**Exceptions**:
- `FlightNotFoundException`: Flight not found
- `InvalidOperationException`: Flight has active bookings

**Business Logic**:
```
1. Validate id > 0

2. Fetch flight:
   - If not found, throw FlightNotFoundException

3. Check deletion eligibility:
   - Count active bookings for flight
   - If active bookings exist:
     - Throw InvalidOperationException
       "Cannot delete flight with active bookings"
   - If flight status is Completed:
     - Allow deletion (no active passengers)

4. Option A: Soft Delete (recommended for audit)
   - Set Status = Cancelled
   - Update UpdatedAt = DateTime.UtcNow
   - Save changes

5. Option B: Hard Delete (only if no audit needed)
   - Delete flight from repository
   - Delete related crew assignments
   - Commit transaction

6. Invalidate cache:
   - Remove flight from cache
   - Clear search results
   - Clear flight list

7. Trigger events:
   - FlightDeletedEvent

8. Return completed task
```

**Usage Example**:
```csharp
// Delete a flight
await _flightService.DeleteFlightAsync(1);

// If flight has bookings, throws:
// InvalidOperationException: "Cannot delete flight with active bookings"
```

**Safety Considerations**:
```
BEFORE Delete - Check:
✓ No pending bookings
✓ No checked-in passengers
✓ Flight date is in the past (optional)
✓ All related records can be handled
```

---

## 🏗️ Service Architecture

### Service Layer Position in Architecture

```
┌────────────────────────────────┐
│      API Layer                 │
│   (Controllers, DTOs)          │
└────────────┬───────────────────┘
             │ DTO Input
             ▼
┌────────────────────────────────┐
│  Application Layer             │
│  (IFlightService)              │
│  - Validation                  │
│  - Mapping (DTO ↔ Entity)      │
│  - Business Logic              │
│  - Transaction Management      │
│  - Cache Management            │
└────────────┬───────────────────┘
             │ Entity Operations
             ▼
┌────────────────────────────────┐
│  Domain Layer                  │
│  - Flight Entity               │
│  - Business Rules              │
│  - Domain Events               │
│  - Value Objects               │
└────────────┬───────────────────┘
             │ Repository Operations
             ▼
┌────────────────────────────────┐
│  Infrastructure Layer          │
│  - IFlightRepository           │
│  - DbContext                   │
│  - Database                    │
└────────────────────────────────┘
```

### Dependency Chain

```csharp
// Service depends on:
public class FlightService : IFlightService
{
    private readonly IFlightRepository _flightRepository;
    private readonly IAirportRepository _airportRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<FlightService> _logger;
    private readonly ICache _cache;
}
```

---

## 📝 Data Flow Patterns

### Create Flow
```
API Request (FlightCreateDto)
    ↓
Controller → Service.CreateFlightAsync(dto)
    ↓
Service validates DTO
    ↓
Service maps DTO → Flight entity
    ↓
Service calls Repository.AddAsync(flight)
    ↓
Service triggers Transaction.SaveAsync()
    ↓
Service invalidates cache
    ↓
Service maps entity → FlightResponseDto
    ↓
Service returns response
    ↓
Controller returns 201 Created
```

### Search Flow
```
API Request (FlightSearchDto)
    ↓
Controller → Service.SearchFlightsAsync(criteria)
    ↓
Service validates criteria
    ↓
Service checks cache for results
    ↓
If cached: return cached results
    ↓
If not cached:
  - Query repository with filters
  - Filter by seat availability
  - Sort by departure time
  - Map entities → FlightResponseDto list
  - Cache results (TTL: 30 min)
    ↓
Service returns list
    ↓
Controller returns 200 OK
```

### Update Flow
```
API Request (int id, FlightUpdateDto)
    ↓
Controller → Service.UpdateFlightAsync(id, dto)
    ↓
Service fetches existing flight
    ↓
Service validates update eligibility
    ↓
Service applies changes (non-null properties only)
    ↓
Service validates updated entity
    ↓
Service calls Repository.UpdateAsync(flight)
    ↓
Service triggers Transaction.SaveAsync()
    ↓
Service invalidates cache
    ↓
Service maps entity → FlightResponseDto
    ↓
Service returns response
    ↓
Controller returns 200 OK
```

---

## 🔍 Validation Strategy

### Input Validation (DTO Level)

**FlightCreateDto**:
```csharp
if (string.IsNullOrEmpty(dto.FlightNumber))
    throw new ValidationException("FlightNumber", "Flight number is required");

if (dto.DepartureAirportId <= 0)
    throw new ValidationException("DepartureAirportId", "Departure airport must be valid");

if (dto.ArrivalAirportId <= 0)
    throw new ValidationException("ArrivalAirportId", "Arrival airport must be valid");

if (dto.TotalSeats <= 0)
    throw new ValidationException("TotalSeats", "Total seats must be greater than 0");

if (dto.BasePrice < 0)
    throw new ValidationException("BasePrice", "Base price cannot be negative");
```

### Business Logic Validation (Service Level)

```csharp
// Airport validation
if (departureAirport == null)
    throw new FlightNotFoundException(dto.DepartureAirportId);

if (arrivalAirport == null)
    throw new FlightNotFoundException(dto.ArrivalAirportId);

// Constraint validation
if (dto.DepartureAirportId == dto.ArrivalAirportId)
    throw new ValidationException("Airports cannot be the same");

if (dto.DepartureTime >= dto.ArrivalTime)
    throw new ValidationException("Departure must be before arrival");

if (dto.DepartureTime <= DateTime.UtcNow)
    throw new ValidationException("Departure cannot be in the past");

// Update eligibility
if (existingFlight.Status == FlightStatus.Cancelled)
    throw new InvalidOperationException("Cannot update cancelled flight");

if (existingFlight.Bookings.Count(b => b.Status != BookingStatus.Cancelled) > 0)
{
    // Can update price, aircraft, but not airports/times
    if (dto.DepartureTime != null || dto.ArrivalTime != null)
        throw new InvalidOperationException("Cannot change departure/arrival of flight with active bookings");
}
```

---

## 💾 Transaction Management

### Pattern 1: Create with Transaction

```csharp
public async Task<FlightResponseDto> CreateFlightAsync(FlightCreateDto dto)
{
    using (var transaction = await _unitOfWork.BeginTransactionAsync())
    {
        try
        {
            var flight = new Flight { /* ... */ };
            await _flightRepository.AddAsync(flight);
            await _unitOfWork.SaveChangesAsync();
            
            transaction.Commit();
            
            _cache.Invalidate("flights:*");
            return _mapper.Map<FlightResponseDto>(flight);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

### Pattern 2: Update with Validation

```csharp
public async Task<FlightResponseDto> UpdateFlightAsync(int id, FlightUpdateDto dto)
{
    var flight = await _flightRepository.GetByIdAsync(id);
    if (flight == null)
        throw new FlightNotFoundException(id);
    
    // Validate update eligibility
    ValidateUpdateEligibility(flight, dto);
    
    // Apply changes
    ApplyUpdates(flight, dto);
    
    // Validate constraints
    ValidateFlightConstraints(flight);
    
    using (var transaction = await _unitOfWork.BeginTransactionAsync())
    {
        try
        {
            await _flightRepository.UpdateAsync(flight);
            await _unitOfWork.SaveChangesAsync();
            transaction.Commit();
            
            _cache.Remove($"flight:{id}");
            return _mapper.Map<FlightResponseDto>(flight);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

---

## 🔐 Exception Handling

### Exception Mapping

| Scenario | Exception | HTTP Status |
|----------|-----------|------------|
| Flight not found | `FlightNotFoundException` | 404 |
| Airport not found | `FlightNotFoundException` | 404 |
| Invalid input | `ValidationException` | 400 |
| Time constraint violated | `ValidationException` | 400 |
| Airport same as destination | `ValidationException` | 400 |
| Flight has active bookings | `InvalidOperationException` | 400 |
| Update ineligible | `InvalidOperationException` | 400 |
| Unexpected error | `Exception` | 500 |

---

## 🎯 Design Principles

✅ **Single Responsibility**
- Service handles orchestration only
- Domain entities handle business logic
- Repositories handle persistence

✅ **Dependency Inversion**
- Depends on interfaces (IFlightRepository, IAirportRepository)
- Not concrete implementations

✅ **Transaction Management**
- All writes within transactions
- Rollback on error
- Cache invalidation after commit

✅ **Async/Await**
- All I/O operations async
- No blocking calls
- Proper task composition

✅ **Separation of Concerns**
- Input: DTOs
- Processing: Service methods
- Output: DTOs
- Persistence: Repositories

---

## 📚 Related Files

- **Service Implementation** (To be created)
- **DTOs**: `FlightCreateDto.cs`, `FlightUpdateDto.cs`, `FlightResponseDto.cs`, `FlightSearchDto.cs`
- **Domain Entity**: `Flight.cs`
- **Repository Interface**: `IFlightRepository.cs` (To be created)
- **Application Guide**: `.github/backend/02_APPLICATION_LAYER_GUIDE.md`

---

**Version**: 1.0  
**Status**: Complete ✅
