# Flight Booking System - Domain Entities & Enums

This document describes all domain entities and enums created for the flight booking system, following the domain-driven design principles outlined in `03_DOMAIN_LAYER_GUIDE.md`.

---

## 📋 Enums

### 1. FlightStatus
**Location**: `Domain/Enums/FlightStatus.cs`

Represents the lifecycle state of a flight.

**Values**:
- `Active = 1` - Flight is available for bookings
- `Cancelled = 2` - Flight has been cancelled
- `Delayed = 3` - Flight is delayed
- `Completed = 4` - Flight has completed

---

### 2. BookingStatus
**Location**: `Domain/Enums/BookingStatus.cs`

Represents the lifecycle state of a booking.

**Values**:
- `Pending = 1` - Awaiting payment confirmation
- `Confirmed = 2` - Payment received and booking confirmed
- `CheckedIn = 3` - Passenger has checked in
- `Cancelled = 4` - Booking cancelled

**State Machine**:
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

### 3. UserStatus
**Location**: `Domain/Enums/UserStatus.cs`

Represents the account status of a user.

**Values**:
- `Active = 1` - Account is active and can perform operations
- `Deactivated = 2` - Account is deactivated by user
- `Suspended = 3` - Account is suspended due to violations

---

### 4. PaymentMethod
**Location**: `Domain/Enums/PaymentMethod.cs`

Represents the method used for payment.

**Values**:
- `CreditCard = 1` - Credit card payment
- `DebitCard = 2` - Debit card payment
- `DigitalWallet = 3` - Digital wallet (PayPal, Google Pay, etc.)
- `BankTransfer = 4` - Bank transfer

---

### 5. PaymentStatus
**Location**: `Domain/Enums/PaymentStatus.cs`

Represents the processing status of a payment.

**Values**:
- `Pending = 1` - Payment is being processed
- `Completed = 2` - Payment successful
- `Failed = 3` - Payment failed
- `Refunded = 4` - Payment refunded

---

## 🏛️ Entities (Aggregate Roots)

### 1. Airport
**Location**: `Domain/Entities/Airport.cs`

Represents an airport in the flight system.

**Responsibilities**:
- Stores airport information (IATA code, name, location)
- Serves as a reference for flight departures and arrivals

**Properties**:
- `Id` - Unique identifier
- `Code` - IATA code (e.g., "LAX", "JFK")
- `Name` - Full airport name
- `City` - City location
- `Country` - Country location
- `Timezone` - Airport timezone
- `CreatedAt` - Creation timestamp
- `UpdatedAt` - Last update timestamp

**Navigation Properties**:
- `DepartingFlights` - Flights departing from this airport
- `ArrivingFlights` - Flights arriving at this airport

---

### 2. Flight (Aggregate Root)
**Location**: `Domain/Entities/Flight.cs`

**Aggregate Root Responsibility**: Manages seat availability and enforces booking limits.

**Invariants**:
- `available_seats <= total_seats`
- `available_seats >= 0`
- `departure_time != arrival_time`
- `departure_airport_id != arrival_airport_id`
- `base_price >= 0`

**Key Properties**:
- `Id` - Unique identifier
- `FlightNumber` - Flight designation (e.g., "AA100")
- `DepartureAirportId` - Departing airport ID
- `ArrivalAirportId` - Arrival airport ID
- `DepartureTime` - Scheduled departure
- `ArrivalTime` - Scheduled arrival
- `Airline` - Airline name
- `AircraftModel` - Aircraft type
- `TotalSeats` - Total seat capacity
- `AvailableSeats` - Currently available seats
- `BasePrice` - Base price per seat
- `Status` - Flight status (see `FlightStatus` enum)
- `CreatedAt`, `UpdatedAt` - Timestamps

**Domain Methods**:
```csharp
// Reserves seats for a booking
void ReserveSeats(int seatCount)
  // Throws InvalidOperationException if insufficient seats

// Releases seats back (e.g., when booking cancelled)
void ReleaseSeats(int seatCount)
  // Throws InvalidOperationException if would exceed total seats

// Status transitions
void Cancel()
void MarkAsDelayed()
void MarkAsCompleted()
```

**Navigation Properties**:
- `DepartureAirport` - Airport entity for departure
- `ArrivalAirport` - Airport entity for arrival
- `Bookings` - All bookings for this flight
- `CrewMembers` - Assigned crew

---

### 3. User (Aggregate Root)
**Location**: `Domain/Entities/User.cs`

**Aggregate Root Responsibility**: Manages user profile and related data.

**Invariants**:
- `email` is unique
- `email` is valid format
- `password hash` exists
- `date_of_birth` is not future date

