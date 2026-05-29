using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WEBDEV_Project.Data;
using WEBDEV_Project.Models;
using WEBDEV_Project.Services;
using WEBDEV_Project.ViewModels;

namespace WEBDEV_Project.Controllers
{
    public class ItemsController : Controller
    {
        private readonly IItemService _itemService;
        private readonly IImageService _imageService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public ItemsController(IItemService itemService, IImageService imageService, UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _itemService = itemService;
            _imageService = imageService;
            _userManager = userManager;
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, int? categoryId, int? buildingId, string? type, string? sortBy, int page = 1)
        {
            var vm = await _itemService.GetPagedItemsAsync(search, categoryId, buildingId, type, sortBy, page, 12);
            
            vm.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            vm.Buildings = await _db.CampusBuildings.OrderBy(b => b.Name).ToListAsync();
            
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var item = await _itemService.GetByIdAsync(id);
            if (item == null || (item.IsDeleted && !User.IsInRole("Admin"))) return NotFound();

            await _itemService.IncrementViewAsync(id);

            var userId = _userManager.GetUserId(User);
            var isOwner = item.UserId == userId;
            var isBookmarked = userId != null && await _db.UserBookmarks.AnyAsync(b => b.UserId == userId && b.ItemId == id);
            var userClaim = userId != null ? item.Claims.FirstOrDefault(c => c.ClaimantId == userId) : null;
            var hasApprovedClaim = item.Claims.Any(c => c.Status == ClaimStatus.Approved);
            var canClaim = userId != null && !isOwner && userClaim == null && item.Status == ItemStatus.Active && !hasApprovedClaim;

            Rating? existingRating = null;
            ApplicationUser? userToRate = null;

            if (item.Status == ItemStatus.Resolved && userId != null)
            {
                var approvedClaim = item.Claims.FirstOrDefault(c => c.Status == ClaimStatus.Approved);
                if (approvedClaim != null)
                {
                    if (isOwner)
                    {
                        userToRate = approvedClaim.Claimant;
                    }
                    else if (approvedClaim.ClaimantId == userId)
                    {
                        userToRate = item.User;
                    }

                    if (userToRate != null)
                    {
                        existingRating = await _db.Ratings.FirstOrDefaultAsync(r => r.ItemId == id && r.RaterId == userId);
                    }
                }
            }

            var vm = new ItemDetailViewModel
            {
                Item = item,
                IsOwner = isOwner,
                IsBookmarked = isBookmarked,
                ClaimsByCurrentUser = userClaim,
                CanClaim = canClaim,
                ClaimForm = new SubmitClaimViewModel { ItemId = id },
                ExistingRating = existingRating,
                UserToRate = userToRate
            };

            return View(vm);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            ViewBag.Buildings = new SelectList(await _db.CampusBuildings.OrderBy(b => b.Name).ToListAsync(), "Id", "Name");
            return View(new CreateItemViewModel { Type = ItemType.Lost });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateItemViewModel vm)
        {
            var userId = _userManager.GetUserId(User)!;
            
            // Check daily limit
            var today = DateTime.UtcNow.Date;
            var postsToday = await _db.Items.CountAsync(i => i.UserId == userId && i.PostedAt >= today);
            if (postsToday >= 10)
            {
                ModelState.AddModelError(string.Empty, "You have reached your daily limit of 10 posts.");
            }

            if (ModelState.IsValid)
            {
                var item = await _itemService.CreateAsync(vm, userId);
                
                if (vm.Images != null && vm.Images.Any())
                {
                    await _imageService.SaveItemImagesAsync(vm.Images, item.Id, _db);
                }

                TempData["Success"] = "Item posted successfully!";
                return RedirectToAction(nameof(Details), new { id = item.Id });
            }

            ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", vm.CategoryId);
            ViewBag.Buildings = new SelectList(await _db.CampusBuildings.OrderBy(b => b.Name).ToListAsync(), "Id", "Name", vm.BuildingId);
            return View(vm);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _itemService.GetByIdAsync(id);
            if (item == null) return NotFound();
            if (item.UserId != _userManager.GetUserId(User)) return Forbid();

            var vm = new EditItemViewModel
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                Type = item.Type,
                CategoryId = item.CategoryId,
                BuildingId = item.BuildingId,
                LocationDetail = item.LocationDetail,
                DateOccurred = item.DateOccurred,
                ExpiresAt = item.ExpiresAt,
                Reward = item.Reward
            };

            ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", item.CategoryId);
            ViewBag.Buildings = new SelectList(await _db.CampusBuildings.OrderBy(b => b.Name).ToListAsync(), "Id", "Name", item.BuildingId);
            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditItemViewModel vm)
        {
            if (id != vm.Id) return BadRequest();
            
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User)!;
                try
                {
                    await _itemService.UpdateAsync(id, vm, userId);
                    
                    if (vm.Images != null && vm.Images.Any())
                    {
                        // In a real app you might want to delete old images or manage them better.
                        // For this prompt, we'll just append new images up to the limit of 5.
                        var currentImageCount = await _db.ItemImages.CountAsync(i => i.ItemId == id);
                        if (currentImageCount < 5)
                        {
                            var filesToUpload = vm.Images.Take(5 - currentImageCount);
                            await _imageService.SaveItemImagesAsync(filesToUpload, id, _db);
                        }
                    }

                    TempData["Success"] = "Item updated successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (UnauthorizedAccessException)
                {
                    return Forbid();
                }
            }

            ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", vm.CategoryId);
            ViewBag.Buildings = new SelectList(await _db.CampusBuildings.OrderBy(b => b.Name).ToListAsync(), "Id", "Name", vm.BuildingId);
            return View(vm);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _itemService.SoftDeleteAsync(id, _userManager.GetUserId(User)!, User.IsInRole("Admin"));
                TempData["Success"] = "Item removed successfully.";
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            
            return RedirectToAction("Index", "Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id)
        {
            try
            {
                await _itemService.ResolveAsync(id, _userManager.GetUserId(User)!);
                TempData["Success"] = "Item marked as resolved!";
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bookmark(int id)
        {
            var isBookmarked = await _itemService.ToggleBookmarkAsync(_userManager.GetUserId(User)!, id);
            return Json(new { bookmarked = isBookmarked });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Flag(int id, string reason)
        {
            var count = await _itemService.AddFlagAsync(_userManager.GetUserId(User)!, id, reason);
            return Json(new { flagCount = count });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRating(int itemId, string ratedUserId, int stars, string? comment)
        {
            var raterId = _userManager.GetUserId(User)!;
            
            // Check if already rated
            var existing = await _db.Ratings.AnyAsync(r => r.ItemId == itemId && r.RaterId == raterId);
            if (!existing)
            {
                var rating = new Rating
                {
                    ItemId = itemId,
                    RaterId = raterId,
                    RatedUserId = ratedUserId,
                    Stars = stars < 1 ? 1 : (stars > 5 ? 5 : stars),
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Ratings.Add(rating);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Your review has been submitted!";
            }
            return RedirectToAction(nameof(Details), new { id = itemId });
        }

        [HttpGet]
        public async Task<IActionResult> Autocomplete(string q)
        {
            var results = await _itemService.AutocompleteAsync(q);
            return Json(results);
        }
    }
}
