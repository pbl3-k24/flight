# 🏛️ Module 3: Domain Layer (Entity/Domain) Guide

## 📋 Mục Đích
Định nghĩa entities, value objects, business rules, enums, và domain interfaces. Đây là "core" của ứng dụng.

---

## 🏗️ Kiến Trúc

```
Domain Layer
├─ Entities (Flight, Booking, User, Payment, ...)
├─ Value Objects (Money, Phone, Email, Address)
├─ Enums (Status, Types)
├─ Interfaces (IEntity, IRepository<T>)
└─ Business Rules (embedded in entities)

⚠️ Domain Layer độc lập, không phụ thuộc vào bất kỳ layer nào khác
```

---

## 🎯 Entity Design Pattern

### 1. **Entity Base Structure**
```
Entity
├─ Identity (ID)
├─ Properties (data)
├─ Navigation Properties (relationships)
├─ Business Methods (behavior)
└─ Timestamps (CreatedAt, UpdatedAt)

Principles:
  - Only public properties (no getter/setter logic)
  - Relationships via navigation properties
  - Business rules encapsulated
  - No dependencies on external libraries
```

### 2. **Entity Relationships**
```
One-to-Many:
  Flight (1) ──→ Bookings (Many)
    └─ Flight.Bookings: ICollection<Booking>
    └─ Booking.Flight: Flight

One-to-One:
  User (1) ──→ Profile (1)
    └─ User.Profile: Profile
    └─ Profile.User: User

Many-to-Many:
  Flight (Many) ──→ Crew (Many)
    └─ Flight.Crew: ICollection<Crew>
    └─ Crew.Flights: ICollection<Flight>
```

### 3. **Aggregate Pattern**
```
Aggregate = Entity + related entities operating together

Flight Aggregate:
  Root: Flight (aggregate root)
  Children:
    - Booking
    - Passenger
  Rules:
    - Only access through Flight
    - Modifications through Flight methods
    - Consistency maintained at aggregate level
```

---

## 💼 Entity Examples & Algorithms

### Entity 1: Flight
```
Properties:
  - Id (primary key)
  - FlightNumber (unique identifier)
  - DepartureAirportId, ArrivalAirportId
  - DepartureTime, ArrivalTime
  - TotalSeats, AvailableSeats
  - BasePrice
  - Status (Active, Cancelled, Completed)
  - CreatedAt, UpdatedAt

Relationships:
  - DepartureAirport (one-to-one or many-to-one)
  - ArrivalAirport (one-to-one or many-to-one)
  - Bookings (one-to-many)
  - Crew (many-to-many)

Business Methods:

  ✓ CanBook(passengerCount):
      return Status == Active && AvailableSeats >= passengerCount

  ✓ ReserveSeats(count):
      if (!CanBook(count)) throw InsufficientSeatsException
      AvailableSeats -= count

  ✓ ReleaseSeats(count):
      AvailableSeats = Math.Min(AvailableSeats + count, TotalSeats)

  ✓ IsDepartureSoon(hours):
      return (DepartureTime - DateTime.UtcNow).TotalHours <= hours

  ✓ CalculatePrice(basePrice, demandFactor, bookingTimeFactor):
      return basePrice × demandFactor × bookingTimeFactor
      (with min/max caps: 0.5x - 2.0x)

  ✓ IsFlightClosed():
      return Status != Active || IsDepartureSoon(2)
```

### Entity 2: Booking
```
Properties:
  - Id
  - FlightId
  - UserId
  - BookingReference (unique, generated)
  - Status (Pending, Confirmed, Cancelled, CheckedIn)
  - TotalPrice
  - BookedAt, UpdatedAt
  - CancelledAt (nullable)

Relationships:
  - Flight (many-to-one)
  - User (many-to-one)
  - Passengers (one-to-many)
  - Payment (one-to-one)

Business Methods:

  ✓ CanCancel(currentDateTime):
      return Status == Confirmed && 
             !IsFlightClosed() && 
             (currentDateTime < DepartureTime - 24h)

  ✓ Cancel():
      if (!CanCancel(DateTime.UtcNow)) 
        throw BookingAlreadyProcessedException
      Status = Cancelled
      CancelledAt = DateTime.UtcNow

  ✓ CheckIn():
      if (Status != Confirmed) throw InvalidBookingStateException
      Status = CheckedIn
      UpdatedAt = DateTime.UtcNow

  ✓ CalculateRefund(cancellationFeePercentage):
      if (Status != Cancelled) return 0m
      baseFare = TotalPrice / PassengerCount
      hoursTillFlight = (DepartureTime - CancelledAt).TotalHours
      
      if (hoursTillFlight > 72) fee = 0%
      else if (hoursTillFlight > 24) fee = 10%
      else fee = 25%
      
      return TotalPrice × (1 - fee/100)

  ✓ GenerateBookingReference():
      return $"{FlightNumber}-{DateTime.UtcNow:yyyyMMdd}-{RandomId}"
```

