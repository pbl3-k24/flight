namespace API.Application.Dtos.Search;

public class AdvancedSearchFilterDto
{
    public string? SearchTerm { get; set; }

    public string? SortBy { get; set; } = "CreatedAt"; // Field name

    public string SortOrder { get; set; } = "DESC"; // ASC, DESC

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    // Flight Filters
    public DateTime? DepartureDateFrom { get; set; }

    public DateTime? DepartureDateTo { get; set; }

    public string? DepartureAirport { get; set; }

    public string? ArrivalAirport { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public int? MinSeatsAvailable { get; set; }

    // Booking Filters
    public int? BookingStatus { get; set; }

    public string? PassengerName { get; set; }

    public string? PassengerEmail { get; set; }

    // User Filters
    public int? UserStatus { get; set; }

    public string? UserRole { get; set; }

    public DateTime? RegistrationDateFrom { get; set; }

    public DateTime? RegistrationDateTo { get; set; }

    // Refund Filters
    public int? RefundStatus { get; set; }

    public decimal? MinRefundAmount { get; set; }

    public decimal? MaxRefundAmount { get; set; }
}

public class SearchResultDto<T>
{
    public List<T> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public int CurrentPage { get; set; }

    public int PageSize { get; set; }

    public bool HasNextPage => CurrentPage < TotalPages;

    public bool HasPreviousPage => CurrentPage > 1;
}
