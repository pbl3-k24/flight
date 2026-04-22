# P1-C IDOR Prevention - Developer Reference Guide

## Quick Start

### For Developers Calling Payment Endpoints

**Old Way** (No longer works):
```csharp
var payment = await paymentService.GetPaymentStatusAsync(paymentId);
```

**New Way** (Required):
```csharp
var userId = User.GetUserIdOrThrow();
var isAdmin = User.IsInRole("Admin");
var payment = await paymentService.GetPaymentStatusAsync(paymentId, userId, isAdmin);
```

---

## API Changes

### GetPaymentStatusAsync

**Signature**:
```csharp
Task<PaymentResponse> GetPaymentStatusAsync(int paymentId, int userId, bool isAdmin = false);
```

**Parameters**:
- `paymentId` (int): The payment to retrieve
- `userId` (int): Current user's ID (from JWT claims)
- `isAdmin` (bool): Whether user is admin (defaults to false)

**Returns**: `PaymentResponse` with payment details

**Throws**:
- `NotFoundException`: Payment doesn't exist
- `UnauthorizedException`: User is not payment owner and not admin

**Example**:
```csharp
[HttpGet("{paymentId}")]
public async Task<ActionResult<PaymentResponse>> GetPaymentStatusAsync(int paymentId)
{
    var userId = User.GetUserIdOrThrow();
    var isAdmin = User.IsInRole("Admin");

    var response = await _paymentService.GetPaymentStatusAsync(paymentId, userId, isAdmin);
    return Ok(response);
}
```

### GetPaymentHistoryAsync

**Signature**:
```csharp
Task<List<PaymentHistoryResponse>> GetPaymentHistoryAsync(int bookingId, int userId, bool isAdmin = false);
```

**Parameters**:
- `bookingId` (int): The booking to get payment history for
- `userId` (int): Current user's ID (from JWT claims)
- `isAdmin` (bool): Whether user is admin (defaults to false)

**Returns**: `List<PaymentHistoryResponse>` with payment history

**Throws**:
- `NotFoundException`: Booking doesn't exist
- `UnauthorizedException`: User is not booking owner and not admin

**Example**:
```csharp
[HttpGet("booking/{bookingId}")]
public async Task<ActionResult<List<PaymentHistoryResponse>>> GetPaymentHistoryAsync(int bookingId)
{
    var userId = User.GetUserIdOrThrow();
    var isAdmin = User.IsInRole("Admin");

    var response = await _paymentService.GetPaymentHistoryAsync(bookingId, userId, isAdmin);
    return Ok(response);
}
```

---

## User Context Extraction

### Getting User ID

```csharp
// From ClaimsPrincipalExtensions (already exists)
using API.Extensions;

// Method 1: Safe parsing (try/catch approach)
if (User.TryGetUserId(out var userId))
{
    // userId is valid
}

// Method 2: Throw on error (recommended for controllers)
var userId = User.GetUserIdOrThrow();  // Throws UnauthorizedException if invalid
```

### Checking Admin Role

```csharp
// Built-in ASP.NET Core method
var isAdmin = User.IsInRole("Admin");

// Alternative: Check multiple roles
var isStaff = User.IsInRole("Staff");
var isAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("Staff");
```

### Getting Other Claims

```csharp
using API.Extensions;

var email = User.GetEmail();       // From ClaimTypes.Email
var name = User.GetName();         // From ClaimTypes.Name
var userId = User.GetUserIdOrThrow();  // From ClaimTypes.NameIdentifier
```

---

## Common Patterns

### Pattern 1: Standard User Request

```csharp
[HttpGet("{paymentId}")]
[Authorize]  // Only authenticated users
public async Task<ActionResult<PaymentResponse>> GetPaymentStatusAsync(int paymentId)
{
    try
    {
        var userId = User.GetUserIdOrThrow();
        var isAdmin = User.IsInRole("Admin");

        var response = await _paymentService.GetPaymentStatusAsync(paymentId, userId, isAdmin);
        return Ok(response);
    }
    catch (UnauthorizedException ex)
    {
        _logger.LogWarning(ex, "Unauthorized access attempt");
        return Unauthorized(new { message = ex.Message });
    }
    catch (NotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
}
```

### Pattern 2: Admin-Only Action

```csharp
[HttpGet("admin/user/{userId}")]
[Authorize(Roles = "Admin")]  // Only admins
public async Task<ActionResult<List<PaymentHistoryResponse>>> GetUserPaymentHistoryAsync(int userId)
{
    // At this point, we're guaranteed to be admin
    var adminUserId = User.GetUserIdOrThrow();

    // Can access any user's data
    var bookings = await _bookingService.GetUserBookingsAsync(userId);

    return Ok(bookings);
}
```

