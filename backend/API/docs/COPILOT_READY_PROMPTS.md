# 🤖 Copy-Paste Ready Prompts for GitHub Copilot

## 📌 Cách Dùng:
1. Tạo file code (ví dụ: User.cs)
2. Mở Copilot Chat (Ctrl+Shift+I)
3. Copy-paste prompt dưới đây
4. Copilot sẽ sinh code

---

# 🔵 PHASE 1: DOMAIN ENTITIES (23 entities)

## Prompt 1: User Entity

```
Tạo Domain/Entities/User.cs cho Flight Booking System.

Entity User có các properties:
- Id (int, PK)
- Email (string, unique, required)
- PasswordHash (string, required)
- FullName (string, required)
- Phone (string, nullable)
- GoogleId (string, unique, nullable)
- Status (int, default=0: 0=Active, 1=Inactive, 2=Suspended)
- CreatedAt (DateTime, UTC)
- UpdatedAt (DateTime, UTC)

Navigation properties:
- Roles: ICollection<Role>
- Bookings: ICollection<Booking>
- UserRoles: ICollection<UserRole>
- EmailVerificationTokens: ICollection<EmailVerificationToken>
- PasswordResetTokens: ICollection<PasswordResetToken>
- NotificationLogs: ICollection<NotificationLog>
- AuditLogs: ICollection<AuditLog> (where ActorId = Id)

Business methods:
- IsActive() -> bool
- IsSuspended() -> bool
- UpdatePassword(string newHash) -> void
- UpdateProfile(string fullName, string phone) -> void

Validation rules:
- Email không null, định dạng hợp lệ
- PasswordHash không null
- FullName không null, max 255 chars
- Phone max 20 chars

Thêm các indexes và constraints phù hợp.
```

## Prompt 2: Role Entity

```
Tạo Domain/Entities/Role.cs

Properties:
- Id (int, PK)
- Name (string, unique, required, max 50)
- Description (string, nullable, max 500)

Navigation:
- Users: ICollection<User>
- UserRoles: ICollection<UserRole>

Có 4 roles mặc định: Admin, User, Staff, Manager

Không cần business logic, là mapping table.
```

## Prompt 3: UserRole Entity (M:M)

```
Tạo Domain/Entities/UserRole.cs

Composite PK: UserId, RoleId

Properties:
- UserId (int, FK)
- RoleId (int, FK)

Navigation:
- User: User
- Role: Role

Đây là junction table cho relationship M:M giữa User và Role.
```

## Prompt 4: EmailVerificationToken Entity

```
Tạo Domain/Entities/EmailVerificationToken.cs

Properties:
- Id (int, PK)
- UserId (int, FK)
- Code (string, required, max 500)
- ExpiresAt (DateTime)
- UsedAt (DateTime, nullable)

Navigation:
- User: User

Business methods:
- IsExpired(DateTime currentDateTime) -> bool
- IsUsed() -> bool
- MarkAsUsed() -> void
```

## Prompt 5: PasswordResetToken Entity

```
Tạo Domain/Entities/PasswordResetToken.cs

Properties:
- Id (int, PK)
- UserId (int, FK)
- Code (string, required, max 500)
- ExpiresAt (DateTime)
- UsedAt (DateTime, nullable)

Navigation:
- User: User

Business methods:
- IsExpired(DateTime currentDateTime) -> bool
- IsUsed() -> bool
- MarkAsUsed() -> void
```

## Prompt 6: Airport Entity

```
Tạo Domain/Entities/Airport.cs

Properties:
- Id (int, PK)
- Code (string, unique, required, max 10) // SYD, LAX, HAN, SGN
- Name (string, required, max 255)
- City (string, required, max 100)
- Province (string, nullable, max 100)
- IsActive (bool, default=true)

Navigation:
- DepartureRoutes: ICollection<Route> (FK DepartureAirportId)
- ArrivalRoutes: ICollection<Route> (FK ArrivalAirportId)

Business methods:
- Activate() -> void
- Deactivate() -> void

Validation:
- Code không null, max 10
- Name không null, max 255
```

## Prompt 7: Aircraft Entity

