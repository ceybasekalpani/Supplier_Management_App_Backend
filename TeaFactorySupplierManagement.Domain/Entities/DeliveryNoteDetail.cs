using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_DeliveryNote_Detail")]
    public class DeliveryNoteDetail : BaseEntity
    {
        public int DeliveryNoteId { get; set; }

        [Required]
        [MaxLength(30)]
        public string DeliveryNoteNo { get; set; } = string.Empty;

        public int SupplierRegNo { get; set; }

        public int DisbursementRecordId { get; set; }

        public int RequestId { get; set; }

        [Required]
        [MaxLength(30)]
        public string ItemType { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Quantity { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        [MaxLength(50)]
        public string? PaymentType { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "dispatched";

        public bool SupplierSignatureReceived { get; set; }

        [MaxLength(500)]
        public string? Remarks { get; set; }

        [ForeignKey(nameof(DeliveryNoteNo))]
        public virtual DeliveryNoteHeader? DeliveryNote { get; set; }

        [ForeignKey(nameof(SupplierRegNo))]
        public virtual RegSupplier? Supplier { get; set; }

        [ForeignKey(nameof(DisbursementRecordId))]
        public virtual DisbursementRecord? DisbursementRecord { get; set; }
    }
}
