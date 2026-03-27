using TeaFactorySupplierManagement.Application.DTOs.Factory;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IFactorySetupService
    {
        Task<FactorySetupDto?> GetFactorySetupAsync();
        Task<FactoryFeaturesDto> GetFactoryFeaturesAsync();
        Task<bool> IsSuperLeafEnabledAsync();
        Task<bool> IsCashRequestEnabledAsync();
        Task<bool> IsItemRequestEnabledAsync();
        Task<bool> IsFertilizerRequestEnabledAsync();
    }
}