using System.ComponentModel.DataAnnotations;

namespace WEBDEV_Project.Models
{
    public class ItemFlag
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        [Required]
        public string ReporterId { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Item? Item { get; set; }
        public ApplicationUser? Reporter { get; set; }
    }

    public class Rating
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        [Required]
        public string RaterId { get; set; } = string.Empty;

        [Required]
        public string RatedUserId { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Stars { get; set; }

        [MaxLength(300)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Item? Item { get; set; }
        public ApplicationUser? Rater { get; set; }
        public ApplicationUser? RatedUser { get; set; }
    }

    public class ItemAuditLog
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        [Required]
        public string ChangedById { get; set; } = string.Empty;

        [Required]
        public string FieldChanged { get; set; } = string.Empty;

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public Item? Item { get; set; }
        public ApplicationUser? ChangedBy { get; set; }
    }

    public class AdminAuditLog
    {
        public int Id { get; set; }

        [Required]
        public string AdminId { get; set; } = string.Empty;

        [Required]
        public string Action { get; set; } = string.Empty;

        [Required]
        public string TargetType { get; set; } = string.Empty;

        [Required]
        public string TargetId { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? Admin { get; set; }
    }

    public class SavedSearch
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string? Query { get; set; }

        public int? CategoryId { get; set; }

        public string? Type { get; set; }

        public DateTime? LastAlertedAt { get; set; }

        public ApplicationUser? User { get; set; }
        public Category? Category { get; set; }
    }

    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        [Required]
        public string CreatedById { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? CreatedBy { get; set; }
    }
}
