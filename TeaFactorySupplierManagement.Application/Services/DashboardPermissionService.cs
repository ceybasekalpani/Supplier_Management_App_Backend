using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class DashboardPermissionService : IDashboardPermissionService
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly ApplicationDbContext _context;

        public DashboardPermissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardPermissionSettingsDto> GetSettingsAsync()
        {
            var users = await _context.SupplierDashboardAdminLogins
                .AsNoTracking()
                .OrderBy(x => x.FullName ?? x.Username)
                .ToListAsync();

            var result = new DashboardPermissionSettingsDto
            {
                Users = users.Select(ToUserDto).ToList(),
                Modules = GetModules()
            };

            foreach (var user in users)
            {
                var permissions = await GetOrCreateUserPermissionsAsync(user);
                result.UserPermissions[user.Id] = permissions;
            }

            return result;
        }

        public async Task<DashboardUserPermissionDto?> GetUserPermissionsAsync(int adminId)
        {
            var user = await _context.SupplierDashboardAdminLogins.FindAsync(adminId);
            if (user == null)
            {
                return null;
            }

            return await GetOrCreateUserPermissionsAsync(user);
        }

        public async Task<DashboardUserPermissionDto?> SaveUserPermissionsAsync(int adminId, SaveDashboardUserPermissionsDto request)
        {
            var user = await _context.SupplierDashboardAdminLogins.FindAsync(adminId);
            if (user == null)
            {
                return null;
            }

            await EnsureDefaultPermissionsAsync(_context, user);

            var rows = await _context.SupplierDashboardAdminPermissions
                .Where(x => x.AdminId == adminId)
                .ToListAsync();

            var modules = GetModules();
            foreach (var module in modules)
            {
                var row = rows.FirstOrDefault(x => x.ModuleId == module.Id);
                if (row == null)
                {
                    row = new SupplierDashboardAdminPermission
                    {
                        AdminId = adminId,
                        ModuleId = module.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.SupplierDashboardAdminPermissions.Add(row);
                }

                var enabled = request.ModulePermissions.TryGetValue(module.Id, out var isEnabled) && isEnabled;
                var requestedSubs = request.SubPermissions.TryGetValue(module.Id, out var subs) ? subs : new List<string>();
                var allowedSubs = module.SubPermissions.Select(x => x.Id).ToHashSet();
                var cleanSubs = requestedSubs.Where(allowedSubs.Contains).Distinct().ToList();

                row.IsEnabled = enabled;
                row.SubPermissionsJson = JsonSerializer.Serialize(enabled ? cleanSubs : new List<string>(), JsonOptions);
                row.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return await GetOrCreateUserPermissionsAsync(user);
        }

        public static async Task EnsureDefaultPermissionsAsync(ApplicationDbContext context, SupplierDashboardAdminLogin user)
        {
            var defaults = GetDefaultsForRole(user.Role);
            var existingModuleIds = await context.SupplierDashboardAdminPermissions
                .Where(x => x.AdminId == user.Id)
                .Select(x => x.ModuleId)
                .ToListAsync();

            var existing = existingModuleIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var addedRows = new List<SupplierDashboardAdminPermission>();

            foreach (var module in GetModules().DistinctBy(x => x.Id))
            {
                if (existing.Contains(module.Id))
                {
                    continue;
                }

                defaults.ModulePermissions.TryGetValue(module.Id, out var enabled);
                defaults.SubPermissions.TryGetValue(module.Id, out var subs);

                var row = new SupplierDashboardAdminPermission
                {
                    AdminId = user.Id,
                    ModuleId = module.Id,
                    IsEnabled = enabled,
                    SubPermissionsJson = JsonSerializer.Serialize(enabled ? subs ?? new List<string>() : new List<string>(), JsonOptions),
                    CreatedAt = DateTime.UtcNow
                };

                context.SupplierDashboardAdminPermissions.Add(row);
                addedRows.Add(row);
                existing.Add(module.Id);
            }

            if (addedRows.Count == 0)
            {
                return;
            }

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicatePermissionInsert(ex))
            {
                foreach (var row in addedRows)
                {
                    context.Entry(row).State = EntityState.Detached;
                }
            }
        }

        private static bool IsDuplicatePermissionInsert(DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlException &&
                   sqlException.Errors.Cast<SqlError>().Any(error => error.Number is 2601 or 2627);
        }

        private async Task<DashboardUserPermissionDto> GetOrCreateUserPermissionsAsync(SupplierDashboardAdminLogin user)
        {
            await EnsureDefaultPermissionsAsync(_context, user);

            var rows = await _context.SupplierDashboardAdminPermissions
                .AsNoTracking()
                .Where(x => x.AdminId == user.Id)
                .ToListAsync();

            var dto = new DashboardUserPermissionDto();
            foreach (var module in GetModules())
            {
                var row = rows.FirstOrDefault(x => x.ModuleId == module.Id);
                dto.ModulePermissions[module.Id] = row?.IsEnabled ?? false;
                dto.SubPermissions[module.Id] = NormalizeSubPermissions(module.Id, ParseSubPermissions(row?.SubPermissionsJson));
            }

            return dto;
        }

        private static DashboardUserPermissionDto GetDefaultsForRole(string? role)
        {
            var modules = GetModules();
            var dto = new DashboardUserPermissionDto();
            var normalizedRole = string.IsNullOrWhiteSpace(role) ? "Viewer" : role.Trim();

            foreach (var module in modules)
            {
                var allSubs = module.SubPermissions.Select(x => x.Id).ToList();
                var viewSubs = allSubs.Where(x => x.EndsWith(".view", StringComparison.OrdinalIgnoreCase)).ToList();

                if (normalizedRole == "Super Admin" || normalizedRole == "Admin")
                {
                    dto.ModulePermissions[module.Id] = true;
                    dto.SubPermissions[module.Id] = normalizedRole == "Super Admin" ? allSubs : allSubs;
                }
                else if (normalizedRole == "Support")
                {
                    var supportModules = new HashSet<string> { "dashboard", "suppliers", "requests", "communication", "disbursementTracking" };
                    dto.ModulePermissions[module.Id] = supportModules.Contains(module.Id);
                    dto.SubPermissions[module.Id] = supportModules.Contains(module.Id) ? viewSubs : new List<string>();
                }
                else
                {
                    var viewerModules = new HashSet<string> { "dashboard", "suppliers", "requests", "disbursementTracking" };
                    dto.ModulePermissions[module.Id] = viewerModules.Contains(module.Id);
                    dto.SubPermissions[module.Id] = viewerModules.Contains(module.Id) ? viewSubs : new List<string>();
                }
            }

            return dto;
        }

        public static List<DashboardModuleDto> GetModules()
        {
            return new List<DashboardModuleDto>
            {
                Module("dashboard", "Dashboard", "View analytics, cards, graphs, and quick actions", "dashboard.view"),
                Module("suppliers", "Suppliers", "Manage supplier information, search, and history", "suppliers.view", "suppliers.manage", "suppliers.export"),
                Module("requests", "Requests", "Handle advance, fertilizer and item requests with approvals", "requests.view", "advance_requests.view", "fertilizer_requests.view", "item_requests.view", "requests.manage", "requests.status_override"),
                Module("disbursements", "Disbursements", "Manage supplier disbursements and completed payment records", "disbursements.view", "disbursements.manage"),
                Module("disbursementTracking", "Disbursement Tracking", "Track disbursement progress and status changes", "disbursements.view", "disbursements.manage"),
                Module("configurations", "Configurations", "Manage fertilizer and item types", "fertilizer.manage", "items.manage", "configurations.manage"),
                Module("communication", "Communication", "News and notifications management", "news.view", "news.manage", "notifications.view", "notifications.send"),
                Module("userManagement", "User Management", "Admin user and role management", "users.view", "users.manage"),
                Module("settings", "Settings", "System configuration and permissions", "permissions.view", "permissions.assign", "permissions.manage")
            };
        }

        public static List<string> BuildPermissionKeys(DashboardUserPermissionDto permissions)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var module in GetModules())
            {
                if (permissions.ModulePermissions.TryGetValue(module.Id, out var enabled) && enabled)
                {
                    keys.Add(module.Id);
                }

                if (permissions.SubPermissions.TryGetValue(module.Id, out var subPermissions))
                {
                    foreach (var subPermission in subPermissions.Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        keys.Add(subPermission);
                    }
                }
            }

            return keys.ToList();
        }

        private static DashboardModuleDto Module(string id, string label, string description, params string[] subPermissionIds)
        {
            return new DashboardModuleDto
            {
                Id = id,
                Label = label,
                Description = description,
                SubPermissions = subPermissionIds.Select(x => new DashboardSubPermissionDto
                {
                    Id = x,
                    Label = ToLabel(x),
                    Description = $"{ToLabel(x)} permission"
                }).ToList()
            };
        }

        private static List<string> ParseSubPermissions(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static List<string> NormalizeSubPermissions(string moduleId, List<string> subPermissions)
        {
            return subPermissions.Select(x => NormalizeSubPermission(moduleId, x)).Distinct().ToList();
        }

        private static string NormalizeSubPermission(string moduleId, string subPermission)
        {
            if (subPermission.Contains('.'))
            {
                return subPermission;
            }

            const string viewPrefix = "view_";
            if (subPermission.StartsWith(viewPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return $"{moduleId}.view";
            }

            const string editPrefix = "edit_";
            if (subPermission.StartsWith(editPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return $"{moduleId}.manage";
            }

            return subPermission.Replace('_', '.');
        }

        private static string ToLabel(string value)
        {
            return string.Join(' ', value.Split('.', '_').Select(x => char.ToUpperInvariant(x[0]) + x[1..]));
        }

        private static DashboardUserDto ToUserDto(SupplierDashboardAdminLogin user)
        {
            return new DashboardUserDto
            {
                Id = user.Id,
                Name = user.FullName ?? user.Username,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Username = user.Username,
                PhoneNo = user.PhoneNo ?? string.Empty,
                Role = user.Role,
                Status = user.IsActive ? "active" : "inactive",
                Avatar = user.AvatarBase64,
                IsSuperAdmin = IsSuperAdmin(user),
                CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd"),
                LastLoginAt = user.LastLoginAt?.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }

        private static bool IsSuperAdmin(SupplierDashboardAdminLogin user)
        {
            return user.Role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase) ||
                   user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) ||
                   user.Username.Equals("mainadmin", StringComparison.OrdinalIgnoreCase) ||
                   user.Username.Equals("main_admin", StringComparison.OrdinalIgnoreCase);
        }
    }
}
