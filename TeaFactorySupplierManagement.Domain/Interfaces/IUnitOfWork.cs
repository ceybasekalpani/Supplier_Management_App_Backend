namespace TeaFactorySupplierManagement.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<RegSupplier> RegSuppliers { get; }
        IRepository<TrLeafCollectionTemp> LeafCollections { get; }
        IRepository<SupplierLogin> SupplierLogins { get; }
        IRepository<FactorySetup> FactorySetups { get; }
        IRepository<CashRequest> CashRequests { get; }
        IRepository<FertilizerRequest> FertilizerRequests { get; }
        IRepository<ItemRequest> ItemRequests { get; }
        IRepository<Notification> Notifications { get; }
        IRepository<News> News { get; }
        IRepository<NotificationRead> NotificationReads { get; }
        IRepository<NewsDismissed> NewsDismissed { get; }
        IRepository<UserSettings> UserSettings { get; }

        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}