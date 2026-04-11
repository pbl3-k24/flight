using FlightBooking.Application.DTOs.Flight;
using FlightBooking.Application.Services.Interfaces;
using FlightBooking.Domain.Entities;
using FlightBooking.Domain.Interfaces.Repositories;

namespace FlightBooking.Application.Services.Implementations;

public class PromotionService(IPriceRuleRepository priceRuleRepository) : IPromotionService
{
    // This implementation uses an in-memory approach since Promotion doesn't have its own repo
    // In production, inject IPromotionRepository
    private static readonly List<Promotion> _promotions = [];

    public Task<PromotionDto> GetByCodeAsync(string code)
    {
        var promo = _promotions.FirstOrDefault(p => p.Code == code && p.IsActive)
            ?? throw new KeyNotFoundException($"Promotion '{code}' not found.");
        return Task.FromResult(MapToDto(promo));
    }

    public Task<IEnumerable<PromotionDto>> GetAllAsync() =>
        Task.FromResult(_promotions.Select(MapToDto));

    public Task<PromotionDto> CreateAsync(CreatePromotionRequest request, Guid adminId)
    {
        var promo = new Promotion
        {
            Id = Guid.NewGuid(),
            Code = request.Code.ToUpperInvariant(),
            Name = request.Name,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinOrderAmount = request.MinOrderAmount,
            MaxDiscountAmount = request.MaxDiscountAmount,
            UsageLimit = request.UsageLimit,
            IsActive = true,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow
        };
        _promotions.Add(promo);
        return Task.FromResult(MapToDto(promo));
    }

    public Task<PromotionDto> UpdateAsync(Guid id, UpdatePromotionRequest request, Guid adminId)
    {
        var promo = _promotions.FirstOrDefault(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Promotion {id} not found.");
        if (request.Name is not null) promo.Name = request.Name;
        if (request.DiscountValue.HasValue) promo.DiscountValue = request.DiscountValue.Value;
        if (request.IsActive.HasValue) promo.IsActive = request.IsActive.Value;
        if (request.EndDate.HasValue) promo.EndDate = request.EndDate.Value;
        return Task.FromResult(MapToDto(promo));
    }

    public Task DeactivateAsync(Guid id, Guid adminId)
    {
        var promo = _promotions.FirstOrDefault(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Promotion {id} not found.");
        promo.IsActive = false;
        return Task.CompletedTask;
    }

    public Task<decimal> ApplyPromotionAsync(string code, Guid? routeId, Guid? fareClassId, decimal baseAmount)
    {
        var promo = _promotions.FirstOrDefault(p =>
            p.Code == code && p.IsActive &&
            p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow &&
            (p.UsageLimit == null || p.UsedCount < p.UsageLimit));

        if (promo is null) return Task.FromResult(0m);

        if (promo.MinOrderAmount.HasValue && baseAmount < promo.MinOrderAmount.Value)
            return Task.FromResult(0m);

        decimal discount = promo.DiscountType == "percent"
            ? Math.Round(baseAmount * promo.DiscountValue / 100, 0)
            : promo.DiscountValue;

        if (promo.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, promo.MaxDiscountAmount.Value);

        promo.UsedCount++;
        return Task.FromResult(discount);
    }

    private static PromotionDto MapToDto(Promotion p) =>
        new(p.Id, p.Code, p.Name, p.DiscountType, p.DiscountValue, p.IsActive, p.StartDate, p.EndDate);
}
