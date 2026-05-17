using System.Security.Cryptography;
using System.Text;
using API.Application.Dtos.Admin;
using API.Application.Dtos.Flight;
using API.Application.Dtos.Payment;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Application.Services;
using API.Domain.Entities;
using API.Infrastructure.Data;
using API.Infrastructure.ExternalServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

internal static class AssertEx
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"{message}. Expected: {expected}, Actual: {actual}");
        }
    }
}

internal sealed class Program
{
    public static async Task Main()
    {
        await TestCreateFlightCreatesSeatInventories();
        await TestPaymentSuccessConfirmsBookingAndConvertsSeats();
        await TestCancelFlightCancelsBookingsQueuesRefundAndSendsEmails();
        await TestPaymentFailedCallbackCancelsBookingAndReleasesHeldSeats();
        await TestPaymentInvalidSignatureDoesNotChangeState();
        await TestFlightSearchValidationErrors();
        await TestAdminCancelBookingQueuesRefundForConfirmedBooking();
        await TestAdminCancelBookingRejectsInvalidStatus();
        await TestPassengerServicesFlow_AddUpdateRemove();
        await TestPassengerServices_RejectInfantOptionalService();
        await TestPassengerServices_RejectServiceNotAllowedBySeatClass();
        Console.WriteLine("PASS");
    }

