# 📍 CÁC LOGIC NGHIỆP VỤ ĐƯỢC XỬ LÝ Ở ĐÂU

## **Tổng Quan Kiến Trúc Clean Architecture**

```
┌─────────────────────────────────────────────────────────────────────┐
│                        API CLIENT                                   │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
                                 ↓
┌─────────────────────────────────────────────────────────────────────┐
│              📍 LAYER 1: CONTROLLERS (API Endpoints)                │
│  - BookingsController.cs                                            │
│  - FlightsController.cs (nếu có)                                    │
│                                                                     │
│  Trách nhiệm:                                                       │
│  ✅ Nhận HTTP request                                              │
│  ✅ Xác thực input cơ bản                                          │
│  ✅ Gọi Services                                                    │
│  ✅ Trả về HTTP response                                           │
│  ❌ KHÔNG xử lý business logic phức tạp                            │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
                                 ↓
┌─────────────────────────────────────────────────────────────────────┐
│     📍 LAYER 2: APPLICATION SERVICES (Business Logic Orchestration)│
│  - FlightService.cs                                                │
│  - BookingService.cs                                               │
│                                                                     │
│  Trách nhiệm:                                                       │
│  ✅ Điều phối business workflows                                   │
│  ✅ Gọi Domain entities để thực hiện business rules               │
│  ✅ Gọi Repositories để lấy/lưu dữ liệu                           │
│  ✅ Xử lý caching logic                                            │
│  ✅ Authorization checks                                           │
│  ❌ KHÔNG chứa pure business rules (đó là Domain's job)          │
└────────────────────────────────┬────────────────────────────────────┘
                                 │
                 ┌───────────────┼───────────────┐
                 ↓               ↓               ↓
    ┌────────────────┐  ┌────────────────┐  ┌──────────────┐
    │ LAYER 3: DOMAIN│  │  Repositories  │  │ Cache Service│
    │   ENTITIES     │  │                │  │              │
    │ (Business Rules)│  │ Data Access    │  │ Cache Logic  │
    └────────────────┘  └────────────────┘  └──────────────┘
```

---

## **📍 LAYER 1: CONTROLLERS (API Endpoints)**

### **File: `API/Controllers/BookingsController.cs`**

**Xử lý:**
- ❌ Logic kinh doanh PHỨC TẠP
- ✅ Validation đầu vào đơn giản (ID > 0, data format)
- ✅ Call services
- ✅ Xử lý HTTP status codes

```csharp
[HttpPost]
public async Task<ActionResult<BookingResponseDto>> Create([FromBody] BookingCreateDto dto)
{
    // 🔍 Chỉ validate cơ bản
    if (!ModelState.IsValid)
        return BadRequest(...);
    
    if (dto.FlightId <= 0)
        return BadRequest(...);
    
    if (dto.PassengerCount <= 0)
        return BadRequest(...);
    
    // 📞 Gọi Service để xử lý business logic
    var booking = await _bookingService.CreateBookingAsync(dto, userId);
    
    // 📤 Return HTTP response
    return CreatedAtAction(nameof(GetById), new { id = booking.Id }, booking);
}
```

**Trách nhiệm của Controller:**
```
1. Nhận HTTP request
2. Parse JSON body → DTO
3. Validate format cơ bản
4. Gọi Application Service
5. Xử lý Exception → HTTP Status Codes
6. Trả về JSON response
```

---

## **📍 LAYER 2: APPLICATION SERVICES (Business Logic Orchestration)**

### **File: `API/Application/Services/BookingService.cs`**

**Xử lý:**
- ✅ Điều phối workflows
- ✅ Gọi Domain Entities
- ✅ Gọi Repositories
- ✅ Authorization checks
- ✅ Caching strategy

