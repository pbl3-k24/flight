# FLIGHT BOOKING API - CRITICAL FIXES IMPLEMENTATION GUIDE

## STATUS SUMMARY

### ALREADY FIXED ✅
1. **Database Migration Error Handling** - Program.cs properly handles failures
2. **Rate Limiting Thread Safety** - Using ConcurrentDictionary  
3. **BookingCodeGenerator Service** - Created with crypto-safe generation
4. **Password Hashing** - DbInitializer uses proper BCrypt hashes
5. **Cancel Booking Endpoint** - CancelBookingAsync implemented in controller
6. **Booking Service Error Handling** - Properly throws exceptions

### STILL NEEDED ⚠️
1. **Payment Signature Verification** - Missing HMACSHA256 validation
2. **Email Verification Flow** - Currently skipped in production
3. **Exception Handling in Services** - Some services swallow exceptions
4. **Passenger Name Parsing** - Doesn't handle multi-word names properly
5. **Input Validation** - Missing numeric range validations  
6. **Transaction Atomicity** - CreateBooking not wrapped in transaction
7. **Duplicate PaymentProviders.cs** - One empty file needs deletion
8. **Cancel Request Handling** - Need to enforce cancellation rules

---

## CRITICAL IMPLEMENTATION #1: PAYMENT SIGNATURE VERIFICATION

**File**: `API/Application/Services/PaymentService.cs`  
**Issue**: No HMACSHA256 signature verification before confirming payment  
**Impact**: Attacker could forge payment confirmations → fraud, revenue loss

### Implementation:

```csharp
// In PaymentService.ProcessPaymentAsync()

public async Task<bool> ProcessPaymentAsync(int paymentId, PaymentCallbackDto callback)
{
    try
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null)
        {
            _logger.LogWarning("Payment not found: {PaymentId}", paymentId);
            return false;
        }

        // FIX: Verify payment signature before processing
        if (!VerifyPaymentSignature(callback))
        {
            _logger.LogError("Invalid payment signature for payment {PaymentId}. Potential fraud attempt!", paymentId);
            payment.Status = 2; // Failed
            await _paymentRepository.UpdateAsync(payment);
            return false;
        }

        // FIX: Verify amount matches
        if (callback.Amount != payment.Amount)
        {
            _logger.LogError("Payment amount mismatch. Expected: {Expected}, Got: {Got}", 
                payment.Amount, callback.Amount);
            return false;
        }

        // FIX: Verify transaction timestamp is recent (not replayed)
        if (callback.Timestamp.AddMinutes(15) < DateTime.UtcNow)
        {
            _logger.LogError("Payment signature expired/replayed: {PaymentId}", paymentId);
            return false;
        }

        // Only then mark as success
        if (callback.Status.ToLower() != "success")
        {
            payment.Status = 2; // Failed
            await _paymentRepository.UpdateAsync(payment);
            return false;
        }

        payment.Status = 1; // Completed
        payment.PaidAt = DateTime.UtcNow;
        payment.UpdatedAt = DateTime.UtcNow;
        await _paymentRepository.UpdateAsync(payment);

        var booking = await _bookingRepository.GetByIdAsync(payment.BookingId);
        if (booking != null)
        {
            booking.Status = 1; // Confirmed
            booking.UpdatedAt = DateTime.UtcNow;
            await _bookingRepository.UpdateAsync(booking);

            await _emailService.SendBookingConfirmationAsync(booking.ContactEmail, booking);
        }

        _logger.LogInformation("Payment verified and processed: {PaymentId}", paymentId);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing payment");
        throw;
    }
}

// Helper method to verify HMACSHA256 signature
private bool VerifyPaymentSignature(PaymentCallbackDto callback)
{
    try
    {
        var hmacSecret = _config.GetValue<string>("Payment:HmacSecret");
        if (string.IsNullOrEmpty(hmacSecret) || string.IsNullOrEmpty(callback.Signature))
        {
            return false;
        }

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hmacSecret)))
        {
            var signatureData = $"{callback.TransactionId}:{callback.Amount}:{callback.BookingId}";
            var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(signatureData)));
            return computedHash == callback.Signature;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error verifying payment signature");
        return false;
    }
}
```

**Configuration** (appsettings.json):
```json
{
    "Payment": {
        "HmacSecret": "your-secret-key-from-provider"
    }
}
```

---

## CRITICAL IMPLEMENTATION #2: CREATEBOOKING TRANSACTION ATOMICITY

**File**: `API/Application/Services/BookingService.cs`  
**Issue**: CreateBookingAsync not atomic - can fail partially  
**Impact**: Booking created but no passengers = corrupted state

