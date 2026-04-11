using FlightBooking.Application.DTOs.Flight;

namespace FlightBooking.Application.DTOs.Booking;

public record PassengerDto(
    string FullName,
    DateOnly DateOfBirth,
    string? Gender,
    string? Nationality,
    string IdentityNumber,
    string PassengerType,
    string? PassportNumber,
    DateTime? PassportExpiry);

public record BookingItemRequest(
    Guid FlightId,
    Guid FareClassId,
    PassengerDto Passenger,
    string? SeatNumber = null);

public record CreateBookingRequest(
    IEnumerable<BookingItemRequest> Items,
    string? PromotionCode = null);

public record BookingDto(
    Guid Id,
    string BookingCode,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    IEnumerable<BookingItemDto> Items,
    IEnumerable<PassengerSummaryDto> Passengers);

public record BookingItemDto(
    Guid Id,
    string FlightNumber,
    string OriginCode,
    string DestinationCode,
    DateTime DepartureTime,
    DateTime ArrivalTime,
    string FareClassCode,
    string? SeatNumber,
    decimal Price,
    decimal TaxAndFee,
    string Status,
    TicketDto? Ticket,
    string PassengerName);

public record PassengerSummaryDto(Guid Id, string FullName, string PassengerType, string IdentityNumber);

public record TicketDto(
    Guid Id,
    string TicketNumber,
    string Status,
    DateTime IssuedAt,
    string? BoardingPassUrl,
    string? ETicketUrl);

public record CheckoutRequest(
    IEnumerable<BookingItemRequest> Items,
    string Gateway,
    string ReturnUrl,
    string? PromotionCode = null);

public record CheckoutSessionDto(
    Guid BookingId,
    string BookingCode,
    decimal TotalAmount,
    string PaymentUrl,
    DateTime ExpiresAt);
