using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WEBDEV_Project.Data;
using WEBDEV_Project.Models;
using WEBDEV_Project.ViewModels;

namespace WEBDEV_Project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalItems = await _db.Items.CountAsync(),
                ActiveItems = await _db.Items.CountAsync(i => i.Status == ItemStatus.Active && !i.IsDeleted),
                ResolvedItems = await _db.Items.CountAsync(i => i.Status == ItemStatus.Resolved && !i.IsDeleted),
                TotalUsers = await _userManager.Users.CountAsync(),
                FlaggedItemsCount = await _db.Items.CountAsync(i => i.FlagCount > 0 && !i.IsDeleted)
            };

            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-11);
            var items = await _db.Items
                .Where(i => i.PostedAt >= new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1))
                .ToListAsync();

            var stats = items.GroupBy(i => new { i.PostedAt.Year, i.PostedAt.Month })
                             .Select(g => new MonthlyStatDto
                             {
                                 Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                                 Count = g.Count()
                             })
                             .ToList();

            // Ensure all 12 months are present even if 0
            for (int i = 0; i < 12; i++)
            {
                var d = DateTime.UtcNow.AddMonths(-11 + i);
                var monthStr = d.ToString("MMM yyyy");
                if (!stats.Any(s => s.Month == monthStr))
                {
                    stats.Add(new MonthlyStatDto { Month = monthStr, Count = 0 });
                }
            }

            vm.MonthlyStats = stats.OrderBy(s => DateTime.ParseExact(s.Month, "MMM yyyy", CultureInfo.InvariantCulture)).ToList();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Items(string search, string status, int page = 1)
        {
            var query = _db.Items.Include(i => i.User).Include(i => i.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(i => i.Title.ToLower().Contains(search) || i.Description.ToLower().Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Deleted") query = query.Where(i => i.IsDeleted);
                else if (Enum.TryParse<ItemStatus>(status, true, out var s)) query = query.Where(i => i.Status == s && !i.IsDeleted);
            }

            int pageSize = 20;
            ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.Status = status;

            var items = await query.OrderByDescending(i => i.PostedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int id)
        {
            var item = await _db.Items.FindAsync(id);
            if (item != null)
            {
                item.Status = ItemStatus.Removed;
                item.IsDeleted = true;
                LogAdminAction("RemoveItem", "Item", id.ToString());
                await _db.SaveChangesAsync();
                TempData["Success"] = "Item removed.";
            }
            return RedirectToAction(nameof(Items));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreItem(int id)
        {
            var item = await _db.Items.FindAsync(id);
            if (item != null)
            {
                item.IsDeleted = false;
                item.Status = ItemStatus.Active;
                item.FlagCount = 0;
                LogAdminAction("RestoreItem", "Item", id.ToString());
                await _db.SaveChangesAsync();
                TempData["Success"] = "Item restored.";
            }
            return RedirectToAction(nameof(Items));
        }

        [HttpGet]
        public async Task<IActionResult> Users(string search, int page = 1)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(u => u.Email!.ToLower().Contains(search) || u.DisplayName.ToLower().Contains(search));
            }

            int pageSize = 20;
            ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;

            var users = await query.OrderBy(u => u.Email).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                await _userManager.UpdateSecurityStampAsync(user); // Force logout
                LogAdminAction("BanUser", "User", id);
                await _db.SaveChangesAsync();
                TempData["Success"] = "User banned.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = true;
                LogAdminAction("UnbanUser", "User", id);
                await _db.SaveChangesAsync();
                TempData["Success"] = "User unbanned.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> Flags(int page = 1)
        {
            int pageSize = 20;
            var query = _db.Items.Where(i => i.FlagCount > 0 && !i.IsDeleted);
            ViewBag.TotalPages = (int)Math.Ceiling(await query.CountAsync() / (double)pageSize);
            ViewBag.CurrentPage = page;

            var items = await query.OrderByDescending(i => i.FlagCount).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissFlags(int id)
        {
            var item = await _db.Items.Include(i => i.Flags).FirstOrDefaultAsync(i => i.Id == id);
            if (item != null)
            {
                item.FlagCount = 0;
                _db.ItemFlags.RemoveRange(item.Flags);
                LogAdminAction("DismissFlags", "Item", id.ToString());
                await _db.SaveChangesAsync();
                TempData["Success"] = "Flags dismissed.";
            }
            return RedirectToAction(nameof(Flags));
        }

        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(string name, string slug)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug)) return BadRequest();
            _db.Categories.Add(new Category { Name = name, Slug = slug });
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string name, string slug)
        {
            var cat = await _db.Categories.FindAsync(id);
            if (cat != null && !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(slug))
            {
                cat.Name = name;
                cat.Slug = slug;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _db.Categories.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id);
            if (cat != null)
            {
                if (cat.Items.Any())
                {
                    TempData["Error"] = "Cannot delete category because it is in use by items.";
                }
                else
                {
                    _db.Categories.Remove(cat);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Category deleted.";
                }
            }
            return RedirectToAction(nameof(Categories));
        }

        [HttpGet]
        public async Task<IActionResult> Announcement()
        {
            var announcement = await _db.Announcements.FirstOrDefaultAsync(a => a.IsActive);
            return View(announcement ?? new Announcement());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetAnnouncement(string message)
        {
            var existing = await _db.Announcements.Where(a => a.IsActive).ToListAsync();
            foreach (var e in existing) e.IsActive = false;

            if (!string.IsNullOrWhiteSpace(message))
            {
                _db.Announcements.Add(new Announcement
                {
                    Message = message,
                    IsActive = true,
                    CreatedById = _userManager.GetUserId(User)!,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Announcement updated.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpGet]
        public async Task<IActionResult> Export()
        {
            var items = await _db.Items
                .Include(i => i.User)
                .Include(i => i.Category)
                .Include(i => i.Building)
                .Select(i => new
                {
                    i.Id,
                    i.Title,
                    Type = i.Type.ToString(),
                    Category = i.Category != null ? i.Category.Name : "",
                    Status = i.Status.ToString(),
                    PostedBy = i.User != null ? i.User.Email : "",
                    i.PostedAt
                })
                .ToListAsync();

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            
            csv.WriteRecords(items);
            writer.Flush();
            
            return File(memoryStream.ToArray(), "text/csv", $"items_export_{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        private void LogAdminAction(string action, string type, string id)
        {
            _db.AdminAuditLogs.Add(new AdminAuditLog
            {
                AdminId = _userManager.GetUserId(User)!,
                Action = action,
                TargetType = type,
                TargetId = id,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
