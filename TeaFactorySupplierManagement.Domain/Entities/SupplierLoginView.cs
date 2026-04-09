namespace TeaFactorySupplierManagement.Domain.Entities
{
    public class SupplierLoginView
    {
        public int LoginId { get; set; }
        public int RegNo { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsLoginActive { get; set; }
        public string? RegName { get; set; }
        public string? Route { get; set; }
        public string? TelNo { get; set; }
        public string? Address { get; set; }
        public string? BankName { get; set; }
        public string? AccCode { get; set; }
        public bool? IsSupplierActive { get; set; }
        public DateTime LoginCreatedAt { get; set; }
    }
}
