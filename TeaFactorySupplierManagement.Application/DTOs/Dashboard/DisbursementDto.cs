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
        public int? DeliveryNoteId { get; set; }
        public string? DeliveryNoteNo { get; set; }
        public string? DispatchedAt { get; set; }
    }

    public class MarkDisbursementReceivedDto
    {
        public string? CompletedBy { get; set; }
        public string? CompletedDevice { get; set; }
    }

    public class GenerateDeliveryNoteDto
    {
        public List<DeliveryNoteSelectionDto> Records { get; set; } = new();
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerRole { get; set; } = string.Empty;
        public string? VehicleNo { get; set; }
        public string? RouteName { get; set; }
        public string? Remarks { get; set; }
    }

    public class DeliveryNoteSelectionDto
    {
        public string IssuedType { get; set; } = string.Empty;
        public int RequestId { get; set; }
        public string? Method { get; set; }
    }

    public class DeliveryNoteDto
    {
        public int Id { get; set; }
        public string DeliveryNoteNo { get; set; } = string.Empty;
        public string DispatchDate { get; set; } = string.Empty;
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerRole { get; set; } = string.Empty;
        public string? VehicleNo { get; set; }
        public string? RouteName { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? CreatedBy { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? CompletedAt { get; set; }
        public string? Remarks { get; set; }
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalQuantity { get; set; }
        public string PrintUrl { get; set; } = string.Empty;
        public List<DeliveryNoteDetailDto> Details { get; set; } = new();
    }

    public class DeliveryNoteDetailDto
    {
        public int Id { get; set; }
        public int SupplierRegNo { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public int DisbursementRecordId { get; set; }
        public int RequestId { get; set; }
        public string ItemType { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public string? PaymentType { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool SupplierSignatureReceived { get; set; }
        public string? Remarks { get; set; }
    }

    public class DeliveryNoteTrackingDto
    {
        public int Id { get; set; }
        public string DeliveryNoteNo { get; set; } = string.Empty;
        public string DispatchDate { get; set; } = string.Empty;
        public string BorrowerName { get; set; } = string.Empty;
        public string BorrowerRole { get; set; } = string.Empty;
        public string? VehicleNo { get; set; }
        public string? RouteName { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public string PrintUrl { get; set; } = string.Empty;
    }

    public class MarkDeliveryNoteStatusDto
    {
        public string? Remarks { get; set; }
    }
}
