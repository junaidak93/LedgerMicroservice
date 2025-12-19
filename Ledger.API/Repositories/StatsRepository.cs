using Microsoft.EntityFrameworkCore;
using Ledger.API.Data;

namespace Ledger.API.Repositories;

public class StatsRepository : IStatsRepository
{
    private readonly ApplicationDbContext _context;

    public StatsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetTotalVolumeAsync()
    {
        return await _context.Transactions
            .Where(t => t.DeletedAt == null)
            .SumAsync(t => t.Amount);
    }

    public async Task<decimal> GetTotalFeesAsync()
    {
        return await _context.Transactions
            .Where(t => t.DeletedAt == null)
            .SumAsync(t => t.Fee);
    }

    public async Task<decimal> GetUserBalanceAsync(Guid userId)
    {
        var user = await _context.Logins.FindAsync(userId);
        return user?.Balance ?? 0;
    }

    public async Task<int> GetUserTransactionCountAsync(Guid userId)
    {
        return await _context.Transactions
            .CountAsync(t => t.UserId == userId && t.DeletedAt == null);
    }
}

