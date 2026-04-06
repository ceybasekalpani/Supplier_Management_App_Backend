using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TeaFactorySupplierManagement.Application.DTOs.Auth;
using TeaFactorySupplierManagement.Application.DTOs.Settings;
using TeaFactorySupplierManagement.Application.Interfaces;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public AuthService(ApplicationDbContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto?> LoginAsync(string username, string password)
        {
            var login = await _context.SupplierLogins
                .Include(l => l.Supplier)
                .FirstOrDefaultAsync(l => l.Username == username && l.IsActive);

            if (login == null)
                return null;

            bool passwordValid;

            // Check if stored password is plain text (not a BCrypt hash).
            // All valid BCrypt hashes start with $2a$, $2b$, or $2y$.
            bool isPlainText = !login.PasswordHash.StartsWith("$2a$") &&
                               !login.PasswordHash.StartsWith("$2b$") &&
                               !login.PasswordHash.StartsWith("$2y$");

            if (isPlainText)
            {
                // Plain text comparison for legacy/seed passwords
                passwordValid = login.PasswordHash == password;

                if (passwordValid)
                {
                    // Auto-upgrade: immediately replace plain text with a BCrypt hash
                    login.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // Standard BCrypt verification for already-hashed passwords
                passwordValid = BCrypt.Net.BCrypt.Verify(password, login.PasswordHash);
            }

            if (!passwordValid)
                return null;

            // Get registrations
            var registrations = await _context.RegSuppliers
                .Where(r => r.RegNo == login.RegNo && (r.Active == true || r.Active == null))
                .Select(r => new RegistrationDto
                {
                    RegNo = r.RegNo.ToString(),
                    Route = r.Route ?? string.Empty,
                    BankName = r.BankName,
                    AccountNumber = r.AccCode
                })
                .ToListAsync();

            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == login.RegNo);

            var token = GenerateJwtToken(login);

            return new LoginResponseDto
            {
                Token = token,
                RegNo = login.RegNo.ToString(),
                Username = login.Username,
                Name = login.Supplier?.RegName ?? login.Username,
                Registrations = registrations,
                Settings = settings != null ? _mapper.Map<UserSettingsDto>(settings) : null
            };
        }

        public async Task<RegSupplier?> GetSupplierByRegNoAsync(string regNo)
        {
            if (int.TryParse(regNo, out int regNoInt))
            {
                return await _context.RegSuppliers
                    .FirstOrDefaultAsync(r => r.RegNo == regNoInt);
            }
            return null;
        }

        public async Task<List<RegistrationDto>> GetSupplierRegistrationsAsync(string supplierId)
        {
            if (int.TryParse(supplierId, out int regNoInt))
            {
                return await _context.RegSuppliers
                    .Where(r => r.RegNo == regNoInt && (r.Active == true || r.Active == null))
                    .Select(r => new RegistrationDto
                    {
                        RegNo = r.RegNo.ToString(),
                        Route = r.Route ?? string.Empty,
                        BankName = r.BankName,
                        AccountNumber = r.AccCode
                    })
                    .ToListAsync();
            }
            return new List<RegistrationDto>();
        }

        public async Task<bool> SelectRegistrationAsync(string regNo)
        {
            if (int.TryParse(regNo, out int regNoInt))
            {
                var registration = await _context.RegSuppliers
                    .FirstOrDefaultAsync(r => r.RegNo == regNoInt);
                return registration != null;
            }
            return false;
        }

        public string GenerateJwtToken(SupplierLogin login)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "default-secret-key-for-development-only-32chars!");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, login.Id.ToString()),
                new Claim(ClaimTypes.Name, login.Username),
                new Claim("RegNo", login.RegNo.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(_configuration["Jwt:ExpiryInHours"] ?? "24")),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}