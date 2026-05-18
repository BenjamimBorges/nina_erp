namespace ERP.Core.Interfaces
{
    public interface IUnitOfWork
    {
        IUserRepository Users { get; }
        Task<int> CommitAsync(CancellationToken cancellationToken = default);
    }
}
