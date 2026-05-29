using System.ComponentModel.DataAnnotations;

namespace WEBDEV_Project.Models
{
    public class Claim
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        [Required]
        public string ClaimantId { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;

        public bool PosterConfirmed { get; set; } = false;

        public bool ClaimantConfirmed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Item? Item { get; set; }
        public ApplicationUser? Claimant { get; set; }
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
