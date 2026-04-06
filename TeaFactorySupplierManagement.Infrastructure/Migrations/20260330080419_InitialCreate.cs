using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           
            migrationBuilder.CreateTable(
                name: "Supplier_Management_Factory_Setup",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactoryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsSuper = table.Column<bool>(type: "bit", nullable: false),
                    IsCashRequest = table.Column<bool>(type: "bit", nullable: false),
                    IsItemRequest = table.Column<bool>(type: "bit", nullable: false),
                    IsFertilizerRequest = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_Factory_Setup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Management_News",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ShowPopup = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_News", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Management_Cash_Requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegNo = table.Column<int>(type: "int", nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Month = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestMonth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_Cash_Requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_Cash_Requests_Reg_Supplier_RegNo",
                        column: x => x.RegNo,
                        principalTable: "Reg_Supplier",
                        principalColumn: "RegNo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Management_Fertilizer_Requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegNo = table.Column<int>(type: "int", nullable: false),
                    FertilizerType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Month = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestMonth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DispatchedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_Fertilizer_Requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_Fertilizer_Requests_Reg_Supplier_RegNo",
                        column: x => x.RegNo,
                        principalTable: "Reg_Supplier",
                        principalColumn: "RegNo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Management_Item_Requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegNo = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestMonth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_Item_Requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_Item_Requests_Reg_Supplier_RegNo",
                        column: x => x.RegNo,
                        principalTable: "Reg_Supplier",
                        principalColumn: "RegNo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Management_Login",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegNo = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_Login", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_Login_Reg_Supplier_RegNo",
                        column: x => x.RegNo,
                        principalTable: "Reg_Supplier",
                        principalColumn: "RegNo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Management_Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TargetRegNo = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_Notifications_Reg_Supplier_TargetRegNo",
                        column: x => x.TargetRegNo,
                        principalTable: "Reg_Supplier",
                        principalColumn: "RegNo");
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Management_Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegNo = table.Column<int>(type: "int", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FontSize = table.Column<int>(type: "int", nullable: true),
                    ProfileImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AccountHolder = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Branch = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_Settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_Settings_Reg_Supplier_RegNo",
                        column: x => x.RegNo,
                        principalTable: "Reg_Supplier",
                        principalColumn: "RegNo",
                        onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateTable(
                name: "Supplier_Management_News_Dismissed",
                columns: table => new
                {
                    NewsId = table.Column<int>(type: "int", nullable: false),
                    RegNo = table.Column<int>(type: "int", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_News_Dismissed", x => new { x.NewsId, x.RegNo });
                    table.ForeignKey(
                        name: "FK_Supplier_Management_News_Dismissed_Reg_Supplier_RegNo",
                        column: x => x.RegNo,
                        principalTable: "Reg_Supplier",
                        principalColumn: "RegNo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_News_Dismissed_Supplier_Management_News_NewsId",
                        column: x => x.NewsId,
                        principalTable: "Supplier_Management_News",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Supplier_Management_Notification_Reads",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false),
                    RegNo = table.Column<int>(type: "int", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supplier_Management_Notification_Reads", x => new { x.NotificationId, x.RegNo });
                    table.ForeignKey(
                        name: "FK_Supplier_Management_Notification_Reads_Reg_Supplier_RegNo",
                        column: x => x.RegNo,
                        principalTable: "Reg_Supplier",
                        principalColumn: "RegNo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Supplier_Management_Notification_Reads_Supplier_Management_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Supplier_Management_Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Cash_Requests_RegNo",
                table: "Supplier_Management_Cash_Requests",
                column: "RegNo");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Cash_Requests_RequestNo",
                table: "Supplier_Management_Cash_Requests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Factory_Setup_FactoryName",
                table: "Supplier_Management_Factory_Setup",
                column: "FactoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Fertilizer_Requests_RegNo",
                table: "Supplier_Management_Fertilizer_Requests",
                column: "RegNo");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Fertilizer_Requests_RequestNo",
                table: "Supplier_Management_Fertilizer_Requests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Item_Requests_RegNo",
                table: "Supplier_Management_Item_Requests",
                column: "RegNo");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Item_Requests_RequestNo",
                table: "Supplier_Management_Item_Requests",
                column: "RequestNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Login_RegNo",
                table: "Supplier_Management_Login",
                column: "RegNo");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Login_Username",
                table: "Supplier_Management_Login",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_News_Dismissed_RegNo",
                table: "Supplier_Management_News_Dismissed",
                column: "RegNo");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Notification_Reads_RegNo",
                table: "Supplier_Management_Notification_Reads",
                column: "RegNo");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Notifications_TargetRegNo",
                table: "Supplier_Management_Notifications",
                column: "TargetRegNo");

            migrationBuilder.CreateIndex(
                name: "IX_Supplier_Management_Settings_RegNo",
                table: "Supplier_Management_Settings",
                column: "RegNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tr_LeafCollection_Temp_RegNo",
                table: "Tr_LeafCollection_Temp",
                column: "RegNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Supplier_Management_Cash_Requests");

            migrationBuilder.DropTable(
                name: "Supplier_Management_Factory_Setup");

            migrationBuilder.DropTable(
                name: "Supplier_Management_Fertilizer_Requests");

            migrationBuilder.DropTable(
                name: "Supplier_Management_Item_Requests");

            migrationBuilder.DropTable(
                name: "Supplier_Management_Login");

            migrationBuilder.DropTable(
                name: "Supplier_Management_News_Dismissed");

            migrationBuilder.DropTable(
                name: "Supplier_Management_Notification_Reads");

            migrationBuilder.DropTable(
                name: "Supplier_Management_Settings");

            

            migrationBuilder.DropTable(
                name: "Supplier_Management_News");

            migrationBuilder.DropTable(
                name: "Supplier_Management_Notifications");
        }
    }
}
