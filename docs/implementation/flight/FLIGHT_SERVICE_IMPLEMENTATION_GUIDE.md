# FlightService Implementation - Complete Guide

## ✅ Overview

Successfully implemented the **FlightService** class implementing **IFlightService** with full business logic, caching strategy, and error handling following the 02_APPLICATION_LAYER_GUIDE.md algorithms.

**Location**: `API/Application/Services/FlightService.cs`

---

## 📦 Components Created

### 1. IFlightRepository Interface
**File**: `API/Application/Interfaces/IFlightRepository.cs`

**Responsibilities**:
- Database queries for flights
- CRUD operations
- Filtering and searching
- Transaction management

**Methods**:
- `GetByIdAsync(id)` - Get single flight
- `GetAllAsync(skip, take)` - Get paginated list
- `GetCountAsync()` - Get total count
- `SearchAsync(...)` - Search with criteria
- `AddAsync(flight)` - Create flight
- `UpdateAsync(flight)` - Update flight
- `DeleteAsync(id)` - Delete flight
- `ExistsAsync(id)` - Check existence
- `SaveChangesAsync()` - Persist changes

---

### 2. ICacheService Interface
**File**: `API/Application/Interfaces/ICacheService.cs`

**Responsibilities**:
- Store and retrieve cached data
- Manage cache expiration (TTL)
- Pattern-based cache invalidation
- Cache lifecycle management

**Methods**:
- `GetAsync<T>(key)` - Retrieve cached value
- `SetAsync<T>(key, value, ttl)` - Store with TTL
- `RemoveAsync(key)` - Remove specific key
- `RemoveByPatternAsync(pattern)` - Remove by pattern
- `ExistsAsync(key)` - Check key existence
- `ClearAllAsync()` - Clear all cache

---

### 3. FlightService Class
**File**: `API/Application/Services/FlightService.cs`

**Implements**: IFlightService

**Dependencies** (Injected):
- `IFlightRepository _flightRepository` - Data access
- `ICacheService _cacheService` - Caching
- `ILogger<FlightService> _logger` - Logging

---

## 🎯 Method Implementations

### 1. GetFlightAsync(id)

**Algorithm**:
```
1. Validate id > 0
2. Check cache with key "flight_{id}"
3. If cached, return immediately
4. Query repository by ID
5. If not found, throw FlightNotFoundException
6. Map entity to ResponseDto
7. Cache result with 1-hour TTL
8. Log successful retrieval
9. Return ResponseDto
```

**Cache Strategy**:
- Key: `flight_{id}` (e.g., `flight_1`)
- TTL: 1 hour
- Returns immediately from cache if available

**Example**:
```csharp
var flight = await _flightService.GetFlightAsync(1);
// Returns FlightResponseDto with complete flight information
```

---

### 2. SearchFlightsAsync(criteria)

**Algorithm**:
```
1. Validate search criteria:
   - DepartureAirportId > 0
   - ArrivalAirportId > 0
   - Airports are different
   - DepartureDate not in past
   - PassengerCount > 0

2. Generate cache key from criteria

3. Check cache for search results

4. If cached, return (filtered by seat class if provided)

5. Query repository with filters:
   - DepartureAirportId
   - ArrivalAirportId
   - DepartureDate
   - MinAvailableSeats

6. Map entities to DTOs

7. Filter by seat class if provided

8. Cache results with 30-minute TTL

9. Log search operation

10. Return results
```

**Cache Strategy**:
- Key: `flights_search_{departure}_{arrival}_{date_ticks}`
- TTL: 30 minutes
- Invalidated on create/update/delete operations

**Input Validation**:
```csharp
// All fields required and validated
criteria.DepartureAirportId > 0 ✓
criteria.ArrivalAirportId > 0 ✓
criteria.DepartureAirportId != criteria.ArrivalAirportId ✓
criteria.DepartureDate.Date >= today ✓
criteria.PassengerCount > 0 ✓
```

**Example**:
```csharp
var searchDto = new FlightSearchDto
{
    DepartureAirportId = 1,
    ArrivalAirportId = 2,
    DepartureDate = new DateTime(2026, 4, 15),
    SeatClass = "Economy",
    PassengerCount = 2
};

var flights = await _flightService.SearchFlightsAsync(searchDto);
// Returns IEnumerable<FlightResponseDto> matching criteria
```

---

### 3. CreateFlightAsync(dto)

