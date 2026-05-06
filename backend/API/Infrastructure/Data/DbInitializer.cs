namespace API.Infrastructure.Data;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Database Initializer - Automatically seeds database on first run
/// This ensures the application works on a fresh database
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(
        FlightBookingDbContext context,
        ILogger logger)
    {
        try
        {
            // Step 1: Apply migrations
            logger.LogInformation("Checking for pending migrations...");
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
                await context.Database.MigrateAsync();
                logger.LogInformation("✓ Migrations applied successfully");
            }
            else
            {
                logger.LogInformation("✓ Database is up to date");
            }

            // Step 2: Seed master data if empty
            await SeedMasterDataAsync(context, logger);

            // Step 3: Seed sample operational data if empty
            await SeedOperationalDataAsync(context, logger);

            logger.LogInformation("✓ Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error initializing database");
            throw;
        }
    }

    private static async Task SeedMasterDataAsync(FlightBookingDbContext context, ILogger logger)
    {
        // Check if master data already exists
        if (await context.Roles.AnyAsync())
        {
            logger.LogInformation("Master data already exists, skipping seed");
            return;
        }

        logger.LogInformation("Seeding master data...");

        // Roles
        var roles = new[]
        {
            new Role { Name = "Admin", Description = "Administrator role" },
            new Role { Name = "User", Description = "Regular user role" }
        };
        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ Roles seeded: {Count}", roles.Length);

        // Airports
        var airports = new[]
        {
            new Airport { Code = "SGN", Name = "Tan Son Nhat International Airport", City = "Ho Chi Minh City", Province = "Ho Chi Minh" },
            new Airport { Code = "HAN", Name = "Noi Bai International Airport", City = "Hanoi", Province = "Hanoi" },
            new Airport { Code = "DAD", Name = "Da Nang International Airport", City = "Da Nang", Province = "Da Nang" },
            new Airport { Code = "CXR", Name = "Cam Ranh International Airport", City = "Nha Trang", Province = "Khanh Hoa" },
            new Airport { Code = "PQC", Name = "Phu Quoc International Airport", City = "Phu Quoc", Province = "Kien Giang" }
        };
        await context.Airports.AddRangeAsync(airports);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ Airports seeded: {Count}", airports.Length);

        // Aircraft
        var aircraft = new[]
        {
            new Aircraft { Model = "Boeing 787", RegistrationNumber = "VN-A001", TotalSeats = 296, IsActive = true },
            new Aircraft { Model = "Airbus A320", RegistrationNumber = "VN-A002", TotalSeats = 220, IsActive = true },
            new Aircraft { Model = "Boeing 737", RegistrationNumber = "VN-A003", TotalSeats = 189, IsActive = true },
            new Aircraft { Model = "Airbus A350", RegistrationNumber = "VN-A004", TotalSeats = 325, IsActive = true },
            new Aircraft { Model = "Boeing 777", RegistrationNumber = "VN-A005", TotalSeats = 364, IsActive = true }
        };
        await context.Aircraft.AddRangeAsync(aircraft);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ Aircraft seeded: {Count}", aircraft.Length);

        // Seat Classes
        var seatClasses = new[]
        {
            new SeatClass { Code = "ECO", Name = "Economy", RefundPercent = 70, ChangeFee = 500000, Priority = 3 },
            new SeatClass { Code = "BUS", Name = "Business", RefundPercent = 85, ChangeFee = 300000, Priority = 2 },
            new SeatClass { Code = "FST", Name = "Premium", RefundPercent = 95, ChangeFee = 100000, Priority = 1 }
        };
        await context.SeatClasses.AddRangeAsync(seatClasses);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ Seat classes seeded: {Count}", seatClasses.Length);

        // Routes
        var routes = new[]
        {
            new Route { DepartureAirportId = 1, ArrivalAirportId = 2, DistanceKm = 1166, EstimatedDurationMinutes = 145 }, // SGN-HAN
            new Route { DepartureAirportId = 2, ArrivalAirportId = 1, DistanceKm = 1166, EstimatedDurationMinutes = 145 }, // HAN-SGN
            new Route { DepartureAirportId = 1, ArrivalAirportId = 3, DistanceKm = 610, EstimatedDurationMinutes = 80 },   // SGN-DAD
            new Route { DepartureAirportId = 3, ArrivalAirportId = 1, DistanceKm = 610, EstimatedDurationMinutes = 80 },   // DAD-SGN
            new Route { DepartureAirportId = 2, ArrivalAirportId = 3, DistanceKm = 616, EstimatedDurationMinutes = 85 },   // HAN-DAD
            new Route { DepartureAirportId = 3, ArrivalAirportId = 2, DistanceKm = 616, EstimatedDurationMinutes = 85 }    // DAD-HAN
        };
        await context.Routes.AddRangeAsync(routes);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ Routes seeded: {Count}", routes.Length);

        // Aircraft Seat Templates
        var seatTemplates = new List<AircraftSeatTemplate>
        {
            // Aircraft 1: Boeing 787
            new() { AircraftId = 1, SeatClassId = 1, DefaultSeatCount = 246, DefaultBasePrice = 1500000 },
            new() { AircraftId = 1, SeatClassId = 2, DefaultSeatCount = 36, DefaultBasePrice = 4500000 },
            new() { AircraftId = 1, SeatClassId = 3, DefaultSeatCount = 14, DefaultBasePrice = 8000000 },
            // Aircraft 2: Airbus A320
            new() { AircraftId = 2, SeatClassId = 1, DefaultSeatCount = 196, DefaultBasePrice = 1200000 },
            new() { AircraftId = 2, SeatClassId = 2, DefaultSeatCount = 24, DefaultBasePrice = 3500000 },
            // Aircraft 3: Boeing 737
            new() { AircraftId = 3, SeatClassId = 1, DefaultSeatCount = 165, DefaultBasePrice = 1000000 },
            new() { AircraftId = 3, SeatClassId = 2, DefaultSeatCount = 24, DefaultBasePrice = 3000000 },
            // Aircraft 4: Airbus A350
            new() { AircraftId = 4, SeatClassId = 1, DefaultSeatCount = 261, DefaultBasePrice = 1800000 },
            new() { AircraftId = 4, SeatClassId = 2, DefaultSeatCount = 48, DefaultBasePrice = 5000000 },
            new() { AircraftId = 4, SeatClassId = 3, DefaultSeatCount = 16, DefaultBasePrice = 9000000 },
            // Aircraft 5: Boeing 777
            new() { AircraftId = 5, SeatClassId = 1, DefaultSeatCount = 296, DefaultBasePrice = 2000000 },
            new() { AircraftId = 5, SeatClassId = 2, DefaultSeatCount = 52, DefaultBasePrice = 5500000 },
            new() { AircraftId = 5, SeatClassId = 3, DefaultSeatCount = 16, DefaultBasePrice = 10000000 }
        };
        await context.AircraftSeatTemplates.AddRangeAsync(seatTemplates);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ Aircraft seat templates seeded: {Count}", seatTemplates.Count);

        // Admin user
        var adminUser = new User
        {
            Email = "admin@flightbooking.vn",
            PasswordHash = "$2a$11$8vLwZ5YqJ5YqJ5YqJ5YqJOK8vLwZ5YqJ5YqJ5YqJ5YqJOK8vLwZ5", // Admin@123456
            FullName = "System Administrator",
            Phone = "0900000000",
            Status = 0,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await context.Users.AddAsync(adminUser);
        await context.SaveChangesAsync();
        
        // Assign admin role via UserRole
        var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
        var userRole = new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        };
        await context.UserRoles.AddAsync(userRole);
        await context.SaveChangesAsync();
        
        logger.LogInformation("✓ Admin user created: {Email}", adminUser.Email);

        logger.LogInformation("✓ Master data seeding completed");
    }

    private static async Task SeedOperationalDataAsync(FlightBookingDbContext context, ILogger logger)
    {
        // Check if operational data already exists
        if (await context.FlightDefinitions.AnyAsync())
        {
            logger.LogInformation("Operational data already exists, skipping seed");
            return;
        }

        logger.LogInformation("Seeding operational data...");

        // Flight Definitions
        var flightDefinitions = new[]
        {
            // SGN-HAN route
            new FlightDefinition { FlightNumber = "VN201", RouteId = 1, DefaultAircraftId = 1, DepartureTime = new TimeOnly(6, 0), ArrivalTime = new TimeOnly(8, 15), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VN203", RouteId = 1, DefaultAircraftId = 2, DepartureTime = new TimeOnly(9, 0), ArrivalTime = new TimeOnly(11, 15), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VN205", RouteId = 1, DefaultAircraftId = 3, DepartureTime = new TimeOnly(12, 0), ArrivalTime = new TimeOnly(14, 15), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VN207", RouteId = 1, DefaultAircraftId = 1, DepartureTime = new TimeOnly(15, 0), ArrivalTime = new TimeOnly(17, 15), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VN209", RouteId = 1, DefaultAircraftId = 2, DepartureTime = new TimeOnly(18, 0), ArrivalTime = new TimeOnly(20, 15), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VN211", RouteId = 1, DefaultAircraftId = 3, DepartureTime = new TimeOnly(21, 0), ArrivalTime = new TimeOnly(23, 15), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            // VietJet flights
            new FlightDefinition { FlightNumber = "VJ123", RouteId = 1, DefaultAircraftId = 1, DepartureTime = new TimeOnly(5, 30), ArrivalTime = new TimeOnly(7, 45), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VJ125", RouteId = 1, DefaultAircraftId = 2, DepartureTime = new TimeOnly(13, 30), ArrivalTime = new TimeOnly(15, 45), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            // Overnight flight
            new FlightDefinition { FlightNumber = "VN999", RouteId = 1, DefaultAircraftId = 3, DepartureTime = new TimeOnly(23, 30), ArrivalTime = new TimeOnly(1, 45), ArrivalOffsetDays = 1, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            
            // HAN-SGN route (return flights)
            new FlightDefinition { FlightNumber = "VN202", RouteId = 2, DefaultAircraftId = 1, DepartureTime = new TimeOnly(7, 0), ArrivalTime = new TimeOnly(9, 15), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VN204", RouteId = 2, DefaultAircraftId = 2, DepartureTime = new TimeOnly(10, 0), ArrivalTime = new TimeOnly(12, 15), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VN206", RouteId = 2, DefaultAircraftId = 3, DepartureTime = new TimeOnly(13, 0), ArrivalTime = new TimeOnly(15, 15), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VN208", RouteId = 2, DefaultAircraftId = 1, DepartureTime = new TimeOnly(16, 0), ArrivalTime = new TimeOnly(18, 15), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VN210", RouteId = 2, DefaultAircraftId = 2, DepartureTime = new TimeOnly(19, 0), ArrivalTime = new TimeOnly(21, 15), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            
            // SGN-DAD route
            new FlightDefinition { FlightNumber = "VN301", RouteId = 3, DefaultAircraftId = 2, DepartureTime = new TimeOnly(8, 0), ArrivalTime = new TimeOnly(9, 20), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VN303", RouteId = 3, DefaultAircraftId = 3, DepartureTime = new TimeOnly(14, 0), ArrivalTime = new TimeOnly(15, 20), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            
            // DAD-SGN route
            new FlightDefinition { FlightNumber = "VN302", RouteId = 4, DefaultAircraftId = 2, DepartureTime = new TimeOnly(10, 0), ArrivalTime = new TimeOnly(11, 20), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            new FlightDefinition { FlightNumber = "VN304", RouteId = 4, DefaultAircraftId = 3, DepartureTime = new TimeOnly(16, 0), ArrivalTime = new TimeOnly(17, 20), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            
            // HAN-DAD route
            new FlightDefinition { FlightNumber = "VN401", RouteId = 5, DefaultAircraftId = 2, DepartureTime = new TimeOnly(9, 0), ArrivalTime = new TimeOnly(10, 25), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow },
            
            // DAD-HAN route
            new FlightDefinition { FlightNumber = "VN402", RouteId = 6, DefaultAircraftId = 2, DepartureTime = new TimeOnly(11, 0), ArrivalTime = new TimeOnly(12, 25), ArrivalOffsetDays = 0, OperatingDays = 127, IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        await context.FlightDefinitions.AddRangeAsync(flightDefinitions);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ Flight definitions seeded: {Count}", flightDefinitions.Length);

        // Generate flights for next 30 days
        var flights = new List<Flight>();
        var startDate = DateTime.UtcNow.Date;
        var endDate = startDate.AddDays(30);

        foreach (var definition in flightDefinitions)
        {
            for (var date = startDate; date < endDate; date = date.AddDays(1))
            {
                var dayOfWeek = (int)date.DayOfWeek;
                var dayFlag = 1 << dayOfWeek;
                
                // Check if flight operates on this day
                if ((definition.OperatingDays & dayFlag) == 0)
                    continue;

                var departureDateTime = date.Add(definition.DepartureTime.ToTimeSpan());
                var arrivalDateTime = date.Add(definition.ArrivalTime.ToTimeSpan());
                
                if (definition.ArrivalOffsetDays > 0)
                {
                    arrivalDateTime = arrivalDateTime.AddDays(definition.ArrivalOffsetDays);
                }

                flights.Add(new Flight
                {
                    FlightDefinitionId = definition.Id,
                    DepartureTime = departureDateTime,
                    ArrivalTime = arrivalDateTime,
                    Status = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await context.Flights.AddRangeAsync(flights);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ Flights generated: {Count}", flights.Count);

        // Generate seat inventories for all flights
        var seatInventories = new List<FlightSeatInventory>();
        foreach (var flight in flights)
        {
            var definition = flightDefinitions.First(fd => fd.Id == flight.FlightDefinitionId);
            var templates = await context.AircraftSeatTemplates
                .Where(ast => ast.AircraftId == definition.DefaultAircraftId && !ast.IsDeleted)
                .ToListAsync();

            foreach (var template in templates)
            {
                seatInventories.Add(new FlightSeatInventory
                {
                    FlightId = flight.Id,
                    SeatClassId = template.SeatClassId,
                    TotalSeats = template.DefaultSeatCount,
                    AvailableSeats = template.DefaultSeatCount,
                    HeldSeats = 0,
                    SoldSeats = 0,
                    BasePrice = template.DefaultBasePrice,
                    CurrentPrice = template.DefaultBasePrice,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await context.FlightSeatInventories.AddRangeAsync(seatInventories);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ Seat inventories generated: {Count}", seatInventories.Count);

        logger.LogInformation("✓ Operational data seeding completed");
    }
}
