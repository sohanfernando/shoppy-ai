using AdvancedOrderSystem.Data;
using AdvancedOrderSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdvancedOrderSystem.Repositories.Impl;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Notification>> GetForUserAsync(int userId, bool isAdmin, int take)
    {
        return await ForUser(userId, isAdmin)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> CountUnreadAsync(int userId, bool isAdmin)
    {
        return await ForUser(userId, isAdmin).CountAsync(n => !n.IsRead);
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<List<Notification>> GetUnreadAsync(int userId, bool isAdmin)
    {
        return await ForUser(userId, isAdmin).Where(n => !n.IsRead).ToListAsync();
    }

    public async Task AddAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
    }

    private IQueryable<Notification> ForUser(int userId, bool isAdmin)
    {
        return isAdmin
            ? _context.Notifications.Where(n => n.RecipientUserId == null)
            : _context.Notifications.Where(n => n.RecipientUserId == userId);
    }
}
