namespace TeaFactorySupplierManagement.Application.DTOs.Factory
{
    public class FactorySetupDto
    {
        public int Id { get; set; }
        public string FactoryName { get; set; } = string.Empty;
        public bool IsSuper { get; set; }
        public bool IsCashRequest { get; set; }
        public bool IsItemRequest { get; set; }
        public bool IsFertilizerRequest { get; set; }
    }

    public class FactoryFeaturesDto
    {
        public bool IsSuper { get; set; }
        public bool IsCashRequestEnabled { get; set; }
        public bool IsItemRequestEnabled { get; set; }
        public bool IsFertilizerRequestEnabled { get; set; }
    }
}