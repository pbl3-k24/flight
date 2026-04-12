# 🐳 PostgreSQL + Docker Configuration Guide

## 📋 Tổng Quan
- **Database**: PostgreSQL 16
- **Containerization**: Docker + Docker Compose
- **Connection**: EF Core npgsql provider
- **Migrations**: Database-first approach

---

## 🐘 PostgreSQL Configuration

### 1. **Connection String Format**

**Development (local):**
```
Host=localhost;Port=5432;Database=FlightTicketBooking;Username=postgres;Password=postgres
```

**Docker (container networking):**
```
Host=postgres;Port=5432;Database=FlightTicketBooking;Username=postgres;Password=postgres
```

**Production (cloud):**
```
Host=db.example.com;Port=5432;Database=FlightTicketBooking;Username=dbuser;Password=securepass;SSL Mode=Require
```

### 2. **appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FlightTicketBooking;Username=postgres;Password=postgres"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### 3. **appsettings.Docker.json** (for container)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=FlightTicketBooking;Username=postgres;Password=postgres"
  }
}
```

---

## 🐳 Docker Setup

### 1. **docker-compose.yml** (Complete Setup)

```yaml
version: '3.8'

services:
  # PostgreSQL Database
  postgres:
    image: postgres:16-alpine
    container_name: flight-booking-db
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: FlightTicketBooking
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      # Optional: custom init scripts
      - ./docker/init-db.sql:/docker-entrypoint-initdb.d/init.sql
    networks:
      - flight-booking-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  # Redis Cache
  redis:
    image: redis:7-alpine
    container_name: flight-booking-cache
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    networks:
      - flight-booking-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  # ASP.NET Core API
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: flight-booking-api
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:80
      # Connection strings
      ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=FlightTicketBooking;Username=postgres;Password=postgres"
      Redis__ConnectionString: "redis:6379"
      # Logging
      Logging__LogLevel__Default: "Information"
      Logging__LogLevel__Microsoft: "Warning"
    ports:
      - "5000:80"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    networks:
      - flight-booking-network
    restart: unless-stopped

volumes:
  postgres_data:
    driver: local
  redis_data:
    driver: local

networks:
  flight-booking-network:
    driver: bridge
```

### 2. **Dockerfile** (ASP.NET Core Multi-stage)

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["FlightTicketBooking.API/FlightTicketBooking.API.csproj", "FlightTicketBooking.API/"]
COPY ["FlightTicketBooking.Application/FlightTicketBooking.Application.csproj", "FlightTicketBooking.Application/"]
COPY ["FlightTicketBooking.Domain/FlightTicketBooking.Domain.csproj", "FlightTicketBooking.Domain/"]
COPY ["FlightTicketBooking.Infrastructure/FlightTicketBooking.Infrastructure.csproj", "FlightTicketBooking.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "FlightTicketBooking.API/FlightTicketBooking.API.csproj"

# Copy all files
COPY . .

# Build
RUN dotnet build "FlightTicketBooking.API/FlightTicketBooking.API.csproj" -c Release -o /app/build

# Publish
RUN dotnet publish "FlightTicketBooking.API/FlightTicketBooking.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-build

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install curl for healthchecks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost/health || exit 1

# Start app
ENTRYPOINT ["dotnet", "FlightTicketBooking.API.dll"]
```

### 3. **docker/init-db.sql** (Optional - Initial Schema)

```sql
-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create schemas (if needed)
-- CREATE SCHEMA IF NOT EXISTS public;

-- Set timezone
SET timezone = 'UTC';

-- Basic initial data (optional)
-- INSERT INTO airports (code, name) VALUES ('JFK', 'John F Kennedy International Airport');
-- INSERT INTO airports (code, name) VALUES ('LAX', 'Los Angeles International Airport');
```

---

## 🚀 Docker Commands

### Start Services

```bash
# Start all services (detached)
docker-compose up -d

# Start with logs visible
docker-compose up

# Start specific service
docker-compose up -d postgres
docker-compose up -d redis
docker-compose up -d api

# Build images first
docker-compose up --build -d
```

### View Logs

```bash
# All services logs
docker-compose logs -f

# Specific service logs
docker-compose logs -f api
docker-compose logs -f postgres
docker-compose logs -f redis

# Last 50 lines
docker-compose logs --tail=50 api
```

### Stop Services