**Algorithm**:
```
1. Validate DTO:
   - FlightNumber not empty
   - DepartureAirportId > 0
   - ArrivalAirportId > 0
   - DepartureAirportId != ArrivalAirportId
   - DepartureTime < ArrivalTime
   - DepartureTime > now
   - TotalSeats > 0
   - BasePrice >= 0

2. Map DTO to Flight entity

3. Initialize properties:
   - AvailableSeats = TotalSeats
   - Status = Active
   - CreatedAt = UtcNow
   - UpdatedAt = UtcNow

4. Add to repository

5. Save changes

6. Invalidate search caches (flights_search_*)

7. Map entity to ResponseDto

8. Log creation with new ID

9. Return ResponseDto
```

**Validation Rules**:
- Flight number required
- Valid airports (both positive IDs)
- Different departure and arrival airports
- Departure before arrival
- Future departure time
- Positive seat count
- Non-negative price

**Example**:
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
// Returns FlightResponseDto with assigned ID and default Status = Active
```

---

### 4. UpdateFlightAsync(id, dto)

**Algorithm**:
```
1. Validate id > 0

2. Fetch existing flight from repository

3. If not found, throw FlightNotFoundException

4. Apply changes from update DTO (only non-null values):
   - FlightNumber (if provided)
   - DepartureAirportId (if provided)
   - ArrivalAirportId (if provided)
   - DepartureTime (if provided)
   - ArrivalTime (if provided)
   - Airline (if provided)
   - AircraftModel (if provided)
   - TotalSeats (if provided, with validation)
   - BasePrice (if provided, with validation)

5. Validate updated entity:
   - DepartureAirportId != ArrivalAirportId
   - DepartureTime < ArrivalTime
   - TotalSeats >= reserved seats

6. Update timestamp: UpdatedAt = UtcNow

7. Save to repository

8. Invalidate flight cache (flight_{id})

9. Invalidate search caches (flights_search_*)

10. Map to ResponseDto

11. Log update

12. Return updated ResponseDto
```

**Nullable Properties** (Partial Update):
Only provided values are updated; null values are skipped.

```csharp
// Partial update - only change price
var updateDto = new FlightUpdateDto
{
    BasePrice = 349.99m
    // All other properties are null (not updated)
};

var updated = await _flightService.UpdateFlightAsync(1, updateDto);
```

**Seat Reduction Validation**:
```csharp
// Cannot reduce seats below reserved seats
int reservedSeats = flight.TotalSeats - flight.AvailableSeats;
if (newTotalSeats < reservedSeats)
    throw new ValidationException("Cannot reduce seats below reserved count");
```

---

### 5. DeleteFlightAsync(id)

**Algorithm**:
```
1. Validate id > 0

2. Fetch flight from repository

3. If not found, throw FlightNotFoundException

4. Check deletion eligibility:
   - Calculate reserved seats = TotalSeats - AvailableSeats
   - If reserved > 0, throw InvalidOperationException
   - Cannot delete flights with active bookings

5. Delete from repository

6. Save changes

7. Invalidate flight cache (flight_{id})

8. Invalidate search caches (flights_search_*)

9. Log deletion

10. Return completed task
```

**Deletion Safety**:
- Cannot delete if seats are reserved
- Prevents orphaned bookings
- Logs detailed information

**Example**:
```csharp
try
{
    await _flightService.DeleteFlightAsync(1);
    // Flight deleted successfully
}
catch (InvalidOperationException ex)
{
    // Flight has active bookings - cannot delete
    Console.WriteLine(ex.Message); // "Cannot delete flight with 45 active bookings."
}
```

---

## 💾 Cache Strategy

### Cache Keys

| Operation | Key Pattern | TTL | Invalidation |
|-----------|------------|-----|--------------|
| GetFlight | `flight_{id}` | 1 hour | On Update/Delete |
| Search | `flights_search_{dep}_{arr}_{date}` | 30 min | On Create/Update/Delete |

### Cache Invalidation

```csharp
// Single flight invalidation
await _cacheService.RemoveAsync("flight_1");

// Pattern-based invalidation
await _cacheService.RemoveByPatternAsync("flights_search_*");
```

### Cache Flow

**Get Flight**:
```
GetFlightAsync(1)
  ↓
Check cache "flight_1"
  ↓
  ├─ HIT: Return cached FlightResponseDto ✓
  │
  └─ MISS: Query DB
    ↓
    Repository.GetByIdAsync(1)
    ↓
    Map to FlightResponseDto
    ↓
    Cache with 1-hour TTL
    ↓
    Return DTO
