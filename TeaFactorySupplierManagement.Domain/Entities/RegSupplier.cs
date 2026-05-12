using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeaFactorySupplierManagement.Domain.Entities
{
    [Table("Reg_Supplier")]
    public partial class RegSupplier
    {
        [Key]
        [Column("RegNo")]
        public int RegNo { get; set; }

        [Column("DealerNo")]
        [MaxLength(50)]
        public string? DealerNo { get; set; }

        [Column("RegName")]
        [MaxLength(200)]
        public string? RegName { get; set; }

        [Column("SuppType")]
        [MaxLength(20)]
        public string? SuppType { get; set; }

        [Column("RegDate")]
        public DateTime? RegDate { get; set; }

        [Column("Route")]
        [MaxLength(200)]
        public string? Route { get; set; }

        [Column("Address")]
        [MaxLength(500)]
        public string? Address { get; set; }

        [Column("IDNo")]
        [MaxLength(20)]
        public string? IDNo { get; set; }

        [Column("TelNo")]
        [MaxLength(15)]
        public string? TelNo { get; set; }

        [Column("TelNo_2")]
        [MaxLength(15)]
        public string? TelNo_2 { get; set; }

        [Column("TelNo_3")]
        [MaxLength(15)]
        public string? TelNo_3 { get; set; }

        [Column("Active")]
        public bool? Active { get; set; }

        [Column("Comment")]
        [MaxLength(500)]
        public string? Comment { get; set; }

        [Column("PayMode")]
        [MaxLength(50)]
        public string? PayMode { get; set; }

        [Column("PayMode_Temp")]
        [MaxLength(50)]
        public string? PayMode_Temp { get; set; }

        [Column("Bank")]
        [MaxLength(100)]
        public string? Bank { get; set; }

        [Column("AccCode")]
        [MaxLength(50)]
        public string? AccCode { get; set; }

        [Column("AccName")]
        [MaxLength(200)]
        public string? AccName { get; set; }

        [Column("CustBank")]
        [MaxLength(100)]
        public string? CustBank { get; set; }

        [Column("CustAccCode")]
        [MaxLength(50)]
        public string? CustAccCode { get; set; }

        [Column("CustAccName")]
        [MaxLength(200)]
        public string? CustAccName { get; set; }

        [Column("BankCode")]
        [MaxLength(50)]
        public string? BankCode { get; set; }

        [Column("BankName")]
        [MaxLength(100)]
        public string? BankName { get; set; }

        [Column("BranchCode")]
        [MaxLength(50)]
        public string? BranchCode { get; set; }

        [Column("BankStatus")]
        [MaxLength(20)]
        public string? BankStatus { get; set; }

        [Column("WelfareCont")]
        public bool? WelfareCont { get; set; }

        [Column("Regd_User")]
        [MaxLength(50)]
        public string? Regd_User { get; set; }

        [Column("Regd_Date")]
        public DateTime? Regd_Date { get; set; }

        [Column("Edit_User")]
        [MaxLength(50)]
        public string? Edit_User { get; set; }

        [Column("Edit_Date")]
        public DateTime? Edit_Date { get; set; }

        // Navigation properties
        public virtual ICollection<SupplierLogin> Logins { get; set; } = new List<SupplierLogin>();
        public virtual ICollection<UserSettings> Settings { get; set; } = new List<UserSettings>();
        public virtual ICollection<CashRequest> CashRequests { get; set; } = new List<CashRequest>();
        public virtual ICollection<FertilizerRequest> FertilizerRequests { get; set; } = new List<FertilizerRequest>();
        public virtual ICollection<ItemRequest> ItemRequests { get; set; } = new List<ItemRequest>();
        public virtual ICollection<TrLeafCollectionTemp> LeafCollections { get; set; } = new List<TrLeafCollectionTemp>();
        public virtual ICollection<NewsDismissed> NewsDismissed { get; set; } = new List<NewsDismissed>();
        public virtual ICollection<NotificationRead> NotificationReads { get; set; } = new List<NotificationRead>();
        public virtual ICollection<NotificationDismissed> NotificationDismissed { get; set; } = new List<NotificationDismissed>();
    }
}





