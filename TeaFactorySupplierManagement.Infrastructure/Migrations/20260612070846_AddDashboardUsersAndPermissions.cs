using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardUsersAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarBase64",
                table: "supplier_dashboard_AdminLogin",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "supplier_dashboard_AdminLogin",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNo",
                table: "supplier_dashboard_AdminLogin",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "supplier_dashboard_AdminLogin",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Admin");

            migrationBuilder.CreateTable(
                name: "supplier_dashboard_AdminPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminId = table.Column<int>(type: "int", nullable: false),
                    ModuleId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SubPermissionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_dashboard_AdminPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supplier_dashboard_AdminPermissions_supplier_dashboard_AdminLogin_AdminId",
                        column: x => x.AdminId,
                        principalTable: "supplier_dashboard_AdminLogin",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_dashboard_AdminLogin_Email",
                table: "supplier_dashboard_AdminLogin",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_dashboard_AdminPermissions_AdminId_ModuleId",
                table: "supplier_dashboard_AdminPermissions",
                columns: new[] { "AdminId", "ModuleId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "supplier_dashboard_AdminPermissions");

            migrationBuilder.DropIndex(
                name: "IX_supplier_dashboard_AdminLogin_Email",
                table: "supplier_dashboard_AdminLogin");

            migrationBuilder.DropColumn(
                name: "AvatarBase64",
                table: "supplier_dashboard_AdminLogin");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "supplier_dashboard_AdminLogin");

            migrationBuilder.DropColumn(
                name: "PhoneNo",
                table: "supplier_dashboard_AdminLogin");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "supplier_dashboard_AdminLogin");
        }
    }
}
