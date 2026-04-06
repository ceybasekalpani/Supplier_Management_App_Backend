using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_Notification_Read")]
    public class NotificationRead
    {
        // Remove any [Key] attributes - configured in Fluent API
        public int NotificationId { get; set; }
        public int RegNo { get; set; }
        public DateTime ReadAt { get; set; }

        // Navigation properties
        [ForeignKey("NotificationId")]
        public virtual Notification? Notification { get; set; }

        [ForeignKey("RegNo")]
        public virtual RegSupplier? Supplier { get; set; }
    }
}