**Key Properties**:
- `Id` - Unique identifier
- `Email` - Unique email address
- `FirstName` - First name
- `LastName` - Last name
- `DateOfBirth` - Date of birth
- `PhoneNumber` - Optional phone number
- `PasswordHash` - Hashed password
- `Status` - Account status (see `UserStatus` enum)
- `CreatedAt`, `UpdatedAt` - Timestamps

**Domain Methods**:
```csharp
void Deactivate()    // Sets status to Deactivated
void Reactivate()    // Sets status to Active
void Suspend()       // Sets status to Suspended
```

**Navigation Properties**:
- `Bookings` - All bookings made by user
- `Payments` - All payments made by user

---

### 4. Booking (Aggregate Root within Flight context)
**Location**: `Domain/Entities/Booking.cs`

**Aggregate Root Responsibility**: Manages passenger information and booking state.

**Invariants**:
- `booking_reference` is unique
- `total_price > 0`
- `passenger_count <= flight.total_seats`
- `user` exists and is active
- `flight` exists and is active

**Key Properties**:
- `Id` - Unique identifier
- `FlightId` - Associated flight ID
- `UserId` - Booking user ID
- `BookingReference` - Unique reference code (e.g., "ABC123XYZ")
- `PassengerCount` - Number of passengers
- `TotalPrice` - Total booking price
- `Status` - Booking status (see `BookingStatus` enum)
- `CreatedAt`, `UpdatedAt` - Timestamps
- `Notes` - Optional special requests

**Domain Methods**:
```csharp
// Transitions booking to confirmed after payment
void Confirm()
  // Throws InvalidOperationException if not Pending

// Transitions booking to checked in
void CheckIn()
  // Throws InvalidOperationException if not Confirmed

// Cancels the booking
void Cancel()
  // Throws InvalidOperationException if already CheckedIn or Cancelled
```

**Navigation Properties**:
- `Flight` - Associated flight entity
- `User` - Booking user entity
- `Passengers` - Passenger details in booking
- `Payment` - Associated payment

---

## 🔗 Child Entities (Not Aggregate Roots)

### 5. Passenger
**Location**: `Domain/Entities/Passenger.cs`

**Parent Aggregate**: Booking

Represents an individual passenger within a booking. Should not exist independently of a booking.

**Invariants**:
- Age validation (international flights require age >= 2)
- Passport format validation
- Name format validation

**Key Properties**:
- `Id` - Unique identifier
- `BookingId` - Parent booking ID
- `FirstName` - Passenger first name
- `LastName` - Passenger last name
- `DateOfBirth` - Passenger age
- `PassportNumber` - Optional passport number
- `Nationality` - Optional nationality code
- `Email` - Passenger email
- `PhoneNumber` - Optional phone number
- `CreatedAt`, `UpdatedAt` - Timestamps

**Domain Methods**:
```csharp
// Calculate passenger's current age
int GetAge()

// Validate for international flight eligibility
bool IsValidForInternationalFlight()
  // Requires: age >= 2, passport number, nationality
```

**Navigation Property**:
- `Booking` - Parent booking entity

---

### 6. Payment
**Location**: `Domain/Entities/Payment.cs`

**Parent Aggregate**: Not strictly tied; manages payment lifecycle

Represents a payment transaction for a booking.

**Invariants**:
- `amount > 0`
- `amount` matches booking total
- `status` transitions are valid
- No duplicate payments for same booking

**Key Properties**:
- `Id` - Unique identifier
- `BookingId` - Associated booking
- `UserId` - User making payment
- `Amount` - Payment amount
- `Method` - Payment method (see `PaymentMethod` enum)
- `Status` - Payment status (see `PaymentStatus` enum)
- `TransactionId` - External transaction ID
- `Notes` - Error/additional information
- `CreatedAt`, `UpdatedAt` - Timestamps

**Domain Methods**:
```csharp
// Mark payment as successful
void Complete(string transactionId)
  // Throws if not Pending, requires transactionId

// Mark payment as failed
void Fail(string errorMessage)
  // Throws if not Pending, requires error message

// Initiate refund
void Refund()
  // Throws if not Completed
```

**Navigation Properties**:
- `Booking` - Associated booking
- `User` - User who made payment

---

### 7. CrewMember
**Location**: `Domain/Entities/CrewMember.cs`

Represents a crew member in the system.

**Key Properties**:
- `Id` - Unique identifier
- `FirstName` - First name
- `LastName` - Last name
- `Role` - Job role (Pilot, Co-Pilot, Flight Attendant, etc.)
- `LicenseNumber` - License/certification number
- `CreatedAt`, `UpdatedAt` - Timestamps

