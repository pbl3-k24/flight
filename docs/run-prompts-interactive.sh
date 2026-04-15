#!/bin/bash

################################################################################
# Flight Booking System - Interactive Copilot Prompt Executor
# 
# Alternative: Không cần Copilot CLI
# 
# Usage: ./run-prompts-interactive.sh
# 
# Cách hoạt động:
# 1. Script hiển thị prompt
# 2. Copy prompt tự động vào clipboard
# 3. Bạn paste vào Copilot Chat (Ctrl+Shift+I)
# 4. Bấm Enter để tiếp tục prompt tiếp theo
#
# Timeline: ~2 hours (với review code)
################################################################################

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
NC='\033[0m'

# Counter
PROMPT_COUNT=0
TOTAL_PROMPTS=50
COMPLETED=0

# Function: Print header
print_header() {
    clear
    echo -e "\n${BLUE}════════════════════════════════════════════════════════════${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${BLUE}════════════════════════════════════════════════════════════${NC}\n"
}

# Function: Print prompt number
print_prompt_num() {
    PROMPT_COUNT=$((PROMPT_COUNT + 1))
    local percent=$((COMPLETED * 100 / TOTAL_PROMPTS))
    echo -e "${GREEN}[Prompt $PROMPT_COUNT/$TOTAL_PROMPTS - $percent% Complete]${NC}"
}

# Function: Copy to clipboard (works on Mac/Linux)
copy_to_clipboard() {
    local text=$1
    
    if command -v pbcopy &> /dev/null; then
        # macOS
        echo "$text" | pbcopy
        return 0
    elif command -v xclip &> /dev/null; then
        # Linux with xclip
        echo "$text" | xclip -selection clipboard
        return 0
    elif command -v xsel &> /dev/null; then
        # Linux with xsel
        echo "$text" | xsel --clipboard --input
        return 0
    elif command -v wl-copy &> /dev/null; then
        # Linux Wayland
        echo "$text" | wl-copy
        return 0
    else
        # Fallback: save to temp file
        echo "$text" > /tmp/copilot_prompt.txt
        return 1
    fi
}

# Function: Display and copy prompt
show_prompt() {
    local title=$1
    local prompt=$2
    local file_path=$3
    
    print_prompt_num
    echo -e "${CYAN}$title${NC}"
    echo -e "${YELLOW}File: $file_path${NC}\n"
    
    echo -e "${MAGENTA}─── PROMPT ───────────────────────────────────────${NC}"
    echo -e "$prompt"
    echo -e "${MAGENTA}───────────────────────────────────────────────────${NC}\n"
    
    # Copy to clipboard
    if copy_to_clipboard "$prompt"; then
        echo -e "${GREEN}✅ Prompt copied to clipboard!${NC}"
    else
        echo -e "${YELLOW}⚠️  Clipboard copy failed. Saved to /tmp/copilot_prompt.txt${NC}"
    fi
    
    echo -e "\n${CYAN}Instructions:${NC}"
    echo -e "1. ${YELLOW}Open VS Code${NC}"
    echo -e "2. ${YELLOW}Open Copilot Chat (Ctrl+Shift+I)${NC}"
    echo -e "3. ${YELLOW}Paste the prompt (Ctrl+V)${NC}"
    echo -e "4. ${YELLOW}Review generated code${NC}"
    echo -e "5. ${YELLOW}Accept changes${NC}"
    echo -e "6. ${YELLOW}Press Enter to continue${NC}\n"
    
    read -p "Press Enter when done with this prompt..."
    COMPLETED=$((COMPLETED + 1))
}

################################################################################
# MAIN SCRIPT
################################################################################

print_header "🚀 Flight Booking System - Interactive Prompt Executor"

echo -e "${CYAN}Project:${NC} Flight Ticket Booking API"
echo -e "${CYAN}Method:${NC} Manual Copy-Paste (No CLI needed)"
echo -e "${CYAN}Timeline:${NC} ~2 hours\n"

