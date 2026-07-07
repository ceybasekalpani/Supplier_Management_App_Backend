using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Domain.Permissions;

namespace TeaFactorySupplierManagement.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            await ResyncIdentitySeedsAsync(context);
            await SeedPermissionCatalogAsync(context);

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

        private static async Task SeedPermissionCatalogAsync(ApplicationDbContext context)
        {
            var catalogCodes = PermissionCatalog.All
                .Select(definition => definition.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var existingPermissions = await context.Permissions.ToListAsync();
            var existingCodes = existingPermissions
                .Select(p => p.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missing = PermissionCatalog.All
                .Where(definition => !existingCodes.Contains(definition.Code))
                .Select(definition => new Permission
                {
                    Module = definition.Module,
                    ModuleLabel = definition.ModuleLabel,
                    Action = definition.Action,
                    Code = definition.Code,
                    DisplayName = definition.ActionLabel,
                    Description = definition.Description,
                    SortOrder = definition.SortOrder,
                    CreatedAt = DateTime.UtcNow
                })
                .ToList();

            if (missing.Count > 0)
            {
                context.Permissions.AddRange(missing);
            }

            // Remove permission rows for actions that no longer exist in PermissionCatalog
            // (e.g. after a module's action list is trimmed), along with any admin grants
            // that reference them, so the catalog served to the frontend never drifts from
            // what PermissionCatalog.cs actually defines.
            var orphaned = existingPermissions
                .Where(p => !catalogCodes.Contains(p.Code))
                .ToList();

            if (orphaned.Count > 0)
            {
                var orphanedIds = orphaned.Select(p => p.Id).ToList();
                var orphanedGrants = await context.SupplierDashboardAdminPermissions
                    .Where(grant => orphanedIds.Contains(grant.PermissionId))
                    .ToListAsync();

                context.SupplierDashboardAdminPermissions.RemoveRange(orphanedGrants);
                context.Permissions.RemoveRange(orphaned);
            }

            if (missing.Count > 0 || orphaned.Count > 0)
            {
                await context.SaveChangesAsync();
            }
        }

        private static async Task ResyncIdentitySeedsAsync(ApplicationDbContext context)
        {
            var identityReseedStatements = new[]
            {
                "IF OBJECT_ID(N'[dbo].[Supplier_Management_Disbursement_Records]', N'U') IS NOT NULL BEGIN DBCC CHECKIDENT ('[dbo].[Supplier_Management_Disbursement_Records]', RESEED) WITH NO_INFOMSGS; END",
                "IF OBJECT_ID(N'[dbo].[Supplier_Management_DeliveryNote_Header]', N'U') IS NOT NULL BEGIN DBCC CHECKIDENT ('[dbo].[Supplier_Management_DeliveryNote_Header]', RESEED) WITH NO_INFOMSGS; END",
                "IF OBJECT_ID(N'[dbo].[Supplier_Management_DeliveryNote_Detail]', N'U') IS NOT NULL BEGIN DBCC CHECKIDENT ('[dbo].[Supplier_Management_DeliveryNote_Detail]', RESEED) WITH NO_INFOMSGS; END",
                "IF OBJECT_ID(N'[dbo].[Supplier_Management_Code_Generation]', N'U') IS NOT NULL BEGIN DBCC CHECKIDENT ('[dbo].[Supplier_Management_Code_Generation]', RESEED) WITH NO_INFOMSGS; END",
                "IF OBJECT_ID(N'[dbo].[Supplier_Management_FactorySetup]', N'U') IS NOT NULL BEGIN DBCC CHECKIDENT ('[dbo].[Supplier_Management_FactorySetup]', RESEED) WITH NO_INFOMSGS; END",
                "IF OBJECT_ID(N'[dbo].[Supplier_Management_Notifications]', N'U') IS NOT NULL BEGIN DBCC CHECKIDENT ('[dbo].[Supplier_Management_Notifications]', RESEED) WITH NO_INFOMSGS; END",
                "IF OBJECT_ID(N'[dbo].[Supplier_Management_News]', N'U') IS NOT NULL BEGIN DBCC CHECKIDENT ('[dbo].[Supplier_Management_News]', RESEED) WITH NO_INFOMSGS; END",
                "IF OBJECT_ID(N'[dbo].[supplier_dashboard_AdminLogin]', N'U') IS NOT NULL BEGIN DBCC CHECKIDENT ('[dbo].[supplier_dashboard_AdminLogin]', RESEED) WITH NO_INFOMSGS; END"
            };

            foreach (var sql in identityReseedStatements)
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
        }
    }
}
