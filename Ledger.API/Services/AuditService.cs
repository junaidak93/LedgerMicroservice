using Ledger.API.Models;
using Ledger.API.Repositories;

namespace Ledger.API.Services;

public class AuditService : IAuditService
{
    private readonly IAuditRepository _auditRepository;

    public AuditService(IAuditRepository auditRepository)
    {
        _auditRepository = auditRepository;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        Guid? entityId = null,
        Guid? createdBy = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? details = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            CreatedBy = createdBy,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ServerTimestamp = DateTime.UtcNow,
            Details = details
        };

        await _auditRepository.CreateAsync(auditLog);
    }
}

