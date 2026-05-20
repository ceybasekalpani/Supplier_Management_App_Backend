using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCreatePinColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCreatePin",
                table: "Supplier_Management_Login",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCreatePin",
                table: "Supplier_Management_Login");
        }
    }
}
