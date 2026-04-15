# 🎯 Business Logic Prompts - Complete Feature Implementation

## 📋 Overview

Các prompts này sinh code cho tất cả chức năng nghiệp vụ:
- ✅ 20+ chức năng chính
- ✅ API Controllers
- ✅ Application Services
- ✅ DTOs
- ✅ Infrastructure (Repositories, External Services)
- ✅ Authentication/Authorization
- ✅ Admin Management
- ✅ Statistics & Analytics
- ✅ Dynamic Pricing

**Tổng cộng: 30+ Prompts**

---

## 🔐 PHASE 1: AUTHENTICATION & USER MANAGEMENT (6 Prompts)

### Prompt 1: User/Auth Service Interface

```
Dựa trên: .github/instructions/backend/application-layer-guide.md

Tạo Application/Interfaces/IAuthService.cs:

Interface: IAuthService
Methods:
- Task<AuthResponse> RegisterAsync(RegisterDto dto) // Đăng ký
  * Validate email format
  * Check email not exists
  * Hash password
  * Create user + email verification token
  * Send verification email
  * Return token and user info

- Task<AuthResponse> LoginAsync(LoginDto dto) // Đăng nhập
  * Find user by email
  * Verify password hash
  * Generate JWT token
  * Return token and user info

- Task<bool> VerifyEmailAsync(string userId, string code) // Verify email
  * Find verification token
  * Check not expired
  * Mark email as verified
  * Delete token

- Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto) // Đổi mật khẩu
  * Get current user
  * Verify old password
  * Hash new password
  * Update user

- Task<bool> RequestPasswordResetAsync(string email) // Yêu cầu reset
  * Find user by email
  * Create password reset token
  * Send reset email
  * Return success

- Task<bool> ResetPasswordAsync(string code, string newPassword) // Reset password
  * Find token
  * Check not expired
  * Hash password
  * Update user
  * Delete token

DTOs:
- RegisterDto: Email, Password, FullName, Phone
- LoginDto: Email, Password
- ChangePasswordDto: OldPassword, NewPassword
- AuthResponse: UserId, Email, FullName, Token, ExpiresAt

Async methods with Task<T> return.
```

### Prompt 2: Auth Service Implementation

```
Tạo Application/Services/AuthService.cs implementing IAuthService:

Constructor inject:
- IUserRepository _userRepository
- IJwtTokenService _jwtTokenService
- IEmailService _emailService
- IPasswordHasher _passwordHasher
- ILogger<AuthService> _logger
- IEmailVerificationTokenRepository _emailTokenRepository
- IPasswordResetTokenRepository _passwordTokenRepository

RegisterAsync method:
1. Validate email format using regex
2. Check user with email not exists -> throw ValidationException
3. Hash password using IPasswordHasher
4. Create new User with Status=Active
5. Save to repository
6. Create EmailVerificationToken (24h expiration)
7. Send verification email via IEmailService
8. Generate JWT token
9. Log registration
10. Return AuthResponse

LoginAsync method:
1. Find user by email -> throw NotFoundException if not found
2. Verify password using hash compare -> throw ValidationException if wrong
3. Check user status is Active
4. Check email is verified
5. Generate JWT token
6. Log login
7. Update LastLoginAt timestamp
8. Return AuthResponse

VerifyEmailAsync method:
1. Find token by code -> throw NotFoundException
2. Check token not expired
3. Find user
4. Mark user email as verified
5. Delete token
6. Log verification
7. Return true

ChangePasswordAsync method:
1. Get user by userId -> throw NotFoundException
2. Verify old password matches
3. Validate new password strength (min 8 chars, uppercase, digit)
4. Hash new password
5. Update user.PasswordHash
6. Save to repository
7. Log password change
8. Return true

RequestPasswordResetAsync method:
1. Find user by email
2. Create PasswordResetToken (1h expiration)
3. Send reset email with link containing code
4. Log request
5. Return true (don't reveal if email exists)

ResetPasswordAsync method:
1. Find token by code -> throw NotFoundException
2. Check token not expired
3. Find user
4. Validate new password strength
5. Hash password
6. Update user.PasswordHash
7. Delete token
8. Log reset
9. Return true

Validation errors return meaningful messages.
All operations async.
```

### Prompt 3: JWT Token Service

```
Tạo Application/Interfaces/IJwtTokenService.cs:

Interface: IJwtTokenService
Methods:
- string GenerateToken(User user) // Generate JWT
  * Include claims: userId, email, roles
  * Expiration: 24 hours
  * Use RSA or HS256

- ClaimsPrincipal ValidateToken(string token) // Validate JWT
  * Validate signature
  * Check expiration
  * Return claims if valid
  * Throw exception if invalid

Tạo Application/Services/JwtTokenService.cs:

Constructor inject:
- IConfiguration _config
- ILogger<JwtTokenService> _logger

GenerateToken implementation:
1. Get secret key from configuration
2. Create claims list:
   - sub: user.Id
   - email: user.Email
   - name: user.FullName
   - roles: user.Roles (comma-separated)
3. Create signing credentials with HS256
4. Create token descriptor with:
   - Subject: claims
   - Expires: DateTime.UtcNow.AddHours(24)
   - SigningCredentials
5. Create and write token
6. Return token string
7. Log token generation

ValidateToken implementation:
1. Get secret key from configuration
2. Create validation parameters:
   - ValidateIssuerSigningKey: true
   - ValidateIssuer: false (optional)
   - ValidateAudience: false (optional)
3. Try to validate token
4. Return ClaimsPrincipal if valid
5. Return null or throw exception if invalid

Configuration example:
- Jwt:Key: <your-secret-key>
- Jwt:ExpirationHours: 24
```

### Prompt 4: Email Service Interface & Implementation

```
Tạo Application/Interfaces/IEmailService.cs:

Interface: IEmailService
Methods:
- Task SendEmailAsync(string email, string subject, string htmlContent)
- Task SendVerificationEmailAsync(string email, string verificationCode)
- Task SendPasswordResetEmailAsync(string email, string resetCode)
- Task SendBookingConfirmationAsync(string email, Booking booking)
- Task SendTicketEmailAsync(string email, Booking booking, List<Ticket> tickets)
- Task SendBookingCancellationAsync(string email, Booking booking)
- Task SendNotificationAsync(string email, string title, string content)

Tạo Infrastructure/Services/SmtpEmailService.cs implementing IEmailService:

Constructor inject:
- IConfiguration _config
- ILogger<SmtpEmailService> _logger

Methods:
- SendEmailAsync:
  1. Validate email format
  2. Create SmtpClient with:
     - Host: configuration[Smtp:Host]
     - Port: configuration[Smtp:Port]
     - EnableSsl: true
  3. Create MailMessage
  4. Set From, To, Subject, Body (IsBodyHtml=true)
  5. Send email
  6. Log success
  7. Handle exceptions and log errors

- SendVerificationEmailAsync:
  1. Create HTML template with verification link
  2. Link: {BaseUrl}/auth/verify-email?code={code}
  3. Call SendEmailAsync

- SendPasswordResetEmailAsync:
  1. Create HTML template with reset link
  2. Link: {BaseUrl}/auth/reset-password?code={code}
  3. Call SendEmailAsync

- SendBookingConfirmationAsync:
  1. Create HTML template with booking details:
     - Booking code
     - Flight info (departure, arrival, time)
     - Total price
     - Next steps (payment, check-in)
  2. Call SendEmailAsync

- SendTicketEmailAsync:
  1. Create PDF or HTML with tickets
  2. Include QR codes if applicable
  3. Attach file or embed HTML
  4. Send email with attachment

- SendBookingCancellationAsync:
  1. Create HTML template with:
     - Booking code
     - Cancellation details
     - Refund information
     - Timeline

- SendNotificationAsync:
  1. Generic notification email
  2. Use template system

Use HTML templates for all emails.
Implement retry logic for failed sends.
Log all operations.
```