```
Tạo Domain/Entities/Aircraft.cs

Properties:
- Id (int, PK)
- Model (string, required, max 100) // Boeing 777-300ER, Airbus A321
- RegistrationNumber (string, unique, required, max 50) // VN-A100
- TotalSeats (int, required) // capacity
- IsActive (bool, default=true)

Navigation:
- SeatTemplates: ICollection<AircraftSeatTemplate>
- Flights: ICollection<Flight>

Business methods:
- GetTotalSeatsByClass(SeatClass seatClass) -> int
- Activate() -> void
- Deactivate() -> void

Validation:
- Model required, max 100
- RegistrationNumber unique
- TotalSeats > 0
```

## Prompt 8: Route Entity

```
Tạo Domain/Entities/Route.cs

Properties:
- Id (int, PK)
- DepartureAirportId (int, FK)
- ArrivalAirportId (int, FK)
- DistanceKm (int, required)
- EstimatedDurationMinutes (int, required)
- IsActive (bool, default=true)

Navigation:
- DepartureAirport: Airport
- ArrivalAirport: Airport
- Flights: ICollection<Flight>

Business methods:
- GetDurationInHours() -> double
- IsValid() -> bool // departure != arrival

Validation:
- DepartureAirportId != ArrivalAirportId
- DistanceKm > 0
- EstimatedDurationMinutes > 0
```

## Prompt 9: SeatClass Entity

```
Tạo Domain/Entities/SeatClass.cs

Properties:
- Id (int, PK)
- Code (string, unique, required, max 20) // ECO, BUS, FIRST
- Name (string, required, max 100)
- RefundPercent (decimal, 0-100)
- ChangeFee (decimal)
- Priority (int) // 1=First, 2=Business, 3=Economy

Navigation:
- AircraftSeatTemplates: ICollection<AircraftSeatTemplate>
- FlightSeatInventories: ICollection<FlightSeatInventory>
- RefundPolicies: ICollection<RefundPolicy>

Pre-defined SeatClasses (seed data):
- ECO (Economy): 80% refund, $50 change fee, priority 3
- BUS (Business): 100% refund, $0 change fee, priority 2
- FIRST (First): 100% refund, $0 change fee, priority 1
```

## Prompt 10: AircraftSeatTemplate Entity

```
Tạo Domain/Entities/AircraftSeatTemplate.cs

Unique constraint: (AircraftId, SeatClassId)

Properties:
- Id (int, PK)
- AircraftId (int, FK)
- SeatClassId (int, FK)
- DefaultSeatCount (int, required) // e.g., 100 economy seats
- DefaultBasePrice (decimal, required)

Navigation:
- Aircraft: Aircraft
- SeatClass: SeatClass

Validation:
- DefaultSeatCount > 0
- DefaultBasePrice > 0
- Unique (AircraftId, SeatClassId)
```

## Prompt 11: Flight Entity (UPDATED)

```
Update Domain/Entities/Flight.cs theo schema mới:

Thay đổi:
- Remove: DepartureAirportId, ArrivalAirportId
- Add: RouteId (FK)

Properties:
- Id (int, PK)
- FlightNumber (string, unique, required, max 20) // AA100, VN123
- RouteId (int, FK) // NEW
- AircraftId (int, FK)
- DepartureTime (DateTime, UTC)
- ArrivalTime (DateTime, UTC)
- Status (int, default=0) // 0=Active, 1=Cancelled, 2=Delayed, 3=Completed
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

Navigation:
- Route: Route // NEW
- Aircraft: Aircraft
- SeatInventories: ICollection<FlightSeatInventory>
- OutboundBookings: ICollection<Booking> (as OutboundFlight)
- ReturnBookings: ICollection<Booking> (as ReturnFlight)

Business methods:
- GetDepartureAirport() -> Airport // through Route
- GetArrivalAirport() -> Airport // through Route
- GetDuration() -> TimeSpan
- CanBook(int seatClassId, int count) -> bool
- ReserveSeats(int seatClassId, int count) -> void
- ReleaseSeats(int seatClassId, int count) -> void
- Cancel() -> void
- IsDepartureSoon(int hours) -> bool

Validation:
- FlightNumber unique
- ArrivalTime > DepartureTime
- RouteId, AircraftId required
```

## Prompt 12: FlightSeatInventory Entity (CRITICAL)

