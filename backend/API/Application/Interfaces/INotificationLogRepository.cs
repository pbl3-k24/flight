namespace API.Application.Interfaces;

using API.Domain.Entities;

public interface INotificationLogRepository
{
    Task<NotificationLog?> GetByIdAsync(int id);

    Task<IEnumerable<NotificationLog>> GetByUserIdAsync(int userId);

    Task<IEnumerable<NotificationLog>> GetByStatusAsync(int status);

    Task<IEnumerable<NotificationLog>> GetAllAsync();

    Task<NotificationLog> CreateAsync(NotificationLog notificationLog);

    Task UpdateAsync(NotificationLog notificationLog);

    Task DeleteAsync(int id);
}