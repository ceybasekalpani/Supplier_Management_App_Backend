namespace TeaFactorySupplierManagement.Application.DTOs.Dashboard
{
    public class DisbursementQueueDto
    {
        public List<DisbursementQueueRowDto> Advances { get; set; } = new();
        public List<DisbursementQueueRowDto> Fertilizers { get; set; } = new();
        public List<DisbursementQueueRowDto> Items { get; set; } = new();
    }

    public class DisbursementQueueRowDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public string RegNo { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string ApprovedDate { get; set; } = string.Empty;
        public decimal? ApprovedAmount { get; set; }
        public string? FertilizerType { get; set; }
        public string? ItemType { get; set; }
        public decimal? ApprovedQty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public bool Issued { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TrackingStatus { get; set; }
    }

    public class IssueDisbursementDto
    {
        public string? Method { get; set; }
    }

    public class DisbursementTrackingDto
    {
        public int Id { get; set; }
        public string IssuedType { get; set; } = string.Empty;
        public int RequestId { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public string RegNo { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public string IssuedDetails { get; set; } = string.Empty;
        public string? ItemName { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Qty { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string RequestDate { get; set; } = string.Empty;
        public string ApprovedDate { get; set; } = string.Empty;
        public string IssueDate { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public string? CompletedDate { get; set; }
        public string? CompletedBy { get; set; }
        public string? CompletedDevice { get; set; }
    }

    public class MarkDisbursementReceivedDto
    {
        public string? CompletedBy { get; set; }
        public string? CompletedDevice { get; set; }
    }
}