### Implementation:

Wrap CreateBookingAsync in database transaction:

```csharp
public async Task<BookingResponse> CreateBookingAsync(int userId, CreateBookingDto dto)
{
    using var transaction = await _bookingRepository.BeginTransactionAsync();

    try
    {
        // 1. Validate flight exists
        var outboundFlight = await _flightRepository.GetByIdAsync(dto.OutboundFlightId);
        if (outboundFlight == null)
        {
            throw new NotFoundException("Flight not found");
        }

        // 2. Validate passenger count
        if (dto.Passengers.Count != dto.PassengerCount || dto.PassengerCount <= 0 || dto.PassengerCount > 9)
        {
            throw new ValidationException("Invalid passenger count (1-9)");
        }

        // 3. Validate seats available
        var outboundInventory = await _seatInventoryRepository.GetByFlightAndSeatClassAsync(
            dto.OutboundFlightId, dto.SeatClassId);
        if (outboundInventory == null || outboundInventory.AvailableSeats < dto.PassengerCount)
        {
            throw new ValidationException("Insufficient seats available");
        }

        // 4. Create booking with unique code
        var bookingCode = await _bookingCodeGenerator.GenerateUniqueCodeAsync();
        var booking = new Booking
        {
            UserId = userId,
            BookingCode = bookingCode,
            OutboundFlightId = dto.OutboundFlightId,
            ReturnFlightId = dto.ReturnFlightId,
            Status = 0, // Pending
            ContactEmail = dto.ContactEmail ?? "",
            TotalAmount = outboundInventory.CurrentPrice * dto.PassengerCount,
            FinalAmount = outboundInventory.CurrentPrice * dto.PassengerCount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            PromotionId = dto.PromotionId
        };

        var createdBooking = await _bookingRepository.CreateAsync(booking);

        // 5. Create passengers (all or nothing)
        foreach (var passengerDto in dto.Passengers)
        {
            // Properly handle Vietnamese multi-word names
            var firstName = passengerDto.FirstName?.Trim() ?? "";
            var lastName = passengerDto.LastName?.Trim() ?? "";

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
            {
                throw new ValidationException("Passenger first name and last name are required");
            }

            var fullName = $"{firstName} {lastName}";
            var passenger = new BookingPassenger
            {
                BookingId = createdBooking.Id,
                FullName = fullName,
                DateOfBirth = passengerDto.DateOfBirth,
                NationalId = passengerDto.NationalId,
                PassengerType = DeterminePassengerType(passengerDto.DateOfBirth),
                FlightSeatInventoryId = outboundInventory.Id
            };

            await _passengerRepository.CreateAsync(passenger);
        }

        // 6. Reserve seats (atomically update inventory)
        outboundInventory.HeldSeats += dto.PassengerCount;
        outboundInventory.AvailableSeats -= dto.PassengerCount;
        outboundInventory.UpdatedAt = DateTime.UtcNow;
        await _seatInventoryRepository.UpdateAsync(outboundInventory);

        // Commit transaction only if all operations succeed
        await transaction.CommitAsync();

        _logger.LogInformation("Booking created successfully: {BookingCode}", bookingCode);
        return await BuildBookingResponseAsync(createdBooking);
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Error creating booking. Transaction rolled back.");
        throw;
    }
}

private int DeterminePassengerType(DateTime dateOfBirth)
{
    var age = DateTime.UtcNow.Year - dateOfBirth.Year;
    if (dateOfBirth > DateTime.UtcNow.AddYears(-age))
        age--;

    return age < 12 ? 1 : (age < 65 ? 0 : 2); // 0=Adult, 1=Child, 2=Senior
}
```

---

## IMPLEMENTATION #3: EMAIL VERIFICATION ENFORCEMENT

**File**: `API/Application/Services/AuthService.cs`  
**Issue**: Email verification disabled in production  
**Impact**: Spam registrations, invalid emails break notification system

### Implementation:

