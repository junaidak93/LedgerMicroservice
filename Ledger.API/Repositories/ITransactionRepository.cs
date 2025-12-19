using Ledger.API.Models;

namespace Ledger.API.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<Transaction>> GetAllAsync(int pageNumber = 1, int pageSize = 10);
    Task<Transaction> CreateAsync(Transaction transaction);
    Task<Transaction> UpdateAsync(Transaction transaction);
    Task DeleteAsync(Guid id);
    Task<int> GetCountByUserIdAsync(Guid userId);
}

