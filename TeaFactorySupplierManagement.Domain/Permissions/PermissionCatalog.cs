namespace TeaFactorySupplierManagement.Domain.Permissions
{
    
    public static class PermissionCatalog
    {
        public static IReadOnlyList<PermissionDefinition> All { get; } = Build();

        private static IReadOnlyList<PermissionDefinition> Build()
        {
            var definitions = new List<PermissionDefinition>();
            var sortOrder = 0;

            void AddModule(
                string module,
                string moduleLabel,
                string moduleDescription,
                params (string Action, string Label, string Description)[] actions)
            {
                foreach (var (action, label, description) in actions)
                {
                    definitions.Add(new PermissionDefinition(module, moduleLabel, moduleDescription, action, label, description, sortOrder++));
                }
            }

            AddModule(PermissionModules.Dashboard, "Dashboard", "Analytics overview, KPI cards, graphs, and quick actions",
                (PermissionActions.View, "View Dashboard", "View analytics, KPI cards, and graphs"),
                (PermissionActions.QuickActions, "Access Quick Actions", "Use the quick-navigation panel to jump to pending approval queues"));

            AddModule(PermissionModules.Suppliers, "Suppliers", "View supplier records, search, and history",
                (PermissionActions.View, "View Suppliers", "View supplier profiles, search, and history"));

            AddModule(PermissionModules.CashRequests, "Cash Requests", "Handle supplier advance/loan cash requests",
                (PermissionActions.View, "View Cash Requests", "View advance and loan cash requests"),
                (PermissionActions.Approve, "Approve Cash Requests", "Approve, reject, or override the status of cash requests"));

            AddModule(PermissionModules.FertilizerRequests, "Fertilizer Requests", "Handle supplier fertilizer requests",
                (PermissionActions.View, "View Fertilizer Requests", "View fertilizer requests"),
                (PermissionActions.Approve, "Approve Fertilizer Requests", "Approve, reject, or override the status of fertilizer requests"));

            AddModule(PermissionModules.ItemRequests, "Item Requests", "Handle supplier item requests",
                (PermissionActions.View, "View Item Requests", "View item requests"),
                (PermissionActions.Approve, "Approve Item Requests", "Approve, reject, or override the status of item requests"));

            AddModule(PermissionModules.Disbursements, "Disbursements", "Manage supplier payment disbursements",
                (PermissionActions.View, "View Disbursements", "View disbursement records"),
                (PermissionActions.Create, "Create Disbursements", "Issue new disbursement records"),
                (PermissionActions.Update, "Update Disbursements", "Edit disbursement records"),
                (PermissionActions.Export, "Export Disbursements", "Export disbursement data to file"));

            AddModule(PermissionModules.DisbursementTracking, "Disbursement Tracking", "Track disbursement progress and delivery note status",
                (PermissionActions.View, "View Disbursement Tracking", "Track disbursement and delivery note progress"),
                (PermissionActions.Update, "Update Disbursement Tracking", "Mark disbursements as received"),
                (PermissionActions.Export, "Export Disbursement Tracking", "Export disbursement tracking data to file"));

            AddModule(PermissionModules.FertilizerConfiguration, "Fertilizer Configuration", "Manage fertilizer types",
                (PermissionActions.View, "View Fertilizer Configuration", "View fertilizer types"),
                (PermissionActions.Create, "Create Fertilizer Configuration", "Add new fertilizer types"),
                (PermissionActions.Update, "Update Fertilizer Configuration", "Edit or activate/deactivate fertilizer types"));

            AddModule(PermissionModules.ItemConfiguration, "Item Configuration", "Manage item types",
                (PermissionActions.View, "View Item Configuration", "View item types"),
                (PermissionActions.Create, "Create Item Configuration", "Add new item types"),
                (PermissionActions.Update, "Update Item Configuration", "Edit or activate/deactivate item types"));

            AddModule(PermissionModules.News, "News", "Publish and manage news announcements",
                (PermissionActions.View, "View News", "View news announcements"),
                (PermissionActions.Create, "Create News", "Publish new news announcements"),
                (PermissionActions.Update, "Update News", "Edit news announcements"),
                (PermissionActions.Delete, "Delete News", "Remove news announcements"));

            AddModule(PermissionModules.Notifications, "Notifications", "Send and manage supplier notifications",
                (PermissionActions.View, "View Notifications", "View notification history"),
                (PermissionActions.Create, "Create Notifications", "Draft and send new notifications"),
                (PermissionActions.Update, "Update Notifications", "Edit draft or scheduled notifications"),
                (PermissionActions.Delete, "Delete Notifications", "Remove notifications"));

            AddModule(PermissionModules.UserManagement, "User Management", "Manage dashboard admin user accounts",
                (PermissionActions.View, "View Dashboard Users", "View dashboard admin accounts"),
                (PermissionActions.Create, "Create Dashboard Users", "Create new dashboard admin accounts"),
                (PermissionActions.Update, "Update Dashboard Users", "Edit dashboard admin accounts, including status and password"),
                (PermissionActions.Delete, "Delete Dashboard Users", "Remove dashboard admin accounts"));

            AddModule(PermissionModules.PermissionManagement, "Permission Management", "Assign module and action permissions to dashboard users",
                (PermissionActions.View, "View Permissions", "View dashboard user permission assignments"),
                (PermissionActions.Assign, "Assign Permissions", "Grant or revoke module and action permissions for dashboard users"));

            return definitions;
        }
    }
}
