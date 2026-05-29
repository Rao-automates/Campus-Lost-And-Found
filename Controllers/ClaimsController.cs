using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WEBDEV_Project.Data;
using WEBDEV_Project.Models;
using WEBDEV_Project.Services;
using WEBDEV_Project.ViewModels;

namespace WEBDEV_Project.Controllers
{
    [Authorize]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClaimsController(ApplicationDbContext db, INotificationService notificationService, IEmailService emailService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _notificationService = notificationService;
            _emailService = emailService;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(SubmitClaimViewModel vm)
        {
            var userId = _userManager.GetUserId(User)!;
            var item = await _db.Items.Include(i => i.User).FirstOrDefaultAsync(i => i.Id == vm.ItemId);

            if (item == null || item.Status != ItemStatus.Active || item.UserId == userId)
                return BadRequest();

            var existingClaim = await _db.Claims.AnyAsync(c => c.ItemId == vm.ItemId && c.ClaimantId == userId);
            if (existingClaim)
                return BadRequest("You have already claimed this item.");

            var hasApprovedClaim = await _db.Claims.AnyAsync(c => c.ItemId == vm.ItemId && c.Status == ClaimStatus.Approved);
            if (hasApprovedClaim)
                return BadRequest("This item already has an approved claim pending handover.");

            var claim = new Claim
            {
                ItemId = vm.ItemId,
                ClaimantId = userId,
                Message = vm.Message,
                Status = ClaimStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _db.Claims.Add(claim);
            await _db.SaveChangesAsync();

            var currentUser = await _userManager.GetUserAsync(User);
            
            await _notificationService.CreateAsync(item.UserId, "ClaimReceived", $"New claim from {currentUser!.DisplayName} on '{item.Title}'", $"/Items/Details/{item.Id}");
            if (item.User?.Email != null)
            {
                _ = _emailService.SendClaimReceivedAsync(item.User.Email, item.Title, currentUser.DisplayName, vm.Message);
            }

            TempData["Success"] = "Your claim has been submitted successfully.";
            return RedirectToAction("Details", "Items", new { id = vm.ItemId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var userId = _userManager.GetUserId(User);
            var claim = await _db.Claims
                .Include(c => c.Item)
                .Include(c => c.Claimant)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null || claim.Item?.UserId != userId) return Forbid();

            claim.Status = ClaimStatus.Approved;
            await _db.SaveChangesAsync();

            await _notificationService.CreateAsync(claim.ClaimantId, "ClaimApproved", $"Your claim for '{claim.Item.Title}' was approved!", $"/Items/Details/{claim.ItemId}");
            if (claim.Claimant?.Email != null)
            {
                _ = _emailService.SendClaimStatusAsync(claim.Claimant.Email, claim.Item.Title, ClaimStatus.Approved);
            }

            TempData["Success"] = "Claim approved.";
            return RedirectToAction("Details", "Items", new { id = claim.ItemId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var userId = _userManager.GetUserId(User);
            var claim = await _db.Claims
                .Include(c => c.Item)
                .Include(c => c.Claimant)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null || claim.Item?.UserId != userId) return Forbid();

            claim.Status = ClaimStatus.Rejected;
            await _db.SaveChangesAsync();

            await _notificationService.CreateAsync(claim.ClaimantId, "ClaimRejected", $"Your claim for '{claim.Item.Title}' was rejected.", $"/Items/Details/{claim.ItemId}");
            if (claim.Claimant?.Email != null)
            {
                _ = _emailService.SendClaimStatusAsync(claim.Claimant.Email, claim.Item.Title, ClaimStatus.Rejected);
            }

            TempData["Success"] = "Claim rejected.";
            return RedirectToAction("Details", "Items", new { id = claim.ItemId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmHandover(int id)
        {
            var userId = _userManager.GetUserId(User);
            var claim = await _db.Claims.Include(c => c.Item).FirstOrDefaultAsync(c => c.Id == id);

            if (claim == null || claim.Status != ClaimStatus.Approved) return BadRequest();

            if (claim.Item!.UserId == userId)
                claim.PosterConfirmed = true;
            else if (claim.ClaimantId == userId)
                claim.ClaimantConfirmed = true;
            else
                return Forbid();

            if (claim.PosterConfirmed && claim.ClaimantConfirmed)
            {
                claim.Item.Status = ItemStatus.Resolved;
                claim.Item.UpdatedAt = DateTime.UtcNow;
                
                await _notificationService.CreateAsync(claim.Item.UserId, "ItemResolved", $"'{claim.Item.Title}' has been resolved.", $"/Items/Details/{claim.ItemId}");
                await _notificationService.CreateAsync(claim.ClaimantId, "ItemResolved", $"'{claim.Item.Title}' has been resolved.", $"/Items/Details/{claim.ItemId}");
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Details", "Items", new { id = claim.ItemId });
        }
    }
}
