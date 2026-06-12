using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_Disbursement_Records")]
    public class DisbursementRecord : BaseEntity
    {
        [Required]
        [MaxLength(20)]
        public string IssuedType { get; set; } = string.Empty;

        [Required]
        public int RequestId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RequestNo { get; set; } = string.Empty;

        [Required]
        public int RegNo { get; set; }

        [MaxLength(200)]
        public string SupplierName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Route { get; set; } = string.Empty;

        [MaxLength(200)]
        public string IssuedDetails { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Quantity { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        [MaxLength(100)]
        public string? ItemName { get; set; }

        public DateTime RequestDate { get; set; }

        public DateTime? ApprovedDate { get; set; }

        public DateTime IssueDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Method { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CurrentStatus { get; set; } = "awaiting";

        [MaxLength(100)]
        public string? IssuedBy { get; set; }

        public DateTime? CompletedDate { get; set; }

        [MaxLength(100)]
        public string? CompletedBy { get; set; }

        [MaxLength(200)]
        public string? CompletedDevice { get; set; }
    }
}
