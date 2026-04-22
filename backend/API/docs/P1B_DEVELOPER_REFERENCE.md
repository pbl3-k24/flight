# P1-B Idempotency - Developer Reference Guide

## Quick Overview

**What is idempotency?**
An operation is idempotent if calling it multiple times with the same input produces the same result as calling it once. Repeated calls don't cause side effects.

**Example**:
- ❌ Non-idempotent: `payment.count++` (increments each time, wrong result on retry)
- ✅ Idempotent: `payment.status = COMPLETED` (same result on retry)

---

## How Idempotency Works in P1-B

### Single Callback (Normal)
```
User initiates payment
↓
Payment provider processes → Returns "success"
↓
POST /payments/{id}/callback (success)
↓
Service checks: status = Pending? YES
↓
Process payment (mutate state)
├─ Update payment to Completed
├─ Confirm seats
├─ Confirm booking
└─ Send email
↓
Return 200 OK ✅
```

### Duplicate Callback (Idempotent)
```
Payment provider retries (same callback)
↓
POST /payments/{id}/callback (same success)
↓
Service checks: status = Pending? NO (already Completed)
↓
Return 200 OK WITHOUT mutation ✅
├─ No database updates
├─ No state changes
├─ No duplicate email
```

---

## Payment Status Values

| Status | Value | Meaning | Next Request Behavior |
|--------|-------|---------|----------------------|
| Pending | 0 | Not processed yet | Process normally |
| Completed | 1 | Payment succeeded | Idempotent (skip) |
| Failed | 2 | Payment failed | Idempotent (skip) |
| Refunded | 3 | Payment refunded | Idempotent (skip) |

---

## Code Flow

### ProcessPaymentAsync Implementation

```csharp
public async Task<bool> ProcessPaymentAsync(int paymentId, PaymentCallbackDto callback)
{
    var payment = await _paymentRepository.GetByIdAsync(paymentId);

    // ─────────────────────────────────────────────────
    // IDEMPOTENCY CHECK (NEW in P1-B)
    // ─────────────────────────────────────────────────
    if (payment.Status != 0) // Not Pending
    {
        // Payment already processed - skip processing
        _logger.LogInformation(
            "Duplicate/Repeated callback for payment {PaymentId}: " +
            "already in status {Status}. " +
            "Returning success without state mutation (idempotent)",
            paymentId, statusString);

        return payment.Status == 1 || payment.Status == 3; // Success for Completed/Refunded
    }

    // ─────────────────────────────────────────────────
    // NORMAL PROCESSING (existing code)
    // ─────────────────────────────────────────────────

    // Validate callback
    var validationResult = await ValidatePaymentCallbackAsync(payment, callback);
    if (!validationResult.IsValid)
    {
        payment.Status = 2; // Failed
        await _paymentRepository.UpdateAsync(payment);
        return false;
    }

    // Process based on callback status
    if (callback.Status.ToLower() != "success")
    {
        // Release seats...
        payment.Status = 2; // Failed
        await _paymentRepository.UpdateAsync(payment);
        return false;
    }

    // Mark as completed
    payment.Status = 1; // Completed
    payment.PaidAt = DateTime.UtcNow;
    await _paymentRepository.UpdateAsync(payment);

    // Update related entities (booking, seats)...

    return true;
}
```

---

## Idempotency Guard

The idempotency check uses the payment status as the "guard":

```csharp
// Guard: If status is not Pending, skip processing
if (payment.Status != 0)
{
    _logger.LogInformation("Duplicate callback... returning idempotent result");
    return payment.Status == 1 || payment.Status == 3;
}
```

**Benefits of this approach**:
- ✅ Simple - single status check
- ✅ Fast - no extra database queries
- ✅ Reliable - status is source of truth
- ✅ Complete - covers all callback states

---

## Scenarios

### Scenario 1: Happy Path (First Callback)

```
Timeline:
[T=0]   Callback arrives (success)
        └─ Status check: Pending → Process
        └─ Update payment: status = Completed
        └─ Confirm seats: Held → Sold
        └─ Confirm booking: Pending → Confirmed
        └─ Send email: ✓
        └─ Return true ✅

[T=5]   Provider retries same callback
        └─ Status check: Completed → Skip
        └─ Log: "Duplicate callback"
        └─ Return true ✅ (idempotent)

[T=10]  Provider retries again
        └─ Status check: Completed → Skip
        └─ Log: "Duplicate callback"
        └─ Return true ✅ (still idempotent)
```

### Scenario 2: Failed Payment (First Callback)

```
Timeline:
[T=0]   Callback arrives (failed)
        └─ Status check: Pending → Process
        └─ Update payment: status = Failed
        └─ Release seats: Sold → Available
        └─ Log: "Payment failed"
        └─ Return false ❌

[T=5]   Provider retries same callback
        └─ Status check: Failed → Skip
        └─ Log: "Duplicate callback"
        └─ Return false ❌ (idempotent)
```

