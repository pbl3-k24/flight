namespace API.Application.Services;

using API.Application.Dtos.Search;
using API.Application.Exceptions;
using API.Application.Interfaces;
using API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class AdvancedSearchService : IAdvancedSearchService
{
    private readonly IFlightRepository _flightRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefundRequestRepository _refundRepository;
    private readonly FlightBookingDbContext _dbContext;
    private readonly ILogger<AdvancedSearchService> _logger;

    public AdvancedSearchService(
        IFlightRepository flightRepository,
        IBookingRepository bookingRepository,
        IUserRepository userRepository,
        IRefundRequestRepository refundRepository,
        FlightBookingDbContext dbContext,
        ILogger<AdvancedSearchService> logger)
    {
        _flightRepository = flightRepository;
        _bookingRepository = bookingRepository;
        _userRepository = userRepository;
        _refundRepository = refundRepository;
        _dbContext = dbContext;
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

    public async Task<SearchResultDto<dynamic>> SearchBookingsAsync(AdvancedSearchFilterDto filter, int requesterUserId, bool isAdmin)
    {
        try
        {
            var page = Math.Max(filter.Page, 1);
            var pageSize = Math.Clamp(filter.PageSize, 1, 100);
            var query = _dbContext.Bookings
                .AsNoTracking()
                .Include(b => b.OutboundFlight)
                .Include(b => b.Passengers)
                .AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(b => b.UserId == requesterUserId);
            }

            if (filter.BookingStatus.HasValue)
            {
                query = query.Where(b => b.Status == filter.BookingStatus.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.FlightNumber))
            {
                var flightNumber = filter.FlightNumber.Trim();
                query = query.Where(b =>
                    b.OutboundFlight != null
                    && string.Equals(b.OutboundFlight.FlightNumber, flightNumber, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(b => b.BookingCode.Contains(term));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new SearchResultDto<dynamic>
            {
                Items = items.Cast<dynamic>().ToList(),
                TotalCount = total,
                TotalPages = (total + pageSize - 1) / pageSize,
                CurrentPage = page,
                PageSize = pageSize
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

    public async Task<Dictionary<string, object>> GlobalSearchAsync(string searchTerm, int requesterUserId, bool isAdmin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return new Dictionary<string, object>();
            }

            var term = searchTerm.Trim();
            var results = new Dictionary<string, object>();

            if (isAdmin)
            {
                var users = await _dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Email.Contains(term) || u.FullName.Contains(term))
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new
                    {
                        u.Id,
                        u.Email,
                        u.FullName,
                        u.Status,
                        u.CreatedAt
                    })
                    .ToListAsync();
                results["users"] = users;
            }

            var bookingsQuery = _dbContext.Bookings
                .AsNoTracking()
                .Where(b => b.BookingCode.Contains(term));

            if (!isAdmin)
            {
                bookingsQuery = bookingsQuery.Where(b => b.UserId == requesterUserId);
            }

            var bookings = await bookingsQuery
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new
                {
                    b.Id,
                    b.BookingCode,
                    b.Status,
                    b.FinalAmount,
                    b.CreatedAt
                })
                .ToListAsync();
            results["bookings"] = bookings;

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
