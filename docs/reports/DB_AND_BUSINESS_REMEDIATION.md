# DB and Business Remediation

## 1) Database issue groups and implemented fixes

- **Migration/Schema compatibility**
  - Replaced SQL Server default timestamp SQL (`GETUTCDATE()`) with PostgreSQL-compatible UTC SQL (`TIMEZONE('UTC', NOW())`) across DbContext/configurations.
- **Data constraints**
  - Added check constraints:
    - `Flights`: seat bounds, positive seats, valid route, valid time order, non-negative price.
    - `Bookings`: positive passenger count, non-negative total price.
    - `Payments`: positive amount.
- **Transaction consistency**
  - Standardized booking transaction contract to `IDbContextTransaction`.
  - Enforced explicit `CommitAsync`/`RollbackAsync` in booking create/cancel flows.
- **Relationship consistency**
  - Standardized booking-payment delete behavior to `Restrict` to avoid accidental payment history loss.

## 2) Core business capabilities vs current implementation

- **Booking**
  - Create booking: implemented.
  - List bookings with pagination: implemented (was previously placeholder).
  - Booking cancel/refund trigger: implemented.
  - Check-in: implemented (requires confirmed status).
- **Ticketing**
  - Booking reference generation and passenger records: implemented.
- **Payment**
  - Refund amount policy and refund trigger service: implemented at application service layer.
- **Schedule**
  - Flight search and seat inventory updates: implemented.
- **Cancel/Refund**
  - Cancellation updates booking status, cancellation timestamp, and restores seats in transaction.

## 3) Priority remediation mapping (impact-first)

1. **P0 - Data correctness / financial impact**
   - DB compatibility defaults, check constraints, transaction commit/rollback.
2. **P1 - Core business continuity**
   - Missing booking list behavior, missing repositories/services causing runtime DI failure.
3. **P2 - Operational hardening**
   - Release checklist and deployment safety checks.

## 4) Release checklist (migration-safe)

- [ ] Backup database before release.
- [ ] Review generated EF migration SQL in staging first.
- [ ] Apply migration in maintenance window.
- [ ] Verify check constraints created successfully.
- [ ] Run smoke tests for booking create/cancel/check-in/search.
- [ ] Verify post-deploy data integrity:
  - [ ] `AvailableSeats` never `< 0` or `> TotalSeats`.
  - [ ] `DepartureTime < ArrivalTime`.
  - [ ] `Booking.PassengerCount > 0`.
  - [ ] `Payment.Amount > 0`.
- [ ] Verify rollback procedure for migration and app deployment.
