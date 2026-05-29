using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WEBDEV_Project.Models;
using WEBDEV_Project.Services;

namespace WEBDEV_Project.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var notifications = await _notificationService.GetAllAsync(_userManager.GetUserId(User)!);
            return View(notifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            await _notificationService.MarkAllReadAsync(_userManager.GetUserId(User)!);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            await _notificationService.MarkReadAsync(id, _userManager.GetUserId(User)!);
            return Json(new { ok = true });
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            if (!User.Identity!.IsAuthenticated) return Json(new { count = 0 });
            var count = await _notificationService.GetUnreadCountAsync(_userManager.GetUserId(User)!);
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> Dropdown()
        {
            if (!User.Identity!.IsAuthenticated) return Json(new object[0]);
            var all = await _notificationService.GetAllAsync(_userManager.GetUserId(User)!);
            var latest = all.Take(5).Select(n => new {
                n.Id,
                n.Message,
                n.Link,
                n.IsRead,
                CreatedAt = n.CreatedAt.ToLocalTime().ToString("MMM dd, HH:mm")
            });
            return Json(latest);
        }
    }
}