### Prompt 5: User Management Controller

```
Tạo Controllers/UsersController.cs:

[ApiController]
[Route("api/v1/[controller]")]
public class UsersController : ControllerBase

Constructor inject:
- IAuthService _authService
- IUserService _userService
- ILogger<UsersController> _logger
- IMapper _mapper

Endpoints:

1. [HttpPost("register")]
   RegisterAsync(RegisterDto dto)
   - Call _authService.RegisterAsync(dto)
   - Return 201 Created
   - Include token and user info

2. [HttpPost("login")]
   LoginAsync(LoginDto dto)
   - Call _authService.LoginAsync(dto)
   - Return 200 with token
   - Set HttpOnly cookie if needed

3. [HttpPost("verify-email")]
   VerifyEmailAsync([FromQuery] string code)
   - Call _authService.VerifyEmailAsync(userId, code)
   - Return 200 if verified
   - Return 400 if code expired

4. [Authorize]
   [HttpPost("change-password")]
   ChangePasswordAsync(ChangePasswordDto dto)
   - Get userId from claims
   - Call _authService.ChangePasswordAsync(userId, dto)
   - Return 200
   - Handle validation errors

5. [HttpPost("forgot-password")]
   ForgotPasswordAsync([FromBody] string email)
   - Call _authService.RequestPasswordResetAsync(email)
   - Return 200 (always, don't reveal if email exists)

6. [HttpPost("reset-password")]
   ResetPasswordAsync(ResetPasswordDto dto)
   - Call _authService.ResetPasswordAsync(dto.Code, dto.NewPassword)
   - Return 200

7. [Authorize]
   [HttpGet("{id}")]
   GetUserAsync(int id)
   - Check authorization (own profile or admin)
   - Get user details
   - Return 200

8. [Authorize]
   [HttpPut("{id}")]
   UpdateProfileAsync(int id, UpdateUserDto dto)
   - Check authorization
   - Update user info
   - Return 200

All endpoints with error handling returning appropriate HTTP codes.
Add logging for all operations.
```

### Prompt 6: User Repository Interface & Implementation

```
Tạo Application/Interfaces/IUserRepository.cs:

Interface: IUserRepository
Methods:
- Task<User> GetByIdAsync(int id)
- Task<User> GetByEmailAsync(string email)
- Task<User> GetWithRolesAsync(int id)
- Task<User> CreateAsync(User user)
- Task<User> UpdateAsync(User user)
- Task<bool> ExistsAsync(int id)
- Task<bool> EmailExistsAsync(string email)
- Task<List<User>> GetAllAsync(int page, int pageSize)
- Task<User> GetByGoogleIdAsync(string googleId) // OAuth

Tạo Infrastructure/Repositories/UserRepository.cs:

Constructor inject:
- FlightBookingDbContext _context
- ILogger<UserRepository> _logger

Implement all methods:
- GetByIdAsync: Find by Id, throw NotFoundException if not found
- GetByEmailAsync: Find by Email, use case-insensitive search
- GetWithRolesAsync: Include UserRole navigation
- CreateAsync: Add user to DbContext, SaveChangesAsync
- UpdateAsync: Update existing user, SaveChangesAsync
- ExistsAsync: Check if user exists
- EmailExistsAsync: Check if email already used
- GetAllAsync: Get paginated list with skip/take
- GetByGoogleIdAsync: Find by GoogleId for OAuth

Add indexes:
- HasIndex(u => u.Email).IsUnique()
- HasIndex(u => u.GoogleId).IsUnique()

Use async queries with .ToListAsync(), .FirstOrDefaultAsync()
Log all database operations
Handle DbUpdateException and log
```

---

## ✈️ PHASE 2: FLIGHT SEARCH & BOOKING (8 Prompts)

### Prompt 7: Flight Search Service

```
Tạo Application/Interfaces/IFlightService.cs:

Methods:
- Task<List<FlightSearchResponse>> SearchAsync(FlightSearchDto criteria)
  * Search by departure airport, arrival airport, departure date
  * Optional: arrival date (for round-trip)
  * Optional: passenger count
  * Return available flights with pricing
  * Apply any active promotions

- Task<FlightDetailResponse> GetFlightAsync(int flightId)
  * Get flight details with:
    - Route info (airports, distance, duration)
    - Aircraft info
    - Seat inventory per class
    - Current pricing
    - Available seats per class

- Task<int> GetAvailableSeatsAsync(int flightId, int seatClassId)
  * Return available seat count
  * Check held seats timeout (15 min)
  * Release expired holds

Search DTO:
- DepartureAirportId (int)
- ArrivalAirportId (int)
- DepartureDate (DateTime)
- ReturnDate (DateTime, nullable)
- PassengerCount (int)
- SeatPreference (enum: ECO, BUS, FIRST, nullable)

SearchResponse:
- FlightId
- FlightNumber
- RouteInfo (departure, arrival, duration)
- DepartureTime
- ArrivalTime
- AvailableSeatsByClass (dictionary)
- PricesByClass (dictionary)
- AirlineInfo
- AircraftInfo

Tạo Application/Services/FlightService.cs:

Constructor inject:
- IFlightRepository _flightRepository
- IFlightSeatInventoryRepository _seatInventoryRepository
- IPromotionService _promotionService
- ICacheService _cacheService
- ILogger<FlightService> _logger

SearchAsync implementation:
1. Validate input dates (departure <= arrival)
2. Check cache for search results
3. Query flights matching:
   - Route matching departure/arrival
   - Departure date matching (can add +- days)
   - Status = Active
4. For each flight:
   a. Get seat inventory for all classes
   b. Get current pricing (including dynamic pricing)
   c. Apply promotions if applicable
   d. Filter by seat availability
5. Sort by price (ascending)
6. Cache results (30 min TTL)
7. Return list of FlightSearchResponse

GetFlightAsync implementation:
1. Check cache first
2. Get flight with Route, Aircraft, SeatInventories
3. Build detailed response
4. Cache (1h TTL)
5. Return response

GetAvailableSeatsAsync implementation:
1. Get FlightSeatInventory by (flightId, seatClassId)
2. Check held seats (ExpiresAt timestamp)
3. Release expired holds (move to available)
4. Return AvailableSeats count

Implement held seat expiration:
- When booking, hold seats with 15-min timeout
- In this method, release expired holds before returning count
```

### Prompt 8: Booking Service - Search & Hold

