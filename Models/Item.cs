using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WEBDEV_Project.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public ItemType Type { get; set; }

        public ItemStatus Status { get; set; } = ItemStatus.Active;

        public int CategoryId { get; set; }

        public int? BuildingId { get; set; }

        [MaxLength(200)]
        public string? LocationDetail { get; set; }

        [Required]
        public DateTime DateOccurred { get; set; }

        public DateTime? ExpiresAt { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? Reward { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public int FlagCount { get; set; } = 0;

        public int ViewCount { get; set; } = 0;

        public DateTime PostedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Category? Category { get; set; }
        public CampusBuilding? Building { get; set; }
        public ApplicationUser? User { get; set; }

        public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
        public ICollection<ItemFlag> Flags { get; set; } = new List<ItemFlag>();
        public ICollection<ItemAuditLog> AuditLogs { get; set; } = new List<ItemAuditLog>();
        public ICollection<UserBookmark> Bookmarks { get; set; } = new List<UserBookmark>();
    }
}
