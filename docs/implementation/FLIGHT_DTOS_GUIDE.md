# Flight DTOs - Implementation Guide

## ✅ Overview

Three Flight-related DTOs have been created following the 01_API_LAYER_GUIDE.md DTO patterns:

1. **FlightCreateDto** - Request DTO for creating new flights
2. **FlightResponseDto** - Response DTO for returning flight information
3. **FlightSearchDto** - Request DTO for searching flights with criteria

**Location**: `API/Application/DTOs/Flight*.cs`

---

## 📋 DTO Specifications

### 1. FlightCreateDto

**Purpose**: Transfer flight creation data from client to application layer.

**Properties**:
```csharp
public class FlightCreateDto
{
    public string FlightNumber { get; set; }           // "AA100"
    public int DepartureAirportId { get; set; }        // 1
    public int ArrivalAirportId { get; set; }          // 2
    public DateTime DepartureTime { get; set; }        // 2026-04-15 08:00:00
    public DateTime ArrivalTime { get; set; }          // 2026-04-15 13:00:00
    public int TotalSeats { get; set; }                // 150
    public decimal BasePrice { get; set; }             // 299.99
}
```

**Design Characteristics**:
- ✅ No ID property (assigned by system)
- ✅ No Status property (defaults to Active)
- ✅ No Airline/AircraftModel (can be system-assigned or service-level)
- ✅ No validation logic (validation in service/validators)
- ✅ No business methods
- ✅ Pure data transfer object

**Usage**:
```csharp
// Client sends
POST /api/v1/flights
{
  "flightNumber": "AA100",
  "departureAirportId": 1,
  "arrivalAirportId": 2,
  "departureTime": "2026-04-15T08:00:00",
  "arrivalTime": "2026-04-15T13:00:00",
  "totalSeats": 150,
  "basePrice": 299.99
}

// Controller receives FlightCreateDto
// Service creates Flight entity
// Service performs validation
// Database stores the flight
```

**Validation** (performed in service layer, not DTO):
- FlightNumber not empty/null
- DepartureAirportId > 0 and exists
- ArrivalAirportId > 0 and exists
- ArrivalAirportId != DepartureAirportId
- DepartureTime < ArrivalTime
- TotalSeats > 0
- BasePrice >= 0

---

### 2. FlightResponseDto

**Purpose**: Transfer flight data from application layer to client in response.

**Properties**:
```csharp
public class FlightResponseDto
{
    // Identity
    public int Id { get; set; }                        // 1
    
    // Flight Info
    public string FlightNumber { get; set; }           // "AA100"
    public string Airline { get; set; }                // "American Airlines"
    public string AircraftModel { get; set; }          // "Boeing 737"
    
    // Departure Info
    public int DepartureAirportId { get; set; }        // 1
    public string DepartureAirportName { get; set; }   // "Los Angeles International"
    public string DepartureAirportCode { get; set; }   // "LAX"
    public DateTime DepartureTime { get; set; }        // 2026-04-15T08:00:00
    
    // Arrival Info
    public int ArrivalAirportId { get; set; }          // 2
    public string ArrivalAirportName { get; set; }     // "John F. Kennedy"
    public string ArrivalAirportCode { get; set; }     // "JFK"
    public DateTime ArrivalTime { get; set; }          // 2026-04-15T13:00:00
    
    // Capacity & Pricing
    public int TotalSeats { get; set; }                // 150
    public int AvailableSeats { get; set; }            // 45
    public decimal BasePrice { get; set; }             // 299.99
    
    // Status & Timestamps
    public string Status { get; set; }                 // "Active"
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

**Design Characteristics**:
- ✅ Includes ID (system-assigned)
- ✅ Includes Status (current state)
- ✅ Flat structure (no nested objects)
- ✅ Airport details included (name, code)
- ✅ Seat availability information
- ✅ Timestamp information
- ✅ All read-only data suitable for response

**Includes Airport Details**:
Instead of nested Airport objects (which would violate flat structure):
```csharp
// ❌ WRONG - Nested object
public AirportResponseDto DepartureAirport { get; set; }

