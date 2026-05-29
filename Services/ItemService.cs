using Microsoft.EntityFrameworkCore;
using WEBDEV_Project.Data;
using WEBDEV_Project.Models;
using WEBDEV_Project.ViewModels;

namespace WEBDEV_Project.Services
{
    public interface IItemService
    {
        Task<ItemListViewModel> GetPagedItemsAsync(string? search, int? categoryId, int? buildingId, string? type, string? sortBy, int page, int pageSize = 10);
        Task<Item?> GetByIdAsync(int id);
        Task IncrementViewAsync(int id);
        Task<Item> CreateAsync(CreateItemViewModel vm, string userId);
        Task UpdateAsync(int id, EditItemViewModel vm, string userId);
        Task SoftDeleteAsync(int id, string requesterId, bool isAdmin);
        Task ResolveAsync(int id, string userId);
        Task<bool> ToggleBookmarkAsync(string userId, int itemId);
        Task<int> AddFlagAsync(string userId, int itemId, string reason);
        Task<List<string>> AutocompleteAsync(string q);
    }

    public class ItemService : IItemService
    {
        private readonly ApplicationDbContext _db;

        public ItemService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<ItemListViewModel> GetPagedItemsAsync(string? search, int? categoryId, int? buildingId, string? type, string? sortBy, int page, int pageSize = 10)
        {
            var query = _db.Items
                .Include(i => i.Category)
                .Include(i => i.Building)
                .Include(i => i.Images)
                .Where(i => i.Status == ItemStatus.Active && !i.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(i => i.Title.ToLower().Contains(search) 
                                      || i.Description.ToLower().Contains(search)
                                      || (i.LocationDetail != null && i.LocationDetail.ToLower().Contains(search)));
            }

            if (categoryId.HasValue)
                query = query.Where(i => i.CategoryId == categoryId.Value);

            if (buildingId.HasValue)
                query = query.Where(i => i.BuildingId == buildingId.Value);

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<ItemType>(type, true, out var parsedType))
            {
                query = query.Where(i => i.Type == parsedType);
            }

            // Sorting
            query = sortBy switch
            {
                "oldest" => query.OrderBy(i => i.PostedAt),
                "views" => query.OrderByDescending(i => i.ViewCount),
                "claims" => query.OrderByDescending(i => i.Claims.Count),
                _ => query.OrderByDescending(i => i.PostedAt) // default newest
            };

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new ItemListViewModel
            {
                Items = items,
                Search = search,
                CategoryId = categoryId,
                BuildingId = buildingId,
                Type = type,
                SortBy = sortBy,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems
            };
        }

        public async Task<Item?> GetByIdAsync(int id)
        {
            return await _db.Items
                .Include(i => i.Images)
                .Include(i => i.Category)
                .Include(i => i.Building)
                .Include(i => i.User)
                .Include(i => i.Claims).ThenInclude(c => c.Claimant)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task IncrementViewAsync(int id)
        {
            var item = await _db.Items.FindAsync(id);
            if (item != null)
            {
                item.ViewCount++;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<Item> CreateAsync(CreateItemViewModel vm, string userId)
        {
            var item = new Item
            {
                Title = vm.Title,
                Description = vm.Description,
                Type = vm.Type,
                CategoryId = vm.CategoryId,
                BuildingId = vm.BuildingId,
                LocationDetail = vm.LocationDetail,
                DateOccurred = vm.DateOccurred,
                ExpiresAt = vm.ExpiresAt,
                Reward = vm.Type == ItemType.Lost ? vm.Reward : null,
                UserId = userId,
                PostedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = ItemStatus.Active,
                IsDeleted = false
            };

            _db.Items.Add(item);
            await _db.SaveChangesAsync();
            return item;
        }

        public async Task UpdateAsync(int id, EditItemViewModel vm, string userId)
        {
            var item = await _db.Items.FindAsync(id);
            if (item == null || item.UserId != userId) throw new UnauthorizedAccessException();

            // Log changes
            if (item.Title != vm.Title) AddAuditLog(id, userId, "Title", item.Title, vm.Title);
            if (item.Description != vm.Description) AddAuditLog(id, userId, "Description", item.Description, vm.Description);

            item.Title = vm.Title;
            item.Description = vm.Description;
            item.Type = vm.Type;
            item.CategoryId = vm.CategoryId;
            item.BuildingId = vm.BuildingId;
            item.LocationDetail = vm.LocationDetail;
            item.DateOccurred = vm.DateOccurred;
            item.ExpiresAt = vm.ExpiresAt;
            item.Reward = vm.Type == ItemType.Lost ? vm.Reward : null;
            item.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        private void AddAuditLog(int itemId, string userId, string field, string? oldVal, string? newVal)
        {
            _db.ItemAuditLogs.Add(new ItemAuditLog
            {
                ItemId = itemId,
                ChangedById = userId,
                FieldChanged = field,
                OldValue = oldVal,
                NewValue = newVal,
                ChangedAt = DateTime.UtcNow
            });
        }

        public async Task SoftDeleteAsync(int id, string requesterId, bool isAdmin)
        {
            var item = await _db.Items.FindAsync(id);
            if (item == null) return;
            if (item.UserId != requesterId && !isAdmin) throw new UnauthorizedAccessException();

            item.IsDeleted = true;
            item.Status = ItemStatus.Removed;
            item.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task ResolveAsync(int id, string userId)
        {
            var item = await _db.Items.FindAsync(id);
            if (item == null || item.UserId != userId) throw new UnauthorizedAccessException();

            item.Status = ItemStatus.Resolved;
            item.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task<bool> ToggleBookmarkAsync(string userId, int itemId)
        {
            var existing = await _db.UserBookmarks.FirstOrDefaultAsync(b => b.UserId == userId && b.ItemId == itemId);
            if (existing != null)
            {
                _db.UserBookmarks.Remove(existing);
                await _db.SaveChangesAsync();
                return false; // Not bookmarked anymore
            }
            else
            {
                _db.UserBookmarks.Add(new UserBookmark { UserId = userId, ItemId = itemId });
                await _db.SaveChangesAsync();
                return true; // Bookmarked
            }
        }

        public async Task<int> AddFlagAsync(string userId, int itemId, string reason)
        {
            var item = await _db.Items.FindAsync(itemId);
            if (item == null) return 0;

            var existingFlag = await _db.ItemFlags.FirstOrDefaultAsync(f => f.ReporterId == userId && f.ItemId == itemId);
            if (existingFlag == null)
            {
                _db.ItemFlags.Add(new ItemFlag { ItemId = itemId, ReporterId = userId, Reason = reason });
                item.FlagCount++;
                if (item.FlagCount >= 3)
                {
                    item.IsDeleted = true;
                    item.Status = ItemStatus.Removed;
                }
                await _db.SaveChangesAsync();
            }
            return item.FlagCount;
        }

        public async Task<List<string>> AutocompleteAsync(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return new List<string>();
            q = q.ToLower();
            
            return await _db.Items
                .Where(i => i.Status == ItemStatus.Active && !i.IsDeleted && i.Title.ToLower().Contains(q))
                .Select(i => i.Title)
                .Distinct()
                .Take(8)
                .ToListAsync();
        }
    }
}
