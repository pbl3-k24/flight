namespace API.Application.Interfaces;

using API.Application.Dtos.Search;

public interface IAdvancedSearchService
{
    /// <summary>
    /// Searches flights with advanced filters.
    /// </summary>
    Task<SearchResultDto<dynamic>> SearchFlightsAsync(AdvancedSearchFilterDto filter);

    /// <summary>
    /// Searches bookings with advanced filters.
    /// </summary>
    Task<SearchResultDto<dynamic>> SearchBookingsAsync(AdvancedSearchFilterDto filter, int requesterUserId, bool isAdmin);

    /// <summary>
    /// Searches users with advanced filters.
    /// </summary>
    Task<SearchResultDto<dynamic>> SearchUsersAsync(AdvancedSearchFilterDto filter);

    /// <summary>
    /// Searches refunds with advanced filters.
    /// </summary>
    Task<SearchResultDto<dynamic>> SearchRefundsAsync(AdvancedSearchFilterDto filter);

    /// <summary>
    /// Global search across all entities.
    /// </summary>
    Task<Dictionary<string, object>> GlobalSearchAsync(string searchTerm, int requesterUserId, bool isAdmin);
}