```csharp
public async Task<BookingResponseDto> CreateBookingAsync(BookingCreateDto dto, int userId)
{
    // 1️⃣ Validate business rules
    if (dto.PassengerCount != dto.Passengers.Count)
        throw new ValidationException("Passenger count mismatch");
    
    // 2️⃣ Get Flight from database (kiểm tra tồn tại & trạng thái)
    var flight = await _flightRepository.GetByIdAsync(dto.FlightId);
    if (flight == null)
        throw new FlightNotFoundException($"Flight {dto.FlightId} not found");
    
    // 3️⃣ Gọi Domain Entity để kiểm tra business rule
    // ❌ NẾU không thực hiện rule này ở Domain layer
    if (!flight.CanBook(dto.PassengerCount))
        throw new InsufficientSeatsException(...);
    
    // 4️⃣ Domain Entity tự cập nhật available seats
    flight.ReserveSeats(dto.PassengerCount);
    
    // 5️⃣ Tạo mới Booking entity
    var booking = new Booking
    {
        FlightId = flight.Id,
        UserId = userId,
        BookingReference = GenerateBookingReference(),
        PassengerCount = dto.PassengerCount,
        TotalPrice = flight.BasePrice * dto.PassengerCount,
        Status = BookingStatus.Pending,
        CreatedAt = DateTime.UtcNow
    };
    
    // 6️⃣ Thêm passengers
    foreach (var passengerDto in dto.Passengers)
    {
        booking.Passengers.Add(new Passenger
        {
            FirstName = passengerDto.FirstName,
            LastName = passengerDto.LastName,
            // ...
        });
    }
    
    // 7️⃣ Lưu vào database
    await _bookingRepository.CreateAsync(booking);
    await _flightRepository.UpdateAsync(flight);
    
    // 8️⃣ Invalidate cache
    await _cacheService.RemoveAsync($"flight-booking:flight:{flight.Id}");
    
    // 9️⃣ Return DTO
    return MapToResponseDto(booking);
}
```

**Trách nhiệm của Service:**
```
1. Validate Complex Business Rules
2. Fetch Domain Entities from Repository
3. Call Domain Entity Methods (CanBook, ReserveSeats, etc.)
4. Perform Authorization Checks
5. Manage Entity Relationships
6. Call Repository to Save Changes
7. Update Cache
8. Handle Transaction/Rollback (nếu cần)
9. Map Entity → DTO
10. Return Result or Throw Exception
```

---

## **📍 LAYER 3: DOMAIN ENTITIES (Pure Business Rules)**

### **File: `API/Domain/Entities/Flight.cs`**

**Xử lý:**
- ✅ Pure business logic
- ✅ Entity state management
- ✅ Invariant validation

```csharp
public class Flight
{
    // 🔐 Business Rule 1: Check if flight can accept booking
    public bool CanBook(int passengerCount)
    {
        return Status == FlightStatus.Active 
            && AvailableSeats >= passengerCount;
    }
    
    // 🔐 Business Rule 2: Reserve seats for booking
    public void ReserveSeats(int seatCount)
    {
        if (seatCount <= 0)
            throw new ArgumentException("Seat count must be > 0");
        
        if (!CanBook(seatCount))
            throw new InvalidOperationException(
                $"Cannot reserve {seatCount} seats. " +
                $"Flight status: {Status}, Available: {AvailableSeats}");
        
        AvailableSeats -= seatCount;
    }
    
    // 🔐 Business Rule 3: Release seats on cancellation
    public void ReleaseSeats(int seatCount)
    {
        if (seatCount <= 0)
            throw new ArgumentException("Seat count must be > 0");
        
        if (AvailableSeats + seatCount > TotalSeats)
            throw new InvalidOperationException(
                $"Cannot release {seatCount} seats. Would exceed total {TotalSeats}");
        
        AvailableSeats += seatCount;
    }
    
    // 🔐 Business Rule 4: Check if departure is soon
    public bool IsDepartureSoon(int hours)
    {
        var hoursUntilDeparture = (DepartureTime - DateTime.UtcNow).TotalHours;
        return hoursUntilDeparture <= hours && hoursUntilDeparture > 0;
    }
    
    // 🔐 Business Rule 5: Cancel flight
    public void Cancel()
    {
        Status = FlightStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // 🔐 Business Rule 6: Mark as delayed
    public void MarkAsDelayed()
    {
        if (Status == FlightStatus.Active)
        {
            Status = FlightStatus.Delayed;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
```

### **File: `API/Domain/Entities/Booking.cs`**

```csharp
public class Booking
{
    // 🔐 Business Rule: Check if booking can be cancelled
    public bool CanCancel(DateTime currentDateTime)
    {
        // Rule 1: Must be in Confirmed status
        if (Status != BookingStatus.Confirmed)
            return false;
        
        // Rule 2: Must have flight reference
        if (Flight == null)
            return false;
        
        // Rule 3: Must be at least 24 hours before departure
        var hoursSuntilDeparture = (Flight.DepartureTime - currentDateTime).TotalHours;
        return hoursSuntilDeparture > 24;
    }
}
```

**Trách nhiệm của Domain Entities:**
```
1. Encapsulate Business Rules
2. Maintain Entity Invariants
3. Provide Methods for State Transitions
4. Validate Business Constraints
5. Contain NO external dependencies (Database, HTTP, etc.)
6. Testable without Infrastructure
```

