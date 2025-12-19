using Ledger.API.DTOs;
using Ledger.API.Repositories;

namespace Ledger.API.Services;

public class StatsService : IStatsService
{
    private readonly IStatsRepository _statsRepository;

    public StatsService(IStatsRepository statsRepository)
    {
        _statsRepository = statsRepository;
    }

    public async Task<GlobalStatsDto> GetGlobalStatsAsync()
    {
        var totalVolume = await _statsRepository.GetTotalVolumeAsync();
        var totalFees = await _statsRepository.GetTotalFeesAsync();

        return new GlobalStatsDto
        {
            TotalVolume = totalVolume,
            TotalFees = totalFees
        };
    }

    public async Task<UserStatsDto> GetUserStatsAsync(Guid userId)
    {
        var balance = await _statsRepository.GetUserBalanceAsync(userId);
        var transactionCount = await _statsRepository.GetUserTransactionCountAsync(userId);

        return new UserStatsDto
        {
            UserId = userId,
            Balance = balance,
            TransactionCount = transactionCount
        };
    }
}

