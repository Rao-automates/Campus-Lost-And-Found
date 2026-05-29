using Microsoft.EntityFrameworkCore;
using WEBDEV_Project.Data;
using WEBDEV_Project.Models;

namespace WEBDEV_Project.Services
{
    public class ArchiveExpiredJob
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;

        public ArchiveExpiredJob(ApplicationDbContext db, INotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        public async Task RunAsync()
        {
            var expiredItems = await _db.Items
                .Where(i => i.Status == ItemStatus.Active && i.ExpiresAt.HasValue && i.ExpiresAt.Value < DateTime.UtcNow)
                .ToListAsync();

            foreach (var item in expiredItems)
            {
                item.Status = ItemStatus.Expired;
                item.UpdatedAt = DateTime.UtcNow;
                
                await _notificationService.CreateAsync(item.UserId, "SystemAlert", $"Your post '{item.Title}' has expired.", $"/Items/Details/{item.Id}");
            }

            if (expiredItems.Any())
            {
                await _db.SaveChangesAsync();
            }
        }
    }

    public class SearchAlertJob
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public SearchAlertJob(ApplicationDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        public async Task RunAsync()
        {
            var savedSearches = await _db.SavedSearches.Include(s => s.User).ToListAsync();

            foreach (var search in savedSearches)
            {
                var queryTime = search.LastAlertedAt ?? DateTime.UtcNow.AddDays(-1);

                var itemsQuery = _db.Items
                    .Where(i => i.Status == ItemStatus.Active && !i.IsDeleted && i.PostedAt > queryTime);

                if (!string.IsNullOrWhiteSpace(search.Query))
                {
                    var q = search.Query.ToLower();
                    itemsQuery = itemsQuery.Where(i => i.Title.ToLower().Contains(q) || i.Description.ToLower().Contains(q));
                }

                if (search.CategoryId.HasValue)
                {
                    itemsQuery = itemsQuery.Where(i => i.CategoryId == search.CategoryId.Value);
                }

                if (!string.IsNullOrEmpty(search.Type) && Enum.TryParse<ItemType>(search.Type, true, out var type))
                {
                    itemsQuery = itemsQuery.Where(i => i.Type == type);
                }

                var newItems = await itemsQuery.Take(5).ToListAsync();

                foreach (var item in newItems)
                {
                    if (search.User?.Email != null)
                    {
                        var link = $"/Items/Details/{item.Id}";
                        await _emailService.SendSearchAlertAsync(search.User.Email, search.Query ?? "Category Alert", item.Title, link);
                    }
                }

                if (newItems.Any())
                {
                    search.LastAlertedAt = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
