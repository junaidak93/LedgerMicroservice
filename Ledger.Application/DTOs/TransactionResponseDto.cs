using Ledger.API.Models;

namespace Ledger.API.DTOs;

public class TransactionResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public decimal Fee { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