```

**Search Flights**:
```
SearchFlightsAsync(criteria)
  ↓
Generate cache key from criteria
  ↓
Check cache
  ↓
  ├─ HIT: Return cached list ✓
  │
  └─ MISS: Query DB
    ↓
    Repository.SearchAsync(...)
    ↓
    Map to DTOs
    ↓
    Cache with 30-min TTL
    ↓
    Return DTOs
```

**Create/Update/Delete**:
```
Operation successful
  ↓
Invalidate "flight_{id}"
  ↓
Invalidate "flights_search_*"
  ↓
Return result
```

---

## 🔐 Error Handling

### Exception Mapping

| Scenario | Exception | Message |
|----------|-----------|---------|
| ID ≤ 0 | ValidationException | "Flight ID must be greater than 0." |
| Flight not found | FlightNotFoundException | "Flight with ID {id} was not found." |
| Invalid search criteria | ValidationException | Specific field error |
| Cannot reduce seats | ValidationException | "Cannot reduce total seats below X" |
| Has active bookings | InvalidOperationException | "Cannot delete flight with X bookings." |
| Airports same | ValidationException | "Airports cannot be the same" |
| Invalid times | ValidationException | "Departure must be before arrival" |
| Future times | ValidationException | "Departure cannot be in the past" |
| Negative price | ValidationException | "Base price cannot be negative" |
| Empty flight number | ValidationException | "Flight number is required" |

---

## 📝 Logging Strategy

### Log Levels

**INFO**:
```csharp
_logger.LogInformation("Fetching flight with ID: {FlightId}", id);
_logger.LogInformation("Flight {FlightId} found in cache", id);
_logger.LogInformation("Successfully retrieved flight {FlightId}: {FlightNumber}", id, number);
_logger.LogInformation("Creating new flight: {FlightNumber}", number);
_logger.LogInformation("Successfully created flight {FlightId}: {FlightNumber}", id, number);
_logger.LogInformation("Updating flight with ID: {FlightId}", id);
_logger.LogInformation("Successfully updated flight {FlightId}: {FlightNumber}", id, number);
_logger.LogInformation("Deleting flight with ID: {FlightId}", id);
_logger.LogInformation("Successfully deleted flight {FlightId}", id);
```

**WARNING**:
```csharp
_logger.LogWarning("Flight with ID {FlightId} not found", id);
_logger.LogWarning("Flight with ID {FlightId} not found for update", id);
_logger.LogWarning("Cannot delete flight {FlightId} with {ReservedSeats} reserved seats", id, count);
_logger.LogWarning("Failed to delete flight with ID: {FlightId}", id);
```

**ERROR**:
```csharp
_logger.LogError(ex, "Error retrieving flight with ID: {FlightId}", id);
_logger.LogError(ex, "Error creating flight");
_logger.LogError(ex, "Error updating flight with ID: {FlightId}", id);
_logger.LogError(ex, "Error deleting flight with ID: {FlightId}", id);
_logger.LogError(ex, "Error searching flights");
```

---

## 🏗️ Architecture Integration

```
FlightsController (API Layer)
    ↓ (DTOs)
FlightService (Application Layer) ← IFlightService interface ✓
    ↓
    ├─→ IFlightRepository (Database)
    ├─→ ICacheService (Cache)
    └─→ ILogger (Logging)
```

**Dependencies Needed**:
- ⏳ IFlightRepository implementation
- ⏳ ICacheService implementation (Redis or in-memory)
- ✓ ILogger (built-in)

---

## 🔄 Data Flow Examples

### Example 1: Get Flight
```
Client GET /api/v1/flights/1
    ↓
FlightsController.GetById(1)
    ↓
FlightService.GetFlightAsync(1)
    ├─ Check cache "flight_1"
    ├─ Not found in cache
    ├─ Query repository
    ├─ Found: Flight entity
    ├─ Map to FlightResponseDto
    ├─ Cache with 1-hour TTL
    ├─ Log: "Successfully retrieved flight 1: AA100"
    └─ Return DTO
    ↓
Controller returns 200 OK with FlightResponseDto
```

### Example 2: Create Flight
```
Client POST /api/v1/flights
Body: FlightCreateDto
    ↓
FlightsController.Create(dto)
    ↓