```
Tạo Application/Interfaces/IBookingService.cs:

Methods:
- Task<BookingResponse> CreateBookingAsync(CreateBookingDto dto)
  * Create booking with multiple passengers
  * Hold seats for 15 minutes
  * Generate booking code
  * Return booking details

- Task<bool> CancelBookingAsync(int bookingId, string reason)
  * Cancel confirmed booking (Confirmed status)
  * Check 24h before departure rule
  * Release held/sold seats
  * Initiate refund request
  * Update booking status to Cancelled

- Task<bool> UpdateBookingAsync(int bookingId, UpdateBookingDto dto)
  * Update passenger info
  * Cannot change seats after confirmation
  * Only allowed before payment

- Task<Booking> GetBookingAsync(int bookingId)
  * Get full booking with passengers and flight details

Tạo Application/Services/BookingService.cs:

Constructor inject:
- IBookingRepository _bookingRepository
- IBookingPassengerRepository _passengerRepository
- IFlightSeatInventoryRepository _seatInventoryRepository
- ITicketRepository _ticketRepository
- IRefundPolicyRepository _refundPolicyRepository
- ILogger<BookingService> _logger
- IUnitOfWork _unitOfWork // For transactions

CreateBookingAsync implementation:
1. Validate flight exists and is Active
2. Validate passenger count matches booking
3. Validate seats available (check hold + sold)
4. START TRANSACTION:
   a. Create Booking entity:
      - Generate unique BookingCode (6 chars)
      - Status = Pending (waiting for payment)
      - ExpiresAt = DateTime.UtcNow.AddMinutes(15)
   b. For each passenger:
      - Create BookingPassenger
      - Assign to FlightSeatInventoryId (seat class)
   c. Reserve seats:
      - Update FlightSeatInventory:
        * Increment HeldSeats
        * Decrement AvailableSeats
      - Use optimistic concurrency (Version field)
   d. Save all to repository
5. COMMIT TRANSACTION
6. Schedule seat release task (15 min timeout)
7. Return BookingResponse

CancelBookingAsync implementation:
1. Get booking with passengers
2. Check status is Confirmed
3. Calculate hours until departure
4. Check >= 24 hours rule
5. START TRANSACTION:
   a. Update booking status to Cancelled
   b. Update all passengers to cancelled
   c. Release seats:
      - Update FlightSeatInventory: decrement SoldSeats
      - Increment AvailableSeats
   d. Create RefundRequest:
      - Get RefundPolicy based on hours & seat class
      - Calculate refund amount
      - Status = Pending
   e. Create Notification log for email
6. COMMIT TRANSACTION
7. Return true

UpdateBookingAsync implementation:
1. Check booking status is Pending (before payment)
2. Update BookingPassenger info (name, DOB, ID)
3. Save changes
4. Return true

GetBookingAsync implementation:
1. Get booking with:
   - Passengers
   - Flight with Route & Aircraft
   - Payment
   - Tickets
2. Return populated Booking entity

Use transactions for booking creation & cancellation.
Implement optimistic concurrency on seat inventory.
Release held seats after 15 minutes (background task).
```

### Prompt 9: Seat Holding & Release

```
Tạo Infrastructure/Services/SeatHoldingService.cs:

Purpose: Manage temporary seat holds during booking

Constructor inject:
- IFlightSeatInventoryRepository _seatInventoryRepository
- IBookingRepository _bookingRepository
- ILogger<SeatHoldingService> _logger
- IBackgroundJobClient _backgroundJob (Hangfire)

Methods:

HoldSeatsAsync(int flightId, int seatClassId, int count, int holdDurationMinutes):
1. Get FlightSeatInventory
2. Check available seats >= count
3. START TRANSACTION:
   a. Update FlightSeatInventory:
      - HeldSeats += count
      - AvailableSeats -= count
      - Use optimistic concurrency (Version)
   b. Save changes
4. Schedule release task via Hangfire:
   - FireAndForgetJob to ReleaseSeatsAsync
   - Scheduled for holdDurationMinutes
5. Return true

ReleaseHeldSeatsAsync(int flightId, int seatClassId, int count):
1. Get FlightSeatInventory
2. Check HeldSeats >= count
3. UPDATE:
   - HeldSeats -= count
   - AvailableSeats += count
4. Save changes
5. Log release
6. Return true

ReleaseExpiredHolds():
1. Get all pending bookings where ExpiresAt < Now
2. For each expired booking:
   a. Get booking passengers (count)
   b. Get seat class from FlightSeatInventory
   c. Release seats
   d. Cancel booking
   e. Log expiration
3. Run as scheduled job (every 5 min)

Tạo Infrastructure/BackgroundJobs/SeatHoldingJob.cs:

Public methods for Hangfire:
- ReleaseSeatsDelayed(int flightId, int seatClassId, int count, int delayMinutes)
  * Scheduled job via Hangfire
  * Called after booking created
  * Runs after 15 minutes
  * Releases seats if booking still pending
  * Check booking not yet paid

- CleanupExpiredBookings()
  * Run every 5 minutes
  * Find all Pending bookings > 15 min old
  * Release their seats
  * Cancel bookings
  * Send cancellation notification

Hangfire Configuration in Program.cs:
- services.AddHangfire(config =>
    config.UseSqlServerStorage(connectionString))
- services.AddHangfireServer()
- RecurringJob.AddOrUpdate<SeatHoldingJob>(
    "cleanup-expired-bookings",
    job => job.CleanupExpiredBookings(),
    Cron.MinuteInterval(5))

Use optimistic concurrency for SeatInventory updates.
Handle race conditions with proper locking.
Log all hold/release operations.
```

### Prompt 10: Seat Inventory & Dynamic Pricing

```
Tạo Application/Interfaces/IPricingService.cs:

Methods:
- Task<decimal> CalculateCurrentPriceAsync(int flightSeatInventoryId)
  * Calculate dynamic price based on:
    1. Base price
    2. Occupancy percentage
    3. Days until departure
    4. Demand level
    5. Promotions

- Task UpdateDynamicPricesAsync()
  * Run daily/hourly
  * Update all flights' pricing
  * Consider:
    - Available seats ratio
    - Days to departure
    - Day of week (premium)
    - Season/holiday

Tạo Application/Services/PricingService.cs:

Constructor inject:
- IFlightSeatInventoryRepository _seatInventoryRepository
- IPromotionService _promotionService
- ILogger<PricingService> _logger

CalculateCurrentPriceAsync implementation:
1. Get FlightSeatInventory with Flight
2. Get BasePrice
3. Calculate occupancy factor:
   - occupancy = (SoldSeats + HeldSeats) / TotalSeats
   - factor: 0.5x at 0%, 1.5x at 90%+
4. Calculate time-based factor:
   - daysUntilDeparture = (Flight.DepartureTime - Now).Days
   - if <= 3 days: 1.3x (last-minute premium)
   - if <= 7 days: 1.2x
   - if > 14 days: 0.8x (early booking discount)
5. Calculate demand factor:
   - Query recent bookings for this route
   - If high demand: 1.15x
6. Formula:
   - currentPrice = BasePrice × occupancyFactor × timeFactorFactor × demandFactor
7. Apply caps:
   - Min: BasePrice × 0.5
   - Max: BasePrice × 2.0
8. Return rounded price

UpdateDynamicPricesAsync implementation:
1. Get all active FlightSeatInventories
2. For each inventory:
   a. Call CalculateCurrentPriceAsync
   b. Update CurrentPrice
   c. Log change if significant (>10%)
3. Save all changes
4. Cache results

Pricing Factors:
- Base: $100
- Occupancy: 50% to 150% multiplier
- Time: 50% to 130% multiplier
- Demand: 100% to 115% multiplier
- Result: $25 to $300

Example:
- BasePrice: 100
- Occupancy 70%: 1.1x
- Days to departure: 5: 1.2x
- Demand: normal: 1.0x
- Final: 100 × 1.1 × 1.2 × 1.0 = 132

Schedule UpdateDynamicPricesAsync:
- Run every 6 hours
- Or triggered by manual admin action
```

