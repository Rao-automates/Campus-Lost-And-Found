using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WEBDEV_Project.Models;

namespace WEBDEV_Project.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items { get; set; }
        public DbSet<ItemImage> ItemImages { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<CampusBuilding> CampusBuildings { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserBookmark> UserBookmarks { get; set; }
        public DbSet<ItemFlag> ItemFlags { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        public DbSet<ItemAuditLog> ItemAuditLogs { get; set; }
        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
        public DbSet<SavedSearch> SavedSearches { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Indexes
            builder.Entity<Category>().HasIndex(c => c.Slug).IsUnique();
            builder.Entity<Claim>().HasIndex(c => new { c.ItemId, c.ClaimantId }).IsUnique();
            builder.Entity<UserBookmark>().HasIndex(b => new { b.UserId, b.ItemId }).IsUnique();
            builder.Entity<Item>().HasIndex(i => new { i.Status, i.Type });
            builder.Entity<Item>().HasIndex(i => i.CategoryId);
            builder.Entity<Item>().HasIndex(i => i.UserId);
            builder.Entity<Notification>().HasIndex(n => new { n.UserId, n.IsRead });

            // Decimal Precision
            builder.Entity<Item>().Property(i => i.Reward).HasColumnType("decimal(10,2)");

            // Relationships & Cascade rules
            builder.Entity<Item>()
                .HasMany(i => i.Images)
                .WithOne(img => img.Item)
                .HasForeignKey(img => img.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Item>()
                .HasMany(i => i.Flags)
                .WithOne(f => f.Item)
                .HasForeignKey(f => f.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Item>()
                .HasMany(i => i.AuditLogs)
                .WithOne(a => a.Item)
                .HasForeignKey(a => a.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Item>()
                .HasMany(i => i.Claims)
                .WithOne(c => c.Item)
                .HasForeignKey(c => c.ItemId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent accidental cascade

            builder.Entity<Claim>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.Claim)
                .HasForeignKey(m => m.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserBookmark>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookmarks)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserBookmark>()
                .HasOne(b => b.Item)
                .WithMany(i => i.Bookmarks)
                .HasForeignKey(b => b.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Rating>()
                .HasOne(r => r.Rater)
                .WithMany(u => u.RatingsGiven)
                .HasForeignKey(r => r.RaterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Rating>()
                .HasOne(r => r.RatedUser)
                .WithMany()
                .HasForeignKey(r => r.RatedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