// ✅ RIGHT - Flat properties
public int DepartureAirportId { get; set; }
public string DepartureAirportName { get; set; }
public string DepartureAirportCode { get; set; }
```

**Usage**:
```csharp
// Controller returns
GET /api/v1/flights/1
200 OK
{
  "id": 1,
  "flightNumber": "AA100",
  "departureAirportId": 1,
  "departureAirportName": "Los Angeles International",
  "departureAirportCode": "LAX",
  "arrivalAirportId": 2,
  "arrivalAirportName": "John F. Kennedy",
  "arrivalAirportCode": "JFK",
  "departureTime": "2026-04-15T08:00:00Z",
  "arrivalTime": "2026-04-15T13:00:00Z",
  "airline": "American Airlines",
  "aircraftModel": "Boeing 737",
  "totalSeats": 150,
  "availableSeats": 45,
  "basePrice": 299.99,
  "status": "Active",
  "createdAt": "2026-04-10T10:00:00Z",
  "updatedAt": "2026-04-10T10:00:00Z"
}
```

---

### 3. FlightSearchDto

**Purpose**: Transfer search criteria from client to application layer for flight search operations.

**Properties**:
```csharp
public class FlightSearchDto
{
    public int DepartureAirportId { get; set; }        // 1 - Required
    public int ArrivalAirportId { get; set; }          // 2 - Required
    public DateTime DepartureDate { get; set; }        // 2026-04-15 - Required
    public string? SeatClass { get; set; }             // "Economy", null = all classes
    public int PassengerCount { get; set; }            // 2 - Required
}
```

**Design Characteristics**:
- ✅ Search criteria only (no result data)
- ✅ Nullable SeatClass (optional filter)
- ✅ Simple flat structure
- ✅ No validation logic
- ✅ No ID field

**Search Parameters Explained**:
- **DepartureAirportId** (int): The airport to depart from
- **ArrivalAirportId** (int): The destination airport
- **DepartureDate** (DateTime): The date to search for (time portion may be ignored, only date used)
- **SeatClass** (string, nullable): Optional filter for seat class ("Economy", "Business", "FirstClass"). Null means all classes.
- **PassengerCount** (int): Number of passengers (used to filter flights with sufficient available seats)

**Usage**:
```csharp
// Client sends search request
POST /api/v1/flights/search
{
  "departureAirportId": 1,
  "arrivalAirportId": 2,
  "departureDate": "2026-04-15",
  "seatClass": "Economy",
  "passengerCount": 2
}

// Controller receives FlightSearchDto
// Service searches database:
//   - Matches departure airport
//   - Matches arrival airport
//   - Matches departure date (ignoring time)
//   - Filters by seat class if provided
//   - Filters by available seats >= passengerCount
//   - Returns matching flights

200 OK
[
  {
    "id": 1,
    "flightNumber": "AA100",
    ...
  },
  {
    "id": 5,
    "flightNumber": "AA200",
    ...
  }
]
```

---

## 🔄 DTO Flow Diagram

```
┌──────────────┐
│   Client     │
└──────┬───────┘
       │
       │ POST /api/v1/flights
       │ {FlightCreateDto}
       │
       ▼
┌──────────────────┐
│   Controller     │
│ (Input Received) │
└──────┬───────────┘
       │
       │ _flightService.CreateAsync(dto)
       │
       ▼
┌──────────────────┐
│  Service Layer   │
│  (Validation)    │
└──────┬───────────┘
       │
       │ Validate business rules
       │ Map DTO → Entity
       │
       ▼
┌──────────────────┐
│ Flight Entity    │
│ (Domain Logic)   │
└──────┬───────────┘
       │
       │ _flightRepository.Add(entity)
       │
       ▼
┌──────────────────┐
│   Database       │
│   (Persisted)    │
└──────┬───────────┘
       │
       │ Retrieve created entity
       │
       ▼
