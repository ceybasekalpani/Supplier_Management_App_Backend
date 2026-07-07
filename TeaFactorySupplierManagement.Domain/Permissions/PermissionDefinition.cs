namespace TeaFactorySupplierManagement.Domain.Permissions
{
    
    public sealed record PermissionDefinition(
        string Module,
        string ModuleLabel,
        string ModuleDescription,
        string Action,
        string ActionLabel,
        string Description,
        int SortOrder)
    {
        public string Code => $"{Module}.{Action}";
    }
}
