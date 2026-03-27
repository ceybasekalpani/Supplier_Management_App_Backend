using TeaFactorySupplierManagement.Application.DTOs.Leaf;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface ILeafService
    {
        Task<MonthlyLeafSummaryDto> GetMonthlyLeafCollectionAsync(string regNo, int year, int month);
        Task<List<SixMonthHistoryDto>> GetSixMonthHistoryAsync(string regNo);
        Task<TodayLeafDto> GetTodayLeafCollectionAsync(string regNo);
        Task<decimal> GetCurrentMonthTotalAsync(string regNo);
        Task<Dictionary<int, LeafCollectionDto>> GetLeafDataByMonthAsync(string regNo, int year, int month);
    }
}