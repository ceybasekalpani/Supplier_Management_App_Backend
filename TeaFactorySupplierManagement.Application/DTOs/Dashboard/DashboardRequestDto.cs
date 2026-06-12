namespace TeaFactorySupplierManagement.Application.DTOs.Dashboard
{
    public class DashboardRequestsResponseDto
    {
        public List<DashboardRequestRowDto> Advance { get; set; } = new();
        public List<DashboardRequestRowDto> Fertilizer { get; set; } = new();
        public List<DashboardRequestRowDto> Items { get; set; } = new();
    }

    public class DashboardRequestRowDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public string RegNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public decimal? Qty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CheckedBy { get; set; } = "-";
        public string Remarks { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class DashboardUpdateRequestStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }
}
