namespace API.Infrastructure.Data;

using API.Domain.Entities;
using API.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Database initializer - handles migrations and seeding
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(FlightBookingDbContext context)
    {
        try
        {
            // Migrations are applied in Program.cs before calling DbInitializer.
            // Keep initializer focused on seeding/repairing seed relations only.

            // Check if database has data
            bool usersExist = context.Users.Any();
            bool rolesExist = context.Roles.Any();
            bool userRolesExist = context.UserRoles.Any();

            if (!usersExist && !rolesExist)
            {
                // Database is empty - seed everything
                await SeedAllDataAsync(context);
            }
            else if (usersExist && !userRolesExist)
            {
                // Database has users but missing UserRoles - create them
                await EnsureUserRolesAsync(context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during database initialization: {ex.Message}");
            throw;
        }
    }

    private static async Task EnsureUserRolesAsync(FlightBookingDbContext context)
    {
        Console.WriteLine("Creating missing UserRoles...");

        // Get or create roles
        var adminRole = context.Roles.FirstOrDefault(r => r.Name == "Admin");
        var userRole = context.Roles.FirstOrDefault(r => r.Name == "User");

        if (adminRole == null)
        {
            adminRole = new Role { Name = "Admin", Description = "Quản trị viên hệ thống" };
            context.Roles.Add(adminRole);
            await context.SaveChangesAsync();
        }

        if (userRole == null)
        {
            userRole = new Role { Name = "User", Description = "Người dùng thường" };
            context.Roles.Add(userRole);
            await context.SaveChangesAsync();
        }

        // Assign admin role to admin user
        var adminUser = context.Users.FirstOrDefault(u => u.Email.ToLower() == "admin@flightbooking.vn");
        if (adminUser != null && !context.UserRoles.Any(ur => ur.UserId == adminUser.Id))
        {
            context.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ Admin role assigned to admin user");
        }

        // Assign user role to other users
        var otherUsers = context.Users.Where(u => u.Email.ToLower() != "admin@flightbooking.vn").ToList();
        foreach (var user in otherUsers)
        {
            if (!context.UserRoles.Any(ur => ur.UserId == user.Id))
            {
                context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = userRole.Id });
            }
        }

        if (otherUsers.Count > 0)
        {
            await context.SaveChangesAsync();
            Console.WriteLine($"✅ User roles assigned to {otherUsers.Count} users");
        }
    }

    private static async Task SeedAllDataAsync(FlightBookingDbContext context)
    {
        Console.WriteLine("Seeding database with initial data...");

        // Seed Roles
        var adminRole = new Role { Name = "Admin", Description = "Quản trị viên hệ thống" };
        var userRole = new Role { Name = "User", Description = "Người dùng thường" };
        var staffRole = new Role { Name = "Staff", Description = "Nhân viên" };

        context.Roles.AddRange(adminRole, userRole, staffRole);
        await context.SaveChangesAsync();

        // Hash passwords
        var passwordHasher = new PasswordHasher();

        // Seed Users
        var adminUser = new User
        {
            Email = "admin@flightbooking.vn",
            FullName = "Quản trị viên",
            Phone = "0901234567",
            PasswordHash = passwordHasher.HashPassword("Admin@123456"),
            Status = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var testUser1 = new User
        {
            Email = "user1@gmail.com",
            FullName = "Nguyễn Văn A",
            Phone = "0912345678",
            PasswordHash = passwordHasher.HashPassword("User1@123456"),
            Status = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var testUser2 = new User
        {
            Email = "user2@gmail.com",
            FullName = "Trần Thị B",
            Phone = "0923456789",
            PasswordHash = passwordHasher.HashPassword("User2@123456"),
            Status = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(adminUser, testUser1, testUser2);
        await context.SaveChangesAsync();

        // Assign roles to users
        context.UserRoles.AddRange(
            new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id },
            new UserRole { UserId = testUser1.Id, RoleId = userRole.Id },
            new UserRole { UserId = testUser2.Id, RoleId = userRole.Id }
        );
        await context.SaveChangesAsync();

        Console.WriteLine("✅ Users and roles seeded successfully");

        // Seed Airports
        var sgAirport = new Airport
        {
            Code = "SGN",
            Name = "Sân bay Tân Sơn Nhất",
            City = "Thành phố Hồ Chí Minh",
            Province = "Hồ Chí Minh",
            IsActive = true
        };

        var hnAirport = new Airport
        {
            Code = "HAN",
            Name = "Sân bay Nội Bài",
            City = "Hà Nội",
            Province = "Hà Nội",
            IsActive = true
        };

        var dnAirport = new Airport
        {
            Code = "DAD",
            Name = "Sân bay Quốc tế Đà Nẵng",
            City = "Đà Nẵng",
            Province = "Đà Nẵng",
            IsActive = true
        };

        var ctAirport = new Airport
        {
            Code = "CTS",
            Name = "Sân bay Cần Thơ",
            City = "Cần Thơ",
            Province = "Cần Thơ",
            IsActive = true
        };

        context.Airports.AddRange(sgAirport, hnAirport, dnAirport, ctAirport);
        await context.SaveChangesAsync();

        // Seed Routes
        var route1 = new Route
        {
            DepartureAirportId = sgAirport.Id,
            ArrivalAirportId = hnAirport.Id,
            DistanceKm = 1700,
            EstimatedDurationMinutes = 145,
            IsActive = true
        };

        var route2 = new Route
        {
            DepartureAirportId = hnAirport.Id,
            ArrivalAirportId = sgAirport.Id,
            DistanceKm = 1700,
            EstimatedDurationMinutes = 145,
            IsActive = true
        };

        context.Routes.AddRange(route1, route2);
        await context.SaveChangesAsync();

        // Seed Seat Classes
        var ecoClass = new SeatClass
        {
            Code = "ECO",
            Name = "Economy",
            RefundPercent = 100,
            ChangeFee = 150000,
            Priority = 3
        };

        var busClass = new SeatClass
        {
            Code = "BUS",
            Name = "Business",
            RefundPercent = 80,
            ChangeFee = 200000,
            Priority = 2
        };

        context.SeatClasses.AddRange(ecoClass, busClass);
        await context.SaveChangesAsync();

        // Seed Aircraft
        var aircraft1 = new Aircraft
        {
            Model = "Boeing 737",
            RegistrationNumber = "VN-ABC123",
            TotalSeats = 180,
            IsActive = true
        };

        var aircraft2 = new Aircraft
        {
            Model = "Airbus A320",
            RegistrationNumber = "VN-XYZ789",
            TotalSeats = 220,
            IsActive = true
        };

        context.Aircraft.AddRange(aircraft1, aircraft2);
        await context.SaveChangesAsync();

        // Seed Flights
        var flight1 = new Flight
        {
            RouteId = route1.Id,
            AircraftId = aircraft1.Id,
            FlightNumber = "VN001",
            DepartureTime = DateTime.UtcNow.AddDays(1).Date.AddHours(8),
            ArrivalTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10).AddMinutes(25),
            Status = 0,
            CreatedAt = DateTime.UtcNow
        };

        var flight2 = new Flight
        {
            RouteId = route2.Id,
            AircraftId = aircraft2.Id,
            FlightNumber = "VN002",
            DepartureTime = DateTime.UtcNow.AddDays(1).Date.AddHours(14),
            ArrivalTime = DateTime.UtcNow.AddDays(1).Date.AddHours(16).AddMinutes(25),
            Status = 0,
            CreatedAt = DateTime.UtcNow
        };

        context.Flights.AddRange(flight1, flight2);
        await context.SaveChangesAsync();

        // Seed Flight Seat Inventories
        var inv1_eco = new FlightSeatInventory
        {
            FlightId = flight1.Id,
            SeatClassId = ecoClass.Id,
            TotalSeats = 150,
            AvailableSeats = 150,
            BasePrice = 1500000,
            CurrentPrice = 1650000
        };

        var inv1_bus = new FlightSeatInventory
        {
            FlightId = flight1.Id,
            SeatClassId = busClass.Id,
            TotalSeats = 30,
            AvailableSeats = 30,
            BasePrice = 3000000,
            CurrentPrice = 3300000
        };

        context.FlightSeatInventories.AddRange(inv1_eco, inv1_bus);
        await context.SaveChangesAsync();

        // Seed Promotions
        var promo1 = new Promotion
        {
            Code = "SUMMER20",
            DiscountType = 0,
            DiscountValue = 20,
            ValidFrom = DateTime.UtcNow,
            ValidTo = DateTime.UtcNow.AddMonths(3),
            UsageLimit = 500,
            UsedCount = 123,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Promotions.Add(promo1);
        await context.SaveChangesAsync();

        // Seed Bookings
        var booking1 = new Booking
        {
            UserId = testUser1.Id,
            OutboundFlightId = flight1.Id,
            BookingCode = GenerateBookingCode(),
            Status = 0,
            ContactEmail = testUser1.Email,
            TotalAmount = 1650000,
            FinalAmount = 1485000,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };

        context.Bookings.Add(booking1);
        await context.SaveChangesAsync();

        // Seed Booking Passengers
        var passenger1 = new BookingPassenger
        {
            BookingId = booking1.Id,
            FullName = "Nguyễn Văn A",
            DateOfBirth = new DateTime(1990, 5, 15),
            NationalId = "001090xxxxxx",
            PassengerType = 0,
            FlightSeatInventoryId = inv1_eco.Id
        };

        context.BookingPassengers.Add(passenger1);
        await context.SaveChangesAsync();

        Console.WriteLine("✅ All sample data seeded successfully!");
    }

    private static string GenerateBookingCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
        return $"BK{code}";
    }
}