┌──────────────────┐
│  Service Layer   │
│  (Map to DTO)    │
└──────┬───────────┘
       │
       │ Entity → FlightResponseDto
       │
       ▼
┌──────────────────┐
│   Controller     │
│ (Output Sent)    │
└──────┬───────────┘
       │
       │ 201 Created
       │ {FlightResponseDto}
       │
       ▼
┌──────────────────┐
│   Client         │
└──────────────────┘
```

---

## 📝 Validation Strategy

### FlightCreateDto Validation

**Level 1: Controller (Format)**
```csharp
if (!ModelState.IsValid)
    return BadRequest("Invalid flight data");
```

**Level 2: Service (Business Rules)**
```csharp
// FlightService.CreateAsync()
if (flightCreateDto.DepartureAirportId == flightCreateDto.ArrivalAirportId)
    throw new ValidationException("Departure and arrival airports cannot be the same");

if (flightCreateDto.DepartureTime >= flightCreateDto.ArrivalTime)
    throw new ValidationException("Departure time must be before arrival time");

if (flightCreateDto.TotalSeats <= 0)
    throw new ValidationException("Total seats must be greater than 0");

if (flightCreateDto.BasePrice < 0)
    throw new ValidationException("Base price cannot be negative");

// Check airports exist
var departureAirport = await _airportRepository.GetByIdAsync(flightCreateDto.DepartureAirportId);
if (departureAirport == null)
    throw new AirportNotFoundException(flightCreateDto.DepartureAirportId);

var arrivalAirport = await _airportRepository.GetByIdAsync(flightCreateDto.ArrivalAirportId);
if (arrivalAirport == null)
    throw new AirportNotFoundException(flightCreateDto.ArrivalAirportId);
```

### FlightSearchDto Validation

**Level 1: Controller (Format)**
```csharp
if (departureAirportId <= 0 || arrivalAirportId <= 0)
    return BadRequest("Airport IDs must be valid");

if (passengerCount <= 0)
    return BadRequest("Passenger count must be at least 1");
```

**Level 2: Service (Business Rules)**
```csharp
// Check departure date is not in the past
if (departureDate.Date < DateTime.UtcNow.Date)
    throw new ValidationException("Departure date cannot be in the past");

// Check airports exist
if (!await _airportRepository.ExistsAsync(departureAirportId))
    throw new AirportNotFoundException(departureAirportId);

if (!await _airportRepository.ExistsAsync(arrivalAirportId))
    throw new AirportNotFoundException(arrivalAirportId);

// Check they are different airports
if (departureAirportId == arrivalAirportId)
    throw new ValidationException("Departure and arrival airports must be different");
```

---

## 🏗️ DTO vs Entity Separation

### Why DTOs?

```
┌─────────────────────────────────┐
│    DTOs (API Contract)          │
│  - Only fields needed for API   │
│  - No business logic            │
│  - Decouples API from entities  │
│  - Easier to version API        │
└─────────────────────────────────┘
            ▲
            │ Map
            │
┌─────────────────────────────────┐
│   Flight Entity (Domain)         │
│  - Full business logic           │
│  - Invariants enforcement        │
│  - Methods (ReserveSeats, etc.)  │
│  - Relations to other entities   │
└─────────────────────────────────┘
```

### Mapping Strategy

```csharp
// In Application Service Layer

