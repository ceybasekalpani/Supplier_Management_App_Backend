namespace TeaFactorySupplierManagement.Application.DTOs.MobileDeviceTokens
{
    public class SaveMobileDeviceTokenDto
    {
        public int? SupplierId { get; set; }
        public int? RegNo { get; set; }
        public string FcmToken { get; set; } = string.Empty;
        public string? Platform { get; set; }
        public string? DeviceId { get; set; }
    }
}