### Scenario 3: Validation Failure (First Callback)

```
Timeline:
[T=0]   Callback arrives (invalid signature)
        └─ Status check: Pending → Process
        └─ Validation: FAIL
        └─ Update payment: status = Failed
        └─ Log: "Validation failed"
        └─ Return false ❌

[T=5]   Provider retries same callback
        └─ Status check: Failed → Skip
        └─ Log: "Duplicate callback"
        └─ Return false ❌ (idempotent)
```

---

## Testing Idempotency

### Unit Test Pattern

```csharp
[Fact]
public async Task ProcessPaymentAsync_FirstCallback_ProcessesPayment()
{
    // Arrange
    var paymentId = 1;
    var payment = new Payment { Id = 1, Status = 0 }; // Pending
    var callback = new PaymentCallbackDto { Status = "success" };

    _paymentRepository.Setup(x => x.GetByIdAsync(paymentId))
        .ReturnsAsync(payment);

    // Act
    var result = await _paymentService.ProcessPaymentAsync(paymentId, callback);

    // Assert
    Assert.True(result);
    _paymentRepository.Verify(x => x.UpdateAsync(payment), Times.Once);
}

[Fact]
public async Task ProcessPaymentAsync_DuplicateCallback_SkipsProcessing()
{
    // Arrange
    var paymentId = 1;
    var payment = new Payment { Id = 1, Status = 1 }; // Completed
    var callback = new PaymentCallbackDto { Status = "success" };

    _paymentRepository.Setup(x => x.GetByIdAsync(paymentId))
        .ReturnsAsync(payment);

    // Act
    var result = await _paymentService.ProcessPaymentAsync(paymentId, callback);

    // Assert
    Assert.True(result);  // Returns true (idempotent)
    _paymentRepository.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Never);
    // No state updates!
}

[Fact]
public async Task ProcessPaymentAsync_TripleCallback_StillIdempotent()
{
    // Arrange
    var paymentId = 1;
    var payment = new Payment { Id = 1, Status = 1 }; // Completed
    var callback = new PaymentCallbackDto { Status = "success" };

    _paymentRepository.Setup(x => x.GetByIdAsync(paymentId))
        .ReturnsAsync(payment);

    // Act - Call multiple times
    var result1 = await _paymentService.ProcessPaymentAsync(paymentId, callback);
    var result2 = await _paymentService.ProcessPaymentAsync(paymentId, callback);
    var result3 = await _paymentService.ProcessPaymentAsync(paymentId, callback);

    // Assert - All return true, no updates
    Assert.True(result1);
    Assert.True(result2);
    Assert.True(result3);
    _paymentRepository.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Never);
}

[Fact]
public async Task ProcessPaymentAsync_FailedPayment_IsIdempotent()
{
    // Arrange
    var paymentId = 1;
    var payment = new Payment { Id = 1, Status = 2 }; // Failed
    var callback = new PaymentCallbackDto { Status = "failed" };

    _paymentRepository.Setup(x => x.GetByIdAsync(paymentId))
        .ReturnsAsync(payment);

    // Act
    var result = await _paymentService.ProcessPaymentAsync(paymentId, callback);

    // Assert
    Assert.False(result);  // Returns false (idempotent)
    _paymentRepository.Verify(x => x.UpdateAsync(It.IsAny<Payment>()), Times.Never);
}
```

---

## Integration Testing

### Manual Test: Replay Webhook

```bash
# First callback (new)
curl -X POST http://localhost/api/v1/payments/100/callback \
  -H "Content-Type: application/json" \
  -d '{"transactionId":"TXN123","status":"success","amount":5000000}'

Response: HTTP 200 OK
Payment status in DB: 1 (Completed)

# Duplicate callback (replay same)
curl -X POST http://localhost/api/v1/payments/100/callback \
  -H "Content-Type: application/json" \
  -d '{"transactionId":"TXN123","status":"success","amount":5000000}'

Response: HTTP 200 OK (no error)
Payment status in DB: Still 1 (Completed, unchanged)
Log: "Duplicate/Repeated callback... already in status Completed"
```

### Log Verification

```
First callback:
[INFO] Payment processed successfully: 100 for booking 50
[INFO] Confirmed 2 held seats as sold for booking 50

Duplicate callback:
[INFO] Duplicate/Repeated callback for payment 100: 
       already in status Completed. 
       Returning success without state mutation (idempotent)
```

---

## Common Issues & Solutions

### Issue: What if payment is stuck in Pending?

**Scenario**: First callback failed midway (exception), payment still Pending

**Solution**: 
- Next callback will check: Status = Pending → Process again
- This allows retry mechanism to work (exactly-once-or-more → at-least-once with idempotency)
- Payment will eventually complete when callback succeeds

