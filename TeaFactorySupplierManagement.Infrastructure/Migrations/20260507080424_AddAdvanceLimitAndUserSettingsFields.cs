using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvanceLimitAndUserSettingsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdvanceLimit",
                table: "Supplier_Management_Factory_Setup",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Supplier_Management_Settings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone2",
                table: "Supplier_Management_Settings",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone3",
                table: "Supplier_Management_Settings",
                type: "nvarchar(15)",
                maxLength: 15,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvanceLimit",
                table: "Supplier_Management_Factory_Setup");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Supplier_Management_Settings");

            migrationBuilder.DropColumn(
                name: "Phone2",
                table: "Supplier_Management_Settings");

            migrationBuilder.DropColumn(
                name: "Phone3",
                table: "Supplier_Management_Settings");
        }
    }
}