```
Tạo Domain/Entities/FlightSeatInventory.cs

Unique constraint: (FlightId, SeatClassId)

Properties:
- Id (int, PK)
- FlightId (int, FK)
- SeatClassId (int, FK)
- TotalSeats (int, required) // allocated seats
- AvailableSeats (int, required) // currently available
- HeldSeats (int, default=0) // temporarily held (15 min)
- SoldSeats (int, default=0) // confirmed bookings
- BasePrice (decimal, required)
- CurrentPrice (decimal, required) // dynamic pricing
- Version (int, default=0) // for optimistic concurrency
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

Navigation:
- Flight: Flight
- SeatClass: SeatClass
- BookingPassengers: ICollection<BookingPassenger>

CRITICAL Invariants (thêm check constraints):
- AvailableSeats >= 0
- SoldSeats >= 0
- HeldSeats >= 0
- AvailableSeats + SoldSeats + HeldSeats <= TotalSeats

Business methods:
- CanBook(int count) -> bool
- ReserveSeats(int count) -> void
- HoldSeats(int count) -> void
- ReleaseHold(int count) -> void
- GetOccupancyPercent() -> decimal
- UpdateDynamicPrice(decimal demandFactor, decimal timeFactor) -> void

Concurrency:
- Use Version field for optimistic concurrency control
- Increment on each update
```

## Prompt 13: Booking Entity (UPDATED)

```
Update Domain/Entities/Booking.cs:

Add properties:
- TripType (int, default=0) // 0=OneWay, 1=RoundTrip
- ReturnFlightId (int, nullable, FK)
- PromotionId (int, nullable, FK)
- ExpiresAt (DateTime, nullable)

Update Status values:
- 0=Pending, 1=Confirmed, 2=CheckedIn, 3=Cancelled

Full Properties list:
- Id (int, PK)
- BookingCode (string, unique, required, max 10) // ABC123
- UserId (int, FK)
- TripType (int, default=0)
- OutboundFlightId (int, FK)
- ReturnFlightId (int, nullable, FK)
- Status (int, default=0)
- ContactEmail (string, required)
- ContactPhone (string, nullable)
- TotalAmount (decimal, required)
- DiscountAmount (decimal, default=0)
- FinalAmount (decimal, required)
- ExpiresAt (DateTime, nullable)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

Navigation:
- User: User
- OutboundFlight: Flight
- ReturnFlight: Flight (nullable)
- Passengers: ICollection<BookingPassenger>
- Payment: Payment (1:1)

Business methods:
- GenerateBookingCode() -> string
- CanCancel(DateTime currentDateTime) -> bool
- Cancel(string reason) -> void
- CheckIn() -> void
- ApplyDiscount(decimal amount) -> void
- HasExpired(DateTime currentDateTime) -> bool

Validation:
- If TripType=RoundTrip: ReturnFlightId required
- BookingCode unique
- FinalAmount = TotalAmount - DiscountAmount
```

## Prompt 14: BookingPassenger Entity

```
Tạo Domain/Entities/BookingPassenger.cs

Properties:
- Id (int, PK)
- BookingId (int, FK)
- FullName (string, required, max 255)
- Gender (string, nullable, max 10) // M, F, Other
- DateOfBirth (Date, nullable)
- NationalId (string, nullable, max 50)
- PassengerType (int, default=0) // 0=Adult, 1=Child, 2=Infant
- FlightSeatInventoryId (int, FK) // which class booked
- FareSnapshot (string, nullable) // JSON: price, taxes, fees

Navigation:
- Booking: Booking
- FlightSeatInventory: FlightSeatInventory
- Services: ICollection<BookingService>
- Ticket: Ticket (1:1)

Business methods:
- IsAdult() -> bool
- IsChild() -> bool
- IsInfant() -> bool
- GetAge() -> int
- GetFareDetails() -> dynamic (deserialize JSON)
```

## Prompt 15: BookingService Entity

```
Tạo Domain/Entities/BookingService.cs

Properties:
- Id (int, PK)
- BookingPassengerId (int, FK)
- ServiceType (string, required, max 50) // LUGGAGE, MEAL, SEAT_UPGRADE
- ServiceName (string, required, max 255)
- Price (decimal, required)

Navigation:
- BookingPassenger: BookingPassenger
```

## Prompt 16: Ticket Entity

