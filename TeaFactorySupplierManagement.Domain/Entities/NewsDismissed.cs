using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_News_Dismissed")]
    public class NewsDismissed
    {
        public int NewsId { get; set; }

        [MaxLength(50)]
        public string RegNo { get; set; } = string.Empty;

        public DateTime DismissedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("NewsId")]
        public virtual News? News { get; set; }

        [ForeignKey("RegNo")]
        public virtual RegSupplier? Supplier { get; set; }
    }
}