using System.ComponentModel.DataAnnotations;

namespace WEBDEV_Project.Models
{
    public class CampusBuilding
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Code { get; set; }

        public string? Campus { get; set; }

        public ICollection<Item> Items { get; set; } = new List<Item>();
    }
}
