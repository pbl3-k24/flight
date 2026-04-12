// Example: Domain Entity Usage Patterns

namespace API.Examples;

using API.Domain.Entities;
using API.Domain.Enums;

/// <summary>
/// This file demonstrates how the domain entities should be used
/// to enforce business rules and maintain data consistency.
/// This is NOT meant to be compiled - it's a reference guide.
/// </summary>
public static class DomainUsageExamples
{
    /// <summary>
    /// Example: Creating and managing a flight booking
    /// </summary>
    public static void BookingCreationExample()
    {
        // 1. Create a flight with seat management
        var flight = new Flight
        {
            Id = 1,
            FlightNumber = "AA100",
            DepartureAirportId = 1,
            ArrivalAirportId = 2,
            DepartureTime = DateTime.UtcNow.AddDays(7),
            ArrivalTime = DateTime.UtcNow.AddDays(7).AddHours(5),
            Airline = "American Airlines",
            AircraftModel = "Boeing 737",
            TotalSeats = 150,
            AvailableSeats = 150,
            BasePrice = 299.99m,
            Status = FlightStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 2. Create a user
        var user = new User
        {
            Id = 1,
            Email = "john.doe@example.com",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1985, 6, 15),
            PhoneNumber = "+1-555-0123",
            PasswordHash = "hashed_password_here",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 3. Create a booking (aggregate root)
        var booking = new Booking
        {
            Id = 1,
            FlightId = flight.Id,
            UserId = user.Id,
            BookingReference = "ABC123XYZ",
            PassengerCount = 2,
            TotalPrice = 599.98m,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Notes = "Window seats preferred"
        };

        // 4. Add passengers to the booking (children of booking aggregate)
        var passenger1 = new Passenger
        {
            BookingId = booking.Id,
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1985, 6, 15),
            Email = "john.doe@example.com",
            PassportNumber = "AB123456789",
            Nationality = "US",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var passenger2 = new Passenger
        {
            BookingId = booking.Id,
            FirstName = "Jane",
            LastName = "Doe",
            DateOfBirth = new DateTime(1988, 3, 22),
            Email = "jane.doe@example.com",
            PassportNumber = "CD987654321",
            Nationality = "US",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        booking.Passengers.Add(passenger1);
        booking.Passengers.Add(passenger2);

        // 5. USE DOMAIN METHODS - This is key!
        // Reserve seats on flight using domain method
        try
        {
            flight.ReserveSeats(booking.PassengerCount);
            // flight.AvailableSeats is now 148 (150 - 2)
        }
        catch (InvalidOperationException ex)
        {
            // Handle insufficient seats
            Console.WriteLine($"Cannot book: {ex.Message}");
        }

        // 6. Create payment
        var payment = new Payment
        {
            BookingId = booking.Id,
            UserId = user.Id,
            Amount = booking.TotalPrice,
            Currency = "USD",
            Method = PaymentMethod.CreditCard,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // 7. Process payment (simulate successful payment)
        payment.Complete("TXN123456789");
        // payment.Status is now PaymentStatus.Completed
        // payment.TransactionId is "TXN123456789"

        // 8. Confirm booking after successful payment
        booking.Confirm();
        // booking.Status is now BookingStatus.Confirmed
    }

    /// <summary>
    /// Example: Handling booking cancellation
    /// </summary>
    public static void BookingCancellationExample()
    {
        var flight = new Flight
        {
            AvailableSeats = 148,
            TotalSeats = 150
        };

        var booking = new Booking
        {
            PassengerCount = 2,
            Status = BookingStatus.Confirmed
        };

        var payment = new Payment
        {
            Status = PaymentStatus.Completed
        };

        // Attempt cancellation workflow
        if (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Pending)
        {
            // 1. Cancel booking
            booking.Cancel();
            // booking.Status is now BookingStatus.Cancelled

            // 2. Release seats back to flight
            flight.ReleaseSeats(booking.PassengerCount);
            // flight.AvailableSeats is now 150 (148 + 2)

            // 3. Refund payment
            if (payment.Status == PaymentStatus.Completed)
            {
                var refundAmount = 225m;  // 75% refund with 25% cancellation fee
                payment.Refund(refundAmount);
                // payment.Status is now PaymentStatus.Refunded
            }
        }
    }

    /// <summary>
    /// Example: International flight validation
    /// </summary>
    public static void PassengerValidationExample()
    {
        var passenger = new Passenger
        {
            FirstName = "Sarah",
            LastName = "Smith",
            DateOfBirth = new DateTime(2023, 8, 15), // Very young passenger
            Email = "sarah@example.com",
            PassportNumber = null,
            Nationality = null
        };

        // Check age
        var age = passenger.GetAge();
        // age = 0 years (born in 2023)

        // Validate for international flight
        var isValidForIntl = passenger.IsValidForInternationalFlight();
        // Returns false - too young AND missing passport/nationality

        if (!isValidForIntl)
        {
            // Cannot book this passenger for international flight
            Console.WriteLine("Passenger too young or missing required documents");
        }

        // Correct the passenger
        passenger.DateOfBirth = new DateTime(2020, 8, 15); // 3+ years old
        passenger.PassportNumber = "XY123456789";
        passenger.Nationality = "UK";

        isValidForIntl = passenger.IsValidForInternationalFlight();
        // Returns true - age >= 2 and has required documents
    }

    /// <summary>
    /// Example: Flight status transitions
    /// </summary>
    public static void FlightStatusTransitionExample()
    {
        var flight = new Flight
        {
            Status = FlightStatus.Active
        };

        // Flight is delayed
        flight.MarkAsDelayed();
        // flight.Status = FlightStatus.Delayed

        // Can still transition to Completed even if delayed
        flight.MarkAsCompleted();
        // flight.Status = FlightStatus.Completed

        // Or flight can be cancelled
        var flight2 = new Flight { Status = FlightStatus.Active };
        flight2.Cancel();
        // flight2.Status = FlightStatus.Cancelled
    }

    /// <summary>
    /// Example: Payment state management
    /// </summary>
    public static void PaymentStateExample()
    {
        var payment = new Payment
        {
            Amount = 599.98m,
            Method = PaymentMethod.CreditCard,
            Status = PaymentStatus.Pending
        };

        // Scenario 1: Successful payment
        try
        {
            payment.Complete("TXN12345");
            // payment.Status = PaymentStatus.Completed
            // payment.TransactionId = "TXN12345"
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Cannot complete: {ex.Message}");
        }

        // Scenario 2: Failed payment
        var failedPayment = new Payment
        {
            Status = PaymentStatus.Pending
        };

        try
        {
            failedPayment.Fail("Card declined - insufficient funds");
            // failedPayment.Status = PaymentStatus.Failed
            // failedPayment.Notes = "Card declined - insufficient funds"
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Cannot fail: {ex.Message}");
        }

        // Scenario 3: Refund a completed payment
        if (payment.Status == PaymentStatus.Completed)
        {
            var refundAmount = payment.Amount * 0.9m;  // 90% refund
            payment.Refund(refundAmount);
            // payment.Status = PaymentStatus.Refunded
        }
    }

    /// <summary>
    /// Example: User account management
    /// </summary>
    public static void UserAccountManagementExample()
    {
        var user = new User
        {
            Status = UserStatus.Active
        };

        // Deactivate account
        user.Deactivate();
        // user.Status = UserStatus.Deactivated

        // Reactivate account
        user.Reactivate();
        // user.Status = UserStatus.Active

        // Suspend account (violation)
        user.Suspend();
        // user.Status = UserStatus.Suspended
    }

    /// <summary>
    /// Example: Demonstrating invariant enforcement
    /// </summary>
    public static void InvariantEnforcementExample()
    {
        var flight = new Flight
        {
            TotalSeats = 150,
            AvailableSeats = 10
        };

        // Attempt to reserve more seats than available
        try
        {
            flight.ReserveSeats(15); // More than 10 available
            // THROWS: InvalidOperationException
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Invariant violated: {ex.Message}");
            // Message: "Insufficient seats available. Requested: 15, Available: 10"
        }

        // Attempt invalid release
        try
        {
            flight.ReleaseSeats(145); // Would exceed total seats
            // THROWS: InvalidOperationException
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Invariant violated: {ex.Message}");
            // Message: "Cannot release 145 seats. Would exceed total seats of 150"
        }

        // Invalid booking state transition
        var booking = new Booking { Status = BookingStatus.CheckedIn };
        try
        {
            booking.Cancel();
            // THROWS: InvalidOperationException
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Invalid state: {ex.Message}");
            // Message: "Cannot cancel booking with status CheckedIn"
        }
    }
}

/*
 * KEY PRINCIPLES:
 * 
 * 1. ALWAYS use domain methods, never directly modify properties
 *    ❌ WRONG: flight.AvailableSeats -= 2;
 *    ✅ RIGHT: flight.ReserveSeats(2);
 * 
 * 2. Domain methods enforce invariants
 *    - ReserveSeats checks availability
 *    - Cancel checks current status
 *    - Complete requires transaction ID
 * 
 * 3. Navigation properties enable relationships
 *    - flight.Bookings gives all bookings for flight
 *    - user.Bookings gives all user's bookings
 *    - booking.Passengers gives all passengers
 * 
 * 4. Enums provide type-safe status values
 *    - No magic numbers or strings
 *    - Compile-time safety
 *    - Clear business semantics
 * 
 * 5. Child entities belong to parent aggregates
 *    - Passenger always belongs to Booking
 *    - FlightCrew links Flight and CrewMember
 *    - Cannot exist independently
 */
