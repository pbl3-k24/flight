# 🔴 CRITICAL ISSUES FOUND - Action Plan

## Issues List (12 Critical Issues)

### 1. 🔴 Unsafe Booking Code Generation
**Severity**: CRITICAL  
**Files**: `Booking.cs:53-57`, `BookingService.cs:301-305`, `DbInitializer.cs:395-401`  
**Problem**:
```csharp
// UNSAFE: Uses Random() - not cryptographically safe
var random = new Random();
BookingCode = new string(Enumerable.Range(0, 10)
    .Select(_ => chars[random.Next(chars.Length)])
    .ToArray());
```

**Risks**:
- ❌ Non-cryptographic PRNG (predictable)
- ❌ No uniqueness check (collision risk at high load)
- ❌ Same instance issue (poor randomness)
- ❌ Easy to brute force

**Fix**:
```csharp
// SAFE: Use crypto-safe RNG with uniqueness check
using System.Security.Cryptography;

private async Task<string> GenerateUniqueBookingCodeAsync()
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    const int maxRetries = 5;

    for (int retry = 0; retry < maxRetries; retry++)
    {
        var code = GenerateCryptoRandomCode(chars, 10);

        // Check uniqueness
        if (!await _bookingRepository.ExistsAsync(b => b.BookingCode == code))
        {
            return code;
        }
    }

    throw new InvalidOperationException("Failed to generate unique booking code");
}

private static string GenerateCryptoRandomCode(string chars, int length)
{
    var result = new char[length];
    using (var rng = new RNGCryptoServiceProvider())
    {
        byte[] data = new byte[length];
        rng.GetBytes(data);

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[data[i] % chars.Length];
        }
    }
    return new string(result);
}
```

---

### 2. 🔴 Rate Limiting Not Thread-Safe
**Severity**: CRITICAL  
**File**: `SecurityMiddleware.cs:95`  
**Problem**:
```csharp
// UNSAFE: Static Dictionary with race conditions
private static readonly Dictionary<string, RateLimitData> IpRequests = new();

// In InvokeAsync:
if (!IpRequests.ContainsKey(ipAddress))  // Race condition here!
{
    IpRequests[ipAddress] = new RateLimitData { ... };
}
```

**Risks**:
- ❌ Race condition at high concurrency
- ❌ Dictionary not thread-safe
- ❌ Can bypass rate limits
- ❌ Can cause crashes (KeyNotFoundException)

**Fix**:
```csharp
private static readonly ConcurrentDictionary<string, RateLimitData> IpRequests 
    = new ConcurrentDictionary<string, RateLimitData>();

public async Task InvokeAsync(HttpContext context)
{
    var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var now = DateTime.UtcNow;

    var limitData = IpRequests.AddOrUpdate(
        ipAddress,
        new RateLimitData { FirstRequest = now, RequestCount = 1, LastRequest = now },
        (key, existing) =>
        {
            if ((now - existing.FirstRequest).TotalSeconds > WindowSeconds)
            {
                return new RateLimitData { FirstRequest = now, RequestCount = 1, LastRequest = now };
            }

            existing.RequestCount++;
            existing.LastRequest = now;
            return existing;
        }
    );

    if (limitData.RequestCount > MaxRequests)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return;
    }

    await _next(context);
}
```

---

### 3. 🔴 Passenger Name Parsing Broken
**Severity**: HIGH  
**File**: `BookingService.cs:271-272`  
**Problem**:
```csharp
// BROKEN: Only splits into 2 parts, fails for names like "Nguyễn Văn A B"
FirstName = p.FullName.Split(' ')[0],
LastName = p.FullName.Contains(' ') ? p.FullName.Split(' ')[1] : "",
```

**Risks**:
- ❌ Loses middle names: "John Paul Smith" → FirstName="John", LastName="Paul"
- ❌ Inconsistent with database (FullName vs FirstName+LastName)
- ❌ Tickets printed wrong

