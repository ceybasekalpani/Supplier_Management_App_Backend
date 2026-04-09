using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_Cash_Requests")]
    public class CashRequest : BaseEntity
    {
        [Required]
        [MaxLength(50)]
        public string RequestNo { get; set; } = string.Empty;

        [Required]
        public int RegNo { get; set; }

   
        [MaxLength(20)]
        public required string RequestType { get; set; } 

        [Required]
        [MaxLength(20)]
        public string Month { get; set; } = string.Empty;

        public DateTime RequestMonth { get; set; }

        public DateTime RequestDate { get; set; }

        public TimeSpan RequestTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        [MaxLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        [ForeignKey("RegNo")]
        public virtual RegSupplier? Supplier { get; set; }

        
    }
}