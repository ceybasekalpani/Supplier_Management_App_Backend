namespace TeaFactorySupplierManagement.Application.DTOs.Dashboard
{
    public class SupplierDashboardResponseDto
    {
        public List<DashboardSupplierDto> Suppliers { get; set; } = new();
        public string ActiveMonth { get; set; } = string.Empty;
        public string PreviousMonth { get; set; } = string.Empty;
    }

    public class DashboardSupplierDto
    {
        public int Id { get; set; }
        public string RegNo { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Phone2 { get; set; }
        public string? Phone3 { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Bank { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string Payment { get; set; } = string.Empty;
        public string Status { get; set; } = "inactive";
        public List<DashboardCashRequestDto> AdvanceRequests { get; set; } = new();
        public List<DashboardFertilizerRequestDto> FertilizerRequests { get; set; } = new();
        public List<DashboardItemRequestDto> ItemRequests { get; set; } = new();
    }

    public class DashboardCashRequestDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public string RegNo { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Month { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CheckedBy { get; set; } = "-";
        public string? Remarks { get; set; }
    }

    public class DashboardFertilizerRequestDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public string RegNo { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CheckedBy { get; set; } = "-";
        public string? Remarks { get; set; }
    }

    public class DashboardItemRequestDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public string RegNo { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Qty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CheckedBy { get; set; } = "-";
        public string? Remarks { get; set; }
    }
}
