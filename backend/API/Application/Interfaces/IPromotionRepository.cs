using API.Domain.Entities;

namespace API.Application.Interfaces;

public interface IPromotionRepository
{
    Task<Promotion?> GetByCodeAsync(string code);
    Task<IEnumerable<Promotion>> GetActiveAsync(DateTime currentDateTime);
}
