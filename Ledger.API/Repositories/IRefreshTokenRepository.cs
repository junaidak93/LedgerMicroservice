using Ledger.API.Models;

namespace Ledger.API.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task<RefreshToken> UpdateAsync(RefreshToken refreshToken);
    Task RevokeTokenAsync(Guid tokenId);
    Task RevokeAllUserTokensAsync(Guid userId);
    Task<IEnumerable<RefreshToken>> GetExpiredTokensAsync();
}

