# 🔌 Module 4: Infrastructure Layer (Data Access) Guide

## 📋 Mục Đích
Quản lý database, repositories, caching, external service integrations, và tất cả implementation chi tiết.

---

## 🏗️ Kiến Trúc

```
Application Layer
    ↓ (calls interfaces)
[Repository Interfaces, Cache Interfaces, etc.]
    ↓
Infrastructure Layer
├─ Data Access (EF Core, Repositories)
├─ Caching (Redis)
├─ External Services (Email, Payment, SMS)
└─ Configuration (DB Context Setup)
    ↓
External Systems (Database, Cache, APIs)
```

---

## 🗄️ Repository Pattern

### 1. **Repository Purpose**
```
Abstract data access logic
├─ Create: Add new entities
├─ Read: Retrieve entities by various criteria
├─ Update: Modify existing entities
├─ Delete: Remove entities
└─ Query: Complex filtering, sorting, pagination
```

### 2. **Repository Interface Pattern**
```
IRepository<T> (Generic base interface)
  ├─ GetByIdAsync(id)
  ├─ GetAllAsync()
  ├─ GetPagedAsync(page, pageSize)
  ├─ AddAsync(entity)
  ├─ UpdateAsync(entity)
  ├─ DeleteAsync(entity)
  └─ SaveChangesAsync()

ISpecificRepository (Domain-specific)
  ├─ GetAvailableAsync(criteria)
  ├─ GetByReferenceAsync(reference)
  ├─ GetWithRelatedDataAsync(id)
  └─ QueryComplexFiltersAsync(filters)

Principle: Interface per aggregate root
```

### 3. **Query Algorithm Patterns**

**Pattern: Simple ID Lookup**
```
Algorithm:
  1. Receive ID
  2. Query: WHERE id = @id
  3. If found → return entity
  4. If not found → return null
  5. Caller checks null and throws exception if needed

Optimization:
  - Index on Id column
  - Cache if frequently accessed
  - Use AsNoTracking() for read-only
```

**Pattern: Complex Search**
```
Input: SearchCriteria (departure, arrival, date, filters)

Algorithm:
  1. Start with base query: SELECT * FROM flights
  2. Build dynamic WHERE clauses:
     a. if (criteria.DepartureAirportId) 
        → AND departure_airport_id = @id
     b. if (criteria.ArrivalAirportId) 
        → AND arrival_airport_id = @id
     c. if (criteria.DepartureDate) 
        → AND DATE(departure_time) = @date
     d. if (criteria.Status) 
        → AND status = @status
  3. Apply sorting: ORDER BY departure_time ASC
  4. Apply pagination:
     OFFSET @skip ROWS
     FETCH NEXT @take ROWS ONLY
  5. Execute query
  6. Return results

Optimization:
  - Composite index: (departure_id, arrival_id, departure_time)
  - Filter before paging
  - Avoid N+1 queries with Include()
```

**Pattern: Get With Related Data**
```
Input: EntityId

Algorithm:
  1. Query entity with Include() for relationships:
     FROM flights
     INCLUDE bookings
     INCLUDE crew
     WHERE id = @id
  2. Alternative: Multiple queries if relationships are large
  3. Return fully loaded entity

Tradeoff:
  - Single query: Simpler, but might load unnecessary data
  - Multiple queries: Flexible, but more network calls
  - Use Include() for small collections (< 100 items)
  - Use separate queries for large collections
```

**Pattern: Pagination**
```
Input: Page = 2, PageSize = 10

Algorithm:
  1. Calculate skip: (Page - 1) × PageSize = 10
  2. Query:
     SELECT * FROM flights
     WHERE (conditions)
     ORDER BY departure_time
     OFFSET @skip ROWS
     FETCH NEXT @pageSize ROWS ONLY
  3. Separately get total count:
     SELECT COUNT(*) FROM flights
     WHERE (conditions)
  4. Calculate:
     TotalPages = ceil(TotalCount / PageSize)
     HasNext = Page < TotalPages
     HasPrev = Page > 1
  5. Return: Items + TotalCount + PageInfo

Optimization:
  - Don't fetch all records before paging
  - Separate counting query (more efficient)
  - Cache total count if data doesn't change often
```

