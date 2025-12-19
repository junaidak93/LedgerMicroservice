using System.ComponentModel.DataAnnotations;
using Ledger.API.Models;

namespace Ledger.API.DTOs;

public class TransactionUpdateDto
{
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required]
    public TransactionType Type { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Fee cannot be negative")]
    public decimal Fee { get; set; } = 0;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime? Timestamp { get; set; }
}

