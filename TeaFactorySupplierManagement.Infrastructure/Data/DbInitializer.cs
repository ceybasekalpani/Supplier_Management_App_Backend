using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // Add factory setup if none exists
            if (!await context.FactorySetups.AnyAsync())
            {
                context.FactorySetups.Add(new FactorySetup
                {
                    FactoryName = "Kandedola Tea Factory",
                    IsSuper = false,
                    IsCashRequest = true,
                    IsItemRequest = true,
                    IsFertilizerRequest = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Add sample notifications if none exist
            if (!await context.Notifications.AnyAsync())
            {
                context.Notifications.Add(new Notification
                {
                    Title = "Welcome to Tea Factory App",
                    Message = "Thank you for using our supplier management system. You can now submit requests and track your leaf collections.",
                    Type = "success",
                    Priority = 5,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow
                });

                context.Notifications.Add(new Notification
                {
                    Title = "New Feature Available",
                    Message = "You can now request fertilizers and items directly from the app. Check the Quick Actions section.",
                    Type = "info",
                    Priority = 3,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Add sample news if none exist
            if (!await context.News.AnyAsync())
            {
                context.News.Add(new News
                {
                    Title = "Tea Leaf Prices Updated",
                    Content = "The tea leaf prices for this month have been updated. Check with the factory for the latest rates.",
                    Priority = 4,
                    IsActive = true,
                    ShowPopup = true,
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(7)
                });
            }

            // Add first dashboard admin if none exists.
            if (!await context.SupplierDashboardAdminLogins.AnyAsync())
            {
                context.SupplierDashboardAdminLogins.Add(new SupplierDashboardAdminLogin
                {
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    FullName = "Dashboard Administrator",
                    Email = "admin@factory.local",
                    Role = "Super Admin",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                var firstAdmin = await context.SupplierDashboardAdminLogins
                    .OrderBy(a => a.Id)
                    .FirstAsync();

                if (string.IsNullOrWhiteSpace(firstAdmin.Role))
                {
                    firstAdmin.Role = "Super Admin";
                }

                if (string.IsNullOrWhiteSpace(firstAdmin.Email))
                {
                    firstAdmin.Email = "admin@factory.local";
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