```csharp
public async Task<AuthResponse> RegisterAsync(RegisterDto dto)
{
    // ... validation code ...

    var createdUser = await _userRepository.CreateAsync(user);

    // Create email verification token
    var verificationCode = GenerateVerificationCode();
    var verificationToken = new EmailVerificationToken
    {
        UserId = createdUser.Id,
        Code = verificationCode,
        ExpiresAt = DateTime.UtcNow.AddHours(24)
    };

    await _emailTokenRepository.CreateAsync(verificationToken);

    // PRODUCTION: Send verification email (required)
    // DEVELOPMENT: Skip only if explicitly configured
    var requireEmailVerification = !app.Environment.IsDevelopment() 
        || _config.GetValue<bool>("Auth:RequireEmailVerificationInDev", false);

    if (requireEmailVerification)
    {
        try
        {
            await _emailService.SendVerificationEmailAsync(createdUser.Email, verificationCode);
            _logger.LogInformation("Verification email sent to: {Email}", createdUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email for: {Email}", createdUser.Email);
            // In production, don't allow registration if email can't be sent
            if (!app.Environment.IsDevelopment())
                throw;
        }
    }
    else
    {
        _logger.LogWarning("Email verification skipped. Development mode only.");
    }

    // Generate temporary token but user can't use booking until email verified
    var token = _jwtTokenService.GenerateToken(createdUser);

    _logger.LogInformation("User registered: {Email}", createdUser.Email);

    return new AuthResponse
    {
        UserId = createdUser.Id,
        Email = createdUser.Email,
        Token = token,
        RequiresEmailVerification = true
    };
}

public async Task<AuthResponse> VerifyEmailAsync(int userId, string verificationCode)
{
    var token = await _emailTokenRepository.GetByUserIdAsync(userId);
    if (token == null || token.Code != verificationCode || token.ExpiresAt < DateTime.UtcNow)
    {
        throw new ValidationException("Invalid or expired verification code");
    }

    var user = await _userRepository.GetByIdAsync(userId);
    if (user == null)
    {
        throw new NotFoundException("User not found");
    }

    // Mark user as verified
    user.EmailVerified = true;
    user.EmailVerifiedAt = DateTime.UtcNow;
    await _userRepository.UpdateAsync(user);

    // Delete used token
    await _emailTokenRepository.DeleteAsync(token.Id);

    _logger.LogInformation("Email verified for user: {Email}", user.Email);

    return new AuthResponse
    {
        UserId = user.Id,
        Email = user.Email,
        Token = _jwtTokenService.GenerateToken(user),
        RequiresEmailVerification = false
    };
}
```

---

## IMPLEMENTATION #4: INPUT VALIDATION ON NUMERIC FIELDS

Add validation to all DTOs and entities:

```csharp
// CreateBookingDto
public class CreateBookingDto
{
    [Required]
    public int OutboundFlightId { get; set; }

    public int? ReturnFlightId { get; set; }

    [Range(1, 9, ErrorMessage = "Passenger count must be between 1 and 9")]
    public int PassengerCount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seat class ID must be valid")]
    public int SeatClassId { get; set; }

    public int? PromotionId { get; set; }

    [EmailAddress]
    public string ContactEmail { get; set; } = null!;

    public string? ContactPhone { get; set; }

    [Required]
    public List<PassengerInfoDto> Passengers { get; set; } = [];
}

// FlightSeatInventory Entity
public class FlightSeatInventory
{
    public int Id { get; set; }

    [Range(0, int.MaxValue)]
    public int TotalSeats { get; set; }

    [Range(0, int.MaxValue)]
    public int AvailableSeats { get; set; }

    [Range(0, int.MaxValue)]
    public int HeldSeats { get; set; }

    [Range(0, int.MaxValue)]
    public int SoldSeats { get; set; }

    [Range(0, double.MaxValue)]
    public decimal BasePrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal CurrentPrice { get; set; }

    // ... rest of properties
}
```

---

## IMPLEMENTATION #5: EXCEPTION HANDLING IN SERVICES

Fix ReportingService and other services that swallow exceptions:

```csharp
public class ReportingService : IReportingService
{
    public async Task<BookingReportDto> GetBookingReportAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            if (startDate > endDate)
            {
                throw new ValidationException("Start date must be before end date");
            }

            var bookings = await _bookingRepository.GetAllAsync();
            if (bookings == null || !bookings.Any())
            {
                _logger.LogWarning("No bookings found for report period");
                return new BookingReportDto { TotalBookings = 0 };
            }

            var filtered = bookings
                .Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate)
                .ToList();

            var report = new BookingReportDto
            {
                TotalBookings = filtered.Count,
                TotalRevenue = filtered.Sum(b => b.FinalAmount),
                ConfirmedBookings = filtered.Count(b => b.Status == 1),
                PendingBookings = filtered.Count(b => b.Status == 0),
                CancelledBookings = filtered.Count(b => b.Status == 3),
                AverageBookingValue = filtered.Count > 0 ? filtered.Average(b => b.FinalAmount) : 0,
                DailyMetrics = GenerateDailyBookingMetrics(filtered, startDate, endDate)
            };

            _logger.LogInformation("Report generated successfully. Period: {Start} to {End}", startDate, endDate);
            return report;
        }
        catch (ValidationException)
        {
            throw; // Re-throw validation errors
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating booking report");
            throw; // Don't swallow exceptions
        }
    }
}
```

