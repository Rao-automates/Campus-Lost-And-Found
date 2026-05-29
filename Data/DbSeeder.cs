using Microsoft.AspNetCore.Identity;
using WEBDEV_Project.Models;

namespace WEBDEV_Project.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Seed Roles
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2. Seed Admin User
            var adminEmail = "admin@campus.edu";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DisplayName = "Site Administrator"
                };

                var createPowerUser = await userManager.CreateAsync(adminUser, "Admin123!");
                if (createPowerUser.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Categories
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Phone", Slug = "phone" },
                    new Category { Name = "Keys", Slug = "keys" },
                    new Category { Name = "Bag or Backpack", Slug = "bag-backpack" },
                    new Category { Name = "Wallet", Slug = "wallet" },
                    new Category { Name = "Laptop", Slug = "laptop" },
                    new Category { Name = "ID Card", Slug = "id-card" },
                    new Category { Name = "Clothing", Slug = "clothing" },
                    new Category { Name = "Stationery", Slug = "stationery" },
                    new Category { Name = "Other", Slug = "other" }
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            // 4. Seed Buildings
            if (!context.CampusBuildings.Any())
            {
                var buildings = new List<CampusBuilding>
                {
                    new CampusBuilding { Name = "Main Block", Code = "MB" },
                    new CampusBuilding { Name = "Library", Code = "LIB" },
                    new CampusBuilding { Name = "Sports Complex", Code = "SC" },
                    new CampusBuilding { Name = "Cafeteria", Code = "CAF" },
                    new CampusBuilding { Name = "Admin Block", Code = "AB" }
                };
                context.CampusBuildings.AddRange(buildings);
                await context.SaveChangesAsync();
            }

            // 5. Seed Initial Items
            if (!context.Items.Any())
            {
                var admin = await userManager.FindByEmailAsync(adminEmail);
                var categoryWallet = context.Categories.FirstOrDefault(c => c.Slug == "wallet");
                var categoryPhone = context.Categories.FirstOrDefault(c => c.Slug == "phone");
                var buildingLib = context.CampusBuildings.FirstOrDefault(b => b.Code == "LIB");
                var buildingCaf = context.CampusBuildings.FirstOrDefault(b => b.Code == "CAF");

                if (admin != null && categoryWallet != null && categoryPhone != null && buildingLib != null && buildingCaf != null)
                {
                    var items = new List<Item>
                    {
                        new Item
                        {
                            Title = "Lost Black Leather Wallet",
                            Description = "I lost my black leather wallet near the library entrance. It has my ID cards and some cash.",
                            Type = ItemType.Lost,
                            CategoryId = categoryWallet.Id,
                            BuildingId = buildingLib.Id,
                            LocationDetail = "Main Entrance",
                            DateOccurred = DateTime.UtcNow.AddDays(-2),
                            UserId = admin.Id
                        },
                        new Item
                        {
                            Title = "Found Blue iPhone 13 Pro",
                            Description = "Found a blue iPhone 13 Pro with a clear case on a table in the cafeteria.",
                            Type = ItemType.Found,
                            CategoryId = categoryPhone.Id,
                            BuildingId = buildingCaf.Id,
                            LocationDetail = "Table near the window",
                            DateOccurred = DateTime.UtcNow.AddDays(-1),
                            UserId = admin.Id
                        }
                    };
                    context.Items.AddRange(items);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
