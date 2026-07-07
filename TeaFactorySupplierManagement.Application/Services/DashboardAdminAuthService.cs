using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TeaFactorySupplierManagement.Application.DTOs.Dashboard;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Domain.Permissions;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class DashboardAdminAuthService : IDashboardAdminAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IDashboardPermissionService _dashboardPermissionService;

        public DashboardAdminAuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IDashboardPermissionService dashboardPermissionService)
        {
            _context = context;
            _configuration = configuration;
            _dashboardPermissionService = dashboardPermissionService;
        }

        public async Task<DashboardAdminLoginResponseDto?> LoginAsync(string username, string password)
        {
            var user = username.Trim();
            var pass = password.Trim();

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                return null;
            }

            var normalizedUser = user.ToLower();
            var admin = await _context.SupplierDashboardAdminLogins
                .FirstOrDefaultAsync(a =>
                    a.IsActive &&
                    (a.Username.ToLower() == normalizedUser ||
                     (a.Email != null && a.Email.ToLower() == normalizedUser)));

            if (admin == null)
            {
                return null;
            }

            var passwordValid = IsPasswordValid(admin, pass);
            if (!passwordValid)
            {
                return null;
            }

            if (!IsBcryptHash(admin.PasswordHash))
            {
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(pass);
            }

            admin.LastLoginAt = DateTime.UtcNow;
            admin.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var expiresAt = DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["Jwt:ExpiryInHours"] ?? "24"));

            var permissions = await _dashboardPermissionService.GetUserPermissionsAsync(admin.Id) ?? new DashboardUserPermissionDto();

            return new DashboardAdminLoginResponseDto
            {
                Token = GenerateJwtToken(admin, expiresAt),
                AdminId = admin.Id,
                Username = admin.Username,
                FullName = admin.FullName,
                Avatar = admin.AvatarBase64,
                ProfileImage = admin.AvatarBase64,
                ExpiresAt = expiresAt,
                IsSuperAdmin = PermissionEvaluationHelpers.IsSuperAdmin(admin),
                Permissions = DashboardPermissionService.BuildPermissionKeys(permissions),
                ModulePermissions = permissions.ModulePermissions,
                SubPermissions = permissions.SubPermissions
            };
        }

        private string GenerateJwtToken(SupplierDashboardAdminLogin admin, DateTime expiresAt)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "default-secret-key-for-development-only-32chars!");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Role, "DashboardAdmin"),
                new Claim("DashboardAdminId", admin.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiresAt,
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static bool IsPasswordValid(SupplierDashboardAdminLogin admin, string password)
        {
            return IsBcryptHash(admin.PasswordHash)
                ? BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash)
                : admin.PasswordHash == password;
        }

        private static bool IsBcryptHash(string passwordHash)
        {
            return passwordHash.StartsWith("$2a$") ||
                   passwordHash.StartsWith("$2b$") ||
                   passwordHash.StartsWith("$2y$");
        }

    }
}