### Entity 3: User (simplified)
```
Properties:
  - Id
  - Email (unique)
  - PasswordHash
  - FirstName, LastName
  - PhoneNumber
  - DateOfBirth
  - Nationality
  - Status (Active, Suspended, Deleted)
  - CreatedAt, UpdatedAt

Relationships:
  - Bookings (one-to-many)
  - Payments (one-to-many)

Business Methods:

  ✓ IsAdult():
      return DateTime.UtcNow.Year - DateOfBirth.Year >= 18

  ✓ CanMakeBooking():
      return Status == Active && IsAdult()

  ✓ UpdateProfile(firstName, lastName, phone):
      // Validation here
      if (string.IsNullOrEmpty(firstName)) throw ArgumentException
      FirstName = firstName
      LastName = lastName
      PhoneNumber = phone
      UpdatedAt = DateTime.UtcNow

  ✓ Suspend(reason):
      Status = Suspended
      // Log suspension reason
```

### Entity 4: Payment
```
Properties:
  - Id
  - BookingId
  - UserId
  - Amount
  - Currency
  - PaymentMethod (Card, Bank, Wallet)
  - Status (Pending, Completed, Failed, Refunded)
  - TransactionId
  - CreatedAt, ProcessedAt

Business Methods:

  ✓ Process(paymentGatewayResponse):
      if (Status != Pending) throw PaymentAlreadyProcessedException
      
      if (paymentGatewayResponse.IsSuccessful)
        Status = Completed
        TransactionId = paymentGatewayResponse.Id
        ProcessedAt = DateTime.UtcNow
      else
        Status = Failed

  ✓ Refund(refundAmount):
      if (Status != Completed) throw InvalidRefundException
      if (refundAmount > Amount) throw RefundAmountExceededException
      Status = Refunded
      // Trigger refund with gateway

  ✓ CanBeProcessed():
      return Status == Pending && Amount > 0
```

---

## 📦 Value Objects

### Purpose:
- Immutable data structures
- No identity (equality by value, not reference)
- Business logic encapsulation
- Type safety

### Examples:

**Email Value Object:**
```
Properties:
  - Address (string)

Rules:
  - Immutable (no setters)
  - Valid email format
  - Equality by Address value
  - No ID needed

Methods:
  ✓ IsValid(email): check format @, domain, etc
  ✓ Equals(other): compare Address values
```

**Money Value Object:**
```
Properties:
  - Amount (decimal)
  - Currency (string)

Rules:
  - Immutable
  - Amount >= 0
  - Only same currency can be compared
  - Always round to 2 decimals

Methods:
  ✓ Add(other): return new Money
  ✓ Subtract(other): return new Money
  ✓ IsGreaterThan(other): boolean
  ✓ Format(): return formatted string
```

**Phone Value Object:**
```
Properties:
  - Number (string)
  - CountryCode (string)

Rules:
  - Immutable
  - Valid format (10-15 digits)
  - Clean/normalize format

Methods:
  ✓ IsValid(): boolean
  ✓ GetFormattedNumber(): string
```

**Address Value Object:**
```
Properties:
  - Street (string)
  - City (string)
  - State/Province (string)
  - Country (string)
  - PostalCode (string)

Rules:
  - All fields immutable
  - Validation on creation
  - Equality by all fields
```

---

## 🎪 Enum (Discriminator) Types

### Purpose:
- Represent fixed sets of values
- Type-safe alternatives to magic strings
- Self-documenting code

### Examples:

**Flight Status:**
```
enum:
  Active = 1        → Flight is available for booking
  Cancelled = 2     → Flight is cancelled, no bookings allowed
  Delayed = 3       → Flight is delayed, bookings frozen
  Completed = 4     → Flight completed, can't modify

Usage:
  if (flight.Status == FlightStatus.Active)
    flight.ReserveSeats(count)
```

**Booking Status:**
```
enum:
  Pending = 1       → Booking created, awaiting payment
  Confirmed = 2     → Payment received, confirmed
  Cancelled = 3     → Booking cancelled by user/system
  CheckedIn = 4     → User checked in

Transition Rules:
  Pending → Confirmed (after payment)
  Confirmed → CheckedIn (at airport)
  Pending/Confirmed → Cancelled (by user)
  CheckedIn → Cannot transition (final state)
```

**Payment Status:**
```
enum:
  Pending = 1       → Payment initiated
  Processing = 2    → Processing with gateway
  Completed = 3     → Successfully processed
  Failed = 4        → Failed, user can retry
  Refunded = 5      → Refund initiated

Transition Rules:
  Pending → Processing
  Processing → Completed or Failed
  Completed → Refunded
  Failed → can retry (back to Pending)
```