### Pattern 3: Role-Based Features

```csharp
[HttpGet("{paymentId}")]
[Authorize]
public async Task<ActionResult<PaymentResponse>> GetPaymentStatusAsync(int paymentId)
{
    var userId = User.GetUserIdOrThrow();
    var isAdmin = User.IsInRole("Admin");

    var response = await _paymentService.GetPaymentStatusAsync(paymentId, userId, isAdmin);

    // Different response based on role
    if (isAdmin)
    {
        // Admin gets full response with internal notes
        response.InternalNotes = "...";
    }

    return Ok(response);
}
```

---

## Ownership Validation Logic

### How the Check Works

```csharp
// Service layer ownership check
public async Task<PaymentResponse> GetPaymentStatusAsync(int paymentId, int userId, bool isAdmin = false)
{
    // Step 1: Get the payment
    var payment = await _paymentRepository.GetByIdAsync(paymentId);
    if (payment == null) throw new NotFoundException(...);

    // Step 2: Get the booking (to find owner)
    var booking = await _bookingRepository.GetByIdAsync(payment.BookingId);
    if (booking == null) throw new NotFoundException(...);

    // Step 3: Check ownership
    // Allow if: (user is owner) OR (user is admin)
    if (!isAdmin && booking.UserId != userId)
    {
        // Neither condition met - access denied
        throw new UnauthorizedException("You cannot access this payment");
    }

    // Step 4: Ownership verified - return data
    return new PaymentResponse { ... };
}
```

### Access Control Truth Table

| Condition | Result |
|-----------|--------|
| isAdmin = true | ✅ Allow (admin bypass) |
| booking.UserId == userId | ✅ Allow (owner) |
| isAdmin = false && booking.UserId != userId | ❌ Deny (IDOR) |

---

## Error Handling

### UnauthorizedException (401)

**When it's thrown**:
- User tries to access payment/booking they don't own
- User is not an admin

**How to handle**:
```csharp
catch (UnauthorizedException ex)
{
    _logger.LogWarning(ex, "Unauthorized access attempt");
    return Unauthorized(new { message = ex.Message });
}
```

**HTTP Response**:
```
HTTP/1.1 401 Unauthorized
{
  "message": "You cannot access this payment"
}
```

### NotFoundException (404)

**When it's thrown**:
- Payment doesn't exist
- Associated booking doesn't exist

**How to handle**:
```csharp
catch (NotFoundException ex)
{
    _logger.LogInformation(ex, "Resource not found");
    return NotFound(new { message = ex.Message });
}
```

**HTTP Response**:
```
HTTP/1.1 404 Not Found
{
  "message": "Payment not found"
}
```

### Why 404 for IDOR?

Using 404 instead of 401 for non-existent resources prevents information leakage:
- If user gets 401: They know the resource exists (information leak)
- If user gets 404: They can't tell if it doesn't exist or they don't own it (no leak)

**Best Practice**: Return 404 for all NotFoundException, regardless of the underlying cause.

---

## Logging and Monitoring

### IDOR Attempt Logging

Every IDOR attempt is logged automatically:

```csharp
_logger.LogWarning(
    "IDOR attempt: User {UserId} tried to access payment {PaymentId} belonging to user {OwnerId}",
    userId, paymentId, booking.UserId);
```

**Log Output**:
```
[Warning] IDOR attempt: User 5 tried to access payment 999 belonging to user 3
```

### Monitoring for IDOR Attacks

Monitor logs for patterns:
- Multiple failed attempts from same user
- Attempts at sequential payment/booking IDs
- Attempts from unusual IPs/times

---

## Testing

### Unit Test Pattern