**Pattern: Batch Operations**
```
Input: List of entities to insert/update

Algorithm:
  1. Split into chunks (e.g., 1000 per batch)
  2. For each chunk:
     a. context.AddRange(chunk)
     b. await context.SaveChangesAsync()
     c. Clear change tracker: context.ChangeTracker.Clear()
  3. Continue next chunk

Benefits:
  - Memory efficient
  - Avoids command timeout on huge batches
  - Rollback per batch, not all-or-nothing

Example:
  const int batchSize = 1000;
  for (int i = 0; i < entities.Count; i += batchSize)
  {
    var batch = entities.Skip(i).Take(batchSize);
    context.AddRange(batch);
    await context.SaveChangesAsync();
    context.ChangeTracker.Clear();
  }
```

---

## 💾 Database Strategy

### 1. **DbContext Design**
```
FlightBookingDbContext (represents database)
├─ DbSet<Flight> Flights
├─ DbSet<Booking> Bookings
├─ DbSet<User> Users
├─ DbSet<Payment> Payments
├─ DbSet<Passenger> Passengers
├─ DbSet<Airport> Airports
└─ DbSet<Crew> Crews

Responsibilities:
  - Define entity mappings
  - Configure relationships
  - Apply data annotations
  - Handle migrations
  - Provide SaveChangesAsync()
```

### 2. **Entity Configuration**
```
IEntityTypeConfiguration<T> (per entity)

ConfigureEntity:
  ├─ Primary Key
  ├─ Properties (length, precision, required)
  ├─ Relationships (foreign keys, navigation)
  ├─ Indexes
  ├─ Constraints (unique, check)
  └─ Default Values

Example for Flight:
  ├─ Primary Key: Id
  ├─ Properties:
  │   ├─ FlightNumber: MaxLength(20), Unique Index
  │   ├─ BasePrice: Precision(18,2)
  │   └─ Status: Default = Active
  ├─ Relationships:
  │   ├─ DepartureAirport: FK, Restrict Delete
  │   ├─ ArrivalAirport: FK, Restrict Delete
  │   └─ Bookings: Cascade Delete
  └─ Indexes:
      └─ Composite: (departure_id, arrival_id, departure_time)
```

### 3. **Migrations Strategy**
```
Migration = Database schema change

Workflow:
  1. Add/Modify entity
  2. Create migration:
     dotnet ef migrations add MeaningfulName
  3. Review generated migration code
  4. Apply migration:
     dotnet ef database update
  5. Commit migration file to git

Naming Convention:
  ✓ AddFlightEntity
  ✓ AddUniqueConstraintFlightNumber
  ✓ CreateBookingTable
  ✓ AlterFlightAddPriceColumn

What to include:
  ✓ Table creation/deletion
  ✓ Column addition/removal
  ✓ Type changes
  ✓ Constraint additions
  ✓ Index creation

What to avoid:
  ✗ Data manipulation in migrations
  ✗ Dropping tables without backup
  ✗ Renaming columns (use create + copy + drop)
```

---

## 💾 Caching Strategy (Redis)

### 1. **Cache Pattern Flow**
```
Service wants data:
  1. Check cache with key
     ├─ Hit (data found) → return immediately
     └─ Miss (not found) → continue
  2. Query database/service
  3. Store result in cache with TTL
  4. Return result

Key Insight:
  - Cache reduces database load
  - TTL prevents stale data
  - Miss = normal, just fetch and cache
  - Hit = fast response
```

### 2. **Cache Key Strategy**
```
Design:
  - Key format: {entity}_{id}_{variant}
  - Examples:
    flight_123
    flight_search_1_2_2024-04-12 (depart_arrive_date)
    user_bookings_456 (userId)
    exchange_rates_usd_eur

Patterns:
  - Use prefix for entity type
  - Include identifiers
  - Include variant info (filters)
  - Keep keys short but clear
  - Avoid spaces and special chars

Namespacing:
  flight:123
  flight:search:1:2:2024-04-12
  user:456:bookings
  payment:789
```