**Fix**:
```csharp
// BETTER: Handle multiple names properly
private (string firstName, string lastName) ParseFullName(string fullName)
{
    var parts = fullName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

    if (parts.Length == 0)
        return ("", "");

    if (parts.Length == 1)
        return (parts[0], "");

    // First part is first name, rest is last name
    var firstName = parts[0];
    var lastName = string.Join(" ", parts.Skip(1));

    return (firstName, lastName);
}

// Usage:
var (firstName, lastName) = ParseFullName(p.FullName);
```

---

### 4. 🔴 Email Verification Skipped in Production
**Severity**: HIGH  
**File**: `AuthService.cs:84-89`  
**Problem**:
```csharp
// COMMENTED OUT - Email verification skipped!
// await _emailService.SendVerificationEmailAsync(user.Email, token.Code);
_logger.LogInformation("Email verification skipped for testing");
```

**Risks**:
- ❌ Anyone can register with fake email
- ❌ No email ownership verification
- ❌ Spam/abuse vector

**Fix**:
```csharp
// PROPER: Always send verification email
var token = new EmailVerificationToken
{
    UserId = user.Id,
    Code = GenerateVerificationCode(),
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddHours(24)
};

await _emailTokenRepository.AddAsync(token);
await _emailTokenRepository.SaveChangesAsync();

// Send email - REQUIRED, not optional
try
{
    await _emailService.SendVerificationEmailAsync(user.Email, token.Code);
}
catch (EmailServiceException ex)
{
    _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
    // Rollback user creation if email fails
    await _userRepository.DeleteAsync(user);
    throw new InvalidOperationException("Failed to send verification email", ex);
}
```

---

### 5. 🔴 Password Hash Placeholders in Seed Data
**Severity**: HIGH  
**File**: `DbInitializer.cs:39,50,61`  
**Problem**:
```csharp
// WRONG: Placeholder hash - these accounts can't login!
PasswordHash = "$2a$11$YourHashedPasswordHere"
```

**Risks**:
- ❌ Test accounts unusable
- ❌ Can't test login flow
- ❌ Confuses developers

**Fix**:
```csharp
// CORRECT: Generate proper password hashes for test data
private static class TestCredentials
{
    public const string AdminPassword = "Admin@123456";  // Store separately, not in DB
    public const string User1Password = "User@123456";
    public const string User2Password = "User@123456";
}

// In InitializeDatabaseAsync:
var passwordHasher = new PasswordHasher();

var adminUser = new User
{
    Email = "admin@flightbooking.vn",
    FullName = "Quản trị viên",
    Phone = "0901234567",
    PasswordHash = passwordHasher.HashPassword(TestCredentials.AdminPassword),
    Status = 0,
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow
};

// ... similar for other users
```

---

### 6. 🔴 Duplicate PaymentProviders.cs Files
**Severity**: MEDIUM  
**Files**: 
- `API\Infrastructure\ExternalServices\PaymentProviders.cs` (EMPTY)
- `API\Infrastructure\ExternalServices\Payment\PaymentProviders.cs` (ACTUAL)

**Problem**:
- ❌ Duplicate files confuses imports
- ❌ One file empty (unused)
- ❌ Maintainability issue