```csharp
// First attempt: Exception mid-processing
try
{
    if (payment.Status != 0) return ... // Skip if not Pending

    // ... process ...
    payment.Status = 1;
    await _paymentRepository.UpdateAsync(payment); // Maybe throws here
}
catch
{
    // Payment status STILL 0 (Pending)!
    return false;
}

// Second attempt (on retry)
{
    if (payment.Status != 0) return ... // Still 0, so process again
    // ... try processing again ...
}
```

### Issue: What if payment.Status has invalid value?

**Scenario**: Database corruption, payment.Status = 99

**Solution**: Return true (acknowledge callback, don't retry)

```csharp
// Code handles it:
return payment.Status == 1 || payment.Status == 3 || payment.Status == 0;
// 99 → returns false, but log warns about invalid status

// Better to return true and log:
if (payment.Status == 0)
{
    // Process normally
}
else if (payment.Status == 1 || payment.Status == 3)
{
    // Already completed/refunded - idempotent success
    return true;
}
else
{
    _logger.LogError("Invalid payment status: {Status}", payment.Status);
    return true; // Acknowledge callback anyway
}
```

### Issue: Seat inventory corrupted due to double release?

**Scenario**: Duplicate failure callback released seats twice

**Solution**: P1-B prevents this!

```
First failure callback:
├─ Status: Pending → Fail
├─ Release seats: 2 Held → Available
├─ Status update: 0 → 2

Duplicate failure callback:
├─ Status check: 2 (Failed) → Skip
├─ NO seat release!
└─ Seats remain: Available (correct)
```

---

## Best Practices

### DO ✅

- ✅ Always use payment.Status as the idempotency guard
- ✅ Log all duplicate callbacks for monitoring
- ✅ Return success for idempotent operations
- ✅ Test with duplicate callbacks in integration tests
- ✅ Monitor logs for unusual callback patterns

### DON'T ❌

- ❌ Don't check timestamp to detect duplicates (time-based checks fail)
- ❌ Don't rely on transaction ID alone (provider might send different IDs)
- ❌ Don't skip validation to "speed up" idempotent handling
- ❌ Don't mutate state after idempotency check
- ❌ Don't send confirmation email on duplicate callback

---

## Monitoring & Alerts

### Key Metrics to Track

```csharp
// In logs, look for this pattern
"Duplicate/Repeated callback for payment {PaymentId}: already in status {Status}"

// Count these occurrences
// Expected: 0-10% of callbacks (normal provider retries)
// Unusual: >50% of callbacks (indicates webhook infrastructure issue)
```

### Alert Conditions

```
IF (duplicate_callbacks > 50%) THEN
  → Alert: High callback retry rate
  → Investigate: Payment provider delivery issues
  THEN
  → Enable monitoring of seats availability
  → Check for race conditions
```

---

## Deployment Checklist

- [x] Code implements idempotency check
- [x] Check occurs BEFORE validation
- [x] All payment statuses handled
- [x] Logging added for duplicates
- [x] Build successful
- [ ] Deploy to staging
- [ ] Test with duplicate callbacks
- [ ] Monitor logs for idempotent hits
- [ ] Verify no seat inventory issues
- [ ] Deploy to production

---

## FAQ

**Q: What if the payment provider sends different data each time?**
A: The idempotency check is at the callback handler level. The validation and processing logic inside doesn't run for duplicate callbacks, so data differences don't matter.

**Q: How does this relate to transaction atomicity (P0-D)?**
A: They work together:
- P0-D: Ensures a single callback processes atomically (all-or-nothing)
- P1-B: Ensures duplicate callbacks don't mutate state (idempotent)

**Q: What if I need to update idempotency logic?**
A: Change the status check condition. Current: `if (payment.Status != 0)`. You could add more conditions:
```csharp
if (payment.Status != 0 && !NeedToRetry(payment, callback))
{
    return true; // Idempotent
}
```

**Q: Can I disable idempotency?**
A: Not recommended. Leave it enabled for production. You can add a feature flag if needed:
```csharp
if (_config.EnableIdempotency && payment.Status != 0)
{
    return true; // Idempotent
}
```

**Q: How do I test this locally?**
A: Replay the same webhook:
```bash
# First time
curl ... POST /payments/100/callback → Returns 200, updates DB

# Second time (same data)
curl ... POST /payments/100/callback → Returns 200, DB unchanged
```

---

## Summary

P1-B implements idempotency by:

1. **Checking payment status early** - Skip if not Pending
2. **Preventing mutations** - No updates after idempotency check
3. **Logging duplicates** - Track all repeated callbacks
4. **Maintaining guarantees** - Same result on retry

Result: Payment callbacks are safe to retry without side effects.

---

**For detailed information, see P1B_IDEMPOTENCY_COMPLETE.md**
