namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface ISeatClassRepository
{
    Task<SeatClass?> GetByIdAsync(int id);

    Task<SeatClass?> GetByCodeAsync(string code);

    Task<IEnumerable<SeatClass>> GetAllAsync();

    Task<SeatClass> CreateAsync(SeatClass seatClass);

    Task UpdateAsync(SeatClass seatClass);

    Task DeleteAsync(int id);
}
