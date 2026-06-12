namespace TeaFactorySupplierManagement.Application.DTOs.Dashboard
{
    public class DashboardMetricsDto
    {
        public List<DashboardStatDto> Stats { get; set; } = new();
        public List<MonthlyRequestVolumeDto> MonthlyRequestVolume { get; set; } = new();
        public List<RequestQueueDto> RequestQueue { get; set; } = new();
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }

    public class DashboardStatDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Tone { get; set; } = string.Empty;
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Total { get; set; }
    }

    public class MonthlyRequestVolumeDto
    {
        public string Month { get; set; } = string.Empty;
        public int Advance { get; set; }
        public int Fertilizer { get; set; }
        public int Items { get; set; }
        public int Total { get; set; }
    }

    public class RequestQueueDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Total { get; set; }
        public string Path { get; set; } = string.Empty;
    }

    public class RecentActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string RequestNo { get; set; } = string.Empty;
        public string RegNo { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CheckedBy { get; set; } = "-";
        public string Date { get; set; } = string.Empty;
        public DateTime ActivityAt { get; set; }
    }
}
