namespace API.Application.Services;

using API.Application.Dtos.Search;
using API.Application.Exceptions;
using API.Application.Interfaces;
using Microsoft.Extensions.Logging;

public class AdvancedSearchService : IAdvancedSearchService
{
    private readonly IFlightRepository _flightRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefundRequestRepository _refundRepository;
    private readonly ILogger<AdvancedSearchService> _logger;

    public AdvancedSearchService(
        IFlightRepository flightRepository,
        IBookingRepository bookingRepository,
        IUserRepository userRepository,
        IRefundRequestRepository refundRepository,
        ILogger<AdvancedSearchService> logger)
    {
        _flightRepository = flightRepository;
        _bookingRepository = bookingRepository;
        _userRepository = userRepository;
        _refundRepository = refundRepository;
        _logger = logger;
    }

    public async Task<SearchResultDto<dynamic>> SearchFlightsAsync(AdvancedSearchFilterDto filter)
    {
        try
        {
            var flights = await _flightRepository.GetAllAsync();
            var filtered = flights.AsEnumerable();

            if (filter.DepartureDateFrom.HasValue)
            {
                filtered = filtered.Where(f => f.DepartureTime >= filter.DepartureDateFrom.Value);
            }

            if (filter.DepartureDateTo.HasValue)
            {
                filtered = filtered.Where(f => f.DepartureTime <= filter.DepartureDateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.FlightNumber))
            {
                var flightNumber = filter.FlightNumber.Trim();
                filtered = filtered.Where(f =>
                    string.Equals(f.FlightNumber, flightNumber, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.MinPrice.HasValue)
            {
                // Would filter by price
            }

            if (filter.MaxPrice.HasValue)
            {
                // Would filter by price
            }

            var total = filtered.Count();
            var items = filtered
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return new SearchResultDto<dynamic>
            {
                Items = items.Cast<dynamic>().ToList(),
                TotalCount = total,
                TotalPages = (total + filter.PageSize - 1) / filter.PageSize,
                CurrentPage = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching flights");
            return new SearchResultDto<dynamic>();
        }
    }

    public async Task<SearchResultDto<dynamic>> SearchBookingsAsync(AdvancedSearchFilterDto filter)
    {
        try
        {
            var bookings = await _bookingRepository.GetAllAsync();
            var filtered = bookings.AsEnumerable();

            if (filter.BookingStatus.HasValue)
            {
                filtered = filtered.Where(b => b.Status == filter.BookingStatus.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.FlightNumber))
            {
                var flightNumber = filter.FlightNumber.Trim();
                filtered = filtered.Where(b =>
                    b.OutboundFlight != null
                    && string.Equals(b.OutboundFlight.FlightNumber, flightNumber, StringComparison.OrdinalIgnoreCase));
            }

            var total = filtered.Count();
            var items = filtered
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return new SearchResultDto<dynamic>
            {
                Items = items.Cast<dynamic>().ToList(),
                TotalCount = total,
                TotalPages = (total + filter.PageSize - 1) / filter.PageSize,
                CurrentPage = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching bookings");
            return new SearchResultDto<dynamic>();
        }
    }

    public async Task<SearchResultDto<dynamic>> SearchUsersAsync(AdvancedSearchFilterDto filter)
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            var filtered = users.AsEnumerable();

            if (filter.UserStatus.HasValue)
            {
                filtered = filtered.Where(u => u.Status == filter.UserStatus.Value);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                filtered = filtered.Where(u => u.Email.Contains(filter.SearchTerm) || u.FullName.Contains(filter.SearchTerm));
            }

            var total = filtered.Count();
            var items = filtered
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return new SearchResultDto<dynamic>
            {
                Items = items.Cast<dynamic>().ToList(),
                TotalCount = total,
                TotalPages = (total + filter.PageSize - 1) / filter.PageSize,
                CurrentPage = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return new SearchResultDto<dynamic>();
        }
    }

    public async Task<SearchResultDto<dynamic>> SearchRefundsAsync(AdvancedSearchFilterDto filter)
    {
        try
        {
            var refunds = await _refundRepository.GetAllAsync();
            var filtered = refunds.AsEnumerable();

            if (filter.RefundStatus.HasValue)
            {
                filtered = filtered.Where(r => r.Status == filter.RefundStatus.Value);
            }

            var total = filtered.Count();
            var items = filtered
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return new SearchResultDto<dynamic>
            {
                Items = items.Cast<dynamic>().ToList(),
                TotalCount = total,
                TotalPages = (total + filter.PageSize - 1) / filter.PageSize,
                CurrentPage = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching refunds");
            return new SearchResultDto<dynamic>();
        }
    }

    public async Task<Dictionary<string, object>> GlobalSearchAsync(string searchTerm)
    {
        try
        {
            var results = new Dictionary<string, object>();

            var users = await _userRepository.GetAllAsync();
            results["users"] = users.Where(u => u.Email.Contains(searchTerm) || u.FullName.Contains(searchTerm)).Take(5).ToList();

            var bookings = await _bookingRepository.GetAllAsync();
            results["bookings"] = bookings.Where(b => b.BookingCode.Contains(searchTerm)).Take(5).ToList();

            _logger.LogInformation("Global search for: {SearchTerm}", searchTerm);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in global search");
            return new Dictionary<string, object>();
        }
    }
}