**Navigation Property**:
- `FlightAssignments` - Flights this crew member is assigned to

---

### 8. FlightCrew
**Location**: `Domain/Entities/FlightCrew.cs`

**Junction Entity**: Links Flight and CrewMember (many-to-many relationship)

Represents the assignment of a crew member to a specific flight.

**Key Properties**:
- `Id` - Unique identifier
- `FlightId` - Associated flight
- `CrewMemberId` - Associated crew member
- `AssignedAt` - Assignment timestamp

**Navigation Properties**:
- `Flight` - Associated flight
- `CrewMember` - Associated crew member

---

## 📊 Entity Relationship Diagram

```
┌─────────────┐
│   Airport   │
│  (Ref)      │
└──────┬──────┘
       │
   ┌───┴───┐
   │       │
   ▼       ▼
  (DepartureAirportId)
  (ArrivalAirportId)
   │       │
   │  ┌────┴─────────────────┐
   │  │                      │
   ▼  ▼                      ▼
┌──────────┐         ┌──────────────┐
│  Flight  │◄────────┤  FlightCrew  │
│  (Root)  │         └──────────────┘
└────┬─────┘              │
     │                    │
     │         (CrewMemberId)
     │                    │
     │                    ▼
     │          ┌──────────────────┐
     │          │   CrewMember     │
     │          │    (Ref)         │
     │          └──────────────────┘
     │
     ├──┐  (FlightId)
     │  │
     ▼  ▼
   ┌──────────┐
   │ Booking  │◄────┐
   │ (Root)   │     │
   └────┬─────┘     │
        │      (BookingId)
        ├──┐        │
        │  ├─────────┤
        │  │  (UserId)
        │  │         │
        │  ▼         ▼
        │ ┌────────────────┐
        │ │  Passenger     │
        │ │   (Child)      │
        │ └────────────────┘
        │
        │
        ▼
   ┌──────────┐
   │ Payment  │◄─────┐
   │ (Root)   │      │
   └──────────┘      │
        │       (UserId)
        │            │
        └─────┬──────┘
              │
              ▼
        ┌─────────────┐
        │    User     │
        │   (Root)    │
        └─────────────┘
```

---

## 🔒 Invariant Enforcement

### Flight Aggregate
```csharp
// Automatic enforcement via business methods
flight.ReserveSeats(5);   // Validates: available_seats >= 5
flight.ReleaseSeats(3);   // Validates: available_seats + 3 <= total_seats

// Status state machine enforced
flight.MarkAsCompleted(); // Only from Active
```

### Booking Aggregate
```csharp
// Status transitions are validated
booking.Confirm();    // Only from Pending
booking.CheckIn();    // Only from Confirmed
booking.Cancel();     // Not from CheckedIn or already Cancelled
```

### Payment Aggregate
```csharp
// Status transitions with required data
payment.Complete("TXN123");  // Requires transactionId
payment.Fail("Invalid card"); // Requires error message
payment.Refund();            // Only from Completed
```

---

## 📁 File Structure

```
Domain/
├── Entities/
│   ├── Airport.cs
│   ├── Flight.cs
│   ├── User.cs
│   ├── Booking.cs
│   ├── Passenger.cs
│   ├── Payment.cs
│   ├── CrewMember.cs
│   └── FlightCrew.cs
└── Enums/
    ├── FlightStatus.cs
    ├── BookingStatus.cs
    ├── UserStatus.cs
    ├── PaymentMethod.cs
    └── PaymentStatus.cs
```

---

## ✅ Design Principles Applied

1. **Encapsulation** - Internal state hidden, behavior exposed via methods
2. **Immutable Values** - Enums provide immutable status values
3. **Rich Domain Model** - Business logic in entities (ReserveSeats, Confirm, etc.)
4. **Explicit Methods** - `ReserveSeats()` instead of direct property access
5. **Invariant Enforcement** - Constraints enforced via public methods
6. **Self-Validating** - Entities ensure own validity through methods
7. **Meaningful Names** - Names reflect business domain (FlightStatus, BookingReference)
8. **No External Dependencies** - Domain layer contains only C# types
9. **Clear Aggregates** - Flight, Booking, User are aggregate roots
10. **Separation of Concerns** - DTOs and services will map these entities

---

## 🚀 Next Steps

Once domain entities are defined, the following layers should be implemented:
1. **Infrastructure Layer** - DbContext, Repositories, EF Core Configurations
2. **Application Layer** - Services, Validators, DTOs, Mappers
3. **API Layer** - Controllers, API endpoints, Request/Response handling

---

**Version**: 1.0 | **Status**: Domain Entities Complete