// DTO → Entity (Create)
var flight = new Flight
{
    FlightNumber = dto.FlightNumber,
    DepartureAirportId = dto.DepartureAirportId,
    ArrivalAirportId = dto.ArrivalAirportId,
    DepartureTime = dto.DepartureTime,
    ArrivalTime = dto.ArrivalTime,
    TotalSeats = dto.TotalSeats,
    AvailableSeats = dto.TotalSeats, // Initialize as full capacity
    BasePrice = dto.BasePrice,
    Status = FlightStatus.Active,    // Default status
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// Entity → DTO (Response)
var response = new FlightResponseDto
{
    Id = flight.Id,
    FlightNumber = flight.FlightNumber,
    DepartureAirportId = flight.DepartureAirportId,
    DepartureAirportName = flight.DepartureAirport.Name,
    DepartureAirportCode = flight.DepartureAirport.Code,
    // ... other properties
};

// Or use AutoMapper
var response = _mapper.Map<FlightResponseDto>(flight);
```

---

## 💡 Design Principles

✅ **Single Responsibility**
- Each DTO has one purpose (create, response, search)
- No mixed concerns

✅ **Flat Structure**
- No nested objects in responses
- Airport info denormalized as properties
- Easier for clients to consume

✅ **No Business Logic**
- DTOs are simple POCO objects
- All validation in service layer
- No methods or complex properties

✅ **Separation of Concerns**
- DTOs don't know about entities
- Entities don't know about DTOs
- Services perform mapping

✅ **API Contract**
- DTOs define API contract
- Can be versioned independently
- Changes to entities don't require API changes

✅ **Immutability**
- DTOs are simple data holders
- All properties are settable (needed for binding)
- No behavior or state changes

---

## 📊 Comparison Table

| Aspect | FlightCreateDto | FlightResponseDto | FlightSearchDto |
|--------|-----------------|-------------------|-----------------|
| **Purpose** | Create flight | Return flight info | Search criteria |
| **Has ID** | ❌ | ✅ | ❌ |
| **Has Status** | ❌ | ✅ | ❌ |
| **Airport Details** | Just IDs | Name + Code | Just IDs |
| **Seat Info** | Total only | Total + Available | Filter only |
| **Used For** | POST request | GET response | POST search |
| **Contains Timestamps** | ❌ | ✅ | ❌ |

---

## 🔄 API Endpoint Usage

### Create Flight
```
POST /api/v1/flights
Content-Type: application/json

FlightCreateDto Input:
{
  "flightNumber": "AA100",
  "departureAirportId": 1,
  "arrivalAirportId": 2,
  "departureTime": "2026-04-15T08:00:00",
  "arrivalTime": "2026-04-15T13:00:00",
  "totalSeats": 150,
  "basePrice": 299.99
}

Response 201 Created:
FlightResponseDto Output:
{
  "id": 1,
  "flightNumber": "AA100",
  "departureAirportId": 1,
  "departureAirportName": "Los Angeles International",
  "departureAirportCode": "LAX",
  ...
}
```

### Search Flights
```
POST /api/v1/flights/search
Content-Type: application/json

FlightSearchDto Input:
{
  "departureAirportId": 1,
  "arrivalAirportId": 2,
  "departureDate": "2026-04-15",
  "seatClass": "Economy",
  "passengerCount": 2
}

Response 200 OK:
List<FlightResponseDto> Output:
[
  {
    "id": 1,
    "flightNumber": "AA100",
    ...
  },
  {
    "id": 5,
    "flightNumber": "AA200",
    ...
  }
]
```

### Get Flight
```
GET /api/v1/flights/1

Response 200 OK:
FlightResponseDto Output:
{
  "id": 1,
  "flightNumber": "AA100",
  ...
}
```

---

## ✅ Build Status

✅ **Compilation**: Successful  
✅ **Build**: Successful  
✅ **Ready for**: Integration with FlightsController

---

## 📚 Next Steps

1. **Create FlightsController**
   - GetAll flights
   - GetById flight
   - Create flight
   - Search flights
   - Update flight
   - Delete/Cancel flight

2. **Create FlightService**
   - Implement business logic
   - Map DTOs to entities
   - Handle validation
   - Interact with repositories

3. **Create FlightValidator**
   - FluentValidation for DTOs
   - Complex validation rules

4. **Create AutoMapper Profile**
   - Automatic DTO ↔ Entity mapping
   - Convention-based mapping

---

**Version**: 1.0  
**Status**: Complete ✅
