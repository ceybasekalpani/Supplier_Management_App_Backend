using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Supplier_Management_Notification_Dismissed")]
    public class NotificationDismissed
    {
        public int NotificationId { get; set; }
        public int RegNo { get; set; }
        public DateTime DismissedAt { get; set; }

        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        [ForeignKey("NotificationId")]
        public virtual Notification? Notification { get; set; }

        [ForeignKey("RegNo")]
        public virtual RegSupplier? Supplier { get; set; }
    }
}
