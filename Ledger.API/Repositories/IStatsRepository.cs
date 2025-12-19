namespace Ledger.API.Repositories;

public interface IStatsRepository
{
    Task<decimal> GetTotalVolumeAsync();
    Task<decimal> GetTotalFeesAsync();
    Task<decimal> GetUserBalanceAsync(Guid userId);
    Task<int> GetUserTransactionCountAsync(Guid userId);
}

