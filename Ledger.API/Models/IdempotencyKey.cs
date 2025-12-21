using System.ComponentModel.DataAnnotations;

namespace Ledger.API.Models;

public class IdempotencyKey
{
    [Key]
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public Guid? UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string ResponseBody { get; set; } = null!;

    public int StatusCode { get; set; }

    public DateTime? ExpiresAt { get; set; }
}
