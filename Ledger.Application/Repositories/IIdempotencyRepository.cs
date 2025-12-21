using Ledger.API.Models;

namespace Ledger.API.Repositories;

public interface IIdempotencyRepository
{
    Task<IdempotencyKey?> GetByKeyAsync(string key);
    Task<IdempotencyKey> CreateAsync(IdempotencyKey entry);
    Task<int> DeleteExpiredAsync(DateTime threshold);
}
