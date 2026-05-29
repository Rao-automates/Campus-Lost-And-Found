using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WEBDEV_Project.Data;
using WEBDEV_Project.Models;
using WEBDEV_Project.Services;

namespace WEBDEV_Project
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
                
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
                {
                    options.SignIn.RequireConfirmedAccount = false;
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 6;
                    options.Password.RequireUppercase = true;
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Register Custom Services
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IImageService, ImageService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IItemService, ItemService>();
            
            // Register Hangfire Jobs
            builder.Services.AddScoped<ArchiveExpiredJob>();
            builder.Services.AddScoped<SearchAlertJob>();

            // Configure Hangfire
            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(connectionString));

            builder.Services.AddHangfireServer();

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Seed Data
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    await DbSeeder.SeedRolesAndAdminAsync(services);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            
            // Hangfire Dashboard Setup (Restrict to Admin eventually, for now allow all in dev)
            app.UseHangfireDashboard("/hangfire");
            
            // Schedule Recurring Jobs
            using (var scope = app.Services.CreateScope())
            {
                var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
                recurringJobManager.AddOrUpdate<ArchiveExpiredJob>("archive-expired", j => j.RunAsync(), Cron.Daily(2, 0));
                recurringJobManager.AddOrUpdate<SearchAlertJob>("search-alerts", j => j.RunAsync(), Cron.Daily(8, 0));
            }

            // Security Headers
            app.Use(async (context, next) =>
            {
                context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
                await next();
            });

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