**Seat Class:**
```
enum:
  Economy = 1
  Business = 2
  First = 3

Pricing Impact:
  Economy: 1.0x multiplier
  Business: 1.8x multiplier
  First: 2.5x multiplier
```

---

## 🔄 Business Rule Validation

### Validation by Domain Constraints:

```
Flight Creation:
  ✓ FlightNumber: not null, 3-20 chars, unique
  ✓ DepartureTime < ArrivalTime
  ✓ ArrivalTime - DepartureTime >= 1 hour (min flight duration)
  ✓ TotalSeats > 0
  ✓ BasePrice > 0
  ✓ DepartureAirportId != ArrivalAirportId

Booking Creation:
  ✓ Flight must exist and be Active
  ✓ User must exist and be Active
  ✓ Available seats >= passenger count
  ✓ Flight departure > now + 2 hours
  ✓ Each passenger has valid details
  ✓ No duplicate booking (user, flight, date)

Passenger Addition:
  ✓ Age validation (if international flight, age >= 2)
  ✓ Document validation (passport number format)
  ✓ Name format validation

Payment:
  ✓ Amount > 0
  ✓ Amount matches booking total
  ✓ User has valid payment method
  ✓ Not processing duplicate payment
```

---

## 🛡️ Invariants (Business Rules That Must Always Be True)

```
Flight Invariants:
  - available_seats <= total_seats
  - available_seats >= 0
  - departure_time != arrival_time
  - departure_airport_id != arrival_airport_id
  - base_price >= 0

Booking Invariants:
  - booking_reference is unique
  - total_price > 0
  - bookings_count <= flight.total_seats
  - user exists and is active
  - flight exists

User Invariants:
  - email is unique
  - email is valid format
  - password hash exists
  - date_of_birth is valid (not future date)
```

---

## 🔐 Entity Consistency

### Aggregate Root Responsibilities:
```
Flight (aggregate root)
  - Manages seat availability
  - Enforces booking limits
  - Controls status transitions
  - Manages child entities (bookings, crew)

Booking (aggregate root within Flight context)
  - Manages passenger info
  - Controls booking state
  - Manages refund logic
  - Orphaned if flight deleted? → Cascade delete

User (aggregate root)
  - Manages personal info
  - Controls profile updates
  - Manages bookings collection
```

### Consistency Rules:
```
When Flight is deleted:
  - All bookings must be cancelled first
  OR
  - Use cascade delete in database
  - Notify users

When Booking is cancelled:
  - Payment must be refunded
  - Seats must be released
  - User must be notified

When Payment fails:
  - Booking status reverted to Pending
  - User can retry with different method
```

---

## 📊 Entity State Transitions

### Flight State Machine:
```
[Active]
  ↓ (system marks as cancelled)
[Cancelled]

[Active]
  ↓ (detected delay)
[Delayed]
  ↓ (delay resolved)
[Active]

[Active]
  ↓ (departure time reached)
[Completed]
```

### Booking State Machine:
```
[Pending]
  ├─ (payment successful) → [Confirmed]
  └─ (payment failed) → [Cancelled]

[Confirmed]
  ├─ (user cancels) → [Cancelled]
  └─ (check-in) → [CheckedIn]

[CheckedIn] (final state)

[Cancelled] (final state)
```

---

## ✅ Best Practices

1. **Encapsulation** - Hide internal state, expose behavior
2. **Immutable Value Objects** - Can't be changed after creation
3. **Rich Domain Model** - Business logic in entities, not services
4. **No Getters/Setters Logic** - Properties only, logic in methods
5. **Explicit Methods** - `ReserveSeats()` instead of `AvailableSeats -= 5`
6. **Invariants** - Enforce constraints via methods
7. **Aggregate Root** - One entity manages the aggregate
8. **No External Dependencies** - Domain layer is pure C#
9. **Self-Validating** - Entity ensures own validity
10. **Meaningful Names** - Names reflect business domain

---

## 📝 Anti-Patterns (Avoid)

❌ **Anemic Domain** - Just data, no behavior
```
// Bad
public class Flight {
  public int AvailableSeats { get; set; }
}
// Then in service: flight.AvailableSeats -= count
```

❌ **Service Logic in Constructor**
```
// Bad
public Flight(FlightCreateDto dto) { // Too much logic }
```

❌ **Getters with Business Logic**
```
// Bad
public decimal Price {
  get { return BasePrice * DemandFactor; }
}
```

❌ **Entity Knows About Database**
```
// Bad
public void Save() { _db.Flights.Add(this); }
```

---

**Module**: Domain Layer | **Version**: 1.0
