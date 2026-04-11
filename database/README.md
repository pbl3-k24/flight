# Database Design — Flight Ticketing System

## Stack

| Concern | Technology |
|---|---|
| Database | **PostgreSQL 16** |
| ORM | **Entity Framework Core 9** (code-first, migrations) |
| Cache / Session | **Redis 7** |
| Local email testing | **MailHog** |

---

## Project Layer Architecture

```
backend/
├── Domain/           Entity classes, enums — no dependencies
├── Application/      Service interfaces, DTOs — depends on Domain only
├── Infrastructure/   EF Core DbContext, configurations, repos — depends on Domain + Application
└── API/              ASP.NET Core entry point — depends on all layers
```

---

## Schema Overview

### 1. User Group

| Table | Purpose |
|---|---|
| `users` | Login credentials, email, status |
| `user_profiles` | Personal info: name, DOB, national ID, passport |
| `roles` | Admin / Staff / Customer |
| `user_roles` | Many-to-many join of users × roles |
| `oauth_accounts` | Google OAuth tokens per user |

### 2. Flight Group

| Table | Purpose |
|---|---|
| `airports` | IATA code, city (15 VN domestic airports seeded) |
| `routes` | Origin → Destination pairs |
| `aircrafts` | Model, seat counts |
| `flights` | Scheduled flights: route, aircraft, departure/arrival times |
| `fare_classes` | Economy / Business rules (baggage, refund, meal) |
| `flight_inventories` | Available / held / sold seats **per flight per fare class** |
| `flight_fare_prices` | Current effective price (auto or admin override) |
| `price_rules` | Auto-pricing engine rules (multiplier, season, day-of-week) |
| `price_override_logs` | Immutable admin price change audit trail |

### 3. Booking Group

| Table | Purpose |
|---|---|
| `bookings` | Master booking record with `expires_at` for hold timeout |
| `passengers` | One row per traveller per booking |
| `booking_items` | One row per **passenger × flight** combination |
| `tickets` | Issued e-ticket, barcode, status |

### 4. Payment Group

| Table | Purpose |
|---|---|
| `payments` | Gateway (VNPay/MoMo/ZaloPay), amount, status, idempotency key |
| `payment_events` | Raw webhook JSON — immutable append-only log |
| `refunds` | Refund request, gateway ref, approval |
| `wallet_ledger` | Double-entry accounting ledger — never updated |

### 5. Notification Group

| Table | Purpose |
|---|---|
| `email_templates` | Handlebars-compatible HTML/text templates |
| `notification_jobs` | Outbox queue with retry logic |

### 6. Infrastructure Group

| Table | Purpose |
|---|---|
| `audit_logs` | Who did what, old/new JSON snapshots |
| `idempotency_keys` | Prevent duplicate payment charges |
| `outbox_events` | Transactional outbox for reliable event publishing |

---

## Key Index Strategy

```sql
-- High-frequency search: flights by route + departure date
ix_flights_route_departure  ON flights(route_id, departure_time)

-- Inventory check for seat availability
ix_flight_inventories_flight_fare  ON flight_inventories(flight_id, fare_class_id)  UNIQUE

-- Booking lookup by code (share with customer)
ix_bookings_booking_code  ON bookings(booking_code)  UNIQUE

-- Expire pending bookings (background job)
ix_bookings_expires_at  ON bookings(expires_at)

-- Payment deduplication
ix_payments_idempotency_key  ON payments(idempotency_key)  UNIQUE  WHERE idempotency_key IS NOT NULL

-- Notification retry queue
ix_notification_jobs_next_retry_at  ON notification_jobs(next_retry_at)

-- Outbox relay pickup
ix_outbox_events_status  ON outbox_events(status)
```

---

## Critical Transaction Boundaries

### Hold Seat (booking creation)
```
BEGIN TRANSACTION
  SELECT ... FROM flight_inventories WHERE flight_id=X AND fare_class_id=Y FOR UPDATE
  UPDATE flight_inventories SET held_seats+=N, available_seats-=N
  INSERT bookings (status=PendingPayment, expires_at=NOW()+15min)
  INSERT passengers, booking_items
  INSERT outbox_events (BookingCreated)
COMMIT
```

### Confirm Payment
```
BEGIN TRANSACTION
  UPDATE payments SET status=Succeeded, paid_at=NOW()
  UPDATE flight_inventories SET held_seats-=N, sold_seats+=N
  INSERT tickets (one per booking_item)
  UPDATE bookings SET status=Confirmed
  INSERT wallet_ledger (Credit entry)
  INSERT notification_jobs (EmailTicketIssued)
  INSERT outbox_events (BookingConfirmed)
COMMIT
```

### Release Expired Holds (background job)
```
BEGIN TRANSACTION
  SELECT * FROM bookings WHERE status='PendingPayment' AND expires_at <= NOW() FOR UPDATE SKIP LOCKED
  UPDATE flight_inventories SET held_seats-=N, available_seats+=N  -- for each booking
  UPDATE bookings SET status=Expired
COMMIT
```

---

## Migration Strategy

### Development
Migrations are applied automatically on startup (`db.Database.MigrateAsync()` in `Program.cs`).

### Staging / Production
Use the idempotent SQL script:
```bash
# Generate
dotnet ef migrations script --idempotent -o database/001_initial_schema.sql \
  --project backend/Infrastructure --startup-project backend/API

# Apply
psql -h $DB_HOST -U $DB_USER -d $DB_NAME -f database/001_initial_schema.sql
```

### Adding new migrations
```bash
dotnet ef migrations add <MigrationName> \
  --project backend/Infrastructure \
  --startup-project backend/API \
  --output-dir Persistence/Migrations
```

---

## Running Locally with Docker

```bash
# From repo root
cd docker
docker compose up -d

# Services:
#   PostgreSQL  → localhost:5432
#   Redis       → localhost:6379
#   MailHog UI  → http://localhost:8025
#   API         → http://localhost:8080
```

---

## Seeded Reference Data

### Airports (15 Vietnamese domestic airports)
| Code | City |
|---|---|
| SGN | Hồ Chí Minh |
| HAN | Hà Nội |
| DAD | Đà Nẵng |
| CXR | Nha Trang |
| PQC | Phú Quốc |
| HPH | Hải Phòng |
| HUI | Huế |
| UIH | Quy Nhơn |
| BMV | Buôn Ma Thuột |
| DLI | Đà Lạt |
| VCL | Tam Kỳ |
| VDH | Đồng Hới |
| VII | Vinh |
| VCA | Cần Thơ |
| VKG | Rạch Giá |

### Fare Classes
| Code | Name |
|---|---|
| Economy | Phổ thông — 23kg baggage, refund fee 30%, date change 15% |
| Business | Thương gia — 40kg baggage, meal, refund fee 10%, date change 5% |

### Roles
`Admin`, `Staff`, `Customer`

---

## 8-Week Rollout Plan

| Week | Deliverable |
|---|---|
| 1 | Project scaffold, Docker, CI/CD, Auth (register/login/OTP/Google OAuth) |
| 2 | Admin CRUD: airport, route, aircraft, flight, fare class, inventory |
| 3 | Search API + auto-pricing engine + admin price override |
| 4 | Booking flow: hold seats, multi-passenger, expiry cleanup job |
| 5 | Payment gateway integration (VNPay/MoMo) + webhook + idempotency |
| 6 | Refund engine + policy rules + admin approval workflow |
| 7 | Email notifications: OTP, booking confirmation, flight change, refund status |
| 8 | Load testing, security hardening, monitoring (Prometheus/Grafana), UAT |
