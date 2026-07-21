using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            var user = username.Trim();
            var pass = password.Trim();
            int.TryParse(user, out int regNoInt);

            SupplierLogin? login = null;

            if (regNoInt > 0)
            {
                login = await _context.SupplierLogins
                    .Include(l => l.Supplier)
                    .FirstOrDefaultAsync(l => l.RegNo == regNoInt && l.IsActive);

                if (login == null && pass == regNoInt.ToString())
                    login = await CreateDefaultLoginIfMissingAsync(regNoInt, pass);
            }

            if (login == null)
                login = await _context.SupplierLogins
                    .Include(l => l.Supplier)
                    .FirstOrDefaultAsync(l => l.Username == user && l.IsActive);

            if (login == null)
                return null;

            bool passwordValid;
            bool isPlainText = !login.PasswordHash.StartsWith("$2a$") &&
                               !login.PasswordHash.StartsWith("$2b$") &&
                               !login.PasswordHash.StartsWith("$2y$");

            if (isPlainText)
            {
                passwordValid = FixedTimeEquals(login.PasswordHash, pass);
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

        private async Task<SupplierLogin?> CreateDefaultLoginIfMissingAsync(int regNo, string password)
        {
            var supplier = await _context.RegSuppliers
                .FirstOrDefaultAsync(r => r.RegNo == regNo);

            if (supplier == null)
                return null;

            var newLogin = new SupplierLogin
            {
                RegNo = regNo,
                Username = regNo.ToString(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                IsCreatePin = false,
                Supplier = supplier,
            };

            _context.SupplierLogins.Add(newLogin);

            try
            {
                await _context.SaveChangesAsync();
                return newLogin;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _context.Entry(newLogin).State = EntityState.Detached;

                return await _context.SupplierLogins
                    .Include(l => l.Supplier)
                    .FirstOrDefaultAsync(l => l.RegNo == regNo && l.IsActive);
            }
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.GetBaseException() is SqlException sqlException &&
                   sqlException.Errors.Cast<SqlError>().Any(error => error.Number is 2601 or 2627);
        }

        // Constant-time comparison so legacy plaintext-password checks don't leak
        // timing information character-by-character, unlike a plain `==` compare.
        private static bool FixedTimeEquals(string a, string b)
        {
            var aBytes = Encoding.UTF8.GetBytes(a);
            var bBytes = Encoding.UTF8.GetBytes(b);

            if (aBytes.Length != bBytes.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
        }

        private async Task<LoginResponseDto> BuildResponse(SupplierLogin login)
        {
            var registrations = await _context.RegSuppliers
                .AsNoTracking()
                .Where(r => r.RegNo == login.RegNo && (r.Active == true || r.Active == null))
                .Select(r => new RegistrationDto
                {
                    RegNo         = r.RegNo.ToString(),
                    Route         = r.Route ?? string.Empty,
                    BankName      = r.BankName,
                    AccountNumber = r.AccCode,
                    LandAcre      = r.Land_Acre,
                    LandRood      = r.Land_Rood,
                    LandPerch     = r.Land_Perch
                })
                .ToListAsync();

            var settings = await _context.UserSettings
                .AsNoTracking()
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
                HasPinSet     = !string.IsNullOrEmpty(login.PinHash),
                IsCreatePin   = login.IsCreatePin 
            };
        }

        public async Task<RegSupplier?> GetSupplierByRegNoAsync(string regNo)
        {
            if (int.TryParse(regNo, out int regNoInt))
            {
                return await _context.RegSuppliers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.RegNo == regNoInt);
            }
            return null;
        }

        public async Task<List<RegistrationDto>> GetSupplierRegistrationsAsync(string supplierId)
        {
            if (int.TryParse(supplierId, out int regNoInt))
            {
                return await _context.RegSuppliers
                    .AsNoTracking()
                    .Where(r => r.RegNo == regNoInt && (r.Active == true || r.Active == null))
                    .Select(r => new RegistrationDto
                    {
                        RegNo = r.RegNo.ToString(),
                        Route = r.Route ?? string.Empty,
                        BankName = r.BankName,
                        AccountNumber = r.AccCode,
                        LandAcre = r.Land_Acre,
                        LandRood = r.Land_Rood,
                        LandPerch = r.Land_Perch
                    })
                    .ToListAsync();
            }
            return new List<RegistrationDto>();
        }

        public async Task<bool> SelectRegistrationAsync(int callerRegNo, string requestedRegNo)
        {
            if (!int.TryParse(requestedRegNo, out int requestedRegNoInt))
                return false;

            // Only allow selecting a registration that actually belongs to the
            // authenticated caller — prevents selecting an arbitrary supplier's RegNo.
            if (requestedRegNoInt != callerRegNo)
                return false;

            var registration = await _context.RegSuppliers
                .FirstOrDefaultAsync(r => r.RegNo == requestedRegNoInt && (r.Active == true || r.Active == null));

            return registration != null;
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
                ? FixedTimeEquals(login.PasswordHash, currentPassword)
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
            var supplier = await _context.RegSuppliers
                .FirstOrDefaultAsync(r => r.RegNo == regNo);

            if (supplier == null)
                return false;

            var existing = await _context.SupplierLogins
                .FirstOrDefaultAsync(l => l.RegNo == regNo);

            // An active login already exists for this RegNo — refuse to overwrite it.
            // Without this check, anyone who knows a RegNo could silently take over
            // an existing supplier's account via this unauthenticated endpoint.
            if (existing != null && existing.IsActive)
                return false;

            if (existing != null)
                _context.SupplierLogins.Remove(existing);

            var login = new SupplierLogin
            {
                RegNo = regNo,
                Username = regNo.ToString(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(initialPassword),
                IsActive = true,
                IsCreatePin = false
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
            login.IsCreatePin = true; 
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

        // NEW METHOD
        public async Task<bool> ChangePinAsync(int regNo, string currentPin, string newPin)
        {
            var login = await _context.SupplierLogins
                .FirstOrDefaultAsync(l => l.RegNo == regNo && l.IsActive);

            if (login == null || string.IsNullOrEmpty(login.PinHash)) return false;

            if (!BCrypt.Net.BCrypt.Verify(currentPin, login.PinHash)) return false;

            login.PinHash = BCrypt.Net.BCrypt.HashPassword(newPin);
            login.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateLandInfoAsync(int regNo, LandInfoDto request)
        {
            var supplier = await _context.RegSuppliers
                .FirstOrDefaultAsync(r => r.RegNo == regNo);

            if (supplier == null)
                return false;

            supplier.Land_Acre = request.LandAcre;
            supplier.Land_Rood = request.LandRood;
            supplier.Land_Perch = request.LandPerch;
            supplier.Edit_Date = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public string GenerateJwtToken(SupplierLogin login)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("JWT Key is not configured.");

            var key = Encoding.ASCII.GetBytes(jwtKey);

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
