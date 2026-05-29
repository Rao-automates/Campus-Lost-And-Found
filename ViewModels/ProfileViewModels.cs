using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using WEBDEV_Project.Models;

namespace WEBDEV_Project.ViewModels
{
    public class ProfileEditViewModel
    {
        [Required]
        [MaxLength(80)]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [MaxLength(300)]
        public string? Bio { get; set; }

        [Display(Name = "Profile Picture (Optional)")]
        public IFormFile? AvatarFile { get; set; }

        public string? CurrentAvatarPath { get; set; }
    }

    public class ProfileDashboardViewModel
    {
        public ApplicationUser User { get; set; } = new ApplicationUser();
        
        public int PostCount { get; set; }
        public int ResolvedCount { get; set; }
        public int ClaimCount { get; set; }
        public int BookmarkCount { get; set; }
        public double AverageRating { get; set; }

        public IEnumerable<Item> ActiveItems { get; set; } = new List<Item>();
        public IEnumerable<Item> ResolvedItems { get; set; } = new List<Item>();
        public IEnumerable<Claim> MyClaims { get; set; } = new List<Claim>();
        public IEnumerable<Item> BookmarkedItems { get; set; } = new List<Item>();
        
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public string ActiveTab { get; set; } = "active";
    }
}