echo -e "${YELLOW}⚠️  Important:${NC}"
echo -e "- Have VS Code open with your project"
echo -e "- Have Copilot extension installed"
echo -e "- Create folders as prompted\n"

read -p "Ready to start? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    exit 1
fi

################################################################################
# PHASE 1: DOMAIN ENTITIES
################################################################################

print_header "PHASE 1: Domain Entities (23 prompts)"

# Prompt 1: User Entity
show_prompt \
    "User Entity" \
    "Tạo Domain/Entities/User.cs cho Flight Booking System.

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
- AuditLogs: ICollection<AuditLog>

Business methods:
- IsActive() -> bool
- IsSuspended() -> bool
- UpdatePassword(string newHash) -> void
- UpdateProfile(string fullName, string phone) -> void

Validation rules:
- Email không null, định dạng hợp lệ
- PasswordHash không null
- FullName không null, max 255 chars
- Phone max 20 chars" \
    "backend/API/Domain/Entities/User.cs"

# Prompt 2: Role Entity
show_prompt \
    "Role Entity" \
    "Tạo Domain/Entities/Role.cs

Properties:
- Id (int, PK)
- Name (string, unique, required, max 50)
- Description (string, nullable, max 500)

Navigation:
- Users: ICollection<User>
- UserRoles: ICollection<UserRole>

Có 4 roles mặc định: Admin, User, Staff, Manager" \
    "backend/API/Domain/Entities/Role.cs"

# Prompt 3: UserRole
show_prompt \
    "UserRole Entity (M:M)" \
    "Tạo Domain/Entities/UserRole.cs

Composite PK: UserId, RoleId

Properties:
- UserId (int, FK)
- RoleId (int, FK)

Navigation:
- User: User
- Role: Role

Đây là junction table cho relationship M:M" \
    "backend/API/Domain/Entities/UserRole.cs"

# Prompt 4: EmailVerificationToken
show_prompt \
    "EmailVerificationToken Entity" \
    "Tạo Domain/Entities/EmailVerificationToken.cs

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
- MarkAsUsed() -> void" \
    "backend/API/Domain/Entities/EmailVerificationToken.cs"

# Prompt 5: PasswordResetToken
show_prompt \
    "PasswordResetToken Entity" \
    "Tạo Domain/Entities/PasswordResetToken.cs

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
- MarkAsUsed() -> void" \
    "backend/API/Domain/Entities/PasswordResetToken.cs"

# Prompt 6: Airport
show_prompt \
    "Airport Entity" \
    "Tạo Domain/Entities/Airport.cs

Properties:
- Id (int, PK)
- Code (string, unique, required, max 10)
- Name (string, required, max 255)
- City (string, required, max 100)
- Province (string, nullable, max 100)
- IsActive (bool, default=true)

Navigation:
- DepartureRoutes: ICollection<Route> (FK DepartureAirportId)
- ArrivalRoutes: ICollection<Route> (FK ArrivalAirportId)

Business methods:
- Activate() -> void
- Deactivate() -> void" \
    "backend/API/Domain/Entities/Airport.cs"

# Prompt 7: Aircraft
show_prompt \
    "Aircraft Entity" \
    "Tạo Domain/Entities/Aircraft.cs

Properties:
- Id (int, PK)
- Model (string, required, max 100)
- RegistrationNumber (string, unique, required, max 50)
- TotalSeats (int, required)
- IsActive (bool, default=true)

Navigation:
- SeatTemplates: ICollection<AircraftSeatTemplate>
- Flights: ICollection<Flight>

Business methods:
- GetTotalSeatsByClass(SeatClass seatClass) -> int
- Activate() -> void
- Deactivate() -> void" \
    "backend/API/Domain/Entities/Aircraft.cs"

# Prompt 8: Route
show_prompt \
    "Route Entity" \
    "Tạo Domain/Entities/Route.cs

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
- IsValid() -> bool" \
    "backend/API/Domain/Entities/Route.cs"