---

## **📍 LAYER 4: REPOSITORIES (Data Access)**

### **File: `API/Infrastructure/Repositories/FlightRepository.cs`**

**Xử lý:**
- ✅ Database queries
- ✅ Entity persistence
- ❌ Business logic

```csharp
public class FlightRepository : BaseRepository<Flight>, IFlightRepository
{
    public async Task<Flight?> GetFlightWithSeatsAsync(int id)
    {
        // 🗄️ Chỉ lấy dữ liệu từ database
        return await _context.Flights
            .Include(f => f.DepartureAirport)
            .Include(f => f.ArrivalAirport)
            .FirstOrDefaultAsync(f => f.Id == id);
    }
    
    public async Task<IEnumerable<Flight>> SearchFlightsAsync(FlightSearchDto criteria)
    {
        // 🗄️ Chỉ query database theo criteria
        return await _context.Flights
            .Where(f => f.DepartureAirportId == criteria.DepartureAirportId)
            .Where(f => f.ArrivalAirportId == criteria.ArrivalAirportId)
            .Where(f => f.DepartureTime.Date == criteria.DepartureDate.Date)
            .Where(f => f.AvailableSeats >= criteria.PassengerCount)
            .Where(f => f.Status == FlightStatus.Active)
            .OrderBy(f => f.DepartureTime)
            .ToListAsync();
    }
}
```

**Trách nhiệm của Repository:**
```
1. Query Database
2. Insert/Update/Delete Records
3. Fetch Related Entities
4. NO Business Logic
5. Abstraction Layer for Data Access
```

---

## **📍 LAYER 5: INFRASTRUCTURE SERVICES (Cache, Email, etc.)**

### **File: `API/Infrastructure/Caching/InMemoryCacheService.cs`**

```csharp
public class InMemoryCacheService : ICacheService
{
    public async Task<T?> GetAsync<T>(string key)
    {
        // 💾 Lấy từ cache
        return _cache.TryGetValue(key, out var value) 
            ? (T?)value 
            : null;
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        // 💾 Lưu vào cache
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiry;
        
        _cache.Set(key, value, options);
    }
    
    public async Task RemoveAsync(string key)
    {
        // 💾 Xóa khỏi cache
        _cache.Remove(key);
    }
}
```

---

## **📊 BUSINESS LOGIC LOCATION MATRIX**

| Business Logic | Controller | Service | Domain | Repository |
|---|---|---|---|---|
| **Input Validation (Format)** | ✅ | - | - | - |
| **Authorization** | - | ✅ | - | - |
| **Flight.CanBook()** | - | - | ✅ | - |
| **Flight.ReserveSeats()** | - | - | ✅ | - |
| **Booking.CanCancel()** | - | - | ✅ | - |
| **Unique BookingReference** | - | ✅ | - | - |
| **Orchestrate Workflow** | - | ✅ | - | - |
| **Database Queries** | - | - | - | ✅ |
| **Caching** | - | ✅ | - | - |
| **HTTP Response Mapping** | ✅ | - | - | - |
| **Exception Handling** | ✅ | ✅ | - | - |

---

## **🔄 BUSINESS LOGIC FLOW EXAMPLE: Create Booking**

```
1. CLIENT
   └─ POST /api/v1/bookings
   
2. CONTROLLER (BookingsController.Create)
   ├─ Validate ModelState
   ├─ Validate flightId > 0
   ├─ Validate passengerCount > 0
   └─ Call Service → _bookingService.CreateBookingAsync(dto, userId)
   
3. SERVICE (BookingService.CreateBookingAsync)
   ├─ Validate passenger count matches
   ├─ Get Flight from Repository
   │  ├─ Check flight exists
   │  └─ Get flight with details
   │
   ├─ Call Domain: flight.CanBook(passengerCount)  ❌ NOT OK
   │  └─ Throw InsufficientSeatsException
   │
   ├─ Call Domain: flight.ReserveSeats(passengerCount)  ✅ OK
   │  └─ Flight.AvailableSeats -= passengerCount
   │
   ├─ Create Booking Entity
   ├─ Add Passengers to Booking
   ├─ Save via Repository
   │  ├─ Insert Booking record
   │  ├─ Insert Passenger records
   │  └─ Update Flight record (available_seats)
   │
   ├─ Invalidate Cache
   │  └─ Remove "flight-booking:flight:{id}"
   │
   └─ Map to DTO → BookingResponseDto
   
4. CONTROLLER (Return Response)
   └─ 201 Created + Location header + BookingResponseDto
   
5. CLIENT
   ├─ Receives booking confirmation
   └─ Shows booking reference
```