### 3. **Cache TTL Strategy**
```
Data Type → TTL Decision

Static Data:
  - Airports, airlines, routes → 1 day
  - Exchange rates → 1 hour
  - Configuration → 1 day

Frequently Changing:
  - Flight availability → 15-30 minutes
  - Seat counts → 15 minutes
  - Pricing → 15 minutes

User-Specific:
  - User bookings → 30 minutes
  - User profile → 1 hour

Search Results:
  - Flight search → 30 minutes
  - Complex queries → 15 minutes

Rules:
  - Static = longer TTL
  - Real-time data = shorter TTL
  - User data = moderate TTL
  - High-traffic = aggressive caching
```

### 4. **Cache Invalidation**
```
Strategy 1: Time-based (TTL expires)
  - Automatic after TTL
  - No code needed
  - Eventual consistency (temporary stale data)

Strategy 2: Event-based (on updates)
  - Immediate invalidation after change
  - Remove key: await cache.RemoveAsync(key)
  - Strong consistency
  - More code logic

Strategy 3: Pattern-based (partial invalidation)
  - Remove keys matching pattern
  - Example: Remove all "flight:*" after flight update
  - Use SCAN in Redis to find matching keys

Strategy 4: Lazy (on read)
  - Check if data is stale
  - Refetch if stale
  - Keep serving while updating background

Best Approach:
  - Combine TTL + Event-based
  - Auto-expire for safety
  - Immediate remove on critical updates
  - Accept some staleness for non-critical data
```

### 5. **Cache Patterns**

**Pattern: Cache-Aside (Most Common)**
```
On Read:
  1. Check cache
  2. If miss → fetch from database
  3. Store in cache
  4. Return

On Write:
  1. Update database
  2. Remove from cache (or update if small)
  3. Next read will refresh cache

Algorithm:
  var key = "flight_" + id;
  var cached = await cache.GetAsync(key);
  if (cached != null) return cached;
  
  var flight = await db.GetFlightAsync(id);
  await cache.SetAsync(key, flight, TTL=1h);
  return flight;
```

**Pattern: Cache-Through**
```
Application always goes through cache
Cache responsible for fetching from DB
More transparent but more complex

Less common in ASP.NET, usually for caching layer
```

**Pattern: Write-Behind (Write-Through)**
```
Write to cache first
Asynchronously write to database
Fast writes, eventual consistency

Risky: data loss if cache fails before DB write
```

---

## 🔗 External Service Integration

### 1. **Email Service**
```
Trigger: Booking confirmation, cancellation, etc.

Flow:
  1. Service composes email content
  2. Queue message to message broker (RabbitMQ, Azure Service Bus)
  3. Background worker picks up message
  4. Calls email provider (SendGrid, AWS SES, etc.)
  5. Retry if fails (3 attempts)
  6. Log result

Considerations:
  - Async/background processing (don't wait)
  - Retry logic (transient failures)
  - Template management
  - Rate limiting (provider limits)
  - Bounce handling
```

### 2. **Payment Gateway**
```
Trigger: Booking checkout

Flow:
  1. Get payment details from user
  2. Call payment provider API with:
     - Amount
     - Currency
     - Card details or payment method ID
     - Idempotency key (prevent duplicates)
     - Booking reference
  3. Gateway returns:
     - Success/Failure status
     - Transaction ID
     - Error code (if failed)
  4. Update payment record with status
  5. If success:
     - Update booking status → Confirmed
     - Trigger success notifications
  6. If failed:
     - Keep payment as Failed
     - Allow user to retry

Error Handling:
  - Network timeout → Retry
  - Validation error → Return error to user
  - Fraud detection → Block and notify
  - Idempotent: Resubmit same request = same result

Webhook Pattern:
  - Gateway sends async confirmation
  - Listen for payment.completed webhook
  - Verify webhook signature
  - Update booking status on webhook
  - Handle late confirmations
```