# Prompt 9: SeatClass
show_prompt \
    "SeatClass Entity" \
    "Tạo Domain/Entities/SeatClass.cs

Properties:
- Id (int, PK)
- Code (string, unique, required, max 20)
- Name (string, required, max 100)
- RefundPercent (decimal, 0-100)
- ChangeFee (decimal)
- Priority (int)

Navigation:
- AircraftSeatTemplates: ICollection<AircraftSeatTemplate>
- FlightSeatInventories: ICollection<FlightSeatInventory>
- RefundPolicies: ICollection<RefundPolicy>

Pre-defined: ECO, BUS, FIRST" \
    "backend/API/Domain/Entities/SeatClass.cs"

# Prompt 10: AircraftSeatTemplate
show_prompt \
    "AircraftSeatTemplate Entity" \
    "Tạo Domain/Entities/AircraftSeatTemplate.cs

Unique constraint: (AircraftId, SeatClassId)

Properties:
- Id (int, PK)
- AircraftId (int, FK)
- SeatClassId (int, FK)
- DefaultSeatCount (int, required)
- DefaultBasePrice (decimal, required)

Navigation:
- Aircraft: Aircraft
- SeatClass: SeatClass" \
    "backend/API/Domain/Entities/AircraftSeatTemplate.cs"

# Prompt 11: Flight (Updated)
show_prompt \
    "Flight Entity (Updated)" \
    "Update Domain/Entities/Flight.cs:

Remove: DepartureAirportId, ArrivalAirportId
Add: RouteId (FK)

Properties:
- Id (int, PK)
- FlightNumber (string, unique, required, max 20)
- RouteId (int, FK)
- AircraftId (int, FK)
- DepartureTime (DateTime, UTC)
- ArrivalTime (DateTime, UTC)
- Status (int, default=0)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

Navigation:
- Route: Route
- Aircraft: Aircraft
- SeatInventories: ICollection<FlightSeatInventory>
- OutboundBookings: ICollection<Booking>
- ReturnBookings: ICollection<Booking>" \
    "backend/API/Domain/Entities/Flight.cs"

# Prompt 12: FlightSeatInventory (CRITICAL)
show_prompt \
    "FlightSeatInventory Entity (CRITICAL)" \
    "Tạo Domain/Entities/FlightSeatInventory.cs

Unique constraint: (FlightId, SeatClassId)

Properties:
- Id (int, PK)
- FlightId (int, FK)
- SeatClassId (int, FK)
- TotalSeats (int, required)
- AvailableSeats (int, required)
- HeldSeats (int, default=0)
- SoldSeats (int, default=0)
- BasePrice (decimal, required)
- CurrentPrice (decimal, required)
- Version (int, default=0)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

CRITICAL Invariants:
- AvailableSeats >= 0
- SoldSeats >= 0
- HeldSeats >= 0
- AvailableSeats + SoldSeats + HeldSeats <= TotalSeats

Methods:
- CanBook(int count) -> bool
- ReserveSeats(int count) -> void
- GetOccupancyPercent() -> decimal" \
    "backend/API/Domain/Entities/FlightSeatInventory.cs"

# Prompt 13: Booking (Updated)
show_prompt \
    "Booking Entity (Updated)" \
    "Update Domain/Entities/Booking.cs:

Add properties:
- TripType (int, default=0)
- ReturnFlightId (int, nullable, FK)
- ExpiresAt (DateTime, nullable)

Full Properties:
- Id (int, PK)
- BookingCode (string, unique, required, max 10)
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
- UpdatedAt (DateTime)" \
    "backend/API/Domain/Entities/Booking.cs"

# Prompt 14: BookingPassenger
show_prompt \
    "BookingPassenger Entity" \
    "Tạo Domain/Entities/BookingPassenger.cs

Properties:
- Id (int, PK)
- BookingId (int, FK)
- FullName (string, required, max 255)
- Gender (string, nullable, max 10)
- DateOfBirth (Date, nullable)
- NationalId (string, nullable, max 50)
- PassengerType (int, default=0)
- FlightSeatInventoryId (int, FK)
- FareSnapshot (string, nullable)

