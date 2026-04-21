namespace API.Infrastructure.Data;

using API.Domain.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Extension methods to add additional test data for searching features
/// </summary>
public static class SampleDataForSearching
{
    /// <summary>
    /// Add comprehensive sample data for testing search features
    /// Call this after DbInitializer.InitializeDatabaseAsync()
    /// </summary>
    public static async Task AddSearchTestDataAsync(FlightBookingDbContext context)
    {
        try
        {
            // Check if sample data already exists
            if (context.Flights.Count() >= 10)
            {
                Console.WriteLine("ℹ️ Sample search data already exists. Skipping...");
                return;
            }

            Console.WriteLine("📊 Adding comprehensive sample data for search testing...");

            // ===== AIRPORTS =====
            var airports = new[]
            {
                new Airport { Code = "SGN", Name = "Sân bay Tân Sơn Nhất", City = "Thành phố Hồ Chí Minh", Province = "Hồ Chí Minh", IsActive = true },
                new Airport { Code = "HAN", Name = "Sân bay Nội Bài", City = "Hà Nội", Province = "Hà Nội", IsActive = true },
                new Airport { Code = "DAD", Name = "Sân bay Quốc tế Đà Nẵng", City = "Đà Nẵng", Province = "Đà Nẵng", IsActive = true },
                new Airport { Code = "CTS", Name = "Sân bay Cần Thơ", City = "Cần Thơ", Province = "Cần Thơ", IsActive = true },
                new Airport { Code = "VCA", Name = "Sân bay Buôn Mê Thuộc", City = "Buôn Mê Thuộc", Province = "Đắk Lắk", IsActive = true },
                new Airport { Code = "HUI", Name = "Sân bay Phú Bài", City = "Huế", Province = "Thừa Thiên Huế", IsActive = true }
            };

            var existingAirports = context.Airports.Select(a => a.Code).ToList();
            var newAirports = airports.Where(a => !existingAirports.Contains(a.Code)).ToList();
            if (newAirports.Count > 0)
            {
                context.Airports.AddRange(newAirports);
                await context.SaveChangesAsync();
            }

            // ===== ROUTES =====
            var sgn = context.Airports.First(a => a.Code == "SGN");
            var han = context.Airports.First(a => a.Code == "HAN");
            var dad = context.Airports.First(a => a.Code == "DAD");
            var cts = context.Airports.First(a => a.Code == "CTS");
            var vca = context.Airports.FirstOrDefault(a => a.Code == "VCA");
            var hui = context.Airports.FirstOrDefault(a => a.Code == "HUI");

            var routes = new[]
            {
                new Route { DepartureAirportId = sgn.Id, ArrivalAirportId = han.Id, DistanceKm = 1700, EstimatedDurationMinutes = 145, IsActive = true },
                new Route { DepartureAirportId = han.Id, ArrivalAirportId = sgn.Id, DistanceKm = 1700, EstimatedDurationMinutes = 145, IsActive = true },
                new Route { DepartureAirportId = sgn.Id, ArrivalAirportId = dad.Id, DistanceKm = 960, EstimatedDurationMinutes = 100, IsActive = true },
                new Route { DepartureAirportId = dad.Id, ArrivalAirportId = sgn.Id, DistanceKm = 960, EstimatedDurationMinutes = 100, IsActive = true },
                new Route { DepartureAirportId = han.Id, ArrivalAirportId = dad.Id, DistanceKm = 500, EstimatedDurationMinutes = 75, IsActive = true },
                new Route { DepartureAirportId = sgn.Id, ArrivalAirportId = cts.Id, DistanceKm = 330, EstimatedDurationMinutes = 55, IsActive = true },
                new Route { DepartureAirportId = cts.Id, ArrivalAirportId = sgn.Id, DistanceKm = 330, EstimatedDurationMinutes = 55, IsActive = true }
            };

            var existingRouteCount = context.Routes.Count();
            if (existingRouteCount < 7)
            {
                context.Routes.AddRange(routes.Skip(existingRouteCount));
                await context.SaveChangesAsync();
            }

            // ===== AIRCRAFT =====
            var aircraft = new[]
            {
                new Aircraft { Model = "Boeing 737", RegistrationNumber = "VN-ABC123", TotalSeats = 180, IsActive = true },
                new Aircraft { Model = "Airbus A320", RegistrationNumber = "VN-XYZ789", TotalSeats = 220, IsActive = true },
                new Aircraft { Model = "Boeing 787", RegistrationNumber = "VN-DEF456", TotalSeats = 242, IsActive = true },
                new Aircraft { Model = "Airbus A321", RegistrationNumber = "VN-GHI789", TotalSeats = 236, IsActive = true },
                new Aircraft { Model = "Boeing 777", RegistrationNumber = "VN-JKL012", TotalSeats = 350, IsActive = true }
            };

            var existingAircraftCount = context.Aircraft.Count();
            if (existingAircraftCount < 5)
            {
                context.Aircraft.AddRange(aircraft.Skip(existingAircraftCount));
                await context.SaveChangesAsync();
            }

            // ===== SEAT CLASSES =====
            var seatClasses = context.SeatClasses.ToList();
            if (!seatClasses.Any(sc => sc.Code == "PRM"))
            {
                context.SeatClasses.Add(new SeatClass { Code = "PRM", Name = "Premium", RefundPercent = 60, ChangeFee = 300000, Priority = 1 });
                await context.SaveChangesAsync();
            }

            var ecoClass = context.SeatClasses.First(sc => sc.Code == "ECO");
            var busClass = context.SeatClasses.First(sc => sc.Code == "BUS");
            var prmClass = context.SeatClasses.FirstOrDefault(sc => sc.Code == "PRM");

            // ===== FLIGHTS - Multiple per day, different times, routes =====
            var now = DateTime.UtcNow;
            var baseDates = Enumerable.Range(0, 7).Select(i => now.AddDays(i).Date).ToList();

            var routeList = context.Routes.ToList();
            var aircraftList = context.Aircraft.ToList();
            var existingFlights = context.Flights.Count();

            if (existingFlights < 30)
            {
                var flights = new List<Flight>();
                int flightCounter = 1;

                foreach (var date in baseDates)
                {
                    // Múltiple flights per day on different times
                    var times = new[] { 6, 8, 10, 12, 14, 16, 18, 20 }; // 8 flights per day

                    foreach (var hour in times)
                    {
                        foreach (var route in routeList.Take(3)) // Mix different routes
                        {
                            var selectedAircraft = aircraftList[(flights.Count % aircraftList.Count)];
                            var flightNumber = $"VN{flightCounter:D3}";
                            var departureTime = date.AddHours(hour).AddMinutes(Random.Shared.Next(0, 60));
                            var duration = route.EstimatedDurationMinutes;
                            var arrivalTime = departureTime.AddMinutes(duration);

                            flights.Add(new Flight
                            {
                                FlightNumber = flightNumber,
                                RouteId = route.Id,
                                AircraftId = selectedAircraft.Id,
                                DepartureTime = departureTime,
                                ArrivalTime = arrivalTime,
                                Status = Random.Shared.Next(0, 2), // 0=Active, 1=Cancelled
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });

                            flightCounter++;
                        }
                    }
                }

                context.Flights.AddRange(flights.Skip(existingFlights));
                await context.SaveChangesAsync();
            }

            // ===== FLIGHT SEAT INVENTORIES =====
            var allFlights = context.Flights.ToList();
            var existingInventories = context.FlightSeatInventories.Count();

            if (existingInventories < allFlights.Count * 2)
            {
                var inventories = new List<FlightSeatInventory>();

                foreach (var flight in allFlights)
                {
                    // Economy seats
                    var totalEco = 150 + Random.Shared.Next(-20, 20);
                    var availableEco = Random.Shared.Next(Math.Max(10, totalEco - 100), totalEco);
                    var priceEco = 1200000 + Random.Shared.Next(-200000, 400000);

                    inventories.Add(new FlightSeatInventory
                    {
                        FlightId = flight.Id,
                        SeatClassId = ecoClass.Id,
                        TotalSeats = totalEco,
                        AvailableSeats = availableEco,
                        SoldSeats = totalEco - availableEco - Random.Shared.Next(0, 20),
                        HeldSeats = Random.Shared.Next(0, 20),
                        BasePrice = priceEco,
                        CurrentPrice = (long)(priceEco * (1 + Random.Shared.NextDouble() * 0.2)),
                        Version = 0
                    });

                    // Business seats
                    var totalBus = 40 + Random.Shared.Next(-5, 15);
                    var availableBus = Random.Shared.Next(Math.Max(5, totalBus - 30), totalBus);
                    var priceBus = 2800000 + Random.Shared.Next(-300000, 600000);

                    inventories.Add(new FlightSeatInventory
                    {
                        FlightId = flight.Id,
                        SeatClassId = busClass.Id,
                        TotalSeats = totalBus,
                        AvailableSeats = availableBus,
                        SoldSeats = totalBus - availableBus - Random.Shared.Next(0, 10),
                        HeldSeats = Random.Shared.Next(0, 10),
                        BasePrice = priceBus,
                        CurrentPrice = (long)(priceBus * (1 + Random.Shared.NextDouble() * 0.2)),
                        Version = 0
                    });

                    // Premium seats (some flights only)
                    if (Random.Shared.Next(0, 2) == 0 && prmClass != null)
                    {
                        var totalPrm = 15 + Random.Shared.Next(-3, 8);
                        var availablePrm = Random.Shared.Next(Math.Max(2, totalPrm - 12), totalPrm);
                        var pricePrm = 4500000 + Random.Shared.Next(-400000, 800000);

                        inventories.Add(new FlightSeatInventory
                        {
                            FlightId = flight.Id,
                            SeatClassId = prmClass.Id,
                            TotalSeats = totalPrm,
                            AvailableSeats = availablePrm,
                            SoldSeats = totalPrm - availablePrm - Random.Shared.Next(0, 5),
                            HeldSeats = Random.Shared.Next(0, 5),
                            BasePrice = pricePrm,
                            CurrentPrice = (long)(pricePrm * (1 + Random.Shared.NextDouble() * 0.2)),
                            Version = 0
                        });
                    }
                }

                context.FlightSeatInventories.AddRange(inventories.Skip(existingInventories));
                await context.SaveChangesAsync();
            }

            // ===== PROMOTIONS =====
            var existingPromos = context.Promotions.Count();
            if (existingPromos < 8)
            {
                var promos = new[]
                {
                    new Promotion { Code = "SUMMER20", DiscountType = 0, DiscountValue = 20, ValidFrom = now.AddDays(-10), ValidTo = now.AddDays(90), UsageLimit = 500, UsedCount = 45, IsActive = true, CreatedAt = now },
                    new Promotion { Code = "SAVE500K", DiscountType = 1, DiscountValue = 500000, ValidFrom = now.AddDays(-5), ValidTo = now.AddDays(30), UsageLimit = 300, UsedCount = 120, IsActive = true, CreatedAt = now },
                    new Promotion { Code = "EARLYBIRD15", DiscountType = 0, DiscountValue = 15, ValidFrom = now, ValidTo = now.AddDays(60), UsageLimit = 200, UsedCount = 80, IsActive = true, CreatedAt = now },
                    new Promotion { Code = "VIP10", DiscountType = 0, DiscountValue = 10, ValidFrom = now, ValidTo = now.AddDays(45), UsageLimit = 50, UsedCount = 50, IsActive = true, CreatedAt = now },
                    new Promotion { Code = "NEWYEAR2025", DiscountType = 0, DiscountValue = 30, ValidFrom = now, ValidTo = now.AddDays(120), UsageLimit = 1000, UsedCount = 5, IsActive = true, CreatedAt = now },
                    new Promotion { Code = "BUSINESS25", DiscountType = 0, DiscountValue = 25, ValidFrom = now.AddDays(-20), ValidTo = now.AddDays(40), UsageLimit = 250, UsedCount = 150, IsActive = true, CreatedAt = now },
                    new Promotion { Code = "WEEKEND", DiscountType = 1, DiscountValue = 300000, ValidFrom = now, ValidTo = now.AddDays(180), UsageLimit = 400, UsedCount = 75, IsActive = true, CreatedAt = now },
                    new Promotion { Code = "STUDENT15", DiscountType = 0, DiscountValue = 15, ValidFrom = now.AddDays(-15), ValidTo = now.AddDays(100), UsageLimit = 600, UsedCount = 200, IsActive = true, CreatedAt = now }
                };

                context.Promotions.AddRange(promos.Skip(existingPromos));
                await context.SaveChangesAsync();
            }

            Console.WriteLine("✅ Sample search data added successfully!");
            Console.WriteLine($"   - Flights: {context.Flights.Count()} records");
            Console.WriteLine($"   - Inventories: {context.FlightSeatInventories.Count()} records");
            Console.WriteLine($"   - Promotions: {context.Promotions.Count()} records");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error adding sample data: {ex.Message}");
            throw;
        }
    }
}