### 3. **SMS Service**
```
Trigger: OTP, flight reminders, booking confirmation

Flow:
  1. Queue message
  2. Background worker calls SMS provider
  3. Track delivery status
  4. Retry on failure

Optimization:
  - Batch messages for volume discounts
  - Respect time zones (don't send midnight)
  - Respect user preferences
```

### 4. **External APIs (Flights Data, Currency, etc.)**
```
Integration Pattern:
  1. Call API with timeout (30 seconds)
  2. Retry on transient errors (3 attempts)
  3. Cache response with TTL
  4. Fallback to stale cache on failure
  5. Log errors

Caching:
  - Fresh data TTL: 1 hour
  - Stale cache fallback: 24 hours
  - Reduce API calls dramatically

Circuit Breaker Pattern:
  - If API fails repeatedly → Stop calling
  - Return cached data or error message
  - Periodically retry (every 5 minutes)
  - Resume when API recovers
```

---

## 🔐 Transaction Management

### 1. **Transaction Scope**
```
Multi-step operations that must succeed together:

Booking Creation:
  1. Insert Booking record
  2. Insert Passenger records
  3. Update Flight.available_seats
  4. Commit all
  5. If any fails → Rollback all

Using(transaction):
  try {
    await bookingRepo.AddAsync(booking);
    await passengerRepo.AddAsync(passengers);
    await flightRepo.UpdateAsync(flight);
    transaction.Commit();
  } catch {
    transaction.Rollback();
    throw;
  }
```

### 2. **Isolation Levels**
```
Read Uncommitted (dirty reads possible):
  - Rarely used, data integrity risk

Read Committed (default):
  - Don't read uncommitted changes
  - Phantom reads possible
  - Good balance for most cases

Repeatable Read:
  - Same read = same data
  - More locking
  - Slower

Serializable (strongest):
  - Complete isolation
  - Like operations running sequentially
  - Most locking, slowest
  - Use only when necessary
```

---

## 🔍 Query Optimization

### 1. **Index Strategy**
```
Create Indexes On:
  - Primary keys (automatic)
  - Foreign keys
  - Frequently filtered columns
  - Frequently sorted columns
  - Columns in WHERE clauses

Composite Index:
  - Index multiple columns together
  - Order matters (most specific first)
  - Example: (departure_id, arrival_id, departure_time)

Avoid Over-indexing:
  - Each index slows writes
  - Indexes consume disk space
  - Maintain when data changes
```

### 2. **N+1 Query Problem**
```
❌ Bad: Causes N+1 queries
foreach(var flight in flights)
{
  flight.Bookings = db.Bookings.Where(b => b.FlightId == flight.Id);
}
// 1 query for flights + N queries for each flight's bookings

✓ Good: Single query with Include
var flights = db.Flights.Include(f => f.Bookings).ToList();
// 1 query joins flights with bookings
```

### 3. **Query Execution Plan**
```
When query runs slow:
  1. Check actual execution plan (SQL Server Management Studio)
  2. Look for:
     - Table scans (bad, should be index seeks)
     - High cost operations
     - Missing indexes
  3. Add index on scanned column
  4. Re-test query

Command (SQL Server):
  SET STATISTICS IO ON;
  -- Run query
  SET STATISTICS IO OFF;
```

---

## ✅ Best Practices

1. **Repository per Aggregate** - Not per entity
2. **Async All I/O** - Never block on database calls
3. **Pagination Always** - Never return all records
4. **Cache Strategic** - High-read, low-write data
5. **Transactions for Consistency** - Multi-step operations
6. **Indexes for Performance** - On key columns
7. **Connection Pooling** - Reuse connections
8. **Parameterized Queries** - Always (EF Core does this)
9. **Log Queries (dev only)** - Track N+1 problems
10. **Monitor Performance** - Track slow queries

---

**Module**: Infrastructure Layer | **Version**: 1.0
