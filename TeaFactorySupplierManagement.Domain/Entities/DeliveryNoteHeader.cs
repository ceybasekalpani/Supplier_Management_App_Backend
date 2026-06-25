using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_DeliveryNote_Header")]
    public class DeliveryNoteHeader : BaseEntity
    {
        [Required]
        [MaxLength(30)]
        public string DN_No { get; set; } = string.Empty;

        public DateTime DispatchDate { get; set; }

        [Required]
        [MaxLength(150)]
        public string BorrowerName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string BorrowerRole { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? VehicleNo { get; set; }

        [MaxLength(200)]
        public string? RouteName { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "issued";

        public int? CreatedBy { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        public virtual ICollection<DeliveryNoteDetail> Details { get; set; } = new List<DeliveryNoteDetail>();
        public virtual ICollection<DisbursementRecord> DisbursementRecords { get; set; } = new List<DisbursementRecord>();
    }
}
