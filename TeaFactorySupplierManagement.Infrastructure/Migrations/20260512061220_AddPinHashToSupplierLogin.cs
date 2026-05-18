using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPinHashToSupplierLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PinHash",
                table: "Supplier_Management_Login",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PinHash",
                table: "Supplier_Management_Login");
        }
    }
}
