using System;
using System.Collections.Generic;

namespace Ledger.API.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public decimal Fee { get; set; }
    public string? Description { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    // New fields for immutable reversal semantics and running balance
    public decimal? CumulativeBalance { get; set; }
    public bool IsReversal { get; set; } = false;
    public Guid? OriginalTransactionId { get; set; }

    // Navigation property
    public virtual Login User { get; set; } = null!;
}
