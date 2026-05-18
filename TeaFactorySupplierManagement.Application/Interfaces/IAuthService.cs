using TeaFactorySupplierManagement.Application.DTOs.Auth;
using TeaFactorySupplierManagement.Domain.Entities;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(string username, string password);
        Task<RegSupplier?> GetSupplierByRegNoAsync(string regNo);
        Task<List<RegistrationDto>> GetSupplierRegistrationsAsync(string supplierId);
        Task<bool> SelectRegistrationAsync(string regNo);
        string GenerateJwtToken(SupplierLogin login);
        Task<bool> ChangePasswordAsync(int regNo, string currentPassword, string newPassword);
        Task<bool> CreateSupplierLoginAsync(int regNo, string initialPassword);
        Task<bool> SetupPinAsync(int regNo, string pin);
        Task<LoginResponseDto?> PinLoginAsync(string regNo, string pin);
    }
}