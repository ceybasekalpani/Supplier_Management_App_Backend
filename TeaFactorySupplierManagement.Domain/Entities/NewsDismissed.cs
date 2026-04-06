using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_News_Dismissed")]
    public class NewsDismissed
    {
        // Remove [Key] attributes - configure in Fluent API instead
        public int NewsId { get; set; }
        public int RegNo { get; set; }
        public DateTime DismissedAt { get; set; }

        // Navigation properties
        [ForeignKey("NewsId")]
        public virtual News? News { get; set; }

        [ForeignKey("RegNo")]
        public virtual RegSupplier? Supplier { get; set; }
    }
}