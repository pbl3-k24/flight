using FlightBooking.Application.DTOs.Flight;

namespace FlightBooking.Application.Services.Interfaces;

public interface IPromotionService
{
    Task<PromotionDto> GetByCodeAsync(string code);
    Task<IEnumerable<PromotionDto>> GetAllAsync();
    Task<PromotionDto> CreateAsync(CreatePromotionRequest request, Guid adminId);
    Task<PromotionDto> UpdateAsync(Guid id, UpdatePromotionRequest request, Guid adminId);
    Task DeactivateAsync(Guid id, Guid adminId);

    /// <summary>Validate and apply a promotion code. Returns discount amount.</summary>
    Task<decimal> ApplyPromotionAsync(string code, Guid? routeId, Guid? fareClassId, decimal baseAmount);
}
