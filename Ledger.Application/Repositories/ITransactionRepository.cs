using Ledger.API.Models;

namespace Ledger.API.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize);
    Task<IEnumerable<Transaction>> GetAllAsync(int pageNumber, int pageSize);
    Task<Transaction> CreateAsync(Transaction transaction);
    Task<Transaction> UpdateAsync(Transaction transaction);
    Task DeleteAsync(Guid id);
}
