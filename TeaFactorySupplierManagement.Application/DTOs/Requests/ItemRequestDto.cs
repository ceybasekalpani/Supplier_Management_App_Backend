namespace TeaFactorySupplierManagement.Application.DTOs.Requests
{
    public class ItemRequestDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Month { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string RequestTime { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? Date { get; set; }
    }

    public class CreateItemRequestDto
    {
        public string ItemType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Month { get; set; } = string.Empty;
    }
}