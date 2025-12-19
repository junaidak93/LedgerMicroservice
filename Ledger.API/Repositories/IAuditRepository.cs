using Ledger.API.Models;

namespace Ledger.API.Repositories;

public interface IAuditRepository
{
    Task<AuditLog> CreateAsync(AuditLog auditLog);
    Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId);
}

