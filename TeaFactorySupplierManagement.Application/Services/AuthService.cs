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
            // Find the login record
            var login = await _context.SupplierLogins
                .Include(l => l.Supplier)
                .FirstOrDefaultAsync(l => l.Username == username && l.IsActive);

            if (login == null)
                return null;

            // Verify password using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(password, login.PasswordHash))
                return null;

            // Get all registrations for this supplier
            var registrations = await _context.RegSuppliers
                .Where(r => r.RegNo == login.RegNo && (r.Active == true || r.Active == null))
                .Select(r => new RegistrationDto
                {
                    RegNo = r.RegNo,
                    Route = r.Route ?? string.Empty,
                    BankName = r.BankName,
                    AccountNumber = r.AccCode
                })
                .ToListAsync();

            // Get user settings
            var settings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.RegNo == login.RegNo);

            // Generate JWT token
            var token = await GenerateJwtTokenAsync(login);

            return new LoginResponseDto
            {
                Token = token,
                RegNo = login.RegNo,
                Username = login.Username,
                Name = login.Supplier?.RegName ?? login.Username,
                Registrations = registrations,
                Settings = settings != null ? _mapper.Map<UserSettingsDto>(settings) : null
            };
        }

        public async Task<RegSupplier?> GetSupplierByRegNoAsync(string regNo)
        {
            return await _context.RegSuppliers
                .FirstOrDefaultAsync(r => r.RegNo == regNo);
        }

        public async Task<List<RegistrationDto>> GetSupplierRegistrationsAsync(string supplierId)
        {
            var registrations = await _context.RegSuppliers
                .Where(r => r.RegNo == supplierId && (r.Active == true || r.Active == null))
                .Select(r => new RegistrationDto
                {
                    RegNo = r.RegNo,
                    Route = r.Route ?? string.Empty,
                    BankName = r.BankName,
                    AccountNumber = r.AccCode
                })
                .ToListAsync();

            return registrations;
        }

        public async Task<bool> SelectRegistrationAsync(string regNo)
        {
            var registration = await _context.RegSuppliers
                .FirstOrDefaultAsync(r => r.RegNo == regNo);

            return registration != null;
        }

        public async Task<string> GenerateJwtTokenAsync(SupplierLogin login)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "default-secret-key-for-development-only-32chars!");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, login.Id.ToString()),
                new Claim(ClaimTypes.Name, login.Username),
                new Claim("RegNo", login.RegNo),
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