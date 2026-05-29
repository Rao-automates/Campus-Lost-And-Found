using WEBDEV_Project.Models;

namespace WEBDEV_Project.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalItems { get; set; }
        public int ActiveItems { get; set; }
        public int ResolvedItems { get; set; }
        public int TotalUsers { get; set; }
        public int FlaggedItemsCount { get; set; }
        
        public List<MonthlyStatDto> MonthlyStats { get; set; } = new List<MonthlyStatDto>();
    }

    public class MonthlyStatDto
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
