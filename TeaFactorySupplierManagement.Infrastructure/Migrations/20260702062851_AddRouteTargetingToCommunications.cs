using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteTargetingToCommunications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Supplier_Management_Notifications_Reg_Supplier_TargetRegNo",
                table: "Supplier_Management_Notifications");

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Supplier_Management_Notifications_IsActive_StartDate_EndDate_TargetRegNo'
                      AND object_id = OBJECT_ID(N'[dbo].[Supplier_Management_Notifications]')
                )
                BEGIN
                    DROP INDEX [IX_Supplier_Management_Notifications_IsActive_StartDate_EndDate_TargetRegNo]
                    ON [dbo].[Supplier_Management_Notifications];
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Supplier_Management_News_IsActive_ShowPopup_StartDate_EndDate'
                      AND object_id = OBJECT_ID(N'[dbo].[Supplier_Management_News]')
                )
                BEGIN
                    DROP INDEX [IX_Supplier_Management_News_IsActive_ShowPopup_StartDate_EndDate]
                    ON [dbo].[Supplier_Management_News];
                END
                """);

            migrationBuilder.AddColumn<string>(
                name: "TargetRoute",
                table: "Supplier_Management_Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetRoute",
                table: "Supplier_Management_News",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Notifications_IsActive_StartDate_EndDate_TargetRegNo_TargetRoute",
                table: "Supplier_Management_Notifications",
                columns: new[] { "IsActive", "StartDate", "EndDate", "TargetRegNo", "TargetRoute" });

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_News_IsActive_ShowPopup_StartDate_EndDate_TargetRegNo_TargetRoute",
                table: "Supplier_Management_News",
                columns: new[] { "IsActive", "ShowPopup", "StartDate", "EndDate", "TargetRegNo", "TargetRoute" });

            migrationBuilder.AddForeignKey(
                name: "FK_Supplier_Management_Notifications_Reg_Supplier_TargetRegNo",
                table: "Supplier_Management_Notifications",
                column: "TargetRegNo",
                principalTable: "Reg_Supplier",
                principalColumn: "RegNo",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Supplier_Management_Notifications_Reg_Supplier_TargetRegNo",
                table: "Supplier_Management_Notifications");

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Supplier_Management_Notifications_IsActive_StartDate_EndDate_TargetRegNo_TargetRoute'
                      AND object_id = OBJECT_ID(N'[dbo].[Supplier_Management_Notifications]')
                )
                BEGIN
                    DROP INDEX [IX_Supplier_Management_Notifications_IsActive_StartDate_EndDate_TargetRegNo_TargetRoute]
                    ON [dbo].[Supplier_Management_Notifications];
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = N'IX_Supplier_Management_News_IsActive_ShowPopup_StartDate_EndDate_TargetRegNo_TargetRoute'
                      AND object_id = OBJECT_ID(N'[dbo].[Supplier_Management_News]')
                )
                BEGIN
                    DROP INDEX [IX_Supplier_Management_News_IsActive_ShowPopup_StartDate_EndDate_TargetRegNo_TargetRoute]
                    ON [dbo].[Supplier_Management_News];
                END
                """);

            migrationBuilder.DropColumn(
                name: "TargetRoute",
                table: "Supplier_Management_Notifications");

            migrationBuilder.DropColumn(
                name: "TargetRoute",
                table: "Supplier_Management_News");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Notifications_IsActive_StartDate_EndDate_TargetRegNo",
                table: "Supplier_Management_Notifications",
                columns: new[] { "IsActive", "StartDate", "EndDate", "TargetRegNo" });

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_News_IsActive_ShowPopup_StartDate_EndDate",
                table: "Supplier_Management_News",
                columns: new[] { "IsActive", "ShowPopup", "StartDate", "EndDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_Supplier_Management_Notifications_Reg_Supplier_TargetRegNo",
                table: "Supplier_Management_Notifications",
                column: "TargetRegNo",
                principalTable: "Reg_Supplier",
                principalColumn: "RegNo");
        }
    }
}