### Prompt 11: Booking Search & History

```
Tạo Controllers/BookingsController.cs:

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BookingsController : ControllerBase

Constructor inject:
- IBookingService _bookingService
- IFlightService _flightService
- IPaymentService _paymentService
- ITicketService _ticketService
- ILogger<BookingsController> _logger
- IMapper _mapper

Endpoints:

1. [HttpPost("search")]
   SearchFlightsAsync(FlightSearchDto dto)
   - Call _flightService.SearchAsync(dto)
   - Return 200 with list of available flights

2. [HttpPost]
   CreateBookingAsync(CreateBookingDto dto)
   - Call _bookingService.CreateBookingAsync(dto)
   - Return 201 Created
   - Include booking code and details

3. [HttpGet("{bookingId}")]
   GetBookingAsync(int bookingId)
   - Check authorization (owner or admin)
   - Call _bookingService.GetBookingAsync(bookingId)
   - Return 200

4. [HttpGet("user/{userId}")]
   GetUserBookingsAsync(int userId, [FromQuery] int page = 1)
   - Check authorization
   - Get user's bookings (paginated)
   - Return 200

5. [HttpPut("{bookingId}")]
   UpdateBookingAsync(int bookingId, UpdateBookingDto dto)
   - Check authorization
   - Call _bookingService.UpdateBookingAsync(bookingId, dto)
   - Return 200

6. [HttpDelete("{bookingId}")]
   CancelBookingAsync(int bookingId, [FromBody] string reason)
   - Check authorization
   - Call _bookingService.CancelBookingAsync(bookingId, reason)
   - Return 200 if success
   - Return 409 if cannot cancel (< 24h, already cancelled)

All endpoints with error handling.
Add logging.
```

---

## 💳 PHASE 3: PAYMENT & TICKETING (6 Prompts)

### Prompt 12: Payment Service Interface

```
Tạo Application/Interfaces/IPaymentService.cs:

Methods:
- Task<PaymentResponse> InitiatePaymentAsync(int bookingId, PaymentMethodDto method)
  * Create Payment entity
  * Generate payment link/QR code
  * Return payment details for client

- Task<bool> ProcessPaymentAsync(int paymentId, PaymentCallbackDto callback)
  * Verify payment callback from provider
  * Update payment status
  * If successful:
    - Mark booking as Confirmed
    - Create tickets
    - Send confirmation email

- Task<PaymentStatus> GetPaymentStatusAsync(int paymentId)
  * Check payment status

- Task<List<PaymentResponse>> GetPaymentHistoryAsync(int bookingId)
  * Get all payment attempts for booking

Payment Methods:
- CARD (Credit/Debit)
- BANK (Bank Transfer)
- WALLET (E-wallet)
- MOMO (Vietnam)
- VNPAY (Vietnam)
- PAYPAL

Tạo Application/Services/PaymentService.cs:

Constructor inject:
- IPaymentRepository _paymentRepository
- IPaymentProviderFactory _paymentProviderFactory
- IBookingService _bookingService
- ITicketService _ticketService
- IEmailService _emailService
- ILogger<PaymentService> _logger
- IUnitOfWork _unitOfWork

InitiatePaymentAsync implementation:
1. Get booking with details
2. Create Payment entity:
   - Status = Pending
   - Amount = booking.FinalAmount
   - Provider, Method from dto
3. Get provider instance from factory
4. Call provider.GeneratePaymentLinkAsync():
   - Passing: amount, bookingId, email, returnUrl
   - Provider generates unique transaction ID
5. Update payment with:
   - TransactionRef = provider's ID
   - QrCodeData (if applicable)
6. Save payment
7. Return PaymentResponse with:
   - PaymentLink
   - QRCode
   - ExpiresAt (1h)
8. Log payment initiation

ProcessPaymentAsync implementation:
1. Get payment by paymentId
2. Get provider instance
3. Verify callback signature using provider
4. If callback invalid: throw SecurityException
5. If payment status success:
   a. START TRANSACTION:
      - Update Payment: Status = Completed, PaidAt = Now
      - Update Booking: Status = Confirmed
      - Call ITicketService.CreateTicketsAsync(bookingId)
      - Save changes
   b. COMMIT TRANSACTION
   c. Send confirmation email
   d. Log success
   e. Return true
6. If payment status failed:
   - Update Payment: Status = Failed
   - Booking stays Pending (can retry)
   - Log failure
   - Return false
7. Handle provider errors gracefully

GetPaymentStatusAsync implementation:
1. Get payment
2. If old (> 1 day) and still Pending:
   - Call provider to verify status
   - Update payment if needed
3. Return payment status

GetPaymentHistoryAsync implementation:
1. Get all payments for booking (ordered by date)
2. Map to PaymentResponse
3. Return list

Implement payment provider factory pattern.
Support multiple providers (Stripe, PayPal, Momo, VNPay).
Verify callback signatures for security.
```

### Prompt 13: Payment Provider Implementations

```
Tạo Application/Interfaces/IPaymentProvider.cs:

Interface for payment providers:
- Task<PaymentProviderResponse> GeneratePaymentLinkAsync(PaymentRequest request)
  * Returns: Link, QRCode, TransactionId, ExpiresAt

- Task<bool> VerifyCallbackSignatureAsync(string signature, string data)
  * Verify callback is authentic

- Task<PaymentStatus> GetPaymentStatusAsync(string transactionId)
  * Check payment status with provider

Tạo Infrastructure/ExternalServices/Payment/MomoPaymentProvider.cs:

Constructor inject:
- IConfiguration _config
- IHttpClientFactory _httpClientFactory
- ILogger<MomoPaymentProvider> _logger

Configuration in appsettings.json:
{
  "Payment": {
    "Momo": {
      "PartnerCode": "MOMO_PARTNER_CODE",
      "SecretKey": "SECRET_KEY",
      "ApiUrl": "https://test-payment.momo.vn/gw_payment/payment",
      "NotifyUrl": "https://yourdomain/api/payments/momo-callback"
    }
  }
}

GeneratePaymentLinkAsync implementation:
1. Prepare request:
   - partnerCode
   - orderId (from bookingId)
   - amount
   - orderInfo: "Flight booking #{bookingId}"
   - returnUrl, notifyUrl
2. Generate requestId and signature
3. Call Momo API
4. Parse response
5. Return PaymentProviderResponse with:
   - PaymentLink
   - QRCode
   - TransactionId (requestId)
   - ExpiresAt

VerifyCallbackSignatureAsync implementation:
1. Extract signature from request
2. Generate signature using same data + secret key
3. Compare signatures using secure string comparison
4. Return true if match

GetPaymentStatusAsync implementation:
1. Call Momo API to query status
2. Parse response
3. Return payment status

Tạo Infrastructure/ExternalServices/Payment/StripePaymentProvider.cs:

Similar structure for Stripe:
- API key from configuration
- Create payment intent
- Return client secret & link
- Verify webhook signature
- Query payment status

Tạo Infrastructure/ExternalServices/Payment/VNPayPaymentProvider.cs:

For VNPay:
- Similar pattern
- Support Vietnamese payment gateway
- Generate secure hash
- Verify callback

Tạo Infrastructure/ExternalServices/Payment/PaymentProviderFactory.cs:

Factory class to create provider instances:
- Get provider type from payment method
- Return appropriate provider
- Support: Momo, VNPay, Stripe, PayPal

All providers implement IPaymentProvider interface.
Each handles provider-specific API calls.
Secure signature verification critical.
Log all provider interactions.
```

