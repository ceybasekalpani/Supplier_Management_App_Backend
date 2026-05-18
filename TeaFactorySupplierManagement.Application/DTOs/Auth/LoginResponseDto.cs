using TeaFactorySupplierManagement.Application.DTOs.Settings;

namespace TeaFactorySupplierManagement.Application.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RegNo { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<RegistrationDto> Registrations { get; set; } = new List<RegistrationDto>();
        public UserSettingsDto? Settings { get; set; }
        public bool HasPinSet { get; set; }
    }

    public class RegistrationDto
    {
        public string RegNo { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
    }
}