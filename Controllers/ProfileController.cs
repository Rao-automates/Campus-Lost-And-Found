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
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IImageService _imageService;

        public ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IImageService imageService)
        {
            _db = db;
            _userManager = userManager;
            _imageService = imageService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string tab = "active", int page = 1)
        {
            var userId = _userManager.GetUserId(User)!;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var vm = new ProfileDashboardViewModel
            {
                User = user,
                PostCount = await _db.Items.CountAsync(i => i.UserId == userId && !i.IsDeleted),
                ResolvedCount = await _db.Items.CountAsync(i => i.UserId == userId && i.Status == ItemStatus.Resolved && !i.IsDeleted),
                ClaimCount = await _db.Claims.CountAsync(c => c.ClaimantId == userId),
                BookmarkCount = await _db.UserBookmarks.CountAsync(b => b.UserId == userId),
                ActiveTab = tab,
                CurrentPage = page
            };

            var ratings = await _db.Ratings.Where(r => r.RatedUserId == userId).ToListAsync();
            vm.AverageRating = ratings.Any() ? ratings.Average(r => r.Stars) : 0;

            int pageSize = 10;
            int skip = (page - 1) * pageSize;

            switch (tab.ToLower())
            {
                case "resolved":
                    var resolvedQuery = _db.Items.Include(i => i.Images).Include(i => i.Category).Where(i => i.UserId == userId && i.Status == ItemStatus.Resolved && !i.IsDeleted);
                    vm.TotalPages = (int)Math.Ceiling(await resolvedQuery.CountAsync() / (double)pageSize);
                    vm.ResolvedItems = await resolvedQuery.OrderByDescending(i => i.PostedAt).Skip(skip).Take(pageSize).ToListAsync();
                    break;
                case "claims":
                    var claimsQuery = _db.Claims.Include(c => c.Item).ThenInclude(i => i.Images).Where(c => c.ClaimantId == userId);
                    vm.TotalPages = (int)Math.Ceiling(await claimsQuery.CountAsync() / (double)pageSize);
                    vm.MyClaims = await claimsQuery.OrderByDescending(c => c.CreatedAt).Skip(skip).Take(pageSize).ToListAsync();
                    break;
                case "bookmarks":
                    var bookmarksQuery = _db.UserBookmarks.Include(b => b.Item).ThenInclude(i => i.Images).Where(b => b.UserId == userId && !b.Item!.IsDeleted);
                    vm.TotalPages = (int)Math.Ceiling(await bookmarksQuery.CountAsync() / (double)pageSize);
                    vm.BookmarkedItems = await bookmarksQuery.OrderByDescending(b => b.SavedAt).Skip(skip).Take(pageSize).Select(b => b.Item!).ToListAsync();
                    break;
                default: // "active"
                    var activeQuery = _db.Items.Include(i => i.Images).Include(i => i.Category).Where(i => i.UserId == userId && i.Status == ItemStatus.Active && !i.IsDeleted);
                    vm.TotalPages = (int)Math.Ceiling(await activeQuery.CountAsync() / (double)pageSize);
                    vm.ActiveItems = await activeQuery.OrderByDescending(i => i.PostedAt).Skip(skip).Take(pageSize).ToListAsync();
                    break;
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var vm = new ProfileEditViewModel
            {
                DisplayName = user.DisplayName,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                CurrentAvatarPath = user.AvatarPath
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileEditViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.DisplayName = vm.DisplayName;
            user.PhoneNumber = vm.PhoneNumber;
            user.Bio = vm.Bio;

            if (vm.AvatarFile != null)
            {
                try
                {
                    user.AvatarPath = await _imageService.SaveAvatarAsync(vm.AvatarFile, user.Id);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("AvatarFile", ex.Message);
                    vm.CurrentAvatarPath = user.AvatarPath;
                    return View(vm);
                }
            }

            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