### Prompt 14: Ticket Service

```
Tạo Application/Interfaces/ITicketService.cs:

Methods:
- Task<List<TicketResponse>> CreateTicketsAsync(int bookingId)
  * Generate ticket for each passenger
  * Return ticket numbers

- Task<TicketResponse> GetTicketAsync(string ticketNumber)
  * Get ticket details

- Task<bool> ChangeTicketAsync(string ticketNumber, ChangeTicketDto dto)
  * Change flight/date for ticket
  * Apply change fee

- Task<bool> DownloadTicketAsync(string ticketNumber, string format = "pdf")
  * Generate ticket document (PDF or HTML)

Tạo Application/Services/TicketService.cs:

Constructor inject:
- ITicketRepository _ticketRepository
- IBookingPassengerRepository _passengerRepository
- IFlightRepository _flightRepository
- IEmailService _emailService
- IPdfGenerator _pdfGenerator
- ILogger<TicketService> _logger

CreateTicketsAsync implementation:
1. Get booking with passengers
2. For each passenger:
   a. Generate unique ticket number (IATA format):
      - Airline code (2 chars) + Booking code + Seq number
      - Example: MH-ABC123-001
   b. Create Ticket entity:
      - Status = Issued
      - IssuedAt = Now
   c. Save to repository
3. Send ticket emails to passenger
4. Log ticket creation
5. Return list of created tickets

GetTicketAsync implementation:
1. Get ticket by number
2. Get associated booking and passenger
3. Build TicketResponse with:
   - Ticket number
   - Passenger name
   - Flight details
   - Booking reference
   - Status

ChangeTicketAsync implementation:
1. Get ticket and booking
2. Check booking not past departure date
3. Check new flight has availability
4. Get SeatClass from original booking
5. Calculate change fee from SeatClass
6. Hold seats on new flight
7. Release seats from old flight
8. Create new ticket (replace old)
9. Log ticket change
10. Send change confirmation email

Ticket numbering format:
- IATA: 3-digit airline + 3-digit flight + sequencial
- Example: 006ABC000123

Email ticket with QR code if applicable.
Support PDF generation with flight details.
```

### Prompt 15: Refund Service

```
Tạo Application/Interfaces/IRefundService.cs:

Methods:
- Task<RefundResponse> RequestRefundAsync(int bookingId, string reason)
  * Create refund request
  * Calculate refund amount based on policy

- Task<bool> ProcessRefundAsync(int refundRequestId)
  * Process refund to customer
  * Call payment provider
  * Update refund status

- Task<RefundResponse> GetRefundStatusAsync(int refundRequestId)
  * Get refund details and status

Tạo Application/Services/RefundService.cs:

Constructor inject:
- IRefundRequestRepository _refundRequestRepository
- IPaymentRepository _paymentRepository
- IRefundPolicyRepository _refundPolicyRepository
- IPaymentProviderFactory _paymentProviderFactory
- IEmailService _emailService
- ILogger<RefundService> _logger

RequestRefundAsync implementation:
1. Get booking with payment
2. Check booking is Confirmed (not Pending or Cancelled)
3. Calculate hours until departure
4. Get RefundPolicy for (SeatClass, HoursBeforeDeparture)
5. Calculate refund amount:
   - baseAmount = Booking.FinalAmount
   - refundPercent = RefundPolicy.RefundPercent
   - penaltyFee = RefundPolicy.PenaltyFee
   - refundAmount = (baseAmount × refundPercent) - penaltyFee
   - refundAmount = max(0, refundAmount)
6. Create RefundRequest:
   - BookingId, PaymentId
   - RefundAmount = calculated
   - Reason = provided
   - Status = Pending (waiting for manual approval)
7. Send admin notification
8. Return RefundResponse

ProcessRefundAsync implementation:
1. Get refund request
2. Check status is Pending
3. Get payment details
4. Get payment provider
5. Initiate refund to provider:
   - Call provider.RefundAsync(transactionId, amount)
   - Handle provider response
6. If refund successful:
   - Update RefundRequest: Status = Processed
   - Update Payment: Status = Refunded
   - Update Booking: Status = Cancelled
   - Send refund confirmation email
   - Log success
7. If refund failed:
   - Update RefundRequest: Status = Rejected
   - Log failure
   - Send retry notification

GetRefundStatusAsync implementation:
1. Get refund request with related entities
2. Build RefundResponse
3. Return details

Refund rules:
- 72+ hours: 100% refund
- 48-72 hours: 80% refund
- 24-48 hours: 50% refund
- < 24 hours: 0% refund (no refund)

Log all refund operations.
Send email notifications at each step.
```

---

## 📧 PHASE 4: NOTIFICATIONS (1 Prompt)

### Prompt 16: Notification Service

```
Tạo Application/Interfaces/INotificationService.cs:

Methods:
- Task SendBookingConfirmationAsync(Booking booking)
- Task SendPaymentConfirmationAsync(Payment payment)
- Task SendTicketsAsync(Booking booking, List<Ticket> tickets)
- Task SendCancellationAsync(Booking booking, RefundRequest refund)
- Task SendChangeConfirmationAsync(Booking booking, Ticket oldTicket, Ticket newTicket)
- Task SendFlightDelayNotificationAsync(Flight flight)
- Task SendFlightReminderAsync(Booking booking)
  * Send 24h before departure reminder

Tạo Application/Services/NotificationService.cs:

Constructor inject:
- IEmailService _emailService
- INotificationLogRepository _notificationLogRepository
- ILogger<NotificationService> _logger
- IConfiguration _config

Each method:
1. Get template for notification type
2. Prepare data for template
3. Send email via IEmailService
4. Log notification sent
5. Handle failures

Email templates (store in wwwroot/templates/emails/):
- booking-confirmation.html
- payment-confirmation.html
- tickets.html
- cancellation.html
- change-confirmation.html
- flight-delay.html
- flight-reminder.html

Templates include:
- Booking code
- Flight details
- Passenger names
- Important information
- Next steps
- Contact info
- QR codes where applicable

Schedule reminder emails:
- 24h before departure: SendFlightReminderAsync
- Use Hangfire background jobs

All notifications logged to NotificationLog table.
Support multiple notification types.
HTML email templates for professional appearance.
```

---

## 👨‍💼 PHASE 5: ADMIN MANAGEMENT (8 Prompts)

### Prompt 17: Admin Flight Management

