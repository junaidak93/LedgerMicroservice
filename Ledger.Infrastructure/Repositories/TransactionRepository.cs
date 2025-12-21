using Microsoft.EntityFrameworkCore;
using Ledger.API.Data;
using Ledger.API.Models;
using Ledger.API.Repositories;

namespace Ledger.Infrastructure.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;

    public TransactionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id)
    {
        return await _context.Transactions
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id && t.DeletedAt == null);
    }

    public async Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 10)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId && t.DeletedAt == null)
            .OrderByDescending(t => t.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync(int pageNumber = 1, int pageSize = 10)
    {
        return await _context.Transactions
            .Where(t => t.DeletedAt == null)
            .Include(t => t.User)
            .OrderByDescending(t => t.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction> UpdateAsync(Transaction transaction)
    {
        transaction.UpdatedAt = DateTime.UtcNow;
        _context.Transactions.Update(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task DeleteAsync(Guid id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            transaction.DeletedAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetCountByUserIdAsync(Guid userId)
    {
        return await _context.Transactions
            .CountAsync(t => t.UserId == userId && t.DeletedAt == null);
    }
}
