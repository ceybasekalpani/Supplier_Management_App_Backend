namespace TeaFactorySupplierManagement.Application.DTOs.Requests
{
    public class FertilizerRequestDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public string FertilizerType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Month { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string RequestTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Date { get; set; }
    }

    public class CreateFertilizerRequestDto
    {
        public string FertilizerType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Month { get; set; } = string.Empty;
    }
}