FlightService.CreateFlightAsync(dto)
    ├─ Validate all fields
    ├─ Create Flight entity
    ├─ Set AvailableSeats = TotalSeats
    ├─ Set Status = Active
    ├─ Add to repository
    ├─ Save changes
    ├─ Invalidate flights_search_* cache
    ├─ Map to FlightResponseDto
    ├─ Log: "Successfully created flight 1: AA100"
    └─ Return DTO with ID
    ↓
Controller returns 201 Created with FlightResponseDto and Location header
```

### Example 3: Search Flights
```
Client POST /api/v1/flights/search
Body: FlightSearchDto (LAX → JFK, 2026-04-15, 2 passengers)
    ↓
FlightsController.Search(criteria)
    ↓
FlightService.SearchFlightsAsync(criteria)
    ├─ Validate criteria
    ├─ Generate cache key from criteria
    ├─ Check cache
    ├─ Not found in cache
    ├─ Query repository
    │   WHERE DepartureAirportId = 1
    │   AND ArrivalAirportId = 2
    │   AND DepartureDate = 2026-04-15
    │   AND AvailableSeats >= 2
    │
    ├─ Found 3 flights
    ├─ Map to DTOs
    ├─ Filter by seat class (if provided)
    ├─ Cache with 30-minute TTL
    ├─ Log: "Flight search completed: Found 3 flights"
    └─ Return List<FlightResponseDto>
    ↓
Controller returns 200 OK with flight list
```

### Example 4: Update Flight
```
Client PUT /api/v1/flights/1
Body: FlightUpdateDto (only BasePrice = 349.99)
    ↓
FlightsController.Update(1, dto)
    ↓
FlightService.UpdateFlightAsync(1, dto)
    ├─ Fetch flight 1
    ├─ Found
    ├─ Apply changes:
    │   BasePrice = 349.99 (updated)
    │   Other fields = null (not updated)
    │
    ├─ Validate constraints
    ├─ Set UpdatedAt = now
    ├─ Save changes
    ├─ Invalidate cache "flight_1"
    ├─ Invalidate cache "flights_search_*"
    ├─ Map to FlightResponseDto
    ├─ Log: "Successfully updated flight 1: AA100"
    └─ Return DTO
    ↓
Controller returns 200 OK with updated FlightResponseDto
```

### Example 5: Delete Flight
```
Client DELETE /api/v1/flights/1
    ↓
FlightsController.Delete(1)
    ↓
FlightService.DeleteFlightAsync(1)
    ├─ Fetch flight 1
    ├─ Found
    ├─ Check reserved seats = 0
    ├─ Can delete
    ├─ Delete from repository
    ├─ Save changes
    ├─ Invalidate cache "flight_1"
    ├─ Invalidate cache "flights_search_*"
    ├─ Log: "Successfully deleted flight 1"
    └─ Return completed task
    ↓
Controller returns 204 No Content
```

**Error Example: Cannot Delete**
```
Client DELETE /api/v1/flights/5
    ↓
FlightService.DeleteFlightAsync(5)
    ├─ Fetch flight 5
    ├─ Found
    ├─ Check reserved seats = 45
    ├─ Cannot delete! ✗
    ├─ Log warning: "Cannot delete flight 5 with 45 reserved seats"
    └─ Throw InvalidOperationException
    ↓
Controller catches exception
    ↓
Controller returns 400 Bad Request
```

---

## ✅ Build Status

✅ **Compilation**: SUCCESSFUL  
✅ **Errors**: 0  
✅ **Warnings**: 0  
✅ **Ready for**: Repository implementation

---

## 📚 Related Files

**Existing**:
- ✅ IFlightService.cs (interface)
- ✅ FlightResponseDto.cs
- ✅ FlightCreateDto.cs
- ✅ FlightUpdateDto.cs
- ✅ FlightSearchDto.cs

**Created**:
- ✅ FlightService.cs (implementation)
- ✅ IFlightRepository.cs (interface)
- ✅ ICacheService.cs (interface)

**Still Needed**:
- ⏳ FlightRepository.cs (implementation)
- ⏳ CacheService.cs (implementation)
- ⏳ FlightsController.cs (controller)

---

## 🚀 Next Steps

1. **Create FlightRepository** - Implement database access
2. **Create CacheService** - Implement Redis or in-memory caching
3. **Create FlightsController** - Create REST endpoints
4. **Register in DI** - Add to Program.cs

---

**Version**: 1.0  
**Status**: Complete ✅  
**Ready for**: Integration Testing
