namespace Ledger.API.DTOs;

public class GlobalStatsDto
{
    public decimal TotalVolume { get; set; }
    public decimal TotalFees { get; set; }
}

public class UserStatsDto
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
}

