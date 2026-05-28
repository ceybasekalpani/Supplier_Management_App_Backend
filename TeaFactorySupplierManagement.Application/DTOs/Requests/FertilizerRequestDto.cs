namespace TeaFactorySupplierManagement.Application.DTOs.Requests
{
    public class FertilizerRequestDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public string FertilizerType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "kg"; 
        public string Month { get; set; } = string.Empty;
        public DateTime RequestMonth { get; set; }
        public DateTime RequestDate { get; set; }
        public string RequestTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? DispatchedAt { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Date { get; set; }
    }

    public class CreateFertilizerRequestDto
    {
        public string FertilizerType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? Month { get; set; }
        public string Unit { get; set; } = "kg";
    }

    public class UpdateFertilizerRequestDto
    {
        public string? Status { get; set; }
        public string? ApprovedBy { get; set; }
        public string? Remarks { get; set; }
    }
}