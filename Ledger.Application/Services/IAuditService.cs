using Ledger.API.Models;

namespace Ledger.API.Services;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        Guid? entityId = null,
        Guid? createdBy = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null);
}