**Fix**: Delete the empty file in `ExternalServices\` root, keep the one in `Payment\` subfolder

---

### 7. 🔴 No Transaction Signature Verification
**Severity**: CRITICAL  
**File**: `PaymentService.cs`  
**Problem**:
```csharp
// MISSING: No signature/provider verification
public async Task<bool> ConfirmPaymentAsync(int bookingId, string providerTransactionId)
{
    var booking = await _bookingRepository.GetByIdAsync(bookingId);
    // Just trusts the client-provided transactionId!
    // ...
}
```

**Risks**:
- ❌ Anyone can claim payment with fake transactionId
- ❌ No provider ownership verification
- ❌ Fraud vulnerability

**Fix**:
```csharp
public async Task<bool> ConfirmPaymentAsync(int bookingId, string providerTransactionId, 
    string paymentSignature, string provider)
{
    var booking = await _bookingRepository.GetByIdAsync(bookingId);
    if (booking == null)
        throw new NotFoundException("Booking not found");

    // 1. Verify signature matches provider's secret key
    if (!VerifyPaymentSignature(providerTransactionId, booking.FinalAmount.ToString(), 
        paymentSignature, provider))
    {
        throw new UnauthorizedException("Invalid payment signature");
    }

    // 2. Query provider to verify transaction actually happened
    var providerPayment = await _paymentProvider.GetTransactionAsync(providerTransactionId);
    if (providerPayment?.Status != "completed")
    {
        throw new ValidationException("Payment not confirmed by provider");
    }

    // 3. Verify amount matches
    if (Math.Abs(providerPayment.Amount - (double)booking.FinalAmount) > 0.01)
    {
        throw new ValidationException("Payment amount mismatch");
    }

    // Only now create payment record
    var payment = new Payment
    {
        BookingId = bookingId,
        Amount = booking.FinalAmount,
        Status = 1, // Confirmed
        TransactionRef = providerTransactionId,
        // ...
    };

    await _paymentRepository.AddAsync(payment);
    booking.Status = 1; // Confirmed
    await _bookingRepository.UpdateAsync(booking);

    return true;
}
```

---

### 8. 🔴 Services Swallowing Exceptions
**Severity**: HIGH  
**Files**: Multiple services  
- `ReportingService.cs:94-95, 121-123, 151-153`
- `AdvancedSearchService.cs:74-75`

**Problem**:
```csharp
try
{
    // ... database query
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error occurred");
    return new List<ReportDto>();  // Empty list hides error!
}
```

**Risks**:
- ❌ Client gets empty data (silently fails)
- ❌ Hard to debug
- ❌ Production errors go unnoticed
- ❌ Invalid data served

**Fix**:
```csharp
try
{
    // ... database query
}
catch (SqlException ex)
{
    _logger.LogError(ex, "Database error in GetReports");
    throw new DataAccessException("Failed to retrieve reports", ex);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error in GetReports");
    throw new ApplicationException("An unexpected error occurred", ex);
}
// Don't catch - let GlobalExceptionMiddleware handle it
```

---

### 9. 🔴 App Runs Despite DB Migration Failure
**Severity**: CRITICAL  
**File**: `Program.cs:168-173`  
**Problem**:
```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Failed to apply database migrations...");
    // Don't throw - allow app to start even without database
}
```

**Risks**:
- ❌ App appears "up" but core functionality broken
- ❌ API returns errors to clients
- ❌ Health checks misleading
- ❌ Hard to diagnose

**Fix**:
```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Failed to initialize database");

    // In DEVELOPMENT: Allow to continue with warning
    if (app.Environment.IsDevelopment())
    {
        logger.LogWarning("Running in development mode without database");
    }
    else
    {
        // In PRODUCTION: Fail hard
        logger.LogCritical("Database initialization failed in production. Aborting startup.");
        throw;  // Don't swallow - let it fail explicitly
    }
}
```

---

### 10. 🔴 CreateBookingAsync Lacks Transaction Management
**Severity**: HIGH  
**File**: `BookingService.cs` CreateBookingAsync  
**Problem**:
```csharp
// Three separate operations with no transaction
var booking = new Booking { ... };
await _bookingRepository.AddAsync(booking);

var passengers = new List<BookingPassenger> { ... };
await _bookingPassengerRepository.AddAsync(passengers);