---

## IMPLEMENTATION #6: DELETE DUPLICATE FILE

```bash
# Remove the empty duplicate file
rm API/Infrastructure/ExternalServices/Payment/PaymentProviders.cs

# Verify only one PaymentProviders.cs remains
ls -la API/Infrastructure/ExternalServices/PaymentProviders.cs
```

---

## IMPLEMENTATION #7: ENHANCED CANCELLATION RULES

```csharp
public class CancellationPolicy
{
    public int SeatClassId { get; set; }
    public int HoursBeforeDeparture { get; set; }
    public decimal RefundPercent { get; set; }
    public decimal PenaltyFee { get; set; }
}

// In BookingService
public async Task<bool> CancelBookingAsync(int bookingId, int userId, string reason)
{
    try
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null || booking.UserId != userId)
        {
            throw new UnauthorizedException("Cannot cancel this booking");
        }

        if (booking.Status == 3) // Already cancelled
        {
            throw new ValidationException("Booking is already cancelled");
        }

        var outboundFlight = await _flightRepository.GetByIdAsync(booking.OutboundFlightId);
        if (outboundFlight == null)
        {
            throw new NotFoundException("Flight not found");
        }

        // Check if cancellation is allowed based on departure time
        var hoursUntilDeparture = (outboundFlight.DepartureTime - DateTime.UtcNow).TotalHours;
        if (hoursUntilDeparture < 2) // No cancellation within 2 hours
        {
            throw new ValidationException("Cannot cancel within 2 hours of departure");
        }

        // Calculate refund based on policy
        var refundPercent = 100m; // Default full refund
        if (hoursUntilDeparture < 24)
        {
            refundPercent = 50m; // 50% refund less than 24 hours
        }

        booking.Status = 3; // Cancelled
        booking.UpdatedAt = DateTime.UtcNow;
        await _bookingRepository.UpdateAsync(booking);

        // Restore held seats
        var passengers = await _passengerRepository.GetByBookingIdAsync(booking.Id);
        var inventory = await _seatInventoryRepository.GetByIdAsync(passengers.First().FlightSeatInventoryId ?? 0);

        if (inventory != null)
        {
            inventory.HeldSeats -= passengers.Count;
            inventory.AvailableSeats += passengers.Count;
            await _seatInventoryRepository.UpdateAsync(inventory);
        }

        _logger.LogInformation("Booking {BookingId} cancelled. Refund: {RefundPercent}%, Reason: {Reason}", 
            bookingId, refundPercent, reason);

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error cancelling booking");
        throw;
    }
}
```

---

## TESTING CHECKLIST

### Unit Tests
- [ ] Payment signature validation (valid/invalid signatures)
- [ ] Booking code uniqueness (10,000 iterations)
- [ ] Transaction rollback on passenger creation failure
- [ ] Email verification token expiration
- [ ] Passenger name parsing (Vietnamese multi-word names)
- [ ] Input validation (negative prices, invalid counts)

### Integration Tests
- [ ] Full booking flow: Create → Payment → Confirm
- [ ] Cancellation flow: Cancel → Refund → Seat restoration
- [ ] Email verification: Register → Verify → Access booking
- [ ] Rate limiting: 100+ concurrent requests

### Load Tests
- [ ] Rate limiting under 1000 concurrent requests
- [ ] Booking code generation (no collisions)
- [ ] Database transaction handling (concurrent bookings)

---

## DEPLOYMENT CHECKLIST

- [ ] All unit tests passing
- [ ] All integration tests passing  
- [ ] Database migrations successful
- [ ] Seed data created correctly
- [ ] SMTP credentials configured (production)
- [ ] Payment provider HMAC secrets configured
- [ ] Rate limiting thresholds reviewed
- [ ] Error logging monitored
- [ ] Security headers verified

---

## ROLL-OUT PLAN

### Phase 1 (TODAY)
- Implement payment signature verification
- Implement transaction atomicity in CreateBooking
- Delete duplicate PaymentProviders.cs
- Test on staging

### Phase 2 (THIS WEEK)
- Implement email verification enforcement
- Add input validation to all DTOs
- Fix exception handling in services
- Run load tests

### Phase 3 (NEXT WEEK)
- Deploy to production
- Monitor error logs for 1 week
- Verify email verification flow
- Track payment fraud metrics

---

**Ready for Review** ✓  
Generated: 2024  
Total Effort: 15-20 hours  
Risk Level: MEDIUM-HIGH (security-critical changes)
