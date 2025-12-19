using Ledger.API.Models;

namespace Ledger.API.Repositories;

public interface ILoginRepository
{
    Task<Login?> GetByIdAsync(Guid id);
    Task<Login?> GetByEmailAsync(string email);
    Task<Login> CreateAsync(Login login);
    Task<Login> UpdateAsync(Login login);
    Task<bool> ExistsAsync(string email);
}