```
Tạo Controllers/Admin/FlightsAdminController.cs:

[ApiController]
[Route("api/v1/admin/flights")]
[Authorize(Roles = "Admin")]
public class FlightsAdminController : ControllerBase

Constructor inject:
- IFlightService _flightService
- IFlightAdminService _adminService
- ILogger<FlightsAdminController> _logger

Endpoints:

1. [HttpGet]
   GetAllFlightsAsync([FromQuery] int page = 1)
   - Get paginated list of flights
   - Include route, aircraft, status

2. [HttpGet("{id}")]
   GetFlightAsync(int id)
   - Get flight details

3. [HttpPost]
   CreateFlightAsync(CreateFlightDto dto)
   - Create new flight
   - Validate: RouteId, AircraftId valid
   - Generate FlightNumber if needed
   - Return 201

4. [HttpPut("{id}")]
   UpdateFlightAsync(int id, UpdateFlightDto dto)
   - Update flight details
   - Cannot change date if bookings exist

5. [HttpDelete("{id}")]
   CancelFlightAsync(int id, [FromBody] string reason)
   - Cancel flight
   - Notify all passengers
   - Initiate refunds

6. [HttpPost("{id}/mark-delayed")]
   MarkFlightDelayedAsync(int id, [FromBody] string reason)
   - Mark flight as Delayed
   - Send notifications

7. [HttpPost("{id}/sync-pricing")]
   UpdateFlightPricingAsync(int id)
   - Trigger dynamic pricing update
   - Recalculate prices for all seat classes

Tạo Application/Interfaces/IFlightAdminService.cs:

Methods:
- Task<Flight> CreateFlightAsync(CreateFlightDto dto)
  * Create new flight
  * Generate flight number
  * Create seat inventories for all classes

- Task<Flight> UpdateFlightAsync(int id, UpdateFlightDto dto)
  * Update flight information
  * Check no bookings before date change

- Task CancelFlightAsync(int id, string reason)
  * Cancel flight
  * Refund all bookings
  * Notify passengers

- Task MarkFlightDelayedAsync(int id, string reason)
  * Update status to Delayed
  * Send delay notifications
  * Log reason

CreateFlightDto:
- FlightNumber (string, auto-generate if not provided)
- RouteId (int)
- AircraftId (int)
- DepartureTime (DateTime)
- ArrivalTime (DateTime)

Tạo Application/Services/FlightAdminService.cs:

Implement all methods with transactions.
When creating flight:
- Generate unique flight number
- Create FlightSeatInventory for each SeatClass:
  - TotalSeats from AircraftSeatTemplate
  - BasePrice from AircraftSeatTemplate
  - CurrentPrice = BasePrice
  - AvailableSeats = TotalSeats
- Save flight and inventories

When cancelling flight:
- Get all confirmed bookings
- For each booking:
  - Create RefundRequest with 100% refund
  - Update booking to Cancelled
  - Send cancellation email
- Update flight status

When marking delayed:
- Update status to Delayed
- Send delay notification to all passengers
- Store delay reason
```

### Prompt 18: Admin Airport & Aircraft Management

```
Tạo Controllers/Admin/AirportsAdminController.cs:

[ApiController]
[Route("api/v1/admin/airports")]
[Authorize(Roles = "Admin")]
public class AirportsAdminController : ControllerBase

Endpoints:
1. [HttpGet] GetAllAirportsAsync()
2. [HttpPost] CreateAirportAsync(CreateAirportDto dto)
   - Code (unique), Name, City, Province
3. [HttpPut("{id}")] UpdateAirportAsync(int id, UpdateAirportDto dto)
4. [HttpDelete("{id}")] DeleteAirportAsync(int id)
   - Can only delete if no flights use it
5. [HttpPost("{id}/activate")] ActivateAirportAsync(int id)
6. [HttpPost("{id}/deactivate")] DeactivateAirportAsync(int id)

Tạo Controllers/Admin/AircraftAdminController.cs:

[ApiController]
[Route("api/v1/admin/aircraft")]
[Authorize(Roles = "Admin")]

Endpoints:
1. [HttpGet] GetAllAircraftAsync()
2. [HttpPost] CreateAircraftAsync(CreateAircraftDto dto)
   - Model, RegistrationNumber, TotalSeats
3. [HttpPut("{id}")] UpdateAircraftAsync(int id, UpdateAircraftDto dto)
4. [HttpDelete("{id}")] DeleteAircraftAsync(int id)
5. [HttpPost("{id}/activate")] ActivateAsync(int id)
6. [HttpPost("{id}/deactivate")] DeactivateAsync(int id)
7. [HttpPost("{id}/add-seat-template")]
   AddSeatTemplateAsync(int id, AddSeatTemplateDto dto)
   - SeatClassId, DefaultSeatCount, DefaultBasePrice

Tạo Controllers/Admin/RoutesAdminController.cs:

Endpoints:
1. [HttpGet] GetAllRoutesAsync()
2. [HttpPost] CreateRouteAsync(CreateRouteDto dto)
   - DepartureAirportId, ArrivalAirportId
   - DistanceKm, EstimatedDurationMinutes
3. [HttpPut("{id}")] UpdateRouteAsync(int id, UpdateRouteDto dto)
4. [HttpDelete("{id}")] DeleteRouteAsync(int id)
5. [HttpPost("{id}/activate")] ActivateAsync(int id)
6. [HttpPost("{id}/deactivate")] DeactivateAsync(int id)

All endpoints with proper authorization checks.
Validation that entities not in use before delete.
Logging of all admin changes.
```

### Prompt 19: Admin Promotion Management

```
Tạo Controllers/Admin/PromotionsAdminController.cs:

[ApiController]
[Route("api/v1/admin/promotions")]
[Authorize(Roles = "Admin")]

Endpoints:

1. [HttpGet]
   GetAllPromotionsAsync([FromQuery] int page = 1)
   - Get all promotions (paginated)
   - Include usage count

2. [HttpPost]
   CreatePromotionAsync(CreatePromotionDto dto)
   - Create new promotion code
   - Validate: unique code, ValidTo > ValidFrom

3. [HttpPut("{id}")]
   UpdatePromotionAsync(int id, UpdatePromotionDto dto)
   - Update promotion details
   - Only if not yet used

4. [HttpDelete("{id}")]
   DeletePromotionAsync(int id)
   - Delete promotion
   - Only if no bookings used it

5. [HttpPost("{id}/activate")]
   ActivatePromotionAsync(int id)
   - Set IsActive = true

6. [HttpPost("{id}/deactivate")]
   DeactivatePromotionAsync(int id)
   - Set IsActive = false

7. [HttpGet("{id}/usage-statistics")]
   GetPromotionUsageAsync(int id)
   - Return: Total uses, Total discount amount, Revenue impact

CreatePromotionDto:
- Code (string, unique, max 50)
- DiscountType (PERCENTAGE or FIXED)
- DiscountValue (decimal)
- ValidFrom (DateTime)
- ValidTo (DateTime)
- UsageLimit (int, nullable)
- Description (string)

Tạo Application/Interfaces/IPromotionAdminService.cs:

Methods:
- Task<Promotion> CreatePromotionAsync(CreatePromotionDto dto)
- Task<Promotion> UpdatePromotionAsync(int id, UpdatePromotionDto dto)
- Task<bool> DeletePromotionAsync(int id)
- Task<PromotionStatistics> GetPromotionUsageAsync(int id)

Implement service with validations.
Check for duplicate codes.
Check usage before delete.
Log all promotion changes.
```

### Prompt 20: Admin Dashboard & Statistics