```
Tạo Domain/Entities/Ticket.cs

Properties:
- Id (int, PK)
- BookingPassengerId (int, FK)
- TicketNumber (string, unique, required, max 50) // IATA format
- Status (int, default=0) // 0=Issued, 1=Used, 2=Refunded, 3=Cancelled
- IssuedAt (DateTime, default=now)
- ReplacedByTicketId (int, nullable, FK) // self-reference

Navigation:
- BookingPassenger: BookingPassenger
- ReplacedByTicket: Ticket (nullable, self-reference)

Business methods:
- IsValid() -> bool
- MarkAsUsed() -> void
- ReplaceWith(int newTicketId) -> void
```

## Prompt 17: Payment Entity (UPDATED)

```
Update Domain/Entities/Payment.cs:

Add properties:
- Provider (string, required, max 50) // STRIPE, PAYPAL, MOMO, VNPAY
- Method (string, required, max 50) // CARD, BANK, WALLET, CASH
- QrCodeData (string, nullable)
- RawCallbackData (string, nullable) // JSON from provider

Full Properties:
- Id (int, PK)
- BookingId (int, FK)
- Provider (string, required, max 50)
- Method (string, required, max 50)
- Amount (decimal, required)
- Status (int, default=0) // 0=Pending, 1=Completed, 2=Failed, 3=Refunded
- TransactionRef (string, nullable)
- QrCodeData (string, nullable)
- PaidAt (DateTime, nullable)
- RawCallbackData (string, nullable)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

Navigation:
- Booking: Booking
- RefundRequest: RefundRequest (1:1, nullable)

Business methods:
- MarkAsCompleted(string transactionRef) -> void
- MarkAsFailed() -> void
- MarkAsRefunded() -> void
- CanBeRefunded() -> bool
```

## Prompt 18: RefundPolicy Entity

```
Tạo Domain/Entities/RefundPolicy.cs

Properties:
- Id (int, PK)
- SeatClassId (int, FK)
- HoursBeforeDeparture (int, required) // e.g., 72, 48, 24
- RefundPercent (decimal, 0-100)
- PenaltyFee (decimal, default=0)

Navigation:
- SeatClass: SeatClass

Example policies:
- ECO + 72 hours: 100% refund
- ECO + 48 hours: 80% refund
- ECO + 24 hours: 50% refund
- ECO + 0 hours: 0% refund

Business methods:
- CalculateRefundAmount(decimal bookingAmount, int hoursBeforeDeparture) -> decimal
- IsRefundable(int hoursBeforeDeparture) -> bool
```

## Prompt 19: RefundRequest Entity

```
Tạo Domain/Entities/RefundRequest.cs

Properties:
- Id (int, PK)
- BookingId (int, FK)
- PaymentId (int, FK)
- RefundAmount (decimal, required)
- Reason (string, nullable, max 500)
- Status (int, default=0) // 0=Pending, 1=Approved, 2=Processed, 3=Rejected
- ProcessedAt (DateTime, nullable)
- CreatedAt (DateTime)

Navigation:
- Booking: Booking
- Payment: Payment

Business methods:
- Approve() -> void
- Process(string transactionId) -> void
- Reject(string reason) -> void
```

## Prompt 20: Promotion Entity

```
Tạo Domain/Entities/Promotion.cs

Properties:
- Id (int, PK)
- Code (string, unique, required, max 50) // SUMMER2024, SAVE20
- DiscountType (int, default=0) // 0=PERCENTAGE, 1=FIXED
- DiscountValue (decimal, required)
- ValidFrom (DateTime, required)
- ValidTo (DateTime, required)
- UsageLimit (int, nullable) // max uses
- UsedCount (int, default=0)
- IsActive (bool, default=true)
- CreatedAt (DateTime)

Navigation:
- PromotionUsages: ICollection<PromotionUsage>

Business methods:
- IsValid(DateTime currentDateTime) -> bool
- IsAvailable() -> bool
- CalculateDiscount(decimal amount) -> decimal
- CanBeUsed() -> bool
- IncrementUsage() -> void

Validation:
- ValidTo > ValidFrom
- UsedCount <= UsageLimit (if limit set)
```

## Prompt 21: PromotionUsage Entity

```
Tạo Domain/Entities/PromotionUsage.cs

Unique constraint: (PromotionId, BookingId)

Properties:
- Id (int, PK)
- PromotionId (int, FK)
- BookingId (int, FK)
- DiscountAmount (decimal, required)
- UsedAt (DateTime, default=now)

Navigation:
- Promotion: Promotion
- Booking: Booking
```

## Prompt 22: NotificationLog Entity

