using Microsoft.EntityFrameworkCore.Storage;
using TeaFactorySupplierManagement.Domain.Entities;
using TeaFactorySupplierManagement.Domain.Interfaces;
using TeaFactorySupplierManagement.Infrastructure.Data;

namespace TeaFactorySupplierManagement.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed;

        private IRepository<RegSupplier>? _regSuppliers;
        private IRepository<TrLeafCollectionTemp>? _leafCollections;
        private IRepository<SupplierLogin>? _supplierLogins;
        private IRepository<FactorySetup>? _factorySetups;
        private IRepository<CashRequest>? _cashRequests;
        private IRepository<FertilizerRequest>? _fertilizerRequests;
        private IRepository<ItemRequest>? _itemRequests;
        private IRepository<Notification>? _notifications;
        private IRepository<News>? _news;
        private IRepository<NotificationRead>? _notificationReads;
        private IRepository<NewsDismissed>? _newsDismissed;
        private IRepository<UserSettings>? _userSettings;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }

        public IRepository<RegSupplier> RegSuppliers =>
            _regSuppliers ??= new Repository<RegSupplier>(_context);

        public IRepository<TrLeafCollectionTemp> LeafCollections =>
            _leafCollections ??= new Repository<TrLeafCollectionTemp>(_context);

        public IRepository<SupplierLogin> SupplierLogins =>
            _supplierLogins ??= new Repository<SupplierLogin>(_context);

        public IRepository<FactorySetup> FactorySetups =>
            _factorySetups ??= new Repository<FactorySetup>(_context);

        public IRepository<CashRequest> CashRequests =>
            _cashRequests ??= new Repository<CashRequest>(_context);

        public IRepository<FertilizerRequest> FertilizerRequests =>
            _fertilizerRequests ??= new Repository<FertilizerRequest>(_context);

        public IRepository<ItemRequest> ItemRequests =>
            _itemRequests ??= new Repository<ItemRequest>(_context);

        public IRepository<Notification> Notifications =>
            _notifications ??= new Repository<Notification>(_context);

        public IRepository<News> News =>
            _news ??= new Repository<News>(_context);

        public IRepository<NotificationRead> NotificationReads =>
            _notificationReads ??= new Repository<NotificationRead>(_context);

        public IRepository<NewsDismissed> NewsDismissed =>
            _newsDismissed ??= new Repository<NewsDismissed>(_context);

        public IRepository<UserSettings> UserSettings =>
            _userSettings ??= new Repository<UserSettings>(_context);

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }
            _disposed = true;
        }
    }
}