```bash
# Stop all services
docker-compose down

# Stop and remove volumes (data loss!)
docker-compose down -v

# Stop specific service
docker-compose stop api
```

### Database Operations

```bash
# Connect to PostgreSQL container
docker-compose exec postgres psql -U postgres -d FlightTicketBooking

# Run migration in container
docker-compose exec api dotnet ef database update

# Create migration
docker-compose exec api dotnet ef migrations add MigrationName

# View database
docker-compose exec postgres psql -U postgres -d FlightTicketBooking -c "\dt"

# Backup database
docker-compose exec postgres pg_dump -U postgres FlightTicketBooking > backup.sql

# Restore database
docker-compose exec -T postgres psql -U postgres FlightTicketBooking < backup.sql
```

### Other Useful Commands

```bash
# Check service status
docker-compose ps

# Restart all services
docker-compose restart

# Rebuild images
docker-compose build

# Remove stopped containers
docker system prune

# View container resource usage
docker stats

# SSH into container
docker-compose exec api bash
docker-compose exec postgres bash
```

---

## 📦 NuGet Packages for PostgreSQL

### Add to .csproj:

```xml
<!-- PostgreSQL Provider for EF Core -->
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />

<!-- PostgreSQL ADO.NET Data Provider (included with above) -->
<PackageReference Include="Npgsql" Version="8.0.0" />

<!-- Entity Framework Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />

<!-- Tools for migrations -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
```

### Or Install via CLI:

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
```

---

## ⚙️ Program.cs Configuration for PostgreSQL

```csharp
var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

// Database: PostgreSQL + EF Core
services.AddDbContext<FlightBookingDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            // Enable UUID
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelaySeconds: 10,
                errorCodesToAdd: null);
            
            // Mapping for enums
            npgsqlOptions.MapEnum<FlightStatus>("flight_status");
            npgsqlOptions.MapEnum<BookingStatus>("booking_status");
            npgsqlOptions.MapEnum<PaymentStatus>("payment_status");
        });
});

// ... rest of configuration
```

---

## 🔄 Migrations with PostgreSQL

### Create Migration (local)

```bash
# Add migration
dotnet ef migrations add InitialCreate --project FlightTicketBooking.Infrastructure

# Review generated migration file
# Check: CreateTable, CreateIndex, etc.
```

### Generated Migration Example (PostgreSQL-specific)

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // PostgreSQL-specific syntax
    migrationBuilder.CreateTable(
        name: "flights",
        columns: table => new
        {
            id = table.Column<int>(type: "integer", nullable: false)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            flight_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            departure_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            arrival_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            total_seats = table.Column<int>(type: "integer", nullable: false),
            available_seats = table.Column<int>(type: "integer", nullable: false),
            base_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
            status = table.Column<int>(type: "integer", nullable: false),
            created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        },
        constraints: table =>
        {
            table.PrimaryKey("pk_flights", x => x.id);
        });

    // Create indexes
    migrationBuilder.CreateIndex(
        name: "ix_flights_flight_number",
        table: "flights",
        column: "flight_number",
        unique: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(name: "flights");
}
```

### Apply Migration (Docker)

```bash
# Run migration inside container
docker-compose exec api dotnet ef database update

# Or manually (if container doesn't have EF tools)
docker-compose exec postgres psql -U postgres -d FlightTicketBooking -f migration.sql
```

---

## 📊 PostgreSQL Best Practices

### 1. **Connection Pooling**
```csharp
// EF Core handles pooling automatically
// Default: 100 connections
// Customize:
options.UseNpgsql(connectionString, npgsqlOptions =>
{
    npgsqlOptions.MaxPoolSize = 50;  // Reduce if needed
});
```

### 2. **Timezone Handling**
```csharp
// Always use UTC in database
// Store as: timestamp with time zone
// Convert to local on client

// In entity:
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

// In configuration:
modelBuilder.Entity<Flight>()
    .Property(f => f.CreatedAt)
    .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");
```

### 3. **Index Strategy**
```sql
-- Single column indexes
CREATE INDEX idx_flight_number ON flights(flight_number);
CREATE INDEX idx_departure_time ON flights(departure_time);

-- Composite indexes (query optimization)
CREATE INDEX idx_flight_route_date 
ON flights(departure_airport_id, arrival_airport_id, departure_time);

-- Unique indexes (constraints)
CREATE UNIQUE INDEX idx_booking_reference ON bookings(booking_reference);
```

