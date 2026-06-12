namespace TeaFactorySupplierManagement.Application.DTOs.Requests
{
    public class ItemRequestDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public string Month { get; set; } = string.Empty;
        public DateTime RequestMonth { get; set; }
        public DateTime RequestDate { get; set; }
        public string RequestTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? IssuedAt { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Date { get; set; }
    }

    public class CreateItemRequestDto
    {
        public string ItemType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Month { get; set; }
    }

    public class UpdateItemRequestDto
    {
        public string? Status { get; set; }
        public string? ApprovedBy { get; set; }
        public string? Remarks { get; set; }
    }
}
