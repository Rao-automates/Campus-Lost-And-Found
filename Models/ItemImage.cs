using System.ComponentModel.DataAnnotations;

namespace WEBDEV_Project.Models
{
    public class ItemImage
    {
        public int Id { get; set; }

        public int ItemId { get; set; }

        [Required]
        public string Path { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Item? Item { get; set; }
    }
}
