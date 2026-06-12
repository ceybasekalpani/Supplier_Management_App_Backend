using TeaFactorySupplierManagement.Application.DTOs.Dashboard;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface IFertilizerItemConfigurationService
    {
        Task<FertilizerItemConfigurationsDto> GetAllAsync(bool includeInactive = true);
        Task<FertilizerItemConfigurationRowDto> CreateAsync(string category, string name);
        Task<FertilizerItemConfigurationRowDto?> UpdateAsync(string category, int id, string name, bool? isActive);
        Task<FertilizerItemConfigurationRowDto?> SetActiveAsync(string category, int id, bool isActive);
    }
}
