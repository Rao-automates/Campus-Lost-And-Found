using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WEBDEV_Project.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(80)]
        public string DisplayName { get; set; } = string.Empty;

        public string? AvatarPath { get; set; }

        [MaxLength(300)]
        public string? Bio { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        // Navigation
        public ICollection<Item> Items { get; set; } = new List<Item>();
        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
        public ICollection<UserBookmark> Bookmarks { get; set; } = new List<UserBookmark>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Rating> RatingsGiven { get; set; } = new List<Rating>();
    }
}
