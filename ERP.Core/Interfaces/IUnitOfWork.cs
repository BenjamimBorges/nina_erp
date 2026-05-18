namespace ERP.Core.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        IClientRepository Clients { get; }
        IProductRepository Products { get; }
        IStockRepository Stocks { get; }
        Task<int> CommitAsync(CancellationToken cancellationToken = default);
    }
}