```
Tạo Controllers/Admin/StatisticsAdminController.cs:

[ApiController]
[Route("api/v1/admin/statistics")]
[Authorize(Roles = "Admin")]

Endpoints:

1. [HttpGet("overview")]
   GetDashboardOverviewAsync()
   - Return:
     * Total bookings (today, week, month)
     * Total revenue
     * Pending payments
     * Upcoming flights (48h)
     * Cancelled bookings

2. [HttpGet("bookings")]
   GetBookingStatisticsAsync([FromQuery] DateTime from, DateTime to)
   - Return:
     * Total bookings in period
     * Successful bookings
     * Cancelled bookings
     * Pending bookings
     * Average booking value

3. [HttpGet("revenue")]
   GetRevenueStatisticsAsync([FromQuery] DateTime from, DateTime to)
   - Return:
     * Total revenue
     * Revenue by payment method
     * Revenue by seat class
     * Revenue trend (daily/weekly)

4. [HttpGet("promotions")]
   GetPromotionStatisticsAsync([FromQuery] DateTime from, DateTime to)
   - Return:
     * Most used promotions
     * Total discount given
     * Discount by promotion
     * Revenue impact

5. [HttpGet("flights")]
   GetFlightStatisticsAsync([FromQuery] DateTime from, DateTime to)
   - Return:
     * Total flights
     * Flights by route
     * Load factor by flight
     * Popular routes

6. [HttpGet("passengers")]
   GetPassengerStatisticsAsync([FromQuery] DateTime from, DateTime to)
   - Return:
     * Total passengers
     * Passengers by nationality
     * Return vs new customers
     * Top airports

Tạo Application/Interfaces/IStatisticsService.cs:

Methods:
- Task<DashboardOverviewResponse> GetDashboardOverviewAsync()
- Task<BookingStatisticsResponse> GetBookingStatisticsAsync(DateTime from, DateTime to)
- Task<RevenueStatisticsResponse> GetRevenueStatisticsAsync(DateTime from, DateTime to)
- Task<PromotionStatisticsResponse> GetPromotionStatisticsAsync(DateTime from, DateTime to)
- Task<FlightStatisticsResponse> GetFlightStatisticsAsync(DateTime from, DateTime to)
- Task<PassengerStatisticsResponse> GetPassengerStatisticsAsync(DateTime from, DateTime to)

Tạo Application/Services/StatisticsService.cs:

Constructor inject:
- IBookingRepository _bookingRepository
- IPaymentRepository _paymentRepository
- IPromotionUsageRepository _promotionUsageRepository
- IFlightRepository _flightRepository
- ICacheService _cacheService
- ILogger<StatisticsService> _logger

Implement methods using LINQ queries:
- Query bookings, payments, flights
- Group by relevant fields
- Calculate aggregates (sum, count, avg)
- Cache results (1h TTL)
- Return typed response objects

Queries to implement:
- Bookings by status
- Revenue by payment method/seat class
- Popular routes (most bookings)
- Promotional discounts total
- Load factor (passengers vs capacity)
- Payment success rate

Use EntityFramework queries with AsNoTracking for read-only.
Optimize queries with .Select() for projection.
Cache expensive calculations.
```

### Prompt 21: Admin User Management

```
Tạo Controllers/Admin/UsersAdminController.cs:

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]

Endpoints:

1. [HttpGet]
   GetAllUsersAsync([FromQuery] int page = 1)
   - Paginated list of users
   - Include role information

2. [HttpGet("{id}")]
   GetUserAsync(int id)
   - User details with bookings

3. [HttpPost("{id}/roles")]
   AssignRoleAsync(int id, [FromBody] string roleName)
   - Add role to user

4. [HttpDelete("{id}/roles/{roleName}")]
   RemoveRoleAsync(int id, string roleName)
   - Remove role from user

5. [HttpPost("{id}/suspend")]
   SuspendUserAsync(int id, [FromBody] string reason)
   - Suspend user account

6. [HttpPost("{id}/activate")]
   ActivateUserAsync(int id)
   - Reactivate suspended account

7. [HttpDelete("{id}")]
   DeleteUserAsync(int id)
   - Delete user and anonymize data

Tạo Application/Interfaces/IUserAdminService.cs:

Methods:
- Task<User> AssignRoleAsync(int userId, string roleName)
- Task<bool> RemoveRoleAsync(int userId, string roleName)
- Task<bool> SuspendUserAsync(int userId, string reason)
- Task<bool> ActivateUserAsync(int userId)
- Task<bool> DeleteUserAsync(int userId) // Soft delete with anonymization

Implement service:
- Validate role exists
- Check user not already has role
- Log role changes
- Notify user of status changes
- Cannot delete users with active bookings (mark as suspended instead)

All admin actions logged for audit trail.
```

---

## 🔄 PHASE 6: SCHEDULING & BACKGROUND JOBS (3 Prompts)

### Prompt 22: Hangfire Scheduled Jobs

```
Tạo Infrastructure/BackgroundJobs/ScheduledJobsSetup.cs:

Configure Hangfire in Program.cs:

services.AddHangfire(config =>
    config.UseSqlServerStorage(connectionString)
    .WithJobExpirationTimeout(TimeSpan.FromDays(7)));

services.AddHangfireServer(options =>
{
    options.Queues = new[] { "default", "critical", "pricing" };
    options.WorkerCount = Environment.ProcessorCount * 2;
});

Register recurring jobs:
RecurringJob.AddOrUpdate<SeatHoldingJob>(
    "cleanup-expired-bookings",
    job => job.CleanupExpiredBookings(),
    Cron.MinuteInterval(5),
    queue: "critical");

RecurringJob.AddOrUpdate<PricingJob>(
    "update-dynamic-pricing",
    job => job.UpdateDynamicPricesAsync(),
    Cron.HourInterval(6),
    queue: "pricing");

RecurringJob.AddOrUpdate<NotificationJob>(
    "send-flight-reminders",
    job => job.SendFlightRemindersAsync(),
    Cron.Daily(8, 0),
    queue: "default");

RecurringJob.AddOrUpdate<ReportJob>(
    "generate-daily-reports",
    job => job.GenerateDailyReportsAsync(),
    Cron.Daily(23, 0),
    queue: "default");

Tạo Infrastructure/BackgroundJobs/SeatHoldingJob.cs:

[DisableConcurrentExecution(60)]
public class SeatHoldingJob
{
    public async Task CleanupExpiredBookings()
    {
        // Get all pending bookings > 15 min old
        // Release seats
        // Cancel bookings
        // Log cleanup
    }
}

Tạo Infrastructure/BackgroundJobs/PricingJob.cs:

[DisableConcurrentExecution(600)]
public class PricingJob
{
    public async Task UpdateDynamicPricesAsync()
    {
        // Get all active flights within next 30 days
        // For each flight, update pricing for all seat classes
        // Log changes
    }
}

Tạo Infrastructure/BackgroundJobs/NotificationJob.cs:

public class NotificationJob
{
    public async Task SendFlightRemindersAsync()
    {
        // Get all bookings with flights in next 24 hours
        // Send reminder email to each passenger
        // Log sent notifications
    }
}

Tạo Infrastructure/BackgroundJobs/ReportJob.cs:

public class ReportJob
{
    public async Task GenerateDailyReportsAsync()
    {
        // Generate daily statistics report
        // Get previous 24 hours data
        // Send to admin email
        // Store report
    }
}

Use [DisableConcurrentExecution] for critical jobs.
Implement proper error handling in each job.
Log all job executions.
Set appropriate queue priority.
```

