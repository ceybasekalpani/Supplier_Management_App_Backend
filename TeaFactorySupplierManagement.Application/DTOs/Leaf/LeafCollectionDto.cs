namespace TeaFactorySupplierManagement.Application.DTOs.Leaf
{
    public class LeafCollectionDto
    {
        public int Day { get; set; }
        public decimal Gross { get; set; }
        public int Bags { get; set; }
        public decimal Water { get; set; }
        public decimal NetWeight { get; set; }
        public decimal? SuperNetWeight { get; set; }
    }

    public class MonthlyLeafSummaryDto
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalNet { get; set; }
        public decimal TotalSuperNet { get; set; }
        public int TotalDays { get; set; }
        public List<LeafCollectionDto> Collections { get; set; } = new List<LeafCollectionDto>();
        public bool HasSuper { get; set; }
    }

    public class SixMonthHistoryDto
    {
        public string MonthKey { get; set; } = string.Empty;
        public string MonthLabel { get; set; } = string.Empty;
        public decimal TotalGross { get; set; }
        public decimal TotalNet { get; set; }
        public decimal TotalSuperNet { get; set; }
        public int Days { get; set; }
    }

    public class YearlyLeafSummaryDto
    {
        public int Year { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalNet { get; set; }
        public decimal TotalSuperNet { get; set; }
        public int TotalDays { get; set; }
        public List<MonthlyLeafSummaryDto> MonthlySummaries { get; set; } = new List<MonthlyLeafSummaryDto>();
        public bool HasSuper { get; set; }
    }
    
    public class TodayLeafDto
    {
        public decimal NormalNet { get; set; }
        public decimal SuperNet { get; set; }
        public decimal TotalNet => NormalNet + SuperNet;
        public bool HasSuper { get; set; }
        public int BagCount { get; set; }
    }
}