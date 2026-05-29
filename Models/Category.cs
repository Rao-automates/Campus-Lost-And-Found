using System.ComponentModel.DataAnnotations;

namespace WEBDEV_Project.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(60)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Slug { get; set; } = string.Empty;

        public ICollection<Item> Items { get; set; } = new List<Item>();
    }
}
