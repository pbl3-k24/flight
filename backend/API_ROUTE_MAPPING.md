# API Route Mapping (Phase A Audit)

## 1) Scope
- Source scanned: `API/Controllers/**/*.cs`
- Goal: normalize all business APIs to `api/v1/...`
- Date: 2026-05-16

## 2) Current Route Groups

### A. Already aligned with `api/v1/...` (keep)
- Public business APIs:
  - `Bookings`, `Flights`, `Payments`, `Tickets`, `Refunds`, `Search`, `Reports`, `Notifications`, `Users`
  - Nested booking service API: `api/v1/bookings/{bookingId}/passengers/{passengerId}/services`
- Admin business APIs:
  - `FlightsAdmin`, `BookingsAdmin`, `UsersAdmin`, `PromotionsAdmin`, `Dashboard`, `RealtimeDashboard`
  - `flight-definitions`, `flight-templates`, `aircraft`, `airports`, `admin/additional-services`

### B. Not aligned (normalize)
- `AdditionalServicesController`
  - Current: `api/AdditionalServices`
  - Target: `api/v1/additional-services`
  - Status: `normalize`

### C. Non-business operational/debug endpoints (deprecate/remove from main runtime)
- `DatabaseFixController`
  - Current: `api/admin/database/*`
  - Target: move to internal-only tool or remove from app runtime
  - Status: `remove`
- `DatabaseResetController`
  - Current: `api/admin/database/*`
  - Target: move to internal-only tool or remove from app runtime
  - Status: `remove`
- `MigrationController`
  - Current: `api/admin/migration/*`
  - Target: move to migration script/CLI pipeline
  - Status: `remove`
- `RoutesDebugController`
  - Current: `api/debug/routes/*`
  - Target: remove (or internal diagnostics app)
  - Status: `remove`
- `TestDataController`
  - Current: `api/test-data/*`
  - Target: remove from runtime, keep in test tooling
  - Status: `remove`
- `TestVnpayController`
  - Current: `api/test/vnpay/*`
  - Target: remove from runtime, keep integration sandbox tool
  - Status: `remove`
- `DebugController`
  - Current: `api/v1/debug/*`
  - Target: remove from runtime (or gated internal env only)
  - Status: `deprecate -> remove`

## 3) Old -> New Mapping

| Old route | New route | Action |
|---|---|---|
| `GET /api/AdditionalServices` | `GET /api/v1/additional-services` | normalize |
| `GET /api/AdditionalServices/by-seat-class/{seatClassId}` | `GET /api/v1/additional-services/by-seat-class/{seatClassId}` | normalize |
| `POST /api/admin/database/fix-payment-constraint` | n/a (ops script) | remove |
| `DELETE /api/admin/database/delete-booking-payments/{bookingId}` | n/a (ops script) | remove |
| `GET /api/admin/database/check-payment-indexes` | n/a (ops script) | remove |
| `POST /api/admin/database/reset` | n/a (ops script) | remove |
| `POST /api/admin/database/clear-data` | n/a (ops script) | remove |
| `POST /api/admin/database/reseed` | n/a (ops script) | remove |
| `GET /api/admin/database/stats` | n/a (ops script) | remove |
| `POST /api/admin/migration/*` | n/a (CI/CD migration pipeline) | remove |
| `GET /api/admin/migration/*` | n/a (CI/CD migration pipeline) | remove |
| `GET /api/debug/routes/*` | n/a | remove |
| `POST /api/debug/routes/create-all-routes` | n/a | remove |
| `POST /api/test-data/*` | n/a | remove |
| `GET /api/test-data/*` | n/a | remove |
| `GET /api/test/vnpay/test-url` | n/a | remove |
| `GET /api/v1/debug/*` | n/a | deprecate then remove |

## 4) Conflict / Consistency Notes
- Prefix inconsistency exists today: `api/`, `api/v1/`, `api/admin/`, `api/debug/`, `api/test/`.
- `api/admin/*` currently overlaps concerns with `api/v1/admin/*`; keep only one standard (`api/v1/admin/*` for business APIs).
- Operational endpoints should not be part of customer-facing runtime API contract.

## 5) Proposed Execution Order (Phase B input)
1. Normalize `AdditionalServicesController` to `api/v1/additional-services`.
2. Add deprecation headers for `api/v1/debug/*` and schedule removal.
3. Remove or conditionally compile all `api/admin/*`, `api/debug/*`, `api/test/*` controllers from production runtime.
4. Regenerate `swagger.json` and update Bruno/Postman collections.
