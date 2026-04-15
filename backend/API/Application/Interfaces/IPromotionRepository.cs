namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface IPromotionRepository
{
    Task<Promotion?> GetByCodeAsync(string code);

    Task<IEnumerable<Promotion>> GetActiveAsync(DateTime currentDateTime);

    Task<Promotion?> GetByIdAsync(int id);

    Task<IEnumerable<Promotion>> GetAllAsync();

    Task<Promotion> CreateAsync(Promotion promotion);

    Task UpdateAsync(Promotion promotion);

    Task DeleteAsync(int id);
}
