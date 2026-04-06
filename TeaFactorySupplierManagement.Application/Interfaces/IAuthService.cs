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
        string GenerateJwtToken(SupplierLogin login);  // Changed from GenerateJwtTokenAsync
    }
}