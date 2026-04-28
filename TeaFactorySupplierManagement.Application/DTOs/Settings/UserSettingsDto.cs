namespace TeaFactorySupplierManagement.Application.DTOs.Settings
{
    public class UserSettingsDto
    {
        public string? Theme { get; set; }
        public string? Language { get; set; }
        public int? FontSize { get; set; }
        public string? ProfileImage { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolder { get; set; }
        public string? Branch { get; set; }
    }

    public class UpdateSettingsDto
    {
        public string? Theme { get; set; }
        public string? Language { get; set; }
        public int? FontSize { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountHolder { get; set; }
        public string? Branch { get; set; }
    }

    public class UpdateProfileImageDto
    {
        public string ProfileImage { get; set; } = string.Empty;
    }

    public class UpdateProfileImageBase64Dto
    {
        public string ImageBase64 { get; set; } = string.Empty;
        public string? MimeType { get; set; }
        public string? FileName { get; set; }
    }
}