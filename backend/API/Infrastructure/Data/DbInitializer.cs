namespace API.Infrastructure.Data;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Seed dữ liệu mẫu cho ứng dụng quản lý đặt vé máy bay
/// Sample data: Airports, Routes, Aircraft, Flights, Users, Promotions
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(FlightBookingDbContext context)
    {
        try
        {
            // Áp dụng migrations
            await context.Database.MigrateAsync();

            // Kiểm tra nếu đã có dữ liệu thì không seed lại
            if (context.Users.Any())
            {
                return; // Database đã có dữ liệu rồi
            }

            // Seed Roles
            var adminRole = new Role { Name = "Admin", Description = "Quản trị viên hệ thống" };
            var userRole = new Role { Name = "User", Description = "Người dùng thường" };
            var staffRole = new Role { Name = "Staff", Description = "Nhân viên" };

            context.Roles.AddRange(adminRole, userRole, staffRole);
            await context.SaveChangesAsync();

            // Seed Users
            var adminUser = new User
            {
                Email = "admin@flightbooking.vn",
                FullName = "Quản trị viên",
                Phone = "0901234567",
                PasswordHash = "$2a$11$YourHashedPasswordHere", // Cần hash thực tế
                Status = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var testUser1 = new User
            {
                Email = "user1@gmail.com",
                FullName = "Nguyễn Văn A",
                Phone = "0912345678",
                PasswordHash = "$2a$11$YourHashedPasswordHere",
                Status = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var testUser2 = new User
            {
                Email = "user2@gmail.com",
                FullName = "Trần Thị B",
                Phone = "0923456789",
                PasswordHash = "$2a$11$YourHashedPasswordHere",
                Status = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(adminUser, testUser1, testUser2);
            await context.SaveChangesAsync();

            // Seed Airports (Sân bay)
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

            // Seed Routes (Đường bay)
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

            var route3 = new Route
            {
                DepartureAirportId = sgAirport.Id,
                ArrivalAirportId = dnAirport.Id,
                DistanceKm = 960,
                EstimatedDurationMinutes = 95,
                IsActive = true
            };

            var route4 = new Route
            {
                DepartureAirportId = dnAirport.Id,
                ArrivalAirportId = hnAirport.Id,
                DistanceKm = 760,
                EstimatedDurationMinutes = 75,
                IsActive = true
            };

            context.Routes.AddRange(route1, route2, route3, route4);
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

            var firstClass = new SeatClass
            {
                Code = "FIRST",
                Name = "First",
                RefundPercent = 50,
                ChangeFee = 300000,
                Priority = 1
            };

            context.SeatClasses.AddRange(ecoClass, busClass, firstClass);
            await context.SaveChangesAsync();

            // Seed Aircraft (Máy bay)
            var aircraft1 = new Aircraft
            {
                Model = "Boeing 737",
                RegistrationNumber = "VN-ABC123",
                TotalSeats = 180,
                IsActive = true
            };

            var aircraft2 = new Aircraft
            {
                Model = "Airbus A321",
                RegistrationNumber = "VN-XYZ789",
                TotalSeats = 220,
                IsActive = true
            };

            context.Aircraft.AddRange(aircraft1, aircraft2);
            await context.SaveChangesAsync();

            // Seed Aircraft Seat Templates
            var template1_eco = new AircraftSeatTemplate
            {
                AircraftId = aircraft1.Id,
                SeatClassId = ecoClass.Id,
                DefaultSeatCount = 140,
                DefaultBasePrice = 1500000 // 1.5 triệu VND
            };

            var template1_bus = new AircraftSeatTemplate
            {
                AircraftId = aircraft1.Id,
                SeatClassId = busClass.Id,
                DefaultSeatCount = 30,
                DefaultBasePrice = 3500000 // 3.5 triệu VND
            };

            var template1_first = new AircraftSeatTemplate
            {
                AircraftId = aircraft1.Id,
                SeatClassId = firstClass.Id,
                DefaultSeatCount = 10,
                DefaultBasePrice = 5500000 // 5.5 triệu VND
            };

            context.AircraftSeatTemplates.AddRange(template1_eco, template1_bus, template1_first);
            await context.SaveChangesAsync();

            // Seed Flights (Chuyến bay)
            var tomorrow = DateTime.UtcNow.AddDays(1);
            var nextWeek = DateTime.UtcNow.AddDays(7);

            var flight1 = new Flight
            {
                FlightNumber = "VN001",
                RouteId = route1.Id,
                AircraftId = aircraft1.Id,
                DepartureTime = tomorrow.AddHours(8),
                ArrivalTime = tomorrow.AddHours(10).AddMinutes(25),
                Status = 0, // Active
                CreatedAt = DateTime.UtcNow
            };

            var flight2 = new Flight
            {
                FlightNumber = "VN002",
                RouteId = route2.Id,
                AircraftId = aircraft2.Id,
                DepartureTime = tomorrow.AddHours(14),
                ArrivalTime = tomorrow.AddHours(16).AddMinutes(25),
                Status = 0,
                CreatedAt = DateTime.UtcNow
            };

            var flight3 = new Flight
            {
                FlightNumber = "VN003",
                RouteId = route3.Id,
                AircraftId = aircraft1.Id,
                DepartureTime = nextWeek.AddHours(9),
                ArrivalTime = nextWeek.AddHours(10).AddMinutes(35),
                Status = 0,
                CreatedAt = DateTime.UtcNow
            };

            context.Flights.AddRange(flight1, flight2, flight3);
            await context.SaveChangesAsync();

            // Seed Flight Seat Inventories
            var inv1_eco = new FlightSeatInventory
            {
                FlightId = flight1.Id,
                SeatClassId = ecoClass.Id,
                TotalSeats = 140,
                AvailableSeats = 135,
                HeldSeats = 5,
                SoldSeats = 0,
                BasePrice = 1500000,
                CurrentPrice = 1650000, // Dynamic pricing
                CreatedAt = DateTime.UtcNow
            };

            var inv1_bus = new FlightSeatInventory
            {
                FlightId = flight1.Id,
                SeatClassId = busClass.Id,
                TotalSeats = 30,
                AvailableSeats = 28,
                HeldSeats = 2,
                SoldSeats = 0,
                BasePrice = 3500000,
                CurrentPrice = 3850000,
                CreatedAt = DateTime.UtcNow
            };

            context.FlightSeatInventories.AddRange(inv1_eco, inv1_bus);
            await context.SaveChangesAsync();

            // Seed Promotions (Khuyến mãi)
            var promo1 = new Promotion
            {
                Code = "SUMMER2024",
                DiscountType = 0, // Percentage
                DiscountValue = 10, // 10%
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddMonths(3),
                UsageLimit = 500,
                UsedCount = 45,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var promo2 = new Promotion
            {
                Code = "EARLYBIRD100K",
                DiscountType = 1, // Fixed amount
                DiscountValue = 100000,
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddMonths(1),
                UsageLimit = 1000,
                UsedCount = 234,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var promo3 = new Promotion
            {
                Code = "NEWUSER20",
                DiscountType = 0,
                DiscountValue = 20,
                ValidFrom = DateTime.UtcNow,
                ValidTo = DateTime.UtcNow.AddMonths(6),
                UsageLimit = 10000,
                UsedCount = 567,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Promotions.AddRange(promo1, promo2, promo3);
            await context.SaveChangesAsync();

            // Seed Refund Policies
            var refundPolicy = new RefundPolicy
            {
                SeatClassId = ecoClass.Id,
                HoursBeforeDeparture = 24,
                RefundPercent = 100,
                PenaltyFee = 0
            };

            context.RefundPolicies.Add(refundPolicy);
            await context.SaveChangesAsync();

            // Seed Bookings (Tùy chọn - để test)
            var booking1 = new Booking
            {
                UserId = testUser1.Id,
                OutboundFlightId = flight1.Id,
                BookingCode = GenerateBookingCode(),
                Status = 0, // Pending
                TotalAmount = 1650000,
                FinalAmount = 1485000, // Sau khi áp dụng khuyến mãi
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

            Console.WriteLine("✅ Dữ liệu mẫu đã được seed thành công!");
            Console.WriteLine("Sample data seeding completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi khi seed dữ liệu: {ex.Message}");
            Console.WriteLine($"Error seeding database: {ex.Message}");
            throw;
        }
    }

    private static string GenerateBookingCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
}
