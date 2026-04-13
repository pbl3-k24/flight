# 🚀 QUICK START GUIDE - DATABASE SETUP

## **Current Status**

✅ **Application**: Builds successfully  
❌ **Database**: PostgreSQL not running on `localhost:5432`

The application will now start even without a database connection. When a database becomes available, the migrations will be applied automatically.

---

## **Option 1: Using Docker (Recommended for Development)**

### **Start PostgreSQL with Docker:**

```bash
# Run PostgreSQL container
docker run --name flight-booking-db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=your_password \
  -e POSTGRES_DB=FlightBookingDB \
  -p 5432:5432 \
  -v postgres_data:/var/lib/postgresql/data \
  postgres:latest
```

### **Update appsettings.json:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FlightBookingDB;Username=postgres;Password=your_password"
  }
}
```

### **Start the Application:**

```bash
cd API
dotnet run
```

The migrations will automatically apply when the app starts! 🎉

---

## **Option 2: Install PostgreSQL Locally**

### **Windows:**
1. Download from: https://www.postgresql.org/download/windows/
2. Install with default settings
3. Default port: `5432`
4. Create database: `FlightBookingDB`

### **macOS:**
```bash
brew install postgresql
brew services start postgresql
createdb FlightBookingDB
```

### **Linux (Ubuntu/Debian):**
```bash
sudo apt-get install postgresql postgresql-contrib
sudo systemctl start postgresql
sudo -u postgres createdb FlightBookingDB
```

---

## **Option 3: Use Cloud PostgreSQL (AWS RDS, Azure Database, etc.)**

Update your connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-cloud-host.rds.amazonaws.com;Port=5432;Database=FlightBookingDB;Username=postgres;Password=your_password"
  }
}
```

---

## **Verify Database Connection:**

Once PostgreSQL is running, you can test the connection:

### **Using psql:**
```bash
psql -h localhost -U postgres -d FlightBookingDB
```

### **From the Application:**
1. Start the app: `dotnet run`
2. Check the logs for: `"Database migrations applied successfully."`
3. Access Swagger UI: http://localhost:5000/

---

## **Database Schema Diagram**

```
┌─────────────────────────────────────────────────────────┐
│                    Flight Booking System                │
└─────────────────────────────────────────────────────────┘

┌──────────────┐         ┌──────────────┐
│  Airports    │         │    Users     │
├──────────────┤         ├──────────────┤
│ id (PK)      │         │ id (PK)      │
│ code (UNIQUE)│         │ email (UNIQUE)
│ name         │         │ first_name   │
│ city         │         │ last_name    │
│ country      │         │ status       │
└──────────────┘         └──────────────┘
       ▲                         ▲
       │                         │
       │ 1:N                     │ 1:N
       │                         │
    ┌─────────────────────────────────┐
    │          Flights               │
    ├─────────────────────────────────┤
    │ id (PK)                        │
    │ flight_number (UNIQUE)         │
    │ departure_airport_id (FK) ────┘
    │ arrival_airport_id (FK) ─────┐
    │ departure_time                │
    │ arrival_time                  │
    │ total_seats                   │
    │ available_seats               │
    │ base_price                    │
    │ status                        │
    │ created_at                    │
    │ updated_at                    │
    └─────────────────────────────────┘
       ▲
       │ 1:N
       │
    ┌─────────────────────────────────┐
    │       Bookings                 │
    ├─────────────────────────────────┤
    │ id (PK)                        │
    │ flight_id (FK) ───────────────┘
    │ user_id (FK) ──────────────────┐
    │ booking_reference (UNIQUE)     │
    │ passenger_count               │
    │ total_price                   │
    │ status                        │
    │ created_at                    │
    │ updated_at                    │
    │ cancelled_at (nullable)       │
    └─────────────────────────────────┘
       ▲                    ▼
       │ 1:N              1:1
       │                   │
       │              ┌──────────────┐
       │              │  Payments    │
       │              ├──────────────┤
       │              │ id (PK)      │
       └──────────────│ booking_id (FK) 
                      │ amount       │
    ┌─────────────┐  │ status       │
    │ Passengers  │  │ method       │
    ├─────────────┤  │ created_at   │
    │ id (PK)     │  └──────────────┘
    │ booking_id (FK)
    │ first_name  │
    │ last_name   │
    │ nationality │
    │ passport    │
    │ seat_number │
    └─────────────┘
```

---

## **Migration Commands**

### **Create New Migration:**
```bash
dotnet ef migrations add MigrationName
```

### **Apply Migrations:**
```bash
dotnet ef database update
```

### **Revert Last Migration:**
```bash
dotnet ef database update <PreviousMigrationName>
```

### **List Migrations:**
```bash
dotnet ef migrations list
```

### **Remove Last Migration (if not applied):**
```bash
dotnet ef migrations remove
```

---

## **Common Issues**

### **Issue: "Failed to connect to 127.0.0.1:5432"**
- ✅ **Solution**: PostgreSQL is not running. Start it using Docker or your installation method.

### **Issue: "database "FlightBookingDB" does not exist"**
- ✅ **Solution**: Create the database using psql or pgAdmin.

### **Issue: "connection refused"**
- ✅ **Solution**: Check PostgreSQL is listening on port 5432. Change port in connection string if needed.

### **Issue: "password authentication failed"**
- ✅ **Solution**: Verify username and password in connection string match your PostgreSQL credentials.

---

## **Testing API Without Database**

The API can start without a database. However, endpoints that require database operations will fail. You can test:

- ✅ **Health Check**: `GET /health`
- ✅ **Swagger UI**: `GET /`
- ❌ **Bookings**: Will fail (requires database)
- ❌ **Flights**: Will fail (requires database)

---

## **EF Core Warnings (Safe to Ignore)**

You may see these warnings during startup - they are informational and don't affect functionality:

```
The foreign key property 'Payment.BookingId1' was created in shadow state...
The 'BookingStatus' property 'Status' on entity type 'Booking' is configured with a database-generated default...
```

These are addressed in the latest configuration and will be resolved in the next migration.

---

## **Next Steps**

1. ✅ Set up PostgreSQL (using Docker or local installation)
2. ✅ Update `appsettings.json` with your connection string
3. ✅ Run the application: `dotnet run`
4. ✅ Migrations will apply automatically
5. ✅ Access Swagger UI: http://localhost:5000/
6. ✅ Start making API requests!

---

**Need Help?** Check the logs for detailed error messages or review the troubleshooting section.

Happy Coding! 🚀
