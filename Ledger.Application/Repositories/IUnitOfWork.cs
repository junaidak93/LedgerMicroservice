namespace Ledger.API.Repositories;

public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync();
    Task RollbackAsync();
}

public interface IUnitOfWork
{
    Task<ITransactionScope> BeginTransactionAsync();
}
