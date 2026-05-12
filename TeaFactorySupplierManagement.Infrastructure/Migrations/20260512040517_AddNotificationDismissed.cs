using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationDismissed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Supplier_Management_Notification_Dismissed",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false),
                    RegNo = table.Column<int>(type: "int", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_Notification_Dismissed", x => new { x.NotificationId, x.RegNo });
                    table.ForeignKey(
                        name: "FK_Supplier_Management_Notification_Dismissed_Reg_Supplier_RegNo",
                        column: x => x.RegNo,
                        principalTable: "Reg_Supplier",
                        principalColumn: "RegNo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_Notification_Dismissed_Supplier_Management_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Supplier_Management_Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Notification_Dismissed_RegNo",
                table: "Supplier_Management_Notification_Dismissed",
                column: "RegNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Supplier_Management_Notification_Dismissed");
        }
    }
}