---

## **💡 BEST PRACTICES FOR BUSINESS LOGIC PLACEMENT**

### ✅ **DO - Đặt ở Domain Layer**
```csharp
// ✅ CORRECT: Pure business rule
public bool CanBook(int passengerCount)
{
    return Status == FlightStatus.Active 
        && AvailableSeats >= passengerCount;
}

// ✅ CORRECT: State transition logic
public void ReserveSeats(int count)
{
    if (!CanBook(count))
        throw new InvalidOperationException();
    AvailableSeats -= count;
}
```

### ✅ **DO - Đặt ở Service Layer**
```csharp
// ✅ CORRECT: Orchestration logic
public async Task<BookingResponseDto> CreateBookingAsync(...)
{
    var flight = await _flightRepository.GetByIdAsync(...);
    
    if (flight == null)
        throw new FlightNotFoundException();
    
    // Call Domain
    flight.ReserveSeats(dto.PassengerCount);
    
    // Call Repository
    await _flightRepository.UpdateAsync(flight);
}

// ✅ CORRECT: Complex validation
public async Task<BookingResponseDto> CancelBookingAsync(int id, int userId)
{
    var booking = await _bookingRepository.GetByIdAsync(id);
    
    // Authorization
    if (booking.UserId != userId)
        throw new UnauthorizedAccessException();
    
    // Business rule check
    if (!booking.CanCancel(DateTime.UtcNow))
        throw new BookingAlreadyCancelledException();
}
```

### ✅ **DO - Đặt ở Controller Layer**
```csharp
// ✅ CORRECT: Input validation (format)
if (id <= 0)
    return BadRequest("ID must be > 0");

if (pageSize < 1 || pageSize > 100)
    return BadRequest("PageSize must be 1-100");

// ✅ CORRECT: Exception handling → HTTP status
try
{
    await _service.DoSomething();
}
catch (FlightNotFoundException ex)
{
    return NotFound(ex.Message);
}
catch (InsufficientSeatsException ex)
{
    return BadRequest(ex.Message);
}
```

### ❌ **DON'T - Đặt ở sai layer**
```csharp
// ❌ WRONG: Business logic ở Controller
[HttpPost]
public async Task CreateBooking(...)
{
    // ❌ Complex validation không nên ở Controller
    if (dto.PassengerCount != dto.Passengers.Count)
        return BadRequest(...);
    
    // ❌ Authorization không nên ở Controller
    if (booking.UserId != userId)
        return Unauthorized();
}

// ❌ WRONG: Database query ở Domain
public class Flight
{
    public bool CanBook(int count)
    {
        // ❌ WRONG: Không thể access database
        var otherBookings = _context.Bookings.Where(...);
        
        return AvailableSeats >= count;
    }
}

// ❌ WRONG: Business logic ở Repository
public async Task CreateBookingAsync(Booking booking)
{
    // ❌ WRONG: Business logic không nên ở Repository
    booking.BookingReference = GenerateUniqueReference();
    booking.TotalPrice = booking.Flight.BasePrice * booking.PassengerCount;
    
    await _context.SaveChangesAsync();
}
```

---

## **🎯 SUMMARY**

| Layer | Responsibility | Business Logic? |
|-------|---|---|
| **Controller** | HTTP handling, Input validation (format), Exception → Status codes | ❌ No |
| **Service** | Orchestration, Authorization, Call Domain methods, Complex validation | ⚠️ Orchestration only |
| **Domain** | Pure business rules, Entity state, Invariant validation | ✅ YES |
| **Repository** | Database queries, CRUD operations | ❌ No |
| **Infrastructure** | Caching, Email, External services | ⚠️ Technical logic only |

---

## **📌 KEY PRINCIPLE: SEPARATION OF CONCERNS**

```
Controller:    "How do we handle HTTP?"
Service:       "How do we orchestrate business workflows?"
Domain:        "What are the business rules?"
Repository:    "How do we persist data?"
```

**Khi thêm feature mới, hãy tự hỏi:**
1. **Đây có phải pure business rule không?** → Domain Entity
2. **Đây có phải orchestration logic không?** → Service
3. **Đây có phải input validation format không?** → Controller
4. **Đây có phải database operation không?** → Repository

---

**Document Version:** 1.0  
**Last Updated:** January 2024
