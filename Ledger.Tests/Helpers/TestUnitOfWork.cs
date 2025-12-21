using Ledger.API.Data;
using Ledger.API.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ledger.Tests.Helpers;

public class TestUnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public TestUnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ITransactionScope> BeginTransactionAsync()
    {
        var tx = await _context.Database.BeginTransactionAsync();
        return new TestTransaction(tx);
    }

    private class TestTransaction : ITransactionScope
    {
        private readonly IDbContextTransaction _tx;

        public TestTransaction(IDbContextTransaction tx) => _tx = tx;

        public Task CommitAsync() => _tx.CommitAsync();
        public Task RollbackAsync() => _tx.RollbackAsync();
        public ValueTask DisposeAsync() => _tx.DisposeAsync();
    }
}