Navigation:
- Booking: Booking
- FlightSeatInventory: FlightSeatInventory
- Services: ICollection<BookingService>
- Ticket: Ticket" \
    "backend/API/Domain/Entities/BookingPassenger.cs"

# Prompt 15: BookingService
show_prompt \
    "BookingService Entity" \
    "Tạo Domain/Entities/BookingService.cs

Properties:
- Id (int, PK)
- BookingPassengerId (int, FK)
- ServiceType (string, required, max 50)
- ServiceName (string, required, max 255)
- Price (decimal, required)

Navigation:
- BookingPassenger: BookingPassenger" \
    "backend/API/Domain/Entities/BookingService.cs"

# Prompt 16: Ticket
show_prompt \
    "Ticket Entity" \
    "Tạo Domain/Entities/Ticket.cs

Properties:
- Id (int, PK)
- BookingPassengerId (int, FK)
- TicketNumber (string, unique, required, max 50)
- Status (int, default=0)
- IssuedAt (DateTime, default=now)
- ReplacedByTicketId (int, nullable, FK)

Navigation:
- BookingPassenger: BookingPassenger
- ReplacedByTicket: Ticket

Business methods:
- IsValid() -> bool
- MarkAsUsed() -> void" \
    "backend/API/Domain/Entities/Ticket.cs"

# Prompt 17: Payment (Updated)
show_prompt \
    "Payment Entity (Updated)" \
    "Update Domain/Entities/Payment.cs:

Add properties:
- Provider (string, required, max 50)
- Method (string, required, max 50)
- QrCodeData (string, nullable)
- RawCallbackData (string, nullable)

Full Properties:
- Id (int, PK)
- BookingId (int, FK)
- Provider (string, required)
- Method (string, required)
- Amount (decimal, required)
- Status (int, default=0)
- TransactionRef (string, nullable)
- QrCodeData (string, nullable)
- PaidAt (DateTime, nullable)
- RawCallbackData (string, nullable)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)" \
    "backend/API/Domain/Entities/Payment.cs"

# Prompt 18: RefundPolicy
show_prompt \
    "RefundPolicy Entity" \
    "Tạo Domain/Entities/RefundPolicy.cs

Properties:
- Id (int, PK)
- SeatClassId (int, FK)
- HoursBeforeDeparture (int, required)
- RefundPercent (decimal, 0-100)
- PenaltyFee (decimal, default=0)

Navigation:
- SeatClass: SeatClass

Business methods:
- CalculateRefundAmount(decimal bookingAmount, int hoursBeforeDeparture) -> decimal
- IsRefundable(int hoursBeforeDeparture) -> bool" \
    "backend/API/Domain/Entities/RefundPolicy.cs"

# Prompt 19: RefundRequest
show_prompt \
    "RefundRequest Entity" \
    "Tạo Domain/Entities/RefundRequest.cs

Properties:
- Id (int, PK)
- BookingId (int, FK)
- PaymentId (int, FK)
- RefundAmount (decimal, required)
- Reason (string, nullable, max 500)
- Status (int, default=0)
- ProcessedAt (DateTime, nullable)
- CreatedAt (DateTime)

Navigation:
- Booking: Booking
- Payment: Payment" \
    "backend/API/Domain/Entities/RefundRequest.cs"

# Prompt 20: Promotion
show_prompt \
    "Promotion Entity" \
    "Tạo Domain/Entities/Promotion.cs

Properties:
- Id (int, PK)
- Code (string, unique, required, max 50)
- DiscountType (int, default=0)
- DiscountValue (decimal, required)
- ValidFrom (DateTime, required)
- ValidTo (DateTime, required)
- UsageLimit (int, nullable)
- UsedCount (int, default=0)
- IsActive (bool, default=true)
- CreatedAt (DateTime)

Navigation:
- PromotionUsages: ICollection<PromotionUsage>

