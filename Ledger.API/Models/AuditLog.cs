using System.Text.Json;

namespace Ledger.API.Models;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? CreatedBy { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime ServerTimestamp { get; set; } = DateTime.UtcNow;
    public string? Details { get; set; }

    // Navigation property
    public virtual Login? User { get; set; }
}

