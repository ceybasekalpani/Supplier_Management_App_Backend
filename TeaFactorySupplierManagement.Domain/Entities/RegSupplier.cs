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
        [MaxLength(50)]
        public string RegNo { get; set; } = string.Empty;

        [Column("RegName")]
        [MaxLength(200)]
        public string? RegName { get; set; }

        [Column("Route")]
        [MaxLength(200)]
        public string? Route { get; set; }

        [Column("Address")]
        [MaxLength(500)]
        public string? Address { get; set; }

        [Column("TelNo")]
        [MaxLength(15)]
        public string? TelNo { get; set; }

        [Column("IDNo")]
        [MaxLength(20)]
        public string? IDNo { get; set; }

        [Column("BankName")]
        [MaxLength(100)]
        public string? BankName { get; set; }

        [Column("AccCode")]
        [MaxLength(50)]
        public string? AccCode { get; set; }

        [Column("AccName")]
        [MaxLength(200)]
        public string? AccName { get; set; }

        [Column("Active")]
        public bool? Active { get; set; }

        // Navigation Properties
        public virtual ICollection<SupplierLogin> Logins { get; set; } = new List<SupplierLogin>();
        public virtual ICollection<UserSettings> Settings { get; set; } = new List<UserSettings>();
        public virtual ICollection<CashRequest> CashRequests { get; set; } = new List<CashRequest>();
        public virtual ICollection<FertilizerRequest> FertilizerRequests { get; set; } = new List<FertilizerRequest>();
        public virtual ICollection<ItemRequest> ItemRequests { get; set; } = new List<ItemRequest>();
        public virtual ICollection<TrLeafCollectionTemp> LeafCollections { get; set; } = new List<TrLeafCollectionTemp>();
    }
}