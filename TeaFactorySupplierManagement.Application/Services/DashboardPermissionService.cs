using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Domain.Permissions;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class DashboardPermissionService : IDashboardPermissionService
    {
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
                result.UserPermissions[user.Id] = await GetOrCreateUserPermissionsAsync(user);
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

        public async Task<bool> HasPermissionAsync(int adminId, string permissionCode)
        {
            var admin = await _context.SupplierDashboardAdminLogins
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == adminId && x.IsActive);

            if (admin == null)
            {
                return false;
            }

            if (PermissionEvaluationHelpers.IsSuperAdmin(admin))
            {
                return true;
            }

            return await _context.SupplierDashboardAdminPermissions
                .AsNoTracking()
                .AnyAsync(x => x.AdminId == adminId && x.IsGranted && x.Permission!.Code == permissionCode);
        }

        public async Task<DashboardUserPermissionDto?> SaveUserPermissionsAsync(int adminId, SaveDashboardUserPermissionsDto request)
        {
            var user = await _context.SupplierDashboardAdminLogins.FindAsync(adminId);
            if (user == null)
            {
                return null;
            }

            var desiredCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var module in GetModules())
            {
                var moduleEnabled = request.ModulePermissions.TryGetValue(module.Id, out var isEnabled) && isEnabled;
                if (!moduleEnabled)
                {
                    continue;
                }

                var requestedCodes = request.SubPermissions.TryGetValue(module.Id, out var subs) ? subs : new List<string>();
                var allowedCodes = module.SubPermissions.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var code in requestedCodes.Where(allowedCodes.Contains))
                {
                    desiredCodes.Add(code);
                }
            }

            var permissionIdsByCode = await _context.Permissions
                .Where(p => desiredCodes.Contains(p.Code))
                .ToDictionaryAsync(p => p.Code, p => p.Id, StringComparer.OrdinalIgnoreCase);

            var existingRows = await _context.SupplierDashboardAdminPermissions
                .Include(x => x.Permission)
                .Where(x => x.AdminId == adminId)
                .ToListAsync();

            foreach (var row in existingRows.Where(row => row.Permission != null && !desiredCodes.Contains(row.Permission.Code)))
            {
                _context.SupplierDashboardAdminPermissions.Remove(row);
            }

            var existingCodes = existingRows
                .Where(row => row.Permission != null)
                .Select(row => row.Permission!.Code)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var code in desiredCodes.Where(code => !existingCodes.Contains(code)))
            {
                if (!permissionIdsByCode.TryGetValue(code, out var permissionId))
                {
                    continue;
                }

                _context.SupplierDashboardAdminPermissions.Add(new SupplierDashboardAdminPermission
                {
                    AdminId = adminId,
                    PermissionId = permissionId,
                    IsGranted = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return await GetOrCreateUserPermissionsAsync(user);
        }

        private async Task<DashboardUserPermissionDto> GetOrCreateUserPermissionsAsync(SupplierDashboardAdminLogin user)
        {
            var grantedCodes = await _context.SupplierDashboardAdminPermissions
                .AsNoTracking()
                .Where(x => x.AdminId == user.Id && x.IsGranted)
                .Select(x => x.Permission!.Code)
                .ToListAsync();

            return BuildPermissionDto(grantedCodes);
        }

        private static DashboardUserPermissionDto BuildPermissionDto(IEnumerable<string> grantedCodes)
        {
            var granted = grantedCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var dto = new DashboardUserPermissionDto();

            foreach (var module in GetModules())
            {
                var grantedSubs = module.SubPermissions.Select(x => x.Id).Where(granted.Contains).ToList();
                dto.SubPermissions[module.Id] = grantedSubs;
                dto.ModulePermissions[module.Id] = grantedSubs.Count > 0;
            }

            return dto;
        }

        public static List<DashboardModuleDto> GetModules()
        {
            return PermissionCatalog.All
                .GroupBy(p => p.Module)
                .OrderBy(g => g.Min(p => p.SortOrder))
                .Select(g => new DashboardModuleDto
                {
                    Id = g.Key,
                    Label = g.First().ModuleLabel,
                    Description = g.First().ModuleDescription,
                    SubPermissions = g.OrderBy(p => p.SortOrder)
                        .Select(p => new DashboardSubPermissionDto
                        {
                            Id = p.Code,
                            Label = p.ActionLabel,
                            Description = p.Description
                        })
                        .ToList()
                })
                .ToList();
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
                AvatarUrl = user.AvatarBase64,
                ProfileImage = user.AvatarBase64,
                IsSuperAdmin = PermissionEvaluationHelpers.IsSuperAdmin(user),
                CreatedAt = user.CreatedAt.ToString("yyyy-MM-dd"),
                LastLoginAt = user.LastLoginAt?.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }
    }
}
