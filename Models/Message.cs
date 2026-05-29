using System.ComponentModel.DataAnnotations;

namespace WEBDEV_Project.Models
{
    public class Message
    {
        public int Id { get; set; }

        public int ClaimId { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Body { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Claim? Claim { get; set; }
        public ApplicationUser? Sender { get; set; }
    }
}
