using System.ComponentModel.DataAnnotations;

namespace WEBDEV_Project.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Message { get; set; } = string.Empty;

        public string? Link { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
    }
}
