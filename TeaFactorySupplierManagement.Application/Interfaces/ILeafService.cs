using TeaFactorySupplierManagement.Application.DTOs.Leaf;

namespace TeaFactorySupplierManagement.Application.Interfaces
{
    public interface ILeafService
{
    Task<MonthlyLeafSummaryDto> GetMonthlyLeafCollectionAsync(int regNo, int year, int month);
    Task<List<SixMonthHistoryDto>> GetSixMonthHistoryAsync(int regNo);
    Task<TodayLeafDto> GetTodayLeafCollectionAsync(int regNo);
    Task<decimal> GetCurrentMonthTotalAsync(int regNo);
    Task<Dictionary<int, LeafCollectionDto>> GetLeafDataByMonthAsync(int regNo, int year, int month);
}
}