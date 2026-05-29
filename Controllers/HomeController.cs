using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WEBDEV_Project.Data;
using WEBDEV_Project.Models;

namespace WEBDEV_Project.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var recentItems = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.Images)
                .Where(i => i.Status == ItemStatus.Active && !i.IsDeleted)
                .OrderByDescending(i => i.PostedAt)
                .Take(6)
                .ToListAsync();

            ViewBag.TotalItems = await _db.Items.CountAsync(i => !i.IsDeleted);
            ViewBag.ResolvedItems = await _db.Items.CountAsync(i => i.Status == ItemStatus.Resolved && !i.IsDeleted);
            ViewBag.TotalCategories = await _db.Categories.CountAsync();
            
            var categories = await _db.Categories.ToListAsync();
            ViewBag.Categories = categories;

            var announcement = await _db.Announcements
                .Include(a => a.CreatedBy)
                .Where(a => a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
                
            ViewBag.Announcement = announcement;

            return View(recentItems);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
