namespace WEBDEV_Project.Models
{
    public class UserBookmark
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public int ItemId { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
        public Item? Item { get; set; }
    }
}
