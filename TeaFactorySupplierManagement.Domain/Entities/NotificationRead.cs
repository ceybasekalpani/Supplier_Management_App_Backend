using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_Notification_Reads")]
    public class NotificationRead
    {
        public int NotificationId { get; set; }

        [MaxLength(50)]
        public string RegNo { get; set; } = string.Empty;

        public DateTime ReadAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("NotificationId")]
        public virtual Notification? Notification { get; set; }

        [ForeignKey("RegNo")]
        public virtual RegSupplier? Supplier { get; set; }
    }
}