    private static async Task TestPassengerServicesFlow_AddUpdateRemove()
    {
        var db = BuildInMemoryDbContext(nameof(TestPassengerServicesFlow_AddUpdateRemove));
        await SeedPassengerServiceBaseDataAsync(db, seatClassId: 1, includedServiceId: 10, optionalServiceId: 11);

        var booking = new Booking
        {
            Id = 9001,
            UserId = 99,
            BookingCode = "BK9001",
            OutboundFlightId = 501,
            Status = (int)BookingStatus.Pending,
            ContactEmail = "booker1@test.com",
            TotalAmount = 1_000_000m,
            FinalAmount = 1_000_000m,
            DiscountAmount = 0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var passenger = new BookingPassenger
        {
            Id = 9101,
            BookingId = 9001,
            FirstName = "Test",
            LastName = "Adult",
            FullName = "Test Adult",
            Email = "adult@test.com",
            Phone = "090",
            FlightSeatInventoryId = 801,
            PassengerType = (int)PassengerType.Adult,
            DocumentCheckStatus = (int)PassengerDocumentCheckStatus.Pending
        };
        db.Bookings.Add(booking);
        db.BookingPassengers.Add(passenger);
        db.BookingServices.Add(new API.Domain.Entities.BookingService
        {
            Id = 9201,
            BookingPassengerId = 9101,
            AdditionalServiceId = 10,
            Quantity = 1,
            Price = 0m,
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var uow = new FakeUnitOfWork
        {
            BookingRepo = new FakeBookingRepository(new List<Booking> { booking }),
            PassengerRepo = new FakeBookingPassengerRepository(new List<BookingPassenger> { passenger })
        };

        var service = BuildBookingServiceForTests(uow, db);

        var added = await service.AddPassengerServiceAsync(9001, 9101, 99, new API.Application.Dtos.Booking.AddPassengerServiceDto
        {
            AdditionalServiceId = 11,
            Quantity = 2
        });

        AssertEx.Equal(2, added.Quantity, "AddPassengerService should persist quantity");
        AssertEx.Equal(1_100_000m, booking.TotalAmount, "Booking total should increase by optional service amount");

        var updated = await service.UpdatePassengerServiceAsync(9001, 9101, added.BookingServiceId, 99, new API.Application.Dtos.Booking.UpdatePassengerServiceDto
        {
            Quantity = 3
        });
        AssertEx.Equal(3, updated.Quantity, "UpdatePassengerService should update quantity");
        AssertEx.Equal(1_150_000m, booking.TotalAmount, "Booking total should adjust by quantity delta");

        await service.RemovePassengerServiceAsync(9001, 9101, added.BookingServiceId, 99);
        AssertEx.Equal(1_000_000m, booking.TotalAmount, "RemovePassengerService should revert optional service amount");
    }

    private static async Task TestPassengerServices_RejectInfantOptionalService()
    {
        var db = BuildInMemoryDbContext(nameof(TestPassengerServices_RejectInfantOptionalService));
        await SeedPassengerServiceBaseDataAsync(db, seatClassId: 1, includedServiceId: 10, optionalServiceId: 11);

        var booking = new Booking
        {
            Id = 9002,
            UserId = 100,
            BookingCode = "BK9002",
            OutboundFlightId = 502,
            Status = (int)BookingStatus.Pending,
            ContactEmail = "booker2@test.com",
            TotalAmount = 100_000m,
            FinalAmount = 100_000m,
            DiscountAmount = 0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var infant = new BookingPassenger
        {
            Id = 9102,
            BookingId = 9002,
            FirstName = "Baby",
            LastName = "Test",
            FullName = "Baby Test",
            Email = "baby@test.com",
            Phone = "091",
            FlightSeatInventoryId = 801,
            PassengerType = (int)PassengerType.Infant,
            DocumentCheckStatus = (int)PassengerDocumentCheckStatus.NotRequired
        };
        db.Bookings.Add(booking);
        db.BookingPassengers.Add(infant);
        await db.SaveChangesAsync();

        var uow = new FakeUnitOfWork
        {
            BookingRepo = new FakeBookingRepository(new List<Booking> { booking }),
            PassengerRepo = new FakeBookingPassengerRepository(new List<BookingPassenger> { infant })
        };
        var service = BuildBookingServiceForTests(uow, db);

        var rejected = false;
        try
        {
            await service.AddPassengerServiceAsync(9002, 9102, 100, new API.Application.Dtos.Booking.AddPassengerServiceDto
            {
                AdditionalServiceId = 11,
                Quantity = 1
            });
        }
        catch (ValidationException)
        {
            rejected = true;
        }

        AssertEx.True(rejected, "Infant passenger must be rejected when adding optional service");
    }

    private static async Task TestPassengerServices_RejectServiceNotAllowedBySeatClass()
    {
        var db = BuildInMemoryDbContext(nameof(TestPassengerServices_RejectServiceNotAllowedBySeatClass));
        await SeedPassengerServiceBaseDataAsync(db, seatClassId: 1, includedServiceId: 10, optionalServiceId: 11);
        db.AdditionalServices.Add(new AdditionalService
        {
            Id = 12,
            ServiceName = "Seat Selection Premium",
            Price = 200_000m,
            Description = "not configured for seat class",
            IsDeleted = false
        });
        await db.SaveChangesAsync();

        var booking = new Booking
        {
            Id = 9003,
            UserId = 101,
            BookingCode = "BK9003",
            OutboundFlightId = 503,
            Status = (int)BookingStatus.Pending,
            ContactEmail = "booker3@test.com",
            TotalAmount = 1_000_000m,
            FinalAmount = 1_000_000m,
            DiscountAmount = 0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var passenger = new BookingPassenger
        {
            Id = 9103,
            BookingId = 9003,
            FirstName = "Rule",
            LastName = "Check",
            FullName = "Rule Check",
            Email = "rule@test.com",
            Phone = "092",
            FlightSeatInventoryId = 801,
            PassengerType = (int)PassengerType.Adult,
            DocumentCheckStatus = (int)PassengerDocumentCheckStatus.Pending
        };
        db.Bookings.Add(booking);
        db.BookingPassengers.Add(passenger);
        await db.SaveChangesAsync();

        var uow = new FakeUnitOfWork
        {
            BookingRepo = new FakeBookingRepository(new List<Booking> { booking }),
            PassengerRepo = new FakeBookingPassengerRepository(new List<BookingPassenger> { passenger })
        };
        var service = BuildBookingServiceForTests(uow, db);

        var rejected = false;
        try
        {
            await service.AddPassengerServiceAsync(9003, 9103, 101, new API.Application.Dtos.Booking.AddPassengerServiceDto
            {
                AdditionalServiceId = 12,
                Quantity = 1
            });
        }
        catch (ValidationException)
        {
            rejected = true;
        }

        AssertEx.True(rejected, "Service outside seat-class optional policy must be rejected");
    }

    private static FlightBookingDbContext BuildInMemoryDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<FlightBookingDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .Options;
        return new FlightBookingDbContext(options);
    }

    private static async Task SeedPassengerServiceBaseDataAsync(
        FlightBookingDbContext db,
        int seatClassId,
        int includedServiceId,
        int optionalServiceId)
    {
        var seatClass = new SeatClass { Id = seatClassId, Code = "ECO", Name = "Economy", Priority = 3 };
        var inventory = new FlightSeatInventory
        {
            Id = 801,
            FlightId = 500,
            SeatClassId = seatClassId,
            TotalSeats = 100,
            AvailableSeats = 100,
            HeldSeats = 0,
            SoldSeats = 0,
            BasePrice = 1_000_000m,
            CurrentPrice = 1_000_000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var includedService = new AdditionalService
        {
            Id = includedServiceId,
            ServiceName = "Carry-on",
            Price = 0m,
            Description = "included",
            IsDeleted = false
        };
        var optionalService = new AdditionalService
        {
            Id = optionalServiceId,
            ServiceName = "Checked Baggage",
            Price = 50_000m,
            Description = "optional",
            IsDeleted = false
        };

        db.SeatClasses.Add(seatClass);
        db.FlightSeatInventories.Add(inventory);
        db.AdditionalServices.AddRange(includedService, optionalService);
        db.ClassServiceConfigs.AddRange(
            new ClassServiceConfig
            {
                SeatClassId = seatClassId,
                AdditionalServiceId = includedServiceId,
                IsIncluded = true
            },
            new ClassServiceConfig
            {
                SeatClassId = seatClassId,
                AdditionalServiceId = optionalServiceId,
                IsIncluded = false
            });
        await db.SaveChangesAsync();
    }

    private static API.Application.Services.BookingService BuildBookingServiceForTests(FakeUnitOfWork uow, FlightBookingDbContext db)
    {
        return new API.Application.Services.BookingService(
            uow,
            new FakePromotionService(),
            new FakeBackgroundJobService(),
            db,
            BuildVnpayProvider("test-secret-hash-key-1234567890"),
            LoggerFactory.Create(_ => { }).CreateLogger<API.Application.Services.BookingService>());
    }

    private static async Task TestCreateFlightCreatesSeatInventories()
    {
        var route = new Route
        {
            Id = 10,
            DepartureAirport = new Airport { Id = 1, Code = "SGN", Name = "Tan Son Nhat", City = "HCM" },
            ArrivalAirport = new Airport { Id = 2, Code = "HAN", Name = "Noi Bai", City = "Ha Noi" }
        };
        var aircraft = new Aircraft
        {
            Id = 20,
            Model = "A321",
            RegistrationNumber = "VN-A321",
            SeatTemplates = new List<AircraftSeatTemplate>
            {
                new() { SeatClassId = 1, DefaultSeatCount = 120, DefaultBasePrice = 1000000m, IsDeleted = false },
                new() { SeatClassId = 2, DefaultSeatCount = 20, DefaultBasePrice = 3000000m, IsDeleted = false }
            }
        };

        var uow = new FakeUnitOfWork
        {
            RoutesRepo = new FakeRouteRepository(route),
            AircraftRepo = new FakeAircraftRepository(aircraft),
            FlightDefinitionRepo = new FakeFlightDefinitionRepository(),
            FlightsRepo = new FakeFlightRepository(),
            SeatInventoryRepo = new FakeSeatInventoryRepository()
        };

        var service = new FlightAdminService(
            LoggerFactory.Create(_ => { }).CreateLogger<FlightAdminService>(),
            uow,
            new FakeBackgroundJobService(),
            new FakeEmailService());

        var created = await service.CreateFlightAsync(new CreateFlightDto
        {
            FlightNumber = "VN123",
            RouteId = 10,
            AircraftId = 20,
            DepartureTime = DateTime.UtcNow.AddDays(2),
            ArrivalTime = DateTime.UtcNow.AddDays(2).AddHours(2),
            IsActive = true
        });

        AssertEx.True(created.FlightId > 0, "CreateFlightAsync should return persisted flight id");
        var inventories = uow.SeatInventoryRepo.Items.Where(x => x.FlightId == created.FlightId).ToList();
        AssertEx.Equal(2, inventories.Count, "CreateFlightAsync should auto-create seat inventories from aircraft templates");
        AssertEx.Equal(140, inventories.Sum(x => x.TotalSeats), "Total seats should match seat template sum");
    }

    private static async Task TestPaymentSuccessConfirmsBookingAndConvertsSeats()
    {
        var booking = new Booking
        {
            Id = 1,
            UserId = 99,
            BookingCode = "BK0001",
            OutboundFlightId = 100,
            Status = (int)BookingStatus.Pending,
            ContactEmail = "user@test.com",
            TotalAmount = 2000000m,
            FinalAmount = 2000000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var payment = new Payment
        {
            Id = 7,
            BookingId = 1,
            Provider = "VNPAY",
            Method = "VNPAY",
            Amount = 2000000m,
            Status = (int)PaymentStatus.Pending,
            TransactionRef = "TXN-001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var inventory = new FlightSeatInventory
        {
            Id = 11,
            FlightId = 100,
            SeatClassId = 1,
            TotalSeats = 100,
            AvailableSeats = 98,
            HeldSeats = 2,
            SoldSeats = 0,
            BasePrice = 1000000m,
            CurrentPrice = 1000000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var passengers = new List<BookingPassenger>
        {
            new() { Id = 1, BookingId = 1, FirstName = "A", LastName = "B", FullName = "A B", Email = "a@test.com", Phone = "1", FlightSeatInventoryId = 11 },
            new() { Id = 2, BookingId = 1, FirstName = "C", LastName = "D", FullName = "C D", Email = "c@test.com", Phone = "2", FlightSeatInventoryId = 11 }
        };

        var uow = new FakeUnitOfWork
        {
            BookingRepo = new FakeBookingRepository(booking),
            PaymentRepo = new FakePaymentRepository(payment),
            SeatInventoryRepo = new FakeSeatInventoryRepository(inventory),
            PassengerRepo = new FakeBookingPassengerRepository(passengers)
        };

        var vnpay = BuildVnpayProvider("test-secret-hash-key-1234567890");
        var ticketService = new FakeTicketService();
        var emailService = new FakeEmailService();

        var service = new PaymentService(
            uow,
            uow.PaymentRepo,
            uow.BookingRepo,
            uow.SeatInventoryRepo,
            uow.PassengerRepo,
            emailService,
            ticketService,
            LoggerFactory.Create(_ => { }).CreateLogger<PaymentService>(),
            vnpay);

        var rawData = "amount=2000000&status=success&transactionId=TXN-001";
        var callback = new PaymentCallbackDto
        {
            TransactionId = "TXN-001",
            Status = "success",
            Amount = 2000000m,
            RawData = rawData,
            Signature = CreateHmacSha512("test-secret-hash-key-1234567890", rawData)
        };

        var ok = await service.ProcessPaymentAsync(7, callback);

        AssertEx.True(ok, "ProcessPaymentAsync should return true on successful callback");
        AssertEx.Equal((int)PaymentStatus.Completed, payment.Status, "Payment should be marked completed");
        AssertEx.Equal((int)BookingStatus.Confirmed, booking.Status, "Booking should be confirmed after successful payment");
        AssertEx.Equal(0, inventory.HeldSeats, "Held seats should be converted");
        AssertEx.Equal(2, inventory.SoldSeats, "Sold seats should increase");
        AssertEx.True(ticketService.CreateCalled, "Ticket creation should be triggered");
    }

    private static VnpayPaymentProvider BuildVnpayProvider(string hashSecret)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["VNPAY:HashSecret"] = hashSecret
            })
            .Build();

        return new VnpayPaymentProvider(
            config,
            LoggerFactory.Create(_ => { }).CreateLogger<VnpayPaymentProvider>(),
            new DummyHttpClientFactory());
    }

    private static string CreateHmacSha512(string secretKey, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static async Task TestCancelFlightCancelsBookingsQueuesRefundAndSendsEmails()
    {
        var flight = new Flight
        {
            Id = 77,
            FlightNumber = "VN777",
            RouteId = 10,
            AircraftId = 20,
            FlightDefinitionId = 99,
            DepartureTime = DateTime.UtcNow.AddDays(1),
            ArrivalTime = DateTime.UtcNow.AddDays(1).AddHours(2),
            Status = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var bookingPaid = new Booking
        {
            Id = 201,
            BookingCode = "BK201",
            UserId = 1,
            OutboundFlightId = 77,
            Status = (int)BookingStatus.Confirmed,
            ContactEmail = "paid@test.com",
            FinalAmount = 1500000m,
            TotalAmount = 1500000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var bookingPending = new Booking
        {
            Id = 202,
            BookingCode = "BK202",
            UserId = 2,
            OutboundFlightId = 77,
            Status = (int)BookingStatus.Pending,
            ContactEmail = "pending@test.com",
            FinalAmount = 900000m,
            TotalAmount = 900000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var paidPayment = new Payment
        {
            Id = 301,
            BookingId = 201,
            Provider = "VNPAY",
            Method = "VNPAY",
            Amount = 1500000m,
            Status = (int)PaymentStatus.Completed,
            TransactionRef = "TXN-201",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var pendingPayment = new Payment
        {
            Id = 302,
            BookingId = 202,
            Provider = "VNPAY",
            Method = "VNPAY",
            Amount = 900000m,
            Status = (int)PaymentStatus.Pending,
            TransactionRef = "TXN-202",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var inventoryConfirmed = new FlightSeatInventory
        {
            Id = 401,
            FlightId = 77,
            SeatClassId = 1,
            TotalSeats = 180,
            AvailableSeats = 176,
            HeldSeats = 0,
            SoldSeats = 4,
            BasePrice = 1000000m,
            CurrentPrice = 1000000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var inventoryPending = new FlightSeatInventory
        {
            Id = 402,
            FlightId = 77,
            SeatClassId = 1,
            TotalSeats = 180,
            AvailableSeats = 178,
            HeldSeats = 2,
            SoldSeats = 0,
            BasePrice = 1000000m,
            CurrentPrice = 1000000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var uow = new FakeUnitOfWork
        {
            FlightsRepo = new FakeFlightRepository(new List<Flight> { flight }),
            BookingRepo = new FakeBookingRepository(new List<Booking> { bookingPaid, bookingPending }),
            PaymentRepo = new FakePaymentRepository(new List<Payment> { paidPayment, pendingPayment }),
            SeatInventoryRepo = new FakeSeatInventoryRepository(new List<FlightSeatInventory> { inventoryConfirmed, inventoryPending }),
            PassengerRepo = new FakeBookingPassengerRepository(new List<BookingPassenger>
            {
                new() { Id = 1, BookingId = 201, FullName = "Paid Pax 1", FirstName = "Paid", LastName = "One", Email = "paid1@test.com", Phone = "1", FlightSeatInventoryId = 401 },
                new() { Id = 2, BookingId = 201, FullName = "Paid Pax 2", FirstName = "Paid", LastName = "Two", Email = "paid2@test.com", Phone = "2", FlightSeatInventoryId = 401 },
                new() { Id = 3, BookingId = 202, FullName = "Pending Pax 1", FirstName = "Pending", LastName = "One", Email = "pending1@test.com", Phone = "3", FlightSeatInventoryId = 402 },
                new() { Id = 4, BookingId = 202, FullName = "Pending Pax 2", FirstName = "Pending", LastName = "Two", Email = "pending2@test.com", Phone = "4", FlightSeatInventoryId = 402 }
            })
        };

        var backgroundJobs = new FakeBackgroundJobService();
        var emailService = new FakeEmailService();
        var service = new FlightAdminService(
            LoggerFactory.Create(_ => { }).CreateLogger<FlightAdminService>(),
            uow,
            backgroundJobs,
            emailService);

        var result = await service.CancelFlightAsync(77, new CancelFlightAdminDto
        {
            Reason = "Operational issue"
        });

        AssertEx.Equal(1, flight.Status, "Flight must be marked cancelled");
        AssertEx.Equal((int)BookingStatus.Cancelled, bookingPaid.Status, "Paid booking must be cancelled");
        AssertEx.Equal((int)BookingStatus.Cancelled, bookingPending.Status, "Pending booking must be cancelled");
        AssertEx.Equal(178, inventoryConfirmed.AvailableSeats, "Confirmed inventory available seats should increase after sold-seat cancellation");
        AssertEx.Equal(2, inventoryConfirmed.SoldSeats, "Confirmed inventory sold seats should decrease");
        AssertEx.Equal(0, inventoryPending.HeldSeats, "Pending inventory held seats should be released");
        AssertEx.Equal(180, inventoryPending.AvailableSeats, "Pending inventory available seats should increase after hold release");
        AssertEx.Equal(1, backgroundJobs.RefundJobs.Count, "Only paid booking should be queued for refund");
        AssertEx.Equal(2, emailService.Notifications.Count, "All affected bookings should receive notification email");
        AssertEx.Equal(2, result.CancelledBookings, "Response cancelled bookings count mismatch");
        AssertEx.Equal(1, result.RefundQueuedBookings, "Response refund queued count mismatch");
        AssertEx.Equal(2, result.NotificationSentBookings, "Response notification count mismatch");
    }

    private static async Task TestPaymentFailedCallbackCancelsBookingAndReleasesHeldSeats()
    {
        var booking = new Booking
        {
            Id = 501,
            UserId = 9,
            BookingCode = "BK501",
            OutboundFlightId = 88,
            Status = (int)BookingStatus.Pending,
            ContactEmail = "pending-cancel@test.com",
            TotalAmount = 1000000m,
            FinalAmount = 1000000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var payment = new Payment
        {
            Id = 502,
            BookingId = 501,
            Provider = "VNPAY",
            Method = "VNPAY",
            Amount = 1000000m,
            Status = (int)PaymentStatus.Pending,
            TransactionRef = "TXN-501",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var inventory = new FlightSeatInventory
        {
            Id = 503,
            FlightId = 88,
            SeatClassId = 1,
            TotalSeats = 100,
            AvailableSeats = 98,
            HeldSeats = 2,
            SoldSeats = 0,
            BasePrice = 1000000m,
            CurrentPrice = 1000000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var passengers = new List<BookingPassenger>
        {
            new() { Id = 1, BookingId = 501, FirstName = "A", LastName = "B", FullName = "A B", Email = "a@x.com", Phone = "1", FlightSeatInventoryId = 503 },
            new() { Id = 2, BookingId = 501, FirstName = "C", LastName = "D", FullName = "C D", Email = "c@x.com", Phone = "2", FlightSeatInventoryId = 503 }
        };

        var uow = new FakeUnitOfWork
        {
            BookingRepo = new FakeBookingRepository(booking),
            PaymentRepo = new FakePaymentRepository(payment),
            SeatInventoryRepo = new FakeSeatInventoryRepository(inventory),
            PassengerRepo = new FakeBookingPassengerRepository(passengers)
        };

        var vnpay = BuildVnpayProvider("test-secret-hash-key-1234567890");
        var service = new PaymentService(
            uow,
            uow.PaymentRepo,
            uow.BookingRepo,
            uow.SeatInventoryRepo,
            uow.PassengerRepo,
            new FakeEmailService(),
            new FakeTicketService(),
            LoggerFactory.Create(_ => { }).CreateLogger<PaymentService>(),
            vnpay);

        var rawData = "amount=1000000&status=failed&transactionId=TXN-501";
        var callback = new PaymentCallbackDto
        {
            TransactionId = "TXN-501",
            Status = "failed",
            Amount = 1000000m,
            RawData = rawData,
            Signature = CreateHmacSha512("test-secret-hash-key-1234567890", rawData)
        };

        var ok = await service.ProcessPaymentAsync(502, callback);
        AssertEx.True(!ok, "Failed payment callback should return false");
        AssertEx.Equal((int)PaymentStatus.Failed, payment.Status, "Payment should be failed");
        AssertEx.Equal((int)BookingStatus.Cancelled, booking.Status, "Booking should be cancelled");
        AssertEx.Equal(0, inventory.HeldSeats, "Held seats should be released");
        AssertEx.Equal(100, inventory.AvailableSeats, "Available seats should increase after release");
    }

    private static async Task TestPaymentInvalidSignatureDoesNotChangeState()
    {
        var booking = new Booking
        {
            Id = 601,
            UserId = 9,
            BookingCode = "BK601",
            OutboundFlightId = 89,
            Status = (int)BookingStatus.Pending,
            ContactEmail = "invalid-signature@test.com",
            TotalAmount = 1200000m,
            FinalAmount = 1200000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var payment = new Payment
        {
            Id = 602,
            BookingId = 601,
            Provider = "VNPAY",
            Method = "VNPAY",
            Amount = 1200000m,
            Status = (int)PaymentStatus.Pending,
            TransactionRef = "TXN-601",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var inventory = new FlightSeatInventory
        {
            Id = 603,
            FlightId = 89,
            SeatClassId = 1,
            TotalSeats = 100,
            AvailableSeats = 99,
            HeldSeats = 1,
            SoldSeats = 0,
            BasePrice = 1000000m,
            CurrentPrice = 1000000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var passengers = new List<BookingPassenger>
        {
            new() { Id = 1, BookingId = 601, FirstName = "A", LastName = "B", FullName = "A B", Email = "a@x.com", Phone = "1", FlightSeatInventoryId = 603 }
        };

        var uow = new FakeUnitOfWork
        {
            BookingRepo = new FakeBookingRepository(booking),
            PaymentRepo = new FakePaymentRepository(payment),
            SeatInventoryRepo = new FakeSeatInventoryRepository(inventory),
            PassengerRepo = new FakeBookingPassengerRepository(passengers)
        };

        var vnpay = BuildVnpayProvider("test-secret-hash-key-1234567890");
        var service = new PaymentService(
            uow,
            uow.PaymentRepo,
            uow.BookingRepo,
            uow.SeatInventoryRepo,
            uow.PassengerRepo,
            new FakeEmailService(),
            new FakeTicketService(),
            LoggerFactory.Create(_ => { }).CreateLogger<PaymentService>(),
            vnpay);

        var callback = new PaymentCallbackDto
        {
            TransactionId = "TXN-601",
            Status = "success",
            Amount = 1200000m,
            RawData = "amount=1200000&status=success&transactionId=TXN-601",
            Signature = "invalid-signature"
        };

        var ok = await service.ProcessPaymentAsync(602, callback);
        AssertEx.True(!ok, "Invalid signature should be rejected");
        AssertEx.Equal((int)PaymentStatus.Pending, payment.Status, "Payment state should remain pending");
        AssertEx.Equal((int)BookingStatus.Pending, booking.Status, "Booking state should remain pending");
        AssertEx.Equal(1, inventory.HeldSeats, "Seat hold should remain unchanged");
    }

    private static async Task TestFlightSearchValidationErrors()
    {
        var service = new FlightService(
            new FakeFlightRepository(),
            new FakeSeatInventoryRepository(),
            new FakeRouteRepository(new Route()),
            new FakeAirportRepository(),
            new FakeAircraftRepository(new Aircraft()),
            new FakeSeatClassRepository(),
            new FakePricingService(),
            new FakePromotionService(),
            new FakeDistributedCache(),
            LoggerFactory.Create(_ => { }).CreateLogger<FlightService>());

        var sameAirportError = false;
        try
        {
            await service.SearchAsync(new FlightSearchDto
            {
                DepartureAirportId = 1,
                ArrivalAirportId = 1,
                DepartureDate = DateTime.UtcNow.AddDays(1),
                PassengerCount = 1
            });
        }
        catch (ValidationException)
        {
            sameAirportError = true;
        }
        AssertEx.True(sameAirportError, "Search should reject same departure and arrival airport");

        var pastDateError = false;
        try
        {
            await service.SearchAsync(new FlightSearchDto
            {
                DepartureAirportId = 1,
                ArrivalAirportId = 2,
                DepartureDate = DateTime.UtcNow.AddDays(-3),
                PassengerCount = 1
            });
        }
        catch (ValidationException)
        {
            pastDateError = true;
        }
        AssertEx.True(pastDateError, "Search should reject departure date in the past");
    }

    private static async Task TestAdminCancelBookingQueuesRefundForConfirmedBooking()
    {
        var booking = new Booking
        {
            Id = 701,
            BookingCode = "BK701",
            UserId = 1,
            OutboundFlightId = 55,
            Status = (int)BookingStatus.Confirmed,
            FinalAmount = 1000000m,
            TotalAmount = 1000000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var seat = new FlightSeatInventory
        {
            Id = 702,
            FlightId = 55,
            SeatClassId = 1,
            TotalSeats = 100,
            AvailableSeats = 98,
            HeldSeats = 0,
            SoldSeats = 2,
            BasePrice = 1000000m,
            CurrentPrice = 1000000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var passengers = new List<BookingPassenger>
        {
            new() { Id = 1, BookingId = 701, FullName = "P1", FirstName = "P", LastName = "1", Email = "p1@test.com", Phone = "1", FlightSeatInventoryId = 702 },
            new() { Id = 2, BookingId = 701, FullName = "P2", FirstName = "P", LastName = "2", Email = "p2@test.com", Phone = "2", FlightSeatInventoryId = 702 }
        };

        var uow = new FakeUnitOfWork
        {
            BookingRepo = new FakeBookingRepository(booking),
            PassengerRepo = new FakeBookingPassengerRepository(passengers),
            SeatInventoryRepo = new FakeSeatInventoryRepository(seat)
        };
        var bg = new FakeBackgroundJobService();
        var service = new BookingAdminService(
            uow,
            uow.BookingRepo,
            new FakeRefundRequestRepository(),
            new FakeFlightRepository(),
            new FakeUserRepository(),
            bg,
            LoggerFactory.Create(_ => { }).CreateLogger<BookingAdminService>());

        var ok = await service.CancelBookingAsync(701, new CancelBookingAdminDto { Reason = "Admin cancel", FullRefund = true });
        AssertEx.True(ok, "Admin cancel should succeed for confirmed booking");
        AssertEx.Equal((int)BookingStatus.Cancelled, booking.Status, "Booking should be cancelled");
        AssertEx.Equal(100, seat.AvailableSeats, "Sold seats should be returned");
        AssertEx.Equal(0, seat.SoldSeats, "Sold seats should reduce to zero");
        AssertEx.Equal(1, bg.RefundJobs.Count, "Refund queue should receive one job");
    }

    private static async Task TestAdminCancelBookingRejectsInvalidStatus()
    {
        var booking = new Booking
        {
            Id = 801,
            BookingCode = "BK801",
            UserId = 1,
            OutboundFlightId = 55,
            Status = (int)BookingStatus.Refunded,
            FinalAmount = 500000m,
            TotalAmount = 500000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var uow = new FakeUnitOfWork
        {
            BookingRepo = new FakeBookingRepository(booking),
            PassengerRepo = new FakeBookingPassengerRepository(new List<BookingPassenger>()),
            SeatInventoryRepo = new FakeSeatInventoryRepository()
        };
        var service = new BookingAdminService(
            uow,
            uow.BookingRepo,
            new FakeRefundRequestRepository(),
            new FakeFlightRepository(),
            new FakeUserRepository(),
            new FakeBackgroundJobService(),
            LoggerFactory.Create(_ => { }).CreateLogger<BookingAdminService>());

        var rejected = false;
        try
        {
            await service.CancelBookingAsync(801, new CancelBookingAdminDto { Reason = "x", FullRefund = true });
        }
        catch (ValidationException)
        {
            rejected = true;
        }

        AssertEx.True(rejected, "Admin cancel should reject non-pending/non-confirmed booking");
    }
}

internal sealed class DummyHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new();
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public FakeBookingRepository BookingRepo { get; set; } = new(new Booking());
    public FakePaymentRepository PaymentRepo { get; set; } = new(new Payment());
    public FakeSeatInventoryRepository SeatInventoryRepo { get; set; } = new();
    public FakeBookingPassengerRepository PassengerRepo { get; set; } = new(new List<BookingPassenger>());
    public FakeFlightRepository FlightsRepo { get; set; } = new();
    public FakeRouteRepository RoutesRepo { get; set; } = new(new Route());
    public FakeAircraftRepository AircraftRepo { get; set; } = new(new Aircraft());
    public FakeFlightDefinitionRepository FlightDefinitionRepo { get; set; } = new();

    public IUserRepository Users => throw new NotImplementedException();
    public IRoleRepository Roles => throw new NotImplementedException();
    public IAirportRepository Airports => throw new NotImplementedException();
    public IAircraftRepository Aircraft => AircraftRepo;
    public IRouteRepository Routes => RoutesRepo;
    public ISeatClassRepository SeatClasses => throw new NotImplementedException();
    public IFlightRepository Flights => FlightsRepo;
    public IFlightSeatInventoryRepository FlightSeatInventories => SeatInventoryRepo;
    public IBookingRepository Bookings => BookingRepo;
    public IBookingPassengerRepository BookingPassengers => PassengerRepo;
    public IPaymentRepository Payments => PaymentRepo;
    public IRefundRequestRepository RefundRequests => throw new NotImplementedException();
    public IPromotionRepository Promotions => throw new NotImplementedException();
    public ITicketRepository Tickets => throw new NotImplementedException();
    public INotificationLogRepository NotificationLogs => throw new NotImplementedException();
    public IAuditLogRepository AuditLogs => throw new NotImplementedException();
    public IFlightScheduleTemplateRepository FlightScheduleTemplates => throw new NotImplementedException();
    public IFlightTemplateDetailRepository FlightTemplateDetails => throw new NotImplementedException();
    public IFlightDefinitionRepository FlightDefinitions => FlightDefinitionRepo;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    public Task BeginTransactionAsync() => Task.CompletedTask;
    public Task CommitAsync() => Task.CompletedTask;
    public Task RollbackAsync() => Task.CompletedTask;
    public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation) => operation();
    public Task ExecuteInTransactionAsync(Func<Task> operation) => operation();
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class FakeFlightDefinitionRepository : IFlightDefinitionRepository
{
    private int _id = 1;
    public readonly List<FlightDefinition> Items = [];
    public Task<FlightDefinition?> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
    public Task<FlightDefinition?> GetByFlightNumberAsync(string flightNumber) => Task.FromResult(Items.FirstOrDefault(x => x.FlightNumber == flightNumber));
    public Task<IEnumerable<FlightDefinition>> GetAllAsync() => Task.FromResult<IEnumerable<FlightDefinition>>(Items);
    public Task<IEnumerable<FlightDefinition>> GetActiveAsync() => Task.FromResult<IEnumerable<FlightDefinition>>(Items.Where(x => x.IsActive));
    public Task<FlightDefinition> CreateAsync(FlightDefinition flightDefinition) { flightDefinition.Id = _id++; Items.Add(flightDefinition); return Task.FromResult(flightDefinition); }
    public Task UpdateAsync(FlightDefinition flightDefinition) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
    public async Task<FlightDefinition> FindOrCreateAsync(string flightNumber, int routeId, int defaultAircraftId, TimeOnly departureTime, TimeOnly arrivalTime, int arrivalOffsetDays = 0)
    {
        var existing = await GetByFlightNumberAsync(flightNumber);
        if (existing != null) return existing;
        return await CreateAsync(new FlightDefinition { FlightNumber = flightNumber, RouteId = routeId, DefaultAircraftId = defaultAircraftId, DepartureTime = departureTime, ArrivalTime = arrivalTime, ArrivalOffsetDays = arrivalOffsetDays, IsActive = true });
    }
}

internal sealed class FakeFlightRepository : IFlightRepository
{
    private int _id = 1;
    public readonly List<Flight> Items = [];
    public FakeFlightRepository() { }
    public FakeFlightRepository(List<Flight> flights) => Items = flights;
    public Task<Flight?> GetByFlightNumberAsync(string flightNumber) => Task.FromResult(Items.FirstOrDefault(x => x.FlightNumber == flightNumber));
    public Task<IEnumerable<Flight>> SearchAsync(int departureId, int arrivalId, DateTime date) => Task.FromResult<IEnumerable<Flight>>([]);
    public Task<Flight?> GetWithInventoriesAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
    public Task<Flight> CreateAsync(Flight flight) { flight.Id = _id++; Items.Add(flight); return Task.FromResult(flight); }
    public Task UpdateAsync(Flight flight) => Task.CompletedTask;
    public Task<Flight?> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
    public Task<Flight?> GetByIdWithDetailsAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
    public Task<IEnumerable<Flight>> GetAllAsync() => Task.FromResult<IEnumerable<Flight>>(Items);
    public Task<IEnumerable<Flight>> GetFlightsByRouteAndDateAsync(int departureAirportId, int arrivalAirportId, DateTime startDate, DateTime endDate) => Task.FromResult<IEnumerable<Flight>>([]);
    public Task<IEnumerable<Flight>> GetFlightsByRouteAndDateAsync(int routeId, DateTime departureDate) => Task.FromResult<IEnumerable<Flight>>([]);
    public Task<bool> ExistsAsync(string flightNumber, DateTime departureTime, int routeId, int aircraftId) => Task.FromResult(Items.Any(x => x.FlightNumber == flightNumber && x.DepartureTime == departureTime && x.RouteId == routeId && x.AircraftId == aircraftId));
    public Task<bool> ExistsByDefinitionAndDepartureAsync(int flightDefinitionId, DateTime departureTime) => Task.FromResult(false);
    public Task<bool> HasAircraftConflictAsync(int aircraftId, DateTime newDeparture, DateTime newArrival, int turnaroundMinutes) => Task.FromResult(false);
    public Task AcquireAircraftGenerationLockAsync(int aircraftId) => Task.CompletedTask;
    public Task<IEnumerable<Flight>> GetUpcomingFlightsAsync(int days = 30) => Task.FromResult<IEnumerable<Flight>>([]);
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class FakeRouteRepository : IRouteRepository
{
    private readonly Route _route;
    public FakeRouteRepository(Route route) { _route = route; }
    public Task<Route?> GetByIdAsync(int id) => Task.FromResult(id == _route.Id ? _route : null);
    public Task<IEnumerable<Route>> GetByAirportsAsync(int departureId, int arrivalId) => Task.FromResult<IEnumerable<Route>>([]);
    public Task<IEnumerable<Route>> GetAllAsync() => Task.FromResult<IEnumerable<Route>>([_route]);
    public Task<Route> CreateAsync(Route route) => Task.FromResult(route);
    public Task UpdateAsync(Route route) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class FakeAirportRepository : IAirportRepository
{
    public Task<Airport?> GetByCodeAsync(string code) => Task.FromResult<Airport?>(null);
    public Task<Airport?> GetByIdAsync(int id) => Task.FromResult<Airport?>(null);
    public Task<IEnumerable<Airport>> GetAllAsync() => Task.FromResult<IEnumerable<Airport>>([]);
    public Task<Airport> CreateAsync(Airport airport) => Task.FromResult(airport);
    public Task UpdateAsync(Airport airport) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class FakeSeatClassRepository : ISeatClassRepository
{
    public Task<SeatClass?> GetByCodeAsync(string code) => Task.FromResult<SeatClass?>(null);
    public Task<SeatClass?> GetByIdAsync(int id) => Task.FromResult<SeatClass?>(null);
    public Task<IEnumerable<SeatClass>> GetAllAsync() => Task.FromResult<IEnumerable<SeatClass>>([]);
    public Task<SeatClass> CreateAsync(SeatClass seatClass) => Task.FromResult(seatClass);
    public Task UpdateAsync(SeatClass seatClass) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class FakePromotionService : IPromotionService
{
    public Task<decimal> ApplyPromotionAsync(decimal amount, int? promotionId) => Task.FromResult(amount);
    public Task<Promotion?> ValidatePromotionCodeAsync(string promotionCode) => Task.FromResult<Promotion?>(null);
    public Task<bool> RecordPromotionUsageAsync(int promotionId, int bookingId, int userId, decimal discountAmount) => Task.FromResult(true);
}

internal sealed class FakePricingService : IPricingService
{
    public Task<decimal> CalculateCurrentPriceAsync(int flightSeatInventoryId) => Task.FromResult(1_000_000m);
    public Task UpdateDynamicPricesAsync() => Task.CompletedTask;
}

internal sealed class FakeDistributedCache : IDistributedCache
{
    private readonly Dictionary<string, byte[]> _store = [];
    public byte[]? Get(string key) => _store.TryGetValue(key, out var val) ? val : null;
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public Task RemoveAsync(string key, CancellationToken token = default) { _store.Remove(key); return Task.CompletedTask; }
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _store[key] = value;
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) { _store[key] = value; return Task.CompletedTask; }
}

internal sealed class FakeAircraftRepository : IAircraftRepository
{
    private readonly Aircraft _aircraft;
    public FakeAircraftRepository(Aircraft aircraft) { _aircraft = aircraft; }
    public Task<Aircraft?> GetByIdAsync(int id) => Task.FromResult(id == _aircraft.Id ? _aircraft : null);
    public Task<Aircraft?> GetByIdWithSeatTemplatesAsync(int id) => Task.FromResult(id == _aircraft.Id ? _aircraft : null);
    public Task<Aircraft?> GetByRegistrationNumberAsync(string registrationNumber) => Task.FromResult<Aircraft?>(null);
    public Task<IEnumerable<Aircraft>> GetAllAsync() => Task.FromResult<IEnumerable<Aircraft>>([_aircraft]);
    public Task<Aircraft> CreateAsync(Aircraft aircraft) => Task.FromResult(aircraft);
    public Task UpdateAsync(Aircraft aircraft) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class FakeSeatInventoryRepository : IFlightSeatInventoryRepository
{
    private int _id = 1;
    public readonly List<FlightSeatInventory> Items = [];

    public FakeSeatInventoryRepository() { }
    public FakeSeatInventoryRepository(FlightSeatInventory item) => Items.Add(item);
    public FakeSeatInventoryRepository(List<FlightSeatInventory> items) => Items = items;
    public Task<FlightSeatInventory?> GetAsync(int flightId, int seatClassId) => Task.FromResult(Items.FirstOrDefault(x => x.FlightId == flightId && x.SeatClassId == seatClassId));
    public Task<IEnumerable<FlightSeatInventory>> GetAllForFlightAsync(int flightId) => Task.FromResult<IEnumerable<FlightSeatInventory>>(Items.Where(x => x.FlightId == flightId));
    public Task<List<FlightSeatInventory>> GetByFlightIdAsync(int flightId) => Task.FromResult(Items.Where(x => x.FlightId == flightId).ToList());
    public Task<FlightSeatInventory?> GetByFlightAndSeatClassAsync(int flightId, int seatClassId) => Task.FromResult(Items.FirstOrDefault(x => x.FlightId == flightId && x.SeatClassId == seatClassId));
    public Task<bool> TryUpdateWithConcurrencyCheckAsync(int id, Func<FlightSeatInventory, bool> updateAction) => Task.FromResult(true);
    public Task<List<FlightSeatInventory>> GetActiveInventoriesAsync() => Task.FromResult(Items);
    public Task<bool> TryHoldSeatsAtomicAsync(int id, int count)
    {
        var item = Items.First(x => x.Id == id);
        if (item.AvailableSeats < count) return Task.FromResult(false);
        item.AvailableSeats -= count; item.HeldSeats += count;
        return Task.FromResult(true);
    }
    public Task<bool> TryConfirmHeldSeatsAtomicAsync(int id, int count)
    {
        var item = Items.First(x => x.Id == id);
        if (item.HeldSeats < count) return Task.FromResult(false);
        item.HeldSeats -= count; item.SoldSeats += count;
        return Task.FromResult(true);
    }
    public Task<bool> TryReleaseHeldSeatsAtomicAsync(int id, int count)
    {
        var item = Items.First(x => x.Id == id);
        if (item.HeldSeats < count) return Task.FromResult(false);
        item.HeldSeats -= count;
        item.AvailableSeats += count;
        return Task.FromResult(true);
    }
    public Task<bool> TryCancelSoldSeatsAtomicAsync(int id, int count)
    {
        var item = Items.First(x => x.Id == id);
        if (item.SoldSeats < count) return Task.FromResult(false);
        item.SoldSeats -= count;
        item.AvailableSeats += count;
        return Task.FromResult(true);
    }
    public Task ReserveSeatsAsync(int id, int count, int version) => Task.CompletedTask;
    public Task UpdateAsync(FlightSeatInventory inventory) => Task.CompletedTask;
    public Task<FlightSeatInventory?> GetByIdAsync(int id) => Task.FromResult(Items.FirstOrDefault(x => x.Id == id));
    public Task<IEnumerable<FlightSeatInventory>> GetAllAsync() => Task.FromResult<IEnumerable<FlightSeatInventory>>(Items);
    public Task CreateAsync(FlightSeatInventory inventory) { inventory.Id = _id++; Items.Add(inventory); return Task.CompletedTask; }
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class FakeBookingRepository : IBookingRepository
{
    private readonly List<Booking> _bookings;
    public FakeBookingRepository(Booking booking) => _bookings = new List<Booking> { booking };
    public FakeBookingRepository(List<Booking> bookings) => _bookings = bookings;
    public Task<Booking?> GetByBookingCodeAsync(string code) => Task.FromResult<Booking?>(null);
    public Task<IEnumerable<Booking>> GetByUserAsync(int userId, int page, int pageSize) => Task.FromResult<IEnumerable<Booking>>([]);
    public Task<List<Booking>> GetByUserIdAsync(int userId, int page, int pageSize) => Task.FromResult(new List<Booking>());
    public Task<Booking?> GetWithPassengersAsync(int id) => Task.FromResult<Booking?>(null);
    public Task<List<Booking>> GetRecentBookingsForFlightAsync(int flightId, int days) => Task.FromResult(new List<Booking>());
    public Task<List<Booking>> GetExpiredPendingBookingsAsync() => Task.FromResult(new List<Booking>());
    public Task<List<Booking>> GetExpiredPendingBookingsAsync(int flightId, int seatClassId) => Task.FromResult(new List<Booking>());
    public Task<Booking> CreateAsync(Booking booking) => Task.FromResult(booking);
    public Task UpdateAsync(Booking booking) => Task.CompletedTask;
    public Task<Booking?> GetByIdAsync(int id) => Task.FromResult(_bookings.FirstOrDefault(b => b.Id == id));
    public Task<IEnumerable<Booking>> GetAllAsync() => Task.FromResult<IEnumerable<Booking>>(_bookings);
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class FakePaymentRepository : IPaymentRepository
{
    private readonly List<Payment> _payments;
    public FakePaymentRepository(Payment payment) => _payments = new List<Payment> { payment };
    public FakePaymentRepository(List<Payment> payments) => _payments = payments;
    public Task<Payment?> GetByIdAsync(int id) => Task.FromResult(_payments.FirstOrDefault(p => p.Id == id));
    public Task<List<Payment>> GetByBookingIdAsync(int bookingId) => Task.FromResult(_payments.Where(p => p.BookingId == bookingId).ToList());
    public Task<IEnumerable<Payment>> GetAllAsync() => Task.FromResult<IEnumerable<Payment>>(_payments);
    public Task<Payment> CreateAsync(Payment payment) => Task.FromResult(payment);
    public Task UpdateAsync(Payment payment) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class FakeBookingPassengerRepository : IBookingPassengerRepository
{
    private readonly List<BookingPassenger> _passengers;
    public FakeBookingPassengerRepository(List<BookingPassenger> passengers) => _passengers = passengers;
    public Task<BookingPassenger?> GetByIdAsync(int id) => Task.FromResult(_passengers.FirstOrDefault(x => x.Id == id));
    public Task<List<BookingPassenger>> GetByBookingIdAsync(int bookingId) => Task.FromResult(_passengers.Where(x => x.BookingId == bookingId).ToList());
    public Task<IEnumerable<BookingPassenger>> GetAllAsync() => Task.FromResult<IEnumerable<BookingPassenger>>(_passengers);
    public Task<BookingPassenger> CreateAsync(BookingPassenger bookingPassenger) => Task.FromResult(bookingPassenger);
    public Task UpdateAsync(BookingPassenger bookingPassenger) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class FakeEmailService : IEmailService
{
    public readonly List<(string Email, string Title)> Notifications = [];
    public Task SendEmailAsync(string email, string subject, string htmlContent) => Task.CompletedTask;
    public Task SendVerificationEmailAsync(string email, string verificationCode) => Task.CompletedTask;
    public Task SendPasswordResetEmailAsync(string email, string resetCode) => Task.CompletedTask;
    public Task SendPasswordChangeOtpEmailAsync(string email, string otpCode) => Task.CompletedTask;
    public Task SendBookingConfirmationAsync(string email, Booking booking) => Task.CompletedTask;
    public Task SendTicketEmailAsync(string email, Booking booking, List<Ticket> tickets) => Task.CompletedTask;
    public Task SendBookingCancellationAsync(string email, Booking booking) => Task.CompletedTask;
    public Task SendNotificationAsync(string email, string title, string content)
    {
        Notifications.Add((email, title));
        return Task.CompletedTask;
    }
}

internal sealed class FakeBackgroundJobService : IBackgroundJobService
{
    public readonly List<(int BookingId, string Reason)> RefundJobs = [];

    public void EnqueueReleaseSeatHolds() { }
    public void EnqueueExpireBookings() { }
    public void EnqueueUpdatePrices() { }
    public void EnqueueBookingReminders() { }
    public void EnqueueRefundNotifications() { }
    public void EnqueueGenerateReports() { }
    public void EnqueueVnpayRefund(int bookingId, string reason) => RefundJobs.Add((bookingId, reason));
    public Task ProcessVnpayRefundQueueAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ProcessExpiredBookingsAsync() => Task.CompletedTask;
    public void StartRecurringJobs() { }
    public Task<Dictionary<string, string>> GetJobStatusAsync() => Task.FromResult(new Dictionary<string, string>());
}

internal sealed class FakeRefundRequestRepository : IRefundRequestRepository
{
    public Task<RefundRequest?> GetByIdAsync(int id) => Task.FromResult<RefundRequest?>(null);
    public Task<List<RefundRequest>> GetByBookingIdAsync(int bookingId) => Task.FromResult(new List<RefundRequest>());
    public Task<IEnumerable<RefundRequest>> GetByStatusAsync(int status) => Task.FromResult<IEnumerable<RefundRequest>>([]);
    public Task<IEnumerable<RefundRequest>> GetAllAsync() => Task.FromResult<IEnumerable<RefundRequest>>([]);
    public Task<RefundRequest> CreateAsync(RefundRequest refundRequest) => Task.FromResult(refundRequest);
    public Task UpdateAsync(RefundRequest refundRequest) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
}

internal sealed class FakeUserRepository : IUserRepository
{
    public Task<User?> GetByEmailAsync(string email) => Task.FromResult<User?>(null);
    public Task<User?> GetByEmailWithRolesAsync(string email) => Task.FromResult<User?>(null);
    public Task<User?> GetByIdAsync(int id) => Task.FromResult<User?>(null);
    public Task<User?> GetWithRolesAsync(int id) => Task.FromResult<User?>(null);
    public Task<IEnumerable<User>> GetAllAsync() => Task.FromResult<IEnumerable<User>>([]);
    public Task<IEnumerable<User>> GetAllWithRolesAsync() => Task.FromResult<IEnumerable<User>>([]);
    public Task<User> CreateAsync(User user) => Task.FromResult(user);
    public Task UpdateAsync(User user) => Task.CompletedTask;
    public Task DeleteAsync(int id) => Task.CompletedTask;
    public Task<bool> ExistsAsync(int id) => Task.FromResult(false);
    public Task<bool> EmailExistsAsync(string email) => Task.FromResult(false);
    public Task<bool> UserHasRoleAsync(int userId, int roleId) => Task.FromResult(false);
    public Task AddRoleAsync(int userId, int roleId) => Task.CompletedTask;
    public Task RemoveRoleAsync(int userId, int roleId) => Task.CompletedTask;
    public Task<User?> GetByGoogleIdAsync(string googleId) => Task.FromResult<User?>(null);
}

internal sealed class FakeTicketService : ITicketService
{
    public bool CreateCalled { get; private set; }
    public Task<List<API.Application.Dtos.Ticket.TicketResponse>> CreateTicketsAsync(int bookingId)
    {
        CreateCalled = true;
        return Task.FromResult(new List<API.Application.Dtos.Ticket.TicketResponse>());
    }
    public Task<API.Application.Dtos.Ticket.TicketResponse> GetTicketAsync(string ticketNumber) => throw new NotImplementedException();
    public Task<bool> ChangeTicketAsync(string ticketNumber, API.Application.Dtos.Ticket.ChangeTicketDto dto) => throw new NotImplementedException();
    public Task<List<API.Application.Dtos.Ticket.TicketResponse>> GetBookingTicketsAsync(int bookingId) => Task.FromResult(new List<API.Application.Dtos.Ticket.TicketResponse>());
    public Task<byte[]> DownloadTicketAsync(string ticketNumber, string format = "pdf") => throw new NotImplementedException();
}
