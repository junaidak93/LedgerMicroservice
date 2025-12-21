using Ledger.API.Models;

namespace Ledger.API.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken> CreateAsync(RefreshToken token);
    Task<RefreshToken> UpdateAsync(RefreshToken token);
}
