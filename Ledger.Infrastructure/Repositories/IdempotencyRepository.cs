using Microsoft.EntityFrameworkCore;
using Ledger.API.Data;
using Ledger.API.Models;
using Ledger.API.Repositories;

namespace Ledger.Infrastructure.Repositories;

public class IdempotencyRepository : IIdempotencyRepository
{
    private readonly ApplicationDbContext _context;

    public IdempotencyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyKey?> GetByKeyAsync(string key)
    {
        return await _context.IdempotencyKeys.FirstOrDefaultAsync(k => k.Key == key);
    }

    public async Task<IdempotencyKey> CreateAsync(IdempotencyKey entry)
    {
        _context.IdempotencyKeys.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    public async Task<int> DeleteExpiredAsync(DateTime threshold)
    {
        var toRemove = await _context.IdempotencyKeys
            .Where(k => k.ExpiresAt != null && k.ExpiresAt < threshold)
            .ToListAsync();

        if (toRemove.Count == 0) return 0;

        _context.IdempotencyKeys.RemoveRange(toRemove);
        return await _context.SaveChangesAsync();
    }
}
