namespace API.Application.DTOs;

/// <summary>
/// DTO for paginated response containing multiple bookings.
/// </summary>
public class PaginatedBookingsResponseDto
{
    /// <summary>List of bookings in the current page.</summary>
    public List<BookingResponseDto> Items { get; set; } = new();

    /// <summary>Total number of bookings available.</summary>
    public int Total { get; set; }

    /// <summary>Current page number.</summary>
    public int Page { get; set; }

    /// <summary>Number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Total number of pages available.</summary>
    public int TotalPages { get; set; }
}
