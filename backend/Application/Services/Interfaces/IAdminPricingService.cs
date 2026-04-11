using FlightBooking.Application.DTOs.Admin;
using FlightBooking.Application.DTOs.Flight;

namespace FlightBooking.Application.Services.Interfaces;

public interface IAdminPricingService
{
    Task<IEnumerable<PriceRuleDto>> GetPriceRulesAsync();
    Task<PriceRuleDto> CreatePriceRuleAsync(CreatePriceRuleRequest request, Guid adminId);
    Task<PriceRuleDto> UpdatePriceRuleAsync(Guid id, UpdatePriceRuleRequest request, Guid adminId);
    Task DeletePriceRuleAsync(Guid id, Guid adminId);
    Task<IEnumerable<PriceOverrideLogDto>> GetOverrideLogsAsync(Guid? flightId = null);
    Task TriggerPriceRecalculationAsync(Guid? flightId, Guid adminId);
}
