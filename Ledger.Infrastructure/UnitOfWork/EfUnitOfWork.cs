using Microsoft.EntityFrameworkCore.Storage;
using Ledger.API.Data;
using Ledger.API.Repositories;

namespace Ledger.Infrastructure.UnitOfWork;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public EfUnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ITransactionScope> BeginTransactionAsync()
    {
        var tx = await _context.Database.BeginTransactionAsync();
        return new EfTransaction(tx);
    }

    private class EfTransaction : ITransactionScope
    {
        private readonly IDbContextTransaction _tx;

        public EfTransaction(IDbContextTransaction tx)
        {
            _tx = tx;
        }

        public Task CommitAsync() => _tx.CommitAsync();
        public Task RollbackAsync() => _tx.RollbackAsync();
        public ValueTask DisposeAsync() => _tx.DisposeAsync();
    }
}
