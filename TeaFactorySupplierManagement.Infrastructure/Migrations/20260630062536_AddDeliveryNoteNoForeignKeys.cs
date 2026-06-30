using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryNoteNoForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Supplier_Management_DeliveryNote_Detail_Supplier_Management_DeliveryNote_Header_DeliveryNoteId",
                table: "Supplier_Management_DeliveryNote_Detail");

            migrationBuilder.DropForeignKey(
                name: "FK_Supplier_Management_Disbursement_Records_Supplier_Management_DeliveryNote_Header_DeliveryNoteId",
                table: "Supplier_Management_Disbursement_Records");

            migrationBuilder.DropIndex(
                name: "IX_Supplier_Management_DeliveryNote_Header_DN_No",
                table: "Supplier_Management_DeliveryNote_Header");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNoteNo",
                table: "Supplier_Management_Disbursement_Records",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNoteNo",
                table: "Supplier_Management_DeliveryNote_Detail",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE dr
                SET DeliveryNoteNo = h.DN_No
                FROM Supplier_Management_Disbursement_Records dr
                INNER JOIN Supplier_Management_DeliveryNote_Header h ON h.Id = dr.DeliveryNoteId
                WHERE dr.DeliveryNoteId IS NOT NULL
                """);

            migrationBuilder.Sql("""
                UPDATE dd
                SET DeliveryNoteNo = h.DN_No
                FROM Supplier_Management_DeliveryNote_Detail dd
                INNER JOIN Supplier_Management_DeliveryNote_Header h ON h.Id = dd.DeliveryNoteId
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Supplier_Management_DeliveryNote_Header_DN_No",
                table: "Supplier_Management_DeliveryNote_Header",
                column: "DN_No");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Disbursement_Records_DeliveryNoteNo",
                table: "Supplier_Management_Disbursement_Records",
                column: "DeliveryNoteNo");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_DeliveryNote_Detail_DeliveryNoteNo",
                table: "Supplier_Management_DeliveryNote_Detail",
                column: "DeliveryNoteNo");

            migrationBuilder.AddForeignKey(
                name: "FK_Supplier_Management_DeliveryNote_Detail_Supplier_Management_DeliveryNote_Header_DeliveryNoteNo",
                table: "Supplier_Management_DeliveryNote_Detail",
                column: "DeliveryNoteNo",
                principalTable: "Supplier_Management_DeliveryNote_Header",
                principalColumn: "DN_No",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Supplier_Management_Disbursement_Records_Supplier_Management_DeliveryNote_Header_DeliveryNoteNo",
                table: "Supplier_Management_Disbursement_Records",
                column: "DeliveryNoteNo",
                principalTable: "Supplier_Management_DeliveryNote_Header",
                principalColumn: "DN_No",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Supplier_Management_DeliveryNote_Detail_Supplier_Management_DeliveryNote_Header_DeliveryNoteNo",
                table: "Supplier_Management_DeliveryNote_Detail");

            migrationBuilder.DropForeignKey(
                name: "FK_Supplier_Management_Disbursement_Records_Supplier_Management_DeliveryNote_Header_DeliveryNoteNo",
                table: "Supplier_Management_Disbursement_Records");

            migrationBuilder.DropIndex(
                name: "IX_Supplier_Management_Disbursement_Records_DeliveryNoteNo",
                table: "Supplier_Management_Disbursement_Records");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Supplier_Management_DeliveryNote_Header_DN_No",
                table: "Supplier_Management_DeliveryNote_Header");

            migrationBuilder.DropIndex(
                name: "IX_Supplier_Management_DeliveryNote_Detail_DeliveryNoteNo",
                table: "Supplier_Management_DeliveryNote_Detail");

            migrationBuilder.DropColumn(
                name: "DeliveryNoteNo",
                table: "Supplier_Management_Disbursement_Records");

            migrationBuilder.DropColumn(
                name: "DeliveryNoteNo",
                table: "Supplier_Management_DeliveryNote_Detail");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_DeliveryNote_Header_DN_No",
                table: "Supplier_Management_DeliveryNote_Header",
                column: "DN_No",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Supplier_Management_DeliveryNote_Detail_Supplier_Management_DeliveryNote_Header_DeliveryNoteId",
                table: "Supplier_Management_DeliveryNote_Detail",
                column: "DeliveryNoteId",
                principalTable: "Supplier_Management_DeliveryNote_Header",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Supplier_Management_Disbursement_Records_Supplier_Management_DeliveryNote_Header_DeliveryNoteId",
                table: "Supplier_Management_Disbursement_Records",
                column: "DeliveryNoteId",
                principalTable: "Supplier_Management_DeliveryNote_Header",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
