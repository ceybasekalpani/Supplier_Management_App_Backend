using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeaFactorySupplierManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionCatalogAndUserGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Snapshot the existing per-module rows (module + free-form JSON sub-permission list)
            // before restructuring the table, so the data can be translated into the new
            // per-permission grant rows below instead of being silently discarded.
            migrationBuilder.Sql(@"
                SELECT * INTO supplier_dashboard_AdminPermissions_Legacy
                FROM supplier_dashboard_AdminPermissions;
            ");

            migrationBuilder.Sql("DELETE FROM supplier_dashboard_AdminPermissions;");

            migrationBuilder.DropIndex(
                name: "IX_supplier_dashboard_AdminPermissions_AdminId_ModuleId",
                table: "supplier_dashboard_AdminPermissions");

            migrationBuilder.DropColumn(
                name: "ModuleId",
                table: "supplier_dashboard_AdminPermissions");

            migrationBuilder.DropColumn(
                name: "SubPermissionsJson",
                table: "supplier_dashboard_AdminPermissions");

            migrationBuilder.RenameColumn(
                name: "IsEnabled",
                table: "supplier_dashboard_AdminPermissions",
                newName: "IsGranted");

            migrationBuilder.AddColumn<int>(
                name: "PermissionId",
                table: "supplier_dashboard_AdminPermissions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "supplier_dashboard_Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModuleLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_dashboard_Permissions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_dashboard_AdminPermissions_AdminId_PermissionId",
                table: "supplier_dashboard_AdminPermissions",
                columns: new[] { "AdminId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_dashboard_AdminPermissions_PermissionId",
                table: "supplier_dashboard_AdminPermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_dashboard_Permissions_Code",
                table: "supplier_dashboard_Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_supplier_dashboard_Permissions_Module_Action",
                table: "supplier_dashboard_Permissions",
                columns: new[] { "Module", "Action" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_dashboard_AdminPermissions_supplier_dashboard_Permissions_PermissionId",
                table: "supplier_dashboard_AdminPermissions",
                column: "PermissionId",
                principalTable: "supplier_dashboard_Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(PermissionCatalogSeedSql);
            migrationBuilder.Sql(LegacyPermissionTranslationSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_supplier_dashboard_AdminPermissions_supplier_dashboard_Permissions_PermissionId",
                table: "supplier_dashboard_AdminPermissions");

            migrationBuilder.DropTable(
                name: "supplier_dashboard_Permissions");

            migrationBuilder.DropIndex(
                name: "IX_supplier_dashboard_AdminPermissions_AdminId_PermissionId",
                table: "supplier_dashboard_AdminPermissions");

            migrationBuilder.DropIndex(
                name: "IX_supplier_dashboard_AdminPermissions_PermissionId",
                table: "supplier_dashboard_AdminPermissions");

            migrationBuilder.DropColumn(
                name: "PermissionId",
                table: "supplier_dashboard_AdminPermissions");

            migrationBuilder.RenameColumn(
                name: "IsGranted",
                table: "supplier_dashboard_AdminPermissions",
                newName: "IsEnabled");

            migrationBuilder.AddColumn<string>(
                name: "ModuleId",
                table: "supplier_dashboard_AdminPermissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubPermissionsJson",
                table: "supplier_dashboard_AdminPermissions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_dashboard_AdminPermissions_AdminId_ModuleId",
                table: "supplier_dashboard_AdminPermissions",
                columns: new[] { "AdminId", "ModuleId" },
                unique: true);

            // Restore the pre-migration rows from the backup snapshot, if it is still present.
            migrationBuilder.Sql(@"
                IF OBJECT_ID(N'dbo.supplier_dashboard_AdminPermissions_Legacy', N'U') IS NOT NULL
                BEGIN
                    DELETE FROM supplier_dashboard_AdminPermissions;

                    INSERT INTO supplier_dashboard_AdminPermissions (AdminId, ModuleId, IsEnabled, SubPermissionsJson, CreatedAt, UpdatedAt)
                    SELECT AdminId, ModuleId, IsEnabled, SubPermissionsJson, CreatedAt, UpdatedAt
                    FROM supplier_dashboard_AdminPermissions_Legacy;

                    DROP TABLE supplier_dashboard_AdminPermissions_Legacy;
                END
            ");
        }

        // One row per (module, action) entry in TeaFactorySupplierManagement.Domain.Permissions.PermissionCatalog.
        // Kept as inline SQL (rather than relying on DbInitializer's runtime seeding) so this migration is
        // self-contained and the legacy data translation below can join against real Permission rows immediately.
        private const string PermissionCatalogSeedSql = @"
INSERT INTO supplier_dashboard_Permissions (Module, ModuleLabel, Action, Code, DisplayName, Description, SortOrder, CreatedAt) VALUES
('dashboard','Dashboard','view','dashboard.view','View Dashboard','View analytics, KPI cards, graphs, and quick actions',0,SYSUTCDATETIME()),
('suppliers','Suppliers','view','suppliers.view','View Suppliers','View supplier profiles, search, and history',1,SYSUTCDATETIME()),
('suppliers','Suppliers','create','suppliers.create','Create Suppliers','Register new suppliers',2,SYSUTCDATETIME()),
('suppliers','Suppliers','update','suppliers.update','Update Suppliers','Edit supplier details',3,SYSUTCDATETIME()),
('suppliers','Suppliers','delete','suppliers.delete','Delete Suppliers','Remove supplier records',4,SYSUTCDATETIME()),
('suppliers','Suppliers','export','suppliers.export','Export Suppliers','Export supplier data to file',5,SYSUTCDATETIME()),
('cashRequests','Cash Requests','view','cashRequests.view','View Cash Requests','View advance and loan cash requests',6,SYSUTCDATETIME()),
('cashRequests','Cash Requests','create','cashRequests.create','Create Cash Requests','Raise a cash request on behalf of a supplier',7,SYSUTCDATETIME()),
('cashRequests','Cash Requests','update','cashRequests.update','Update Cash Requests','Edit pending cash requests',8,SYSUTCDATETIME()),
('cashRequests','Cash Requests','delete','cashRequests.delete','Delete Cash Requests','Remove cash requests',9,SYSUTCDATETIME()),
('cashRequests','Cash Requests','approve','cashRequests.approve','Approve Cash Requests','Approve, reject, or override the status of cash requests',10,SYSUTCDATETIME()),
('cashRequests','Cash Requests','export','cashRequests.export','Export Cash Requests','Export cash request data to file',11,SYSUTCDATETIME()),
('fertilizerRequests','Fertilizer Requests','view','fertilizerRequests.view','View Fertilizer Requests','View fertilizer requests',12,SYSUTCDATETIME()),
('fertilizerRequests','Fertilizer Requests','create','fertilizerRequests.create','Create Fertilizer Requests','Raise a fertilizer request on behalf of a supplier',13,SYSUTCDATETIME()),
('fertilizerRequests','Fertilizer Requests','update','fertilizerRequests.update','Update Fertilizer Requests','Edit pending fertilizer requests',14,SYSUTCDATETIME()),
('fertilizerRequests','Fertilizer Requests','delete','fertilizerRequests.delete','Delete Fertilizer Requests','Remove fertilizer requests',15,SYSUTCDATETIME()),
('fertilizerRequests','Fertilizer Requests','approve','fertilizerRequests.approve','Approve Fertilizer Requests','Approve, reject, or override the status of fertilizer requests',16,SYSUTCDATETIME()),
('fertilizerRequests','Fertilizer Requests','export','fertilizerRequests.export','Export Fertilizer Requests','Export fertilizer request data to file',17,SYSUTCDATETIME()),
('itemRequests','Item Requests','view','itemRequests.view','View Item Requests','View item requests',18,SYSUTCDATETIME()),
('itemRequests','Item Requests','create','itemRequests.create','Create Item Requests','Raise an item request on behalf of a supplier',19,SYSUTCDATETIME()),
('itemRequests','Item Requests','update','itemRequests.update','Update Item Requests','Edit pending item requests',20,SYSUTCDATETIME()),
('itemRequests','Item Requests','delete','itemRequests.delete','Delete Item Requests','Remove item requests',21,SYSUTCDATETIME()),
('itemRequests','Item Requests','approve','itemRequests.approve','Approve Item Requests','Approve, reject, or override the status of item requests',22,SYSUTCDATETIME()),
('itemRequests','Item Requests','export','itemRequests.export','Export Item Requests','Export item request data to file',23,SYSUTCDATETIME()),
('disbursements','Disbursements','view','disbursements.view','View Disbursements','View disbursement records',24,SYSUTCDATETIME()),
('disbursements','Disbursements','create','disbursements.create','Create Disbursements','Issue new disbursement records',25,SYSUTCDATETIME()),
('disbursements','Disbursements','update','disbursements.update','Update Disbursements','Edit disbursement records',26,SYSUTCDATETIME()),
('disbursements','Disbursements','approve','disbursements.approve','Approve Disbursements','Approve or complete disbursements',27,SYSUTCDATETIME()),
('disbursements','Disbursements','export','disbursements.export','Export Disbursements','Export disbursement data to file',28,SYSUTCDATETIME()),
('disbursementTracking','Disbursement Tracking','view','disbursementTracking.view','View Disbursement Tracking','Track disbursement and delivery note progress',29,SYSUTCDATETIME()),
('disbursementTracking','Disbursement Tracking','export','disbursementTracking.export','Export Disbursement Tracking','Export disbursement tracking data to file',30,SYSUTCDATETIME()),
('fertilizerConfiguration','Fertilizer Configuration','view','fertilizerConfiguration.view','View Fertilizer Configuration','View fertilizer types and store items',31,SYSUTCDATETIME()),
('fertilizerConfiguration','Fertilizer Configuration','create','fertilizerConfiguration.create','Create Fertilizer Configuration','Add new fertilizer types',32,SYSUTCDATETIME()),
('fertilizerConfiguration','Fertilizer Configuration','update','fertilizerConfiguration.update','Update Fertilizer Configuration','Edit fertilizer types',33,SYSUTCDATETIME()),
('fertilizerConfiguration','Fertilizer Configuration','delete','fertilizerConfiguration.delete','Delete Fertilizer Configuration','Remove fertilizer types',34,SYSUTCDATETIME()),
('itemConfiguration','Item Configuration','view','itemConfiguration.view','View Item Configuration','View item types and store items',35,SYSUTCDATETIME()),
('itemConfiguration','Item Configuration','create','itemConfiguration.create','Create Item Configuration','Add new item types',36,SYSUTCDATETIME()),
('itemConfiguration','Item Configuration','update','itemConfiguration.update','Update Item Configuration','Edit item types',37,SYSUTCDATETIME()),
('itemConfiguration','Item Configuration','delete','itemConfiguration.delete','Delete Item Configuration','Remove item types',38,SYSUTCDATETIME()),
('news','News','view','news.view','View News','View news announcements',39,SYSUTCDATETIME()),
('news','News','create','news.create','Create News','Publish new news announcements',40,SYSUTCDATETIME()),
('news','News','update','news.update','Update News','Edit news announcements',41,SYSUTCDATETIME()),
('news','News','delete','news.delete','Delete News','Remove news announcements',42,SYSUTCDATETIME()),
('news','News','send','news.send','Send News','Push news announcements to suppliers',43,SYSUTCDATETIME()),
('notifications','Notifications','view','notifications.view','View Notifications','View notification history',44,SYSUTCDATETIME()),
('notifications','Notifications','create','notifications.create','Create Notifications','Draft new notifications',45,SYSUTCDATETIME()),
('notifications','Notifications','send','notifications.send','Send Notifications','Push notifications to suppliers',46,SYSUTCDATETIME()),
('userManagement','User Management','view','userManagement.view','View Dashboard Users','View dashboard admin accounts',47,SYSUTCDATETIME()),
('userManagement','User Management','create','userManagement.create','Create Dashboard Users','Create new dashboard admin accounts',48,SYSUTCDATETIME()),
('userManagement','User Management','update','userManagement.update','Update Dashboard Users','Edit dashboard admin accounts',49,SYSUTCDATETIME()),
('userManagement','User Management','delete','userManagement.delete','Delete Dashboard Users','Remove dashboard admin accounts',50,SYSUTCDATETIME()),
('userManagement','User Management','manage','userManagement.manage','Manage Dashboard Users','Activate, deactivate, or reset credentials for dashboard admin accounts',51,SYSUTCDATETIME()),
('permissionManagement','Permission Management','view','permissionManagement.view','View Permissions','View dashboard user permission assignments',52,SYSUTCDATETIME()),
('permissionManagement','Permission Management','assign','permissionManagement.assign','Assign Permissions','Grant or revoke module and action permissions for dashboard users',53,SYSUTCDATETIME()),
('settings','Settings','view','settings.view','View Settings','View system and factory configuration',54,SYSUTCDATETIME()),
('settings','Settings','update','settings.update','Update Settings','Edit system and factory configuration',55,SYSUTCDATETIME());
";

        // Translates the legacy (ModuleId, free-form sub-permission code) rows captured in
        // supplier_dashboard_AdminPermissions_Legacy into the new normalized per-permission grants.
        // Every distinct legacy code observed in production data is mapped explicitly; a module
        // that was enabled with no sub-permissions selected falls back to granting that module's
        // "view" permission(s), matching how the old dashboard UI treated an enabled-but-unconfigured module.
        private const string LegacyPermissionTranslationSql = @"
CREATE TABLE #LegacyPermissionMap (
    LegacyModule NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL,
    LegacyCode NVARCHAR(200) COLLATE DATABASE_DEFAULT NOT NULL,
    NewCode NVARCHAR(100) COLLATE DATABASE_DEFAULT NOT NULL
);

INSERT INTO #LegacyPermissionMap (LegacyModule, LegacyCode, NewCode) VALUES
('dashboard','dashboard.view','dashboard.view'),
('dashboard','view_dashboard','dashboard.view'),
('dashboard','__default__','dashboard.view'),

('suppliers','suppliers.view','suppliers.view'),
('suppliers','suppliers.manage','suppliers.create'),
('suppliers','suppliers.manage','suppliers.update'),
('suppliers','suppliers.manage','suppliers.delete'),
('suppliers','suppliers.export','suppliers.export'),
('suppliers','view_suppliers','suppliers.view'),
('suppliers','__default__','suppliers.view'),

('requests','requests.view','cashRequests.view'),
('requests','requests.view','fertilizerRequests.view'),
('requests','requests.view','itemRequests.view'),
('requests','advance_requests.view','cashRequests.view'),
('requests','fertilizer_requests.view','fertilizerRequests.view'),
('requests','item_requests.view','itemRequests.view'),
('requests','requests.manage','cashRequests.create'),
('requests','requests.manage','cashRequests.update'),
('requests','requests.manage','cashRequests.delete'),
('requests','requests.manage','fertilizerRequests.create'),
('requests','requests.manage','fertilizerRequests.update'),
('requests','requests.manage','fertilizerRequests.delete'),
('requests','requests.manage','itemRequests.create'),
('requests','requests.manage','itemRequests.update'),
('requests','requests.manage','itemRequests.delete'),
('requests','requests.status_override','cashRequests.approve'),
('requests','requests.status_override','fertilizerRequests.approve'),
('requests','requests.status_override','itemRequests.approve'),
('requests','view_requests','cashRequests.view'),
('requests','view_requests','fertilizerRequests.view'),
('requests','view_requests','itemRequests.view'),
('requests','__default__','cashRequests.view'),
('requests','__default__','fertilizerRequests.view'),
('requests','__default__','itemRequests.view'),

('disbursements','disbursements.view','disbursements.view'),
('disbursements','disbursements.manage','disbursements.create'),
('disbursements','disbursements.manage','disbursements.update'),
('disbursements','disbursements.manage','disbursements.approve'),
('disbursements','__default__','disbursements.view'),

('disbursementTracking','disbursements.view','disbursementTracking.view'),
('disbursementTracking','disbursements.manage','disbursementTracking.export'),
('disbursementTracking','__default__','disbursementTracking.view'),

('configurations','configurations.manage','fertilizerConfiguration.view'),
('configurations','configurations.manage','fertilizerConfiguration.create'),
('configurations','configurations.manage','fertilizerConfiguration.update'),
('configurations','configurations.manage','fertilizerConfiguration.delete'),
('configurations','configurations.manage','itemConfiguration.view'),
('configurations','configurations.manage','itemConfiguration.create'),
('configurations','configurations.manage','itemConfiguration.update'),
('configurations','configurations.manage','itemConfiguration.delete'),
('configurations','fertilizer.manage','fertilizerConfiguration.view'),
('configurations','fertilizer.manage','fertilizerConfiguration.create'),
('configurations','fertilizer.manage','fertilizerConfiguration.update'),
('configurations','fertilizer.manage','fertilizerConfiguration.delete'),
('configurations','items.manage','itemConfiguration.view'),
('configurations','items.manage','itemConfiguration.create'),
('configurations','items.manage','itemConfiguration.update'),
('configurations','items.manage','itemConfiguration.delete'),
('configurations','__default__','fertilizerConfiguration.view'),
('configurations','__default__','itemConfiguration.view'),

('communication','news.view','news.view'),
('communication','news.manage','news.create'),
('communication','news.manage','news.update'),
('communication','news.manage','news.delete'),
('communication','notifications.view','notifications.view'),
('communication','notifications.send','notifications.create'),
('communication','notifications.send','notifications.send'),
('communication','view_communication','news.view'),
('communication','view_communication','notifications.view'),
('communication','__default__','news.view'),
('communication','__default__','notifications.view'),

('userManagement','users.view','userManagement.view'),
('userManagement','users.manage','userManagement.create'),
('userManagement','users.manage','userManagement.update'),
('userManagement','users.manage','userManagement.delete'),
('userManagement','users.manage','userManagement.manage'),
('userManagement','__default__','userManagement.view'),

('settings','permissions.view','permissionManagement.view'),
('settings','permissions.assign','permissionManagement.assign'),
('settings','permissions.manage','permissionManagement.assign'),
('settings','__default__','permissionManagement.view');

;WITH LegacyExpanded AS (
    SELECT
        l.AdminId,
        l.ModuleId,
        CAST(j.[value] AS NVARCHAR(200)) AS LegacyCode
    FROM supplier_dashboard_AdminPermissions_Legacy l
    CROSS APPLY OPENJSON(l.SubPermissionsJson) j
    WHERE l.IsEnabled = 1

    UNION ALL

    SELECT
        l.AdminId,
        l.ModuleId,
        '__default__' AS LegacyCode
    FROM supplier_dashboard_AdminPermissions_Legacy l
    WHERE l.IsEnabled = 1
      AND (l.SubPermissionsJson IS NULL OR l.SubPermissionsJson = '[]' OR l.SubPermissionsJson = '')
)
INSERT INTO supplier_dashboard_AdminPermissions (AdminId, PermissionId, IsGranted, CreatedAt)
SELECT DISTINCT le.AdminId, p.Id, 1, SYSUTCDATETIME()
FROM LegacyExpanded le
INNER JOIN #LegacyPermissionMap m ON m.LegacyModule = le.ModuleId AND m.LegacyCode = le.LegacyCode
INNER JOIN supplier_dashboard_Permissions p ON p.Code = m.NewCode;

DROP TABLE #LegacyPermissionMap;
";
    }
}