### Prompt 23: Dynamic Pricing Scheduler

```
Tạo Infrastructure/Services/DynamicPricingScheduler.cs:

Purpose: Update flight prices based on demand and time

Methods:

UpdateDynamicPricesAsync():
1. Get all active flights in next 30 days
2. For each flight:
   a. Get all seat classes for that flight
   b. For each seat class:
      - Calculate new price using IPricingService
      - Compare with current price
      - If change > 10%: log and notify
      - Update CurrentPrice in database
   c. Cache updated prices
3. Log total flights/classes updated

Scheduled to run:
- Every 6 hours via Hangfire
- Or triggered manually by admin

Price adjustment factors:
- Occupancy (sold+held / total seats)
- Days until departure
- Demand level
- Season/holiday

Log significant price changes for audit trail.
Notify admin of major adjustments.
Cache prices to avoid recalculation.
```

### Prompt 24: Email Notification Scheduler

```
Tạo Infrastructure/BackgroundJobs/EmailNotificationJob.cs:

Methods:

SendFlightRemindersAsync():
1. Find all bookings with:
   - Status = Confirmed
   - Flight departure in 24 hours
   - Not yet reminded
2. For each booking:
   a. Get passengers
   b. Send reminder email with:
      - Booking details
      - Check-in instructions
      - Baggage allowance
      - Contact info
   c. Mark as reminded
   d. Log sent
3. Log summary (X emails sent)

SendPaymentRemindersAsync():
1. Find all bookings with:
   - Status = Pending
   - ExpiresAt in next 2 hours
2. For each:
   - Send payment reminder
   - Include payment link
   - Include booking details

Scheduled via Hangfire:
- Flight reminders: Daily at 8 AM
- Payment reminders: Every 2 hours

Use IEmailService for sending.
Track sent notifications in database.
Log all operations.
```

---

## 🔐 PHASE 7: SECURITY & VALIDATION (2 Prompts)

### Prompt 25: Input Validation & Error Handling

```
Tạo Application/Validators/ folder with FluentValidation:

Validators to create:
- RegisterDtoValidator
- LoginDtoValidator
- CreateFlightDtoValidator
- CreateBookingDtoValidator
- CreatePromotionDtoValidator
- UpdateFlightDtoValidator

Example: CreateBookingDtoValidator

public class CreateBookingDtoValidator : AbstractValidator<CreateBookingDto>
{
    public CreateBookingDtoValidator()
    {
        RuleFor(x => x.OutboundFlightId)
            .GreaterThan(0).WithMessage("Invalid flight ID");

        RuleFor(x => x.PassengerCount)
            .GreaterThan(0).WithMessage("At least 1 passenger required")
            .LessThanOrEqualTo(9).WithMessage("Max 9 passengers per booking");

        RuleForEach(x => x.Passengers)
            .SetValidator(new PassengerValidator());

        RuleFor(x => x.Passengers)
            .Must(p => p.Count == x.PassengerCount)
            .WithMessage("Passenger count mismatch");
    }
}

Register validators in Program.cs:
services.AddValidatorsFromAssemblyContaining<CreateBookingDtoValidator>();

Tạo Middleware/GlobalExceptionHandlingMiddleware.cs:

Handle exceptions globally:
- ValidationException -> 400 Bad Request
- NotFoundException -> 404 Not Found
- UnauthorizedException -> 401 Unauthorized
- ForbiddenException -> 403 Forbidden
- DomainException -> 422 Unprocessable Entity
- Generic Exception -> 500 Internal Server Error

Return standardized error response:
{
  "status": 400,
  "message": "Validation failed",
  "errors": [
    {
      "field": "email",
      "message": "Invalid email format"
    }
  ],
  "timestamp": "2024-01-15T10:30:00Z"
}

Register in Program.cs:
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

Tạo Application/Common/AppException.cs:

Base exception class with:
- Status code
- Error message
- Error details

Create specific exceptions:
- ValidationException
- NotFoundException
- UnauthorizedException
- ForbiddenException
- DomainException
- PaymentException
- ExternalServiceException

All exceptions inherit from AppException.
```

### Prompt 26: Authorization & Role-Based Access Control

```
Tạo Application/Attributes/AuthorizeAttribute.cs:

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute
{
    public string[] Roles { get; set; }
}

Usage:
[Authorize(Roles = "Admin")]
[Authorize(Roles = "Admin,Manager")]

Tạo Middleware/JwtAuthenticationMiddleware.cs:

Middleware to validate JWT tokens:
1. Extract token from Authorization header
2. Validate token signature
3. Extract claims
4. Check expiration
5. Add claims to HttpContext.User
6. Continue to next middleware

Register in Program.cs:
app.UseAuthentication();
app.UseAuthorization();

Tạo Infrastructure/Security/PasswordHasher.cs:

Implement password hashing:
- Use PBKDF2 with SHA256
- 10,000 iterations minimum
- Generate random salt

Methods:
- string HashPassword(string password)
- bool VerifyPassword(string password, string hash)

Use BCrypt or Argon2 for better security:
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
bool isValid = BCrypt.Net.BCrypt.Verify(password, hashedPassword);

Tạo Infrastructure/Security/DataProtectionService.cs:

Protect sensitive data:
- Encrypt sensitive fields (passport number, etc.)
- Decrypt when needed
- Use ASP.NET Core Data Protection API

Methods:
- string Encrypt(string plaintext)
- string Decrypt(string ciphertext)

Use IDataProtectionProvider for key management.

Authorization checks:
- User can only view own bookings
- Admin can view all
- Only booking owner can cancel
- Admin can perform admin actions
- Check roles before sensitive operations

Log all authorization failures.
```

---

## 📝 Summary: 26 Complete Prompts

### Coverage:
- ✅ **Authentication & User**: 6 prompts
- ✅ **Flight Search & Booking**: 8 prompts
- ✅ **Payment & Ticketing**: 6 prompts
- ✅ **Notifications**: 1 prompt
- ✅ **Admin Management**: 8 prompts
  - Flight management
  - Airport/Aircraft/Route management
  - Promotion management
  - Statistics & Dashboard
  - User management
- ✅ **Scheduling & Background Jobs**: 3 prompts
- ✅ **Security & Validation**: 2 prompts

### Features Implemented:
✅ User registration, login, password reset
✅ Flight search with multiple filters
✅ Booking with multiple passengers
✅ Seat holding (15-min timeout)
✅ Payment processing (multiple providers)
✅ Ticket generation & email
✅ Booking cancellation & refunds
✅ Dynamic pricing
✅ Email notifications
✅ Admin CRUD operations
✅ Statistics & analytics
✅ Scheduled background jobs
✅ Global error handling
✅ Authorization & role-based access

### Tech Stack:
- ASP.NET Core 8.0
- Entity Framework Core
- SQL Server / PostgreSQL
- Hangfire (background jobs)
- FluentValidation
- JWT Authentication
- Payment APIs (Momo, VNPay, Stripe, PayPal)
- Email services
- Caching (Redis/In-Memory)

---

**Status**: ✅ All 26 business logic prompts ready  
**Total features**: 20+ major features  
**Complexity**: Enterprise-grade  
**Time to implement**: 4-6 weeks with team
