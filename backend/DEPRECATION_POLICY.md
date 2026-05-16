# API Deprecation Policy

## Effective date
- Policy published: 2026-05-16

## Goals
- Avoid breaking existing clients immediately.
- Provide explicit migration path for every deprecated route.
- Remove non-business/debug endpoints from runtime contract.

## Deprecation headers
Deprecated endpoints must return:
- `Deprecation: true`
- `Sunset: <RFC1123 date>`
- `Link: <new-route>; rel="successor-version"`
- `Warning: 299 - "Deprecated API route. Please migrate to successor-version."`

## Deprecation timeline

### Wave 1 (Public route normalization)
- Deprecated:
  - `/api/AdditionalServices`
  - `/api/AdditionalServices/by-seat-class/{seatClassId}`
- Successor:
  - `/api/v1/additional-services`
  - `/api/v1/additional-services/by-seat-class/{seatClassId}`
- Sunset date: `2026-06-30`
- Action after sunset: remove legacy route alias and keep only `/api/v1/...`.

### Wave 2 (Debug/API diagnostics)
- Deprecated:
  - `/api/v1/debug/*`
- Successor:
  - No direct public successor. Use admin/business APIs where applicable.
- Sunset date: `2026-06-15`
- Action after sunset: remove `DebugController` from runtime.

### Wave 3 (Operational/test/migration routes)
- Routes:
  - `/api/admin/database/*`
  - `/api/admin/migration/*`
  - `/api/debug/routes/*`
  - `/api/test-data/*`
  - `/api/test/vnpay/*`
- Status:
  - Not part of public API contract.
  - Hidden from Swagger and blocked outside permitted environments.
- Final target:
  - Remove from main runtime app, move to internal tooling/scripts.

## Client migration checklist
1. Replace all calls to `/api/AdditionalServices*` with `/api/v1/additional-services*`.
2. Remove dependencies on `/api/v1/debug/*`.
3. Ensure no production client calls `/api/admin/*`, `/api/debug/*`, `/api/test*`.

## Release checklist for removal
1. Confirm no calls in logs for deprecated routes over 7 consecutive days.
2. Remove route aliases/controllers.
3. Regenerate OpenAPI.
4. Update Bruno/Postman collections.
