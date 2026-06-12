using System.Text.Json;
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
            var exists = await context.SupplierDashboardAdminPermissions.AnyAsync(x => x.AdminId == user.Id);
            if (exists)
            {
                return;
            }

            var defaults = GetDefaultsForRole(user.Role);
            foreach (var module in GetModules())
            {
                defaults.ModulePermissions.TryGetValue(module.Id, out var enabled);
                defaults.SubPermissions.TryGetValue(module.Id, out var subs);

                context.SupplierDashboardAdminPermissions.Add(new SupplierDashboardAdminPermission
                {
                    AdminId = user.Id,
                    ModuleId = module.Id,
                    IsEnabled = enabled,
                    SubPermissionsJson = JsonSerializer.Serialize(enabled ? subs ?? new List<string>() : new List<string>(), JsonOptions),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
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
                dto.SubPermissions[module.Id] = ParseSubPermissions(row?.SubPermissionsJson);
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
                var viewSubs = allSubs.Where(x => x.StartsWith("view_", StringComparison.OrdinalIgnoreCase)).ToList();

                if (normalizedRole == "Super Admin" || normalizedRole == "Admin")
                {
                    dto.ModulePermissions[module.Id] = true;
                    dto.SubPermissions[module.Id] = normalizedRole == "Super Admin" ? allSubs : allSubs;
                }
                else if (normalizedRole == "Support")
                {
                    var supportModules = new HashSet<string> { "dashboard", "suppliers", "requests", "communication" };
                    dto.ModulePermissions[module.Id] = supportModules.Contains(module.Id);
                    dto.SubPermissions[module.Id] = supportModules.Contains(module.Id) ? viewSubs : new List<string>();
                }
                else
                {
                    var viewerModules = new HashSet<string> { "dashboard", "suppliers", "requests" };
                    dto.ModulePermissions[module.Id] = viewerModules.Contains(module.Id);
                    dto.SubPermissions[module.Id] = viewerModules.Contains(module.Id) ? viewSubs : new List<string>();
                }
            }

            return dto;
        }

        private static List<DashboardModuleDto> GetModules()
        {
            return new List<DashboardModuleDto>
            {
                Module("dashboard", "Dashboard", "View analytics, cards, graphs, and quick actions", "view_dashboard"),
                Module("suppliers", "Suppliers", "Manage supplier information, search, and history", "view_suppliers", "edit_suppliers", "export_suppliers"),
                Module("requests", "Requests", "Handle advance, fertilizer and item requests with approvals", "view_requests", "approve_requests", "reject_requests", "export_requests"),
                Module("configurations", "Configurations", "Manage fertilizer and item types", "view_configurations", "create_configurations", "edit_configurations", "delete_configurations"),
                Module("communication", "Communication", "News and notifications management", "view_communication", "create_communication", "edit_communication", "delete_communication"),
                Module("userManagement", "User Management", "Admin user and role management", "view_users", "create_users", "edit_users", "delete_users"),
                Module("settings", "Settings", "System configuration and permissions", "view_settings", "edit_permissions")
            };
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

        private static string ToLabel(string value)
        {
            return string.Join(' ', value.Split('_').Select(x => char.ToUpperInvariant(x[0]) + x[1..]));
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
                CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd"),
                LastLoginAt = user.LastLoginAt?.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }
    }
}