### 4. **Enum Mapping**
```csharp
// Map C# enums to PostgreSQL enum type
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseNpgsql(
        connectionString,
        options => options.MapEnum<FlightStatus>("flight_status"));
}

// Or in migration:
migrationBuilder.Sql(
    "CREATE TYPE flight_status AS ENUM ('Active', 'Cancelled', 'Delayed', 'Completed');");
```

### 5. **Backup Strategy**
```bash
# Full backup
docker-compose exec postgres pg_dump -U postgres FlightTicketBooking > backup_$(date +%Y%m%d_%H%M%S).sql

# Compressed backup
docker-compose exec postgres pg_dump -U postgres --format=custom FlightTicketBooking > backup.dump

# Restore from backup
docker-compose exec -T postgres pg_restore -U postgres -d FlightTicketBooking < backup.dump
```

---

## 🔍 Troubleshooting

### Issue 1: Connection Refused
```
Error: "could not connect to server: Connection refused"

Solution:
1. Check postgres container is running: docker-compose ps
2. Wait for healthcheck to pass: docker-compose logs postgres
3. Verify connection string (Host=postgres for Docker, localhost for local)
4. Check port 5432 isn't blocked
```

### Issue 2: Database Already Exists
```
Error: "database already exists"

Solution:
1. Drop database: docker-compose exec postgres dropdb -U postgres FlightTicketBooking
2. Or use: docker-compose down -v (removes volumes)
3. Recreate: docker-compose up -d postgres
```

### Issue 3: Migration Fails
```
Error during migration

Solution:
1. Check syntax: Review generated migration file
2. Test manually: docker-compose exec postgres psql -U postgres -d FlightTicketBooking
3. Rollback: dotnet ef migrations remove
4. Recreate: dotnet ef migrations add (fixed)
```

### Issue 4: Slow Queries
```
Solution:
1. Enable query logging:
   options.UseNpgsql(connectionString, o => 
       o.CommandTimeout(60));

2. Check indexes: SELECT * FROM pg_stat_user_indexes;

3. EXPLAIN query: EXPLAIN (ANALYZE, BUFFERS) SELECT ...;

4. Add indexes for frequently filtered columns
```

### Issue 5: Out of Memory
```
Solution:
1. Reduce MaxPoolSize
2. Limit container memory: docker-compose.yml
   services:
     postgres:
       deploy:
         resources:
           limits:
             memory: 1G
```

---

## 📋 Development Workflow

### Local Development (no Docker)
```bash
# Install PostgreSQL locally
# Or use Docker just for DB:
docker run -d --name pgdb -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16-alpine

# Connection string: Host=localhost;Port=5432;...
dotnet ef database update
dotnet run
```

### Docker Development
```bash
# Start all services
docker-compose up -d

# Wait for postgres healthcheck (10-20 seconds)
docker-compose ps  # Check (healthy)

# Apply migrations
docker-compose exec api dotnet ef database update

# View logs
docker-compose logs -f api

# Stop everything
docker-compose down
```

### Production Deployment
```bash
# Build image
docker build -t flight-booking-api:1.0 .

# Push to registry
docker tag flight-booking-api:1.0 yourregistry/flight-booking-api:1.0
docker push yourregistry/flight-booking-api:1.0

# Deploy with docker-compose or Kubernetes
docker-compose -f docker-compose.prod.yml up -d
```

---

## ✅ Checklist

- [ ] PostgreSQL 16 installed locally or Docker running
- [ ] Npgsql NuGet packages added
- [ ] Connection string configured in appsettings.json
- [ ] docker-compose.yml created and tested
- [ ] Dockerfile created and builds successfully
- [ ] DbContext configured for PostgreSQL (UseNpgsql)
- [ ] First migration created
- [ ] docker-compose up -d runs successfully
- [ ] Migrations applied: docker-compose exec api dotnet ef database update
- [ ] API connects to database and responds
- [ ] Redis container running and responding
- [ ] All health checks passing: docker-compose ps

---

## 🚀 Next Steps

1. **Clone/Create repository** with docker-compose.yml
2. **Run services**: `docker-compose up -d`
3. **Apply migrations**: `docker-compose exec api dotnet ef database update`
4. **Test API**: `curl http://localhost:5000/swagger`
5. **Monitor logs**: `docker-compose logs -f`

---

**Module**: PostgreSQL + Docker | **Version**: 1.0 | **Tested**: PostgreSQL 16 + Docker Compose
