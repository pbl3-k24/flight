using FlightBooking.Application.DTOs.Booking;

namespace FlightBooking.Application.Services.Interfaces;

public interface IBookingService
{
    Task<BookingDto> GetByIdAsync(Guid id);
    Task<BookingDto> GetByCodeAsync(string bookingCode);
    Task<IEnumerable<BookingDto>> GetByUserAsync(Guid userId, int page, int pageSize);
    Task<IEnumerable<BookingDto>> GetAllAsync(int page, int pageSize);
    Task<BookingDto> CreateAsync(CreateBookingRequest request, Guid userId);
    Task CancelAsync(Guid id, string reason, Guid requestedBy);
    Task ExpireBookingAsync(Guid id);
}
