using Microsoft.EntityFrameworkCore;
using WEBDEV_Project.Data;
using WEBDEV_Project.Models;

namespace WEBDEV_Project.Services
{
    public interface INotificationService
    {
        Task CreateAsync(string userId, string type, string message, string? link = null);
        Task<int> GetUnreadCountAsync(string userId);
        Task<List<Notification>> GetAllAsync(string userId);
        Task MarkAllReadAsync(string userId);
        Task MarkReadAsync(int id, string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;

        public NotificationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task CreateAsync(string userId, string type, string message, string? link = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Message = message,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<List<Notification>> GetAllAsync(string userId)
        {
            return await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkAllReadAsync(string userId)
        {
            var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
            foreach (var n in unread)
            {
                n.IsRead = true;
            }
            await _db.SaveChangesAsync();
        }

        public async Task MarkReadAsync(int id, string userId)
        {
            var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            if (notif != null)
            {
                notif.IsRead = true;
                await _db.SaveChangesAsync();
            }
        }
    }
}
