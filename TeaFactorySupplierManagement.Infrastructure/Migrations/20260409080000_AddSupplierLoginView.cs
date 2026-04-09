using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TeaFactorySupplierManagement.Infrastructure.Data;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260409080000_AddSupplierLoginView")]
    public partial class AddSupplierLoginView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE VIEW vw_SupplierLoginDetails AS
                SELECT
                    sl.Id           AS LoginId,
                    sl.RegNo        AS RegNo,
                    sl.Username     AS Username,
                    sl.IsActive     AS IsLoginActive,
                    sl.CreatedAt    AS LoginCreatedAt,
                    rs.RegName      AS RegName,
                    rs.Route        AS Route,
                    rs.TelNo        AS TelNo,
                    rs.Address      AS Address,
                    rs.BankName     AS BankName,
                    rs.AccCode      AS AccCode,
                    rs.Active       AS IsSupplierActive
                FROM Supplier_Management_Login sl
                INNER JOIN Reg_Supplier rs ON sl.RegNo = rs.RegNo;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS vw_SupplierLoginDetails;");
        }
    }
}
