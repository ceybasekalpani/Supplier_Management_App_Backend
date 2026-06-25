namespace TeaFactorySupplierManagement.Application.DTOs.Requests
{
    public class CashRequestDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public string RequestType { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string SalaryDate { get; set; } = string.Empty;
        public DateTime RequestMonth { get; set; }
        public DateTime RequestDate { get; set; }
        public string RequestTime { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? RequestedDate { get; set; }
    }

    public class CreateCashRequestDto
    {
        public string RequestType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Month { get; set; }
        public string? SalaryDate { get; set; }
    }

    public class UpdateCashRequestDto
    {
        public string? Status { get; set; }
        public string? ApprovedBy { get; set; }
        public string? Remarks { get; set; }
    }
}
