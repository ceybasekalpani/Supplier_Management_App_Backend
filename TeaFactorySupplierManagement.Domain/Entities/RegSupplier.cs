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

        [Column("Land_Acre")]
        public int? Land_Acre { get; set; }

        [Column("Land_Rood")]
        public int? Land_Rood { get; set; }

        [Column("Land_Perch")]
        public int? Land_Perch { get; set; }

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

        [Column("NoTransport")]
        public bool? NoTransport { get; set; }

        [Column("SpTransRate")]
        public decimal? SpTransRate { get; set; }

        [Column("SpecialRate")]
        public bool? SpecialRate { get; set; }

        [Column("PayRate")]
        public decimal? PayRate { get; set; }

        [Column("PayRate_Super")]
        public decimal? PayRate_Super { get; set; }

        [Column("LeafTot")]
        public int? LeafTot { get; set; }

        [Column("LeafTot_Super")]
        public int? LeafTot_Super { get; set; }

        [Column("LeafAmount")]
        public decimal? LeafAmount { get; set; }

        [Column("TransIncentive")]
        public decimal? TransIncentive { get; set; }

        [Column("OtherPay")]
        public decimal? OtherPay { get; set; }

        [Column("Subsidy")]
        public decimal? Subsidy { get; set; }

        [Column("PrvDebt")]
        public decimal? PrvDebt { get; set; }

        [Column("LoanInstalment")]
        public decimal? LoanInstalment { get; set; }

        [Column("LoanInt")]
        public decimal? LoanInt { get; set; }

        [Column("LoanBalance")]
        public decimal? LoanBalance { get; set; }

        [Column("AdvanceAmt")]
        public decimal? AdvanceAmt { get; set; }

        [Column("AdvanceInt")]
        public decimal? AdvanceInt { get; set; }

        [Column("Manure_Prv")]
        public decimal? Manure_Prv { get; set; }

        [Column("Manure")]
        public decimal? Manure { get; set; }

        [Column("Manure3rd")]
        public decimal? Manure3rd { get; set; }

        [Column("Manure_Next")]
        public decimal? Manure_Next { get; set; }

        [Column("Chemical")]
        public decimal? Chemical { get; set; }

        [Column("Tea")]
        public decimal? Tea { get; set; }

        [Column("LeafBag")]
        public decimal? LeafBag { get; set; }

        [Column("DryFoods")]
        public decimal? DryFoods { get; set; }

        [Column("Stationaries")]
        public decimal? Stationaries { get; set; }

        [Column("Transport")]
        public decimal? Transport { get; set; }

        [Column("Stamp")]
        public decimal? Stamp { get; set; }

        [Column("Welfare")]
        public decimal? Welfare { get; set; }

        [Column("SavingRate")]
        public decimal? SavingRate { get; set; }

        [Column("SavingAmount")]
        public decimal? SavingAmount { get; set; }

        [Column("SavingTodate")]
        public decimal? SavingTodate { get; set; }

        [Column("OtherDeduct")]
        public decimal? OtherDeduct { get; set; }

        [Column("TotDeduct")]
        public decimal? TotDeduct { get; set; }

        [Column("BalancePay")]
        public decimal? BalancePay { get; set; }

        [Column("NextDebt")]
        public decimal? NextDebt { get; set; }

        [Column("Paid")]
        public bool? Paid { get; set; }

        [Column("VouNo")]
        [MaxLength(50)]
        public string? VouNo { get; set; }

        [Column("ChqNo")]
        [MaxLength(50)]
        public string? ChqNo { get; set; }

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
    }
}