// If this fails, booking + passengers exist but inventory is wrong!
await UpdateInventoryAsync(booking.OutboundFlightId, ...);
```

**Risks**:
- ❌ Partial failure leaves invalid data
- ❌ Overbooking possible
- ❌ Inventory out of sync
- ❌ Data corruption

**Fix**:
```csharp
public async Task<BookingResponse> CreateBookingAsync(CreateBookingDto dto)
{
    // Use database transaction
    using (var transaction = await _bookingRepository.BeginTransactionAsync())
    {
        try
        {
            // 1. Create booking
            var booking = new Booking { ... };
            await _bookingRepository.AddAsync(booking);
            await _bookingRepository.SaveChangesAsync();

            // 2. Add passengers
            var passengers = dto.Passengers.Select(p => new BookingPassenger
            {
                BookingId = booking.Id,
                FullName = p.FullName,
                // ...
            }).ToList();
            await _bookingPassengerRepository.AddRangeAsync(passengers);
            await _bookingPassengerRepository.SaveChangesAsync();

            // 3. Update inventory
            await UpdateFlightInventoryAsync(booking.OutboundFlightId, 
                passengers.Count, FlightSeatUpdateType.Reserved);

            if (booking.ReturnFlightId.HasValue)
            {
                await UpdateFlightInventoryAsync(booking.ReturnFlightId.Value,
                    passengers.Count, FlightSeatUpdateType.Reserved);
            }

            // Commit if all succeeded
            await transaction.CommitAsync();

            return new BookingResponse { BookingCode = booking.BookingCode, /* ... */ };
        }
        catch (Exception ex)
        {
            // Rollback on any failure
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Booking creation failed, rolled back");
            throw new InvalidOperationException("Failed to create booking", ex);
        }
    }
}
```

---

### 11. 🔴 Missing Validation for Numeric Fields
**Severity**: HIGH  
**Problem**: No enforcement of valid ranges for prices, quantities, percentages

**Fix**:
Create validation attributes and enforce in model:
```csharp
[Range(0, 100, ErrorMessage = "Refund percent must be between 0 and 100")]
public decimal RefundPercent { get; set; }

[Range(0.01, double.MaxValue, ErrorMessage = "Price must be positive")]
public decimal Price { get; set; }

[Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
public int Quantity { get; set; }
```

---

### 12. 🔴 Cancel Request Handling Missing
**Severity**: MEDIUM  
**Problem**: No proper exception handling when user cancels request

**Fix**:
```csharp
[HttpPost("bookings/{id}/cancel")]
public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelBookingDto dto)
{
    try
    {
        await _bookingService.CancelBookingAsync(id, dto.Reason);
        return Ok(new { message = "Booking cancelled successfully" });
    }
    catch (OperationCanceledException)
    {
        return BadRequest(new { error = "Request was cancelled" });
    }
    catch (NotFoundException)
    {
        return NotFound(new { error = "Booking not found" });
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error cancelling booking {BookingId}", id);
        throw;  // Let middleware handle
    }
}
```

---

## Priority Fix Order

### 🔴 CRITICAL (Fix First)
1. Unsafe booking code generation (collision risk)
2. Rate limiting race condition (security)
3. No transaction signature verification (fraud)
4. App runs despite DB failure (availability)

### 🟠 HIGH (Fix Soon)
5. Email verification skipped (security)
6. Password placeholders (usability)
7. Exception swallowing (debuggability)
8. Missing transaction in CreateBooking (data integrity)

### 🟡 MEDIUM (Fix Later)
9. Passenger name parsing
10. Duplicate PaymentProviders.cs
11. Missing input validation
12. No cancel request handling

---

## Estimated Effort

| Severity | Issue Count | Effort | Time |
|----------|------------|--------|------|
| Critical | 4 | High | 6-8 hours |
| High | 4 | Medium | 4-6 hours |
| Medium | 4 | Low | 2-3 hours |
| **TOTAL** | **12** | - | **12-17 hours** |

---

## Testing Required After Fixes

- [ ] Booking code uniqueness under load test
- [ ] Rate limiting with concurrent requests
- [ ] Payment signature verification tests
- [ ] Database transaction rollback scenarios
- [ ] Exception handling for all error cases
- [ ] Passenger name parsing for various formats
- [ ] Email verification flow end-to-end
