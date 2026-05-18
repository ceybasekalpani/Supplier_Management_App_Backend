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
            var user = username.Trim();
            var pass = password.Trim();
            int.TryParse(user, out int regNoInt);

            // ── RegNo shortcut ────────────────────────────────────────────────────────
            // When username == password == a valid RegNo integer, we always allow login.
            // Search for ANY login row for that RegNo (active or inactive), reset its
            // password hash, and reactivate it.  This is the first-login and
            // self-recovery path — no admin setup needed.
            if (regNoInt > 0 && pass == regNoInt.ToString())
            {
                // Find any existing row, regardless of IsActive
                var existingLogin = await _context.SupplierLogins
                    .Include(l => l.Supplier)
                    .FirstOrDefaultAsync(l => l.RegNo == regNoInt);

                if (existingLogin != null)
                {
                    // Row exists — reset password and activate
                    existingLogin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(pass);
                    existingLogin.UpdatedAt    = DateTime.Now;
                    existingLogin.IsActive     = true;
                    await _context.SaveChangesAsync();
                    return await BuildResponse(existingLogin);
                }

                // No row yet — look up Reg_Supplier (needed for the FK constraint)
                var supplier = await _context.RegSuppliers
                    .FirstOrDefaultAsync(r => r.RegNo == regNoInt);

                if (supplier == null)
                    return null; // RegNo not in Reg_Supplier — deny

                var newLogin = new SupplierLogin
                {
                    RegNo        = regNoInt,
                    Username     = regNoInt.ToString(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(pass),
                    IsActive     = true,
                    Supplier     = supplier,
                };
                _context.SupplierLogins.Add(newLogin);
                await _context.SaveChangesAsync();
                return await BuildResponse(newLogin);
            }

            // ── Normal password login ─────────────────────────────────────────────────
            SupplierLogin? login = null;

            if (regNoInt > 0)
                login = await _context.SupplierLogins
                    .Include(l => l.Supplier)
                    .FirstOrDefaultAsync(l => l.RegNo == regNoInt && l.IsActive);

            if (login == null)
                login = await _context.SupplierLogins
                    .Include(l => l.Supplier)
                    .FirstOrDefaultAsync(l => l.Username == user && l.IsActive);

            if (login == null)
                return null;

            // Verify stored password (handles both legacy plain-text and BCrypt)
            bool passwordValid;
            bool isPlainText = !login.PasswordHash.StartsWith("$2a$") &&
                               !login.PasswordHash.StartsWith("$2b$") &&
                               !login.PasswordHash.StartsWith("$2y$");

            if (isPlainText)
            {
                passwordValid = login.PasswordHash == pass;
                if (passwordValid)
                {
                    login.PasswordHash = BCrypt.Net.BCrypt.HashPassword(pass);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                passwordValid = BCrypt.Net.BCrypt.Verify(pass, login.PasswordHash);
            }

            if (!passwordValid)
                return null;

            return await BuildResponse(login);
        }

        private async Task<LoginResponseDto> BuildResponse(SupplierLogin login)
        {
            var registrations = await _context.RegSuppliers
                .Where(r => r.RegNo == login.RegNo && (r.Active == true || r.Active == null))
                .Select(r => new RegistrationDto
                {
                    RegNo         = r.RegNo.ToString(),
                    Route         = r.Route ?? string.Empty,
                    BankName      = r.BankName,
                    AccountNumber = r.AccCode
                })
                .ToListAsync();

            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == login.RegNo);

            var token = GenerateJwtToken(login);

            return new LoginResponseDto
            {
                Token         = token,
                RegNo         = login.RegNo.ToString(),
                Username      = login.Username,
                Name          = login.Supplier?.RegName ?? login.Username,
                Registrations = registrations,
                Settings      = settings != null ? _mapper.Map<UserSettingsDto>(settings) : null,
                HasPinSet     = !string.IsNullOrEmpty(login.PinHash)
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

        public async Task<bool> ChangePasswordAsync(int regNo, string currentPassword, string newPassword)
        {
            var login = await _context.SupplierLogins
                .FirstOrDefaultAsync(l => l.RegNo == regNo && l.IsActive);

            if (login == null)
                return false;

            bool isPlainText = !login.PasswordHash.StartsWith("$2a$") &&
                               !login.PasswordHash.StartsWith("$2b$") &&
                               !login.PasswordHash.StartsWith("$2y$");

            bool currentValid = isPlainText
                ? login.PasswordHash == currentPassword
                : BCrypt.Net.BCrypt.Verify(currentPassword, login.PasswordHash);

            if (!currentValid)
                return false;

            login.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            login.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CreateSupplierLoginAsync(int regNo, string initialPassword)
        {
            // Verify the supplier exists in Reg_Supplier
            var supplier = await _context.RegSuppliers
                .FirstOrDefaultAsync(r => r.RegNo == regNo);

            if (supplier == null)
                return false;

            // Remove any existing login for this RegNo first
            var existing = await _context.SupplierLogins
                .FirstOrDefaultAsync(l => l.RegNo == regNo);

            if (existing != null)
                _context.SupplierLogins.Remove(existing);

            var login = new SupplierLogin
            {
                RegNo = regNo,
                Username = regNo.ToString(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(initialPassword),
                IsActive = true
            };

            _context.SupplierLogins.Add(login);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetupPinAsync(int regNo, string pin)
        {
            var login = await _context.SupplierLogins
                .FirstOrDefaultAsync(l => l.RegNo == regNo && l.IsActive);

            if (login == null) return false;

            login.PinHash  = BCrypt.Net.BCrypt.HashPassword(pin);
            login.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<LoginResponseDto?> PinLoginAsync(string regNo, string pin)
        {
            if (!int.TryParse(regNo, out int regNoInt)) return null;

            var login = await _context.SupplierLogins
                .Include(l => l.Supplier)
                .FirstOrDefaultAsync(l => l.RegNo == regNoInt && l.IsActive);

            if (login == null || string.IsNullOrEmpty(login.PinHash)) return null;

            if (!BCrypt.Net.BCrypt.Verify(pin, login.PinHash)) return null;

            return await BuildResponse(login);
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