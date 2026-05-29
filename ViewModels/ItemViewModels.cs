using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using WEBDEV_Project.Models;

namespace WEBDEV_Project.ViewModels
{
    public class CreateItemViewModel
    {
        [Required]
        [MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public ItemType Type { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Building (Optional)")]
        public int? BuildingId { get; set; }

        [MaxLength(200)]
        [Display(Name = "Specific Location (e.g. Room 301)")]
        public string? LocationDetail { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date Occurred")]
        public DateTime DateOccurred { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Expires At (Optional)")]
        public DateTime? ExpiresAt { get; set; }

        [Display(Name = "Reward (Optional)")]
        public decimal? Reward { get; set; }

        [Display(Name = "Images (Max 5, 5MB each)")]
        public IEnumerable<IFormFile> Images { get; set; } = new List<IFormFile>();
    }

    public class EditItemViewModel : CreateItemViewModel
    {
        public int Id { get; set; }
    }

    public class ItemListViewModel
    {
        public IEnumerable<Item> Items { get; set; } = new List<Item>();
        
        // Filters
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int? BuildingId { get; set; }
        public string? Type { get; set; }
        public string? SortBy { get; set; }

        // Dropdown Data
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IEnumerable<CampusBuilding> Buildings { get; set; } = new List<CampusBuilding>();

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
    }

    public class ItemDetailViewModel
    {
        public Item Item { get; set; } = new Item();
        public bool IsOwner { get; set; }
        public bool IsBookmarked { get; set; }
        public Claim? ClaimsByCurrentUser { get; set; }
        public bool CanClaim { get; set; }
        public SubmitClaimViewModel ClaimForm { get; set; } = new SubmitClaimViewModel();
        public Rating? ExistingRating { get; set; }
        public ApplicationUser? UserToRate { get; set; }
    }

    public class SubmitClaimViewModel
    {
        [Required]
        public int ItemId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;
    }
}