```
Tạo Domain/Entities/NotificationLog.cs

Properties:
- Id (int, PK)
- UserId (int, FK)
- Email (string, nullable)
- Type (string, required, max 50) // EMAIL, SMS, PUSH
- Title (string, required, max 255)
- Content (string, required)
- Status (int, default=0) // 0=Pending, 1=Sent, 2=Failed
- SentAt (DateTime, nullable)
- CreatedAt (DateTime)

Navigation:
- User: User

Business methods:
- MarkAsSent() -> void
- MarkAsFailed() -> void
- IsPending() -> bool
```

## Prompt 23: AuditLog Entity

```
Tạo Domain/Entities/AuditLog.cs

Properties:
- Id (int, PK)
- ActorId (int, nullable, FK) // who made change
- Action (string, required, max 100) // CREATE, UPDATE, DELETE, PAYMENT, REFUND
- EntityType (string, required, max 100) // BOOKING, PAYMENT, REFUND, USER
- EntityId (int, required)
- BeforeJson (string, nullable) // previous state JSON
- AfterJson (string, nullable) // new state JSON
- CreatedAt (DateTime, default=now)

Navigation:
- Actor: User (nullable)

Business methods:
- GetChangesSummary() -> string
- GetBeforeState() -> dynamic
- GetAfterState() -> dynamic
- IsCreateAction() -> bool
- IsUpdateAction() -> bool
```

---

# 🟠 PHASE 2: ENTITY CONFIGURATIONS (23 files)

## Prompt 24: UserConfiguration

```
Tạo Infrastructure/Configurations/UserConfiguration.cs

Implement IEntityTypeConfiguration<User>

Configure method:
1. Primary Key: Id
2. Properties:
   - Email: HasMaxLength(255), IsRequired(), HasIndex()
   - PasswordHash: IsRequired()
   - FullName: HasMaxLength(255), IsRequired()
   - Phone: HasMaxLength(20)
   - GoogleId: HasMaxLength(255)
   - Status: HasDefaultValue(0)

3. Unique constraints:
   - Email unique
   - GoogleId unique

4. Relationships:
   - HasMany(u => u.Bookings).WithOne(b => b.User).HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade)
   - HasMany(u => u.UserRoles).WithOne(ur => ur.User).HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade)
   - HasMany(u => u.EmailVerificationTokens).WithOne(e => e.User).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade)
   - HasMany(u => u.PasswordResetTokens).WithOne(p => p.User).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade)
   - HasMany(u => u.NotificationLogs).WithOne(n => n.User).HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade)

5. Table: ToTable("Users")

6. Indexes:
   - HasIndex(u => u.Email).IsUnique()
   - HasIndex(u => u.GoogleId).IsUnique()
```

## Prompt 25-47: Remaining Configurations

(Tương tự Prompt 24 cho các entities còn lại)

```
Tạo 23 configuration files:
- RoleConfiguration.cs
- UserRoleConfiguration.cs
- EmailVerificationTokenConfiguration.cs
- PasswordResetTokenConfiguration.cs
- AirportConfiguration.cs
- AircraftConfiguration.cs
- RouteConfiguration.cs
- SeatClassConfiguration.cs
- AircraftSeatTemplateConfiguration.cs
- FlightConfiguration.cs
- FlightSeatInventoryConfiguration.cs
- BookingConfiguration.cs
- BookingPassengerConfiguration.cs
- BookingServiceConfiguration.cs
- TicketConfiguration.cs
- PaymentConfiguration.cs
- RefundPolicyConfiguration.cs
- RefundRequestConfiguration.cs
- PromotionConfiguration.cs
- PromotionUsageConfiguration.cs
- NotificationLogConfiguration.cs
- AuditLogConfiguration.cs

Mỗi file:
1. Implement IEntityTypeConfiguration<T>
2. Configure PK, properties, constraints
3. Configure relationships
4. Add indexes
5. Set table name
```

---

# 🟢 PHASE 3: DATABASE CONTEXT

## Prompt 48: Update FlightBookingDbContext

