using AdvancedOrderSystem.Models.Entities;

namespace AdvancedOrderSystem.Repositories;

public interface INotificationRepository
{
    // Admin notifications have no recipient, so admins see the shared ones
    Task<List<Notification>> GetForUserAsync(int userId, bool isAdmin, int take);

    Task<int> CountUnreadAsync(int userId, bool isAdmin);

    Task<Notification?> GetByIdAsync(int id);

    Task<List<Notification>> GetUnreadAsync(int userId, bool isAdmin);

    Task AddAsync(Notification notification);
}