Business methods:
- IsValid(DateTime currentDateTime) -> bool
- CalculateDiscount(decimal amount) -> decimal" \
    "backend/API/Domain/Entities/Promotion.cs"

# Prompt 21: PromotionUsage
show_prompt \
    "PromotionUsage Entity" \
    "Tạo Domain/Entities/PromotionUsage.cs

Unique constraint: (PromotionId, BookingId)

Properties:
- Id (int, PK)
- PromotionId (int, FK)
- BookingId (int, FK)
- DiscountAmount (decimal, required)
- UsedAt (DateTime, default=now)

Navigation:
- Promotion: Promotion
- Booking: Booking" \
    "backend/API/Domain/Entities/PromotionUsage.cs"

# Prompt 22: NotificationLog
show_prompt \
    "NotificationLog Entity" \
    "Tạo Domain/Entities/NotificationLog.cs

Properties:
- Id (int, PK)
- UserId (int, FK)
- Email (string, nullable)
- Type (string, required, max 50)
- Title (string, required, max 255)
- Content (string, required)
- Status (int, default=0)
- SentAt (DateTime, nullable)
- CreatedAt (DateTime)

Navigation:
- User: User" \
    "backend/API/Domain/Entities/NotificationLog.cs"

# Prompt 23: AuditLog
show_prompt \
    "AuditLog Entity" \
    "Tạo Domain/Entities/AuditLog.cs

Properties:
- Id (int, PK)
- ActorId (int, nullable, FK)
- Action (string, required, max 100)
- EntityType (string, required, max 100)
- EntityId (int, required)
- BeforeJson (string, nullable)
- AfterJson (string, nullable)
- CreatedAt (DateTime, default=now)

Navigation:
- Actor: User" \
    "backend/API/Domain/Entities/AuditLog.cs"

################################################################################
# PHASE 2: CONFIGURATIONS
################################################################################

print_header "PHASE 2: Entity Configurations (Prompts 24-47)"

show_prompt \
    "All Entity Configurations (1 combined prompt)" \
    "Tạo 23 configuration files trong Infrastructure/Configurations/

Mỗi file implement IEntityTypeConfiguration<T>

Files cần tạo:
1. UserConfiguration.cs - Configure User entity (Email unique, indexes)
2. RoleConfiguration.cs - Configure Role entity
3. UserRoleConfiguration.cs - Configure UserRole composite key
4. EmailVerificationTokenConfiguration.cs
5. PasswordResetTokenConfiguration.cs
6. AirportConfiguration.cs - Configure Airport (Code unique, indexes)
7. AircraftConfiguration.cs - Configure Aircraft
8. RouteConfiguration.cs - Configure Route with relationships
9. SeatClassConfiguration.cs - Configure SeatClass
10. AircraftSeatTemplateConfiguration.cs - Configure with unique (AircraftId, SeatClassId)
11. FlightConfiguration.cs - Configure Flight with Route FK
12. FlightSeatInventoryConfiguration.cs - CRITICAL: Configure with unique, invariants, optimistic concurrency
13. BookingConfiguration.cs - Configure Booking with round-trip
14. BookingPassengerConfiguration.cs
15. BookingServiceConfiguration.cs
16. TicketConfiguration.cs - Configure with unique TicketNumber
17. PaymentConfiguration.cs - Configure Payment with all properties
18. RefundPolicyConfiguration.cs
19. RefundRequestConfiguration.cs
20. PromotionConfiguration.cs - Configure Promotion with unique Code
21. PromotionUsageConfiguration.cs - Configure unique (PromotionId, BookingId)
22. NotificationLogConfiguration.cs
23. AuditLogConfiguration.cs

