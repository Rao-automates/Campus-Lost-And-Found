using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WEBDEV_Project.Data;
using WEBDEV_Project.Models;
using WEBDEV_Project.Services;

namespace WEBDEV_Project.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(ApplicationDbContext db, INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Thread(int claimId)
        {
            var userId = _userManager.GetUserId(User);
            var claim = await _db.Claims
                .Include(c => c.Item)
                .Include(c => c.Claimant)
                .Include(c => c.Messages).ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(c => c.Id == claimId);

            if (claim == null) return NotFound();
            if (claim.Item!.UserId != userId && claim.ClaimantId != userId) return Forbid();

            // Mark other party's messages as read
            var unreadMessages = claim.Messages.Where(m => m.SenderId != userId && !m.IsRead).ToList();
            foreach (var m in unreadMessages)
            {
                m.IsRead = true;
            }
            if (unreadMessages.Any()) await _db.SaveChangesAsync();

            // Sort messages
            claim.Messages = claim.Messages.OrderBy(m => m.SentAt).ToList();

            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int claimId, string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return RedirectToAction("Thread", new { claimId });

            var userId = _userManager.GetUserId(User)!;
            var claim = await _db.Claims.Include(c => c.Item).FirstOrDefaultAsync(c => c.Id == claimId);

            if (claim == null || (claim.Item!.UserId != userId && claim.ClaimantId != userId)) 
                return Forbid();

            var message = new Message
            {
                ClaimId = claimId,
                SenderId = userId,
                Body = body,
                IsRead = false,
                SentAt = DateTime.UtcNow
            };

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            var recipientId = claim.Item.UserId == userId ? claim.ClaimantId : claim.Item.UserId;
            await _notificationService.CreateAsync(recipientId, "MessageReceived", $"New message regarding '{claim.Item.Title}'", $"/Messages/Thread?claimId={claimId}");

            return RedirectToAction("Thread", new { claimId });
        }
    }
}