```csharp
[Fact]
public async Task GetPaymentStatusAsync_WhenUserOwnsPayment_ReturnsOk()
{
    // Arrange
    var userId = 5;
    var paymentId = 10;
    var mockPayment = new Payment { Id = 10, BookingId = 20 };
    var mockBooking = new Booking { Id = 20, UserId = 5 };

    // Act
    var result = await _paymentService.GetPaymentStatusAsync(paymentId, userId, isAdmin: false);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(paymentId, result.PaymentId);
}

[Fact]
public async Task GetPaymentStatusAsync_WhenUserDoesNotOwnPayment_ThrowsUnauthorized()
{
    // Arrange
    var userId = 5;
    var paymentId = 10;
    var mockPayment = new Payment { Id = 10, BookingId = 20 };
    var mockBooking = new Booking { Id = 20, UserId = 3 };  // Different user

    // Act & Assert
    var ex = await Assert.ThrowsAsync<UnauthorizedException>(
        () => _paymentService.GetPaymentStatusAsync(paymentId, userId, isAdmin: false));

    Assert.Equal("You cannot access this payment", ex.Message);
}

[Fact]
public async Task GetPaymentStatusAsync_WhenAdminUserRequestsPayment_ReturnsOk()
{
    // Arrange
    var adminUserId = 1;
    var paymentId = 10;
    var mockPayment = new Payment { Id = 10, BookingId = 20 };
    var mockBooking = new Booking { Id = 20, UserId = 3 };  // Different owner

    // Act
    var result = await _paymentService.GetPaymentStatusAsync(paymentId, adminUserId, isAdmin: true);

    // Assert
    Assert.NotNull(result);  // Admin can view despite not owning
}
```

---

## Frequently Asked Questions

### Q: What if a user's JWT token is compromised?
**A**: The token itself provides authentication. If someone has a valid token, they can act as that user. IDOR prevention ensures they can only access their own resources (or anything if they're admin). Implement token expiration and revocation for additional security.

### Q: Can I bypass the ownership check?
**A**: Only if you set `isAdmin = true`. This should only be done for actual admin users verified via JWT claims. Never hardcode or allow user-controlled bypass.

### Q: What if I need to access multiple users' payments in a batch?
**A**: This is not supported for regular users (security by design). Admins can call the method multiple times with `isAdmin = true` for each payment.

### Q: How do I handle the case where a user deletes their booking?
**A**: The booking lookup will throw `NotFoundException`, which the controller catches and returns 404. Users can't access payments from deleted bookings.

### Q: Can I cache the payment response?
**A**: Be careful with caching. If you cache responses, ensure the cache key includes userId. Never return cached payment responses for different users.

### Q: What about service-to-service calls?
**A**: If services call each other, you might not have a user context. Create a system user (e.g., userId = -1) for internal calls, or pass the original userId through the call chain.

---

## Troubleshooting

### Issue: "User context is invalid or missing"

**Cause**: `User.GetUserIdOrThrow()` failed to extract user ID

**Debug**:
```csharp
var claim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
if (claim == null)
{
    _logger.LogError("No NameIdentifier claim found");
}
```

**Fix**: Ensure JWT token includes NameIdentifier claim

### Issue: Admin check not working

**Cause**: User doesn't have "Admin" role in JWT token

**Debug**:
```csharp
var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role);
_logger.LogInformation("User roles: {Roles}", string.Join(", ", roles));
```

**Fix**: Verify token includes role claims, check role name case sensitivity

### Issue: Always getting 404 even for owned resources

**Cause**: Booking lookup failing (payment.BookingId might be null)

**Debug**:
```csharp
var payment = await _paymentRepository.GetByIdAsync(paymentId);
_logger.LogInformation("Payment BookingId: {BookingId}", payment?.BookingId);
```

**Fix**: Ensure payment records have valid BookingId foreign keys

---

## Security Checklist

Before deploying to production:

- [ ] User.GetUserIdOrThrow() is called before accessing service
- [ ] User.IsInRole("Admin") is used to check admin status
- [ ] No hardcoded userId values (except system user = -1)
- [ ] All IDOR attempts are logged
- [ ] Error handling catches both UnauthorizedException and NotFoundException
- [ ] Tests cover ownership validation scenarios
- [ ] Admin bypass is only used for actual admins
- [ ] No sensitive data in error messages
- [ ] JWT token includes NameIdentifier and Role claims

---

## Migration Checklist

Updating existing code to use P1-C:

- [ ] Find all calls to `GetPaymentStatusAsync` without userId/isAdmin
- [ ] Find all calls to `GetPaymentHistoryAsync` without userId/isAdmin
- [ ] Add `var userId = User.GetUserIdOrThrow();` before each call
- [ ] Add `var isAdmin = User.IsInRole("Admin");` before each call
- [ ] Pass userId and isAdmin to the methods
- [ ] Update error handling to catch UnauthorizedException
- [ ] Test ownership validation scenarios
- [ ] Deploy to staging, verify functionality
- [ ] Monitor logs for any IDOR attempts

---

**For more details, see P1C_IDOR_PREVENTION_COMPLETE.md**