```
Update Infrastructure/Data/FlightBookingDbContext.cs:

Add 23 DbSet properties:
- DbSet<User> Users
- DbSet<Role> Roles
- DbSet<UserRole> UserRoles
- DbSet<EmailVerificationToken> EmailVerificationTokens
- DbSet<PasswordResetToken> PasswordResetTokens
- DbSet<Airport> Airports
- DbSet<Aircraft> Aircraft
- DbSet<Route> Routes
- DbSet<SeatClass> SeatClasses
- DbSet<AircraftSeatTemplate> AircraftSeatTemplates
- DbSet<Flight> Flights
- DbSet<FlightSeatInventory> FlightSeatInventories
- DbSet<Booking> Bookings
- DbSet<BookingPassenger> BookingPassengers
- DbSet<BookingService> BookingServices
- DbSet<Ticket> Tickets
- DbSet<Payment> Payments
- DbSet<RefundPolicy> RefundPolicies
- DbSet<RefundRequest> RefundRequests
- DbSet<Promotion> Promotions
- DbSet<PromotionUsage> PromotionUsages
- DbSet<NotificationLog> NotificationLogs
- DbSet<AuditLog> AuditLogs

OnModelCreating method:
- base.OnModelCreating(modelBuilder)
- modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlightBookingDbContext).Assembly)

SaveChangesAsync override:
- Set UpdatedAt = DateTime.UtcNow for modified entities
- Increment Version for FlightSeatInventory
- Call await base.SaveChangesAsync(cancellationToken)
```

---

# 🟡 PHASE 4: REPOSITORIES

## Prompt 49: Create Repository Interfaces

```
Tạo các interface files trong Application/Interfaces/:

IUserRepository.cs
- Task<User> GetByEmailAsync(string email)
- Task<User> GetWithRolesAsync(int id)
- Task<User> CreateAsync(User user)
- Task UpdateAsync(User user)
- Task<bool> ExistsAsync(int id)

IAirportRepository.cs
- Task<Airport> GetByCodeAsync(string code)
- Task<IEnumerable<Airport>> GetAllActiveAsync()
- Task<Airport> CreateAsync(Airport airport)

IFlightRepository.cs
- Task<Flight> GetByFlightNumberAsync(string flightNumber)
- Task<IEnumerable<Flight>> SearchAsync(int departureId, int arrivalId, DateTime date)
- Task<Flight> GetWithInventoriesAsync(int id)
- Task<Flight> CreateAsync(Flight flight)

IFlightSeatInventoryRepository.cs
- Task<FlightSeatInventory> GetAsync(int flightId, int seatClassId)
- Task<IEnumerable<FlightSeatInventory>> GetAllForFlightAsync(int flightId)
- Task ReserveSeatsAsync(int id, int count, int version) // optimistic concurrency

IBookingRepository.cs
- Task<Booking> GetByBookingCodeAsync(string code)
- Task<IEnumerable<Booking>> GetByUserAsync(int userId, int page, int pageSize)
- Task<Booking> GetWithPassengersAsync(int id)
- Task<Booking> CreateAsync(Booking booking)

IPromotionRepository.cs
- Task<Promotion> GetByCodeAsync(string code)
- Task<IEnumerable<Promotion>> GetActiveAsync(DateTime currentDateTime)

Tất cả methods là async với Task<T> return type.
```

---

# 🔵 PHASE 5: MIGRATIONS

## Prompt 50: Create Initial Migration

```
Tạo EF Core migration với lệnh:

dotnet ef migrations add InitialCreate --project backend

Sau đó update database:

dotnet ef database update --project backend

Migration sẽ:
1. Tạo tất cả 23 tables
2. Tạo tất cả foreign keys
3. Tạo tất cả unique constraints
4. Tạo tất cả indexes
5. Seed initial data (roles, seat classes)
```

---

## 📌 USAGE SUMMARY

### Step by step:

```
1. Phase 1: Copy Prompt 1-23
   → Tạo 23 domain entities

2. Phase 2: Copy Prompt 24-47
   → Tạo 23 configurations

3. Phase 3: Copy Prompt 48
   → Update DbContext

4. Phase 4: Copy Prompt 49
   → Tạo repository interfaces

5. Phase 5: Copy Prompt 50
   → Tạo migration và update database

Timeline:
- Phase 1: 30 minutes
- Phase 2: 30 minutes
- Phase 3: 10 minutes
- Phase 4: 20 minutes
- Phase 5: 10 minutes

Total: ~2 hours
```

---

**Status**: ✅ Sẵn sàng copy-paste  
**Total Prompts**: 50  
**Output**: Toàn bộ database layer + 23 entities + configurations + DbContext
