using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ModifyFertilizerItemStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Supplier_Management_FertilizerItem_Store");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Supplier_Management_FertilizerItem_Store");

            migrationBuilder.AddColumn<string>(
                name: "FertilizerType",
                table: "Supplier_Management_FertilizerItem_Store",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemType",
                table: "Supplier_Management_FertilizerItem_Store",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FertilizerType",
                table: "Supplier_Management_FertilizerItem_Store");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "Supplier_Management_FertilizerItem_Store");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Supplier_Management_FertilizerItem_Store",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Supplier_Management_FertilizerItem_Store",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
