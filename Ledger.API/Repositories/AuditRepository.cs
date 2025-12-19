using Microsoft.EntityFrameworkCore;
using Ledger.API.Data;
using Ledger.API.Models;

namespace Ledger.API.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly ApplicationDbContext _context;

    public AuditRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog> CreateAsync(AuditLog auditLog)
    {
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
        return auditLog;
    }

    public async Task<IEnumerable<AuditLog>> GetByUserIdAsync(Guid userId, int pageNumber = 1, int pageSize = 10)
    {
        return await _context.AuditLogs
            .Where(a => a.CreatedBy == userId)
            .OrderByDescending(a => a.ServerTimestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetByEntityAsync(string entityType, Guid entityId)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.ServerTimestamp)
            .ToListAsync();
    }
}

