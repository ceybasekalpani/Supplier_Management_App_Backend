using TeaFactorySupplierManagement.Application.DTOs.MobileDeviceTokens;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IMobileDeviceTokenService
    {
        Task SaveTokenAsync(int regNo, SaveMobileDeviceTokenDto request);
        Task DeactivateTokenAsync(int regNo, string fcmToken);
    }
}