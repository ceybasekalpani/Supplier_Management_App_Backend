using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TeaFactorySupplierManagement.Infrastructure.Data;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260615090000_AddFirebaseDeviceTokensAndTargetedNews")]
    public partial class AddFirebaseDeviceTokensAndTargetedNews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TargetRegNo",
                table: "Supplier_Management_News",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Supplier_Management_MobileDeviceTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegNo = table.Column<int>(type: "int", nullable: false),
                    FcmToken = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_MobileDeviceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_MobileDeviceTokens_Reg_Supplier_RegNo",
                        column: x => x.RegNo,
                        principalTable: "Reg_Supplier",
                        principalColumn: "RegNo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_News_TargetRegNo",
                table: "Supplier_Management_News",
                column: "TargetRegNo");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_MobileDeviceTokens_FcmToken",
                table: "Supplier_Management_MobileDeviceTokens",
                column: "FcmToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_MobileDeviceTokens_RegNo_IsActive",
                table: "Supplier_Management_MobileDeviceTokens",
                columns: new[] { "RegNo", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Supplier_Management_News_Reg_Supplier_TargetRegNo",
                table: "Supplier_Management_News",
                column: "TargetRegNo",
                principalTable: "Reg_Supplier",
                principalColumn: "RegNo",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Supplier_Management_News_Reg_Supplier_TargetRegNo",
                table: "Supplier_Management_News");

            migrationBuilder.DropTable(
                name: "Supplier_Management_MobileDeviceTokens");

            migrationBuilder.DropIndex(
                name: "IX_Supplier_Management_News_TargetRegNo",
                table: "Supplier_Management_News");

            migrationBuilder.DropColumn(
                name: "TargetRegNo",
                table: "Supplier_Management_News");
        }
    }
}
