using Ledger.API.DTOs;

namespace Ledger.API.Services;

public interface IStatsService
{
    Task<GlobalStatsDto> GetGlobalStatsAsync();
    Task<UserStatsDto> GetUserStatsAsync(Guid userId);
}

