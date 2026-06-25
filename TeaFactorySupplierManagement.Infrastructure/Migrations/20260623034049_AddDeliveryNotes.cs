using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryNoteId",
                table: "Supplier_Management_Disbursement_Records",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchedAt",
                table: "Supplier_Management_Disbursement_Records",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Supplier_Management_Code_Generation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodePrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastNo = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_Code_Generation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Management_DeliveryNote_Header",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DN_No = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DispatchDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BorrowerName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BorrowerRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VehicleNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RouteName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_DeliveryNote_Header", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Management_DeliveryNote_Detail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeliveryNoteId = table.Column<int>(type: "int", nullable: false),
                    SupplierRegNo = table.Column<int>(type: "int", nullable: false),
                    DisbursementRecordId = table.Column<int>(type: "int", nullable: false),
                    RequestId = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PaymentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SupplierSignatureReceived = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_DeliveryNote_Detail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_DeliveryNote_Detail_Reg_Supplier_SupplierRegNo",
                        column: x => x.SupplierRegNo,
                        principalTable: "Reg_Supplier",
                        principalColumn: "RegNo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_DeliveryNote_Detail_Supplier_Management_DeliveryNote_Header_DeliveryNoteId",
                        column: x => x.DeliveryNoteId,
                        principalTable: "Supplier_Management_DeliveryNote_Header",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_DeliveryNote_Detail_Supplier_Management_Disbursement_Records_DisbursementRecordId",
                        column: x => x.DisbursementRecordId,
                        principalTable: "Supplier_Management_Disbursement_Records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Disbursement_Records_DeliveryNoteId",
                table: "Supplier_Management_Disbursement_Records",
                column: "DeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Code_Generation_CodePrefix_Year",
                table: "Supplier_Management_Code_Generation",
                columns: new[] { "CodePrefix", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_DeliveryNote_Detail_DeliveryNoteId",
                table: "Supplier_Management_DeliveryNote_Detail",
                column: "DeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_DeliveryNote_Detail_DisbursementRecordId",
                table: "Supplier_Management_DeliveryNote_Detail",
                column: "DisbursementRecordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_DeliveryNote_Detail_SupplierRegNo",
                table: "Supplier_Management_DeliveryNote_Detail",
                column: "SupplierRegNo");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_DeliveryNote_Header_DN_No",
                table: "Supplier_Management_DeliveryNote_Header",
                column: "DN_No",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_DeliveryNote_Header_Status_DispatchDate",
                table: "Supplier_Management_DeliveryNote_Header",
                columns: new[] { "Status", "DispatchDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Supplier_Management_Disbursement_Records_Supplier_Management_DeliveryNote_Header_DeliveryNoteId",
                table: "Supplier_Management_Disbursement_Records",
                column: "DeliveryNoteId",
                principalTable: "Supplier_Management_DeliveryNote_Header",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Supplier_Management_Disbursement_Records_Supplier_Management_DeliveryNote_Header_DeliveryNoteId",
                table: "Supplier_Management_Disbursement_Records");

            migrationBuilder.DropTable(
                name: "Supplier_Management_Code_Generation");

            migrationBuilder.DropTable(
                name: "Supplier_Management_DeliveryNote_Detail");

            migrationBuilder.DropTable(
                name: "Supplier_Management_DeliveryNote_Header");

            migrationBuilder.DropIndex(
                name: "IX_Supplier_Management_Disbursement_Records_DeliveryNoteId",
                table: "Supplier_Management_Disbursement_Records");

            migrationBuilder.DropColumn(
                name: "DeliveryNoteId",
                table: "Supplier_Management_Disbursement_Records");

            migrationBuilder.DropColumn(
                name: "DispatchedAt",
                table: "Supplier_Management_Disbursement_Records");
        }
    }
}