Mỗi configuration:
- Define PK
- Define properties (maxlength, required, etc.)
- Define FK relationships
- Define unique constraints
- Add indexes (especially on search fields)
- Set table names
- Configure cascade delete behaviors" \
    "backend/API/Infrastructure/Configurations/*.cs"

################################################################################
# PHASE 3: DBCONTEXT
################################################################################

print_header "PHASE 3: DbContext Update (Prompt 48)"

show_prompt \
    "Update FlightBookingDbContext" \
    "Update Infrastructure/Data/FlightBookingDbContext.cs:

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
- Call await base.SaveChangesAsync(cancellationToken)" \
    "backend/API/Infrastructure/Data/FlightBookingDbContext.cs"

################################################################################
# PHASE 4: REPOSITORIES
################################################################################

print_header "PHASE 4: Repository Interfaces (Prompt 49)"

show_prompt \
    "Create Repository Interfaces" \
    "Tạo các interface files trong Application/Interfaces/:

IUserRepository.cs:
- Task<User> GetByEmailAsync(string email)
- Task<User> GetWithRolesAsync(int id)
- Task<User> CreateAsync(User user)
- Task UpdateAsync(User user)

IAirportRepository.cs:
- Task<Airport> GetByCodeAsync(string code)
- Task<IEnumerable<Airport>> GetAllActiveAsync()

IFlightRepository.cs:
- Task<Flight> GetByFlightNumberAsync(string flightNumber)
- Task<IEnumerable<Flight>> SearchAsync(int departureId, int arrivalId, DateTime date)
- Task<Flight> GetWithInventoriesAsync(int id)

IFlightSeatInventoryRepository.cs:
- Task<FlightSeatInventory> GetAsync(int flightId, int seatClassId)
- Task ReserveSeatsAsync(int id, int count, int version)

IBookingRepository.cs:
- Task<Booking> GetByBookingCodeAsync(string code)
- Task<IEnumerable<Booking>> GetByUserAsync(int userId, int page, int pageSize)
- Task<Booking> GetWithPassengersAsync(int id)

IPromotionRepository.cs:
- Task<Promotion> GetByCodeAsync(string code)
- Task<IEnumerable<Promotion>> GetActiveAsync(DateTime currentDateTime)

Tất cả methods async với Task<T> return type." \
    "backend/API/Application/Interfaces/*.cs"

################################################################################
# PHASE 5: MIGRATION
################################################################################

print_header "PHASE 5: Database Migration (Prompt 50)"

echo -e "${CYAN}Final Step: Create Database Migration${NC}\n"

echo -e "${YELLOW}Run these commands in terminal:${NC}\n"
echo -e "${MAGENTA}cd backend${NC}"
echo -e "${MAGENTA}dotnet ef migrations add InitialCreate${NC}"
echo -e "${MAGENTA}dotnet ef database update${NC}\n"

read -p "Press Enter when migration is complete..."
COMPLETED=$((COMPLETED + 1))

################################################################################
# SUMMARY
################################################################################

print_header "✅ All Prompts Completed!"

echo -e "${GREEN}Summary:${NC}"
echo -e "  Phase 1: ✅ 23 Domain Entities"
echo -e "  Phase 2: ✅ 23 Entity Configurations"
echo -e "  Phase 3: ✅ DbContext Updated"
echo -e "  Phase 4: ✅ Repository Interfaces"
echo -e "  Phase 5: ✅ Database Migration"

echo -e "\n${CYAN}Completed Prompts: $COMPLETED/$TOTAL_PROMPTS (100%)${NC}"

echo -e "\n${GREEN}Next Steps:${NC}"
echo -e "1. ✅ Review all generated code"
echo -e "2. ✅ Run: ${YELLOW}dotnet build${NC}"
echo -e "3. ✅ Run: ${YELLOW}dotnet test${NC}"
echo -e "4. ⏳ Create repository implementations"
echo -e "5. ⏳ Create service interfaces & implementations"
echo -e "6. ⏳ Create controllers & endpoints"
echo -e "7. ⏳ Add authentication & authorization"

echo -e "\n${MAGENTA}════════════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}🎉 All 50 Prompts Successfully Generated!${NC}"
echo -e "${MAGENTA}════════════════════════════════════════════════════════════${NC}\n"

################################################################################
# END
################################################################################
