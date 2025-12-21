using Microsoft.EntityFrameworkCore;
using Ledger.API.Data;
using Ledger.API.Models;

namespace Ledger.API.Repositories;

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
}
