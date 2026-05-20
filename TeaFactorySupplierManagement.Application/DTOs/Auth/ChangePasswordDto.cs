using System.ComponentModel.DataAnnotations;

namespace TeaFactorySupplierManagement.Application.DTOs.Auth
{
    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class CreateSupplierLoginDto
    {
        [Required]
        public int RegNo { get; set; }

        [Required]
        [MinLength(6)]
        public string InitialPassword { get; set; } = string.Empty;
    }

    // New DTO for changing the PIN
    public class ChangePinDto
    {
        [Required]
        public string CurrentPin { get; set; } = string.Empty;

        [Required]
        [MinLength(4)]
        public string NewPin { get; set; } = string.Empty;
    }
}