using Microsoft.EntityFrameworkCore;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Domain.Permissions;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class DashboardUserService : IDashboardUserService
    {
        private static readonly List<string> Roles = new() { "Super Admin", "Admin", "Support", "Viewer" };
        private readonly ApplicationDbContext _context;

        public DashboardUserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardUserManagementDto> GetUsersAsync(string? search, string? status)
        {
            var query = _context.SupplierDashboardAdminLogins.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var cleanSearch = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Username.ToLower().Contains(cleanSearch) ||
                    (x.FullName != null && x.FullName.ToLower().Contains(cleanSearch)) ||
                    (x.Email != null && x.Email.ToLower().Contains(cleanSearch)));
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var active = NormalizeStatus(status) == "active";
                query = query.Where(x => x.IsActive == active);
            }

            var allUsers = await _context.SupplierDashboardAdminLogins.AsNoTracking().ToListAsync();
            var users = await query.OrderBy(x => x.FullName ?? x.Username).ToListAsync();

            return new DashboardUserManagementDto
            {
                Summary = new DashboardUserSummaryDto
                {
                    TotalAdministrators = allUsers.Count,
                    ActiveUsers = allUsers.Count(x => x.IsActive),
                    InactiveUsers = allUsers.Count(x => !x.IsActive)
                },
                Users = users.Select(ToDto).ToList(),
                Roles = Roles
            };
        }

        public async Task<DashboardUserDto> CreateUserAsync(CreateDashboardUserDto request)
        {
            ValidateUser(request.FullName, request.Email, request.Username, request.Password, true);
            await EnsureUniqueAsync(request.Username, request.Email, null);

            var now = DateTime.UtcNow;
            var user = new SupplierDashboardAdminLogin
            {
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                Username = request.Username.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim()),
                PhoneNo = CleanOptional(request.PhoneNo),
                Role = NormalizeRole(request.Role),
                IsActive = NormalizeStatus(request.Status) == "active",
                AvatarBase64 = CleanOptional(request.Avatar),
                CreatedAt = now
            };

            _context.SupplierDashboardAdminLogins.Add(user);
            await _context.SaveChangesAsync();

            return ToDto(user);
        }

        public async Task<DashboardUserDto?> UpdateUserAsync(int id, UpdateDashboardUserDto request)
        {
            ValidateUser(request.FullName, request.Email, request.Username, request.Password, false);
            await EnsureUniqueAsync(request.Username, request.Email, id);

            var user = await _context.SupplierDashboardAdminLogins.FindAsync(id);
            if (user == null)
            {
                return null;
            }

            user.FullName = request.FullName.Trim();
            user.Email = request.Email.Trim();
            user.Username = request.Username.Trim();
            user.PhoneNo = CleanOptional(request.PhoneNo);
            user.Role = NormalizeRole(request.Role);
            user.IsActive = NormalizeStatus(request.Status) == "active";
            if (request.Avatar != null)
            {
                user.AvatarBase64 = CleanOptional(request.Avatar);
            }
            user.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim());
            }

            await _context.SaveChangesAsync();
            return ToDto(user);
        }

        public async Task<string?> GetUserAvatarAsync(int id)
        {
            return await _context.SupplierDashboardAdminLogins
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => x.AvatarBase64)
                .FirstOrDefaultAsync();
        }

        public async Task<DashboardUserDto?> UpdateUserAvatarAsync(int id, string avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                throw new ArgumentException("Profile image URL is required.");
            }

            var user = await _context.SupplierDashboardAdminLogins.FindAsync(id);
            if (user == null)
            {
                return null;
            }

            user.AvatarBase64 = avatarUrl.Trim();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ToDto(user);
        }

        public async Task<DashboardUserDto?> ClearUserAvatarAsync(int id)
        {
            var user = await _context.SupplierDashboardAdminLogins.FindAsync(id);
            if (user == null)
            {
                return null;
            }

            user.AvatarBase64 = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ToDto(user);
        }

        public async Task<bool> DeleteUserAsync(int id, int? currentAdminId)
        {
            if (currentAdminId.HasValue && currentAdminId.Value == id)
            {
                throw new InvalidOperationException("You cannot delete the currently logged-in admin user.");
            }

            var user = await _context.SupplierDashboardAdminLogins.FindAsync(id);
            if (user == null)
            {
                return false;
            }

            _context.SupplierDashboardAdminLogins.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task EnsureUniqueAsync(string username, string email, int? currentId)
        {
            var cleanUsername = username.Trim().ToLower();
            var cleanEmail = email.Trim().ToLower();

            var usernameExists = await _context.SupplierDashboardAdminLogins
                .AnyAsync(x => x.Username.ToLower() == cleanUsername && (!currentId.HasValue || x.Id != currentId.Value));
            if (usernameExists)
            {
                throw new InvalidOperationException("Username already exists.");
            }

            var emailExists = await _context.SupplierDashboardAdminLogins
                .AnyAsync(x => x.Email != null && x.Email.ToLower() == cleanEmail && (!currentId.HasValue || x.Id != currentId.Value));
            if (emailExists)
            {
                throw new InvalidOperationException("Email already exists.");
            }
        }
        private static DashboardUserDto ToDto(SupplierDashboardAdminLogin user)
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
                LastLoginAt = user.LastLoginAt?.ToString("yyyy-MM-ddTHH:mm:ss"),
                UpdatedAt = user.UpdatedAt?.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }

        private static void ValidateUser(string fullName, string email, string username, string? password, bool requirePassword)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(username) ||
                (requirePassword && string.IsNullOrWhiteSpace(password)))
            {
                throw new ArgumentException("Full name, email, username and password are required.");
            }

            if (!email.Contains('@') || !email.Contains('.'))
            {
                throw new ArgumentException("A valid email address is required.");
            }

            if (!string.IsNullOrWhiteSpace(password) && password.Trim().Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters.");
            }
        }

        private static string NormalizeRole(string? role)
        {
            var cleanRole = string.IsNullOrWhiteSpace(role) ? "Admin" : role.Trim();
            return Roles.Contains(cleanRole) ? cleanRole : "Admin";
        }

        private static string NormalizeStatus(string? status)
        {
            return status?.Trim().Equals("inactive", StringComparison.OrdinalIgnoreCase) == true ? "inactive" : "active";
        }

        private static string? CleanOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
