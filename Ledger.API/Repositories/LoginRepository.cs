using Microsoft.EntityFrameworkCore;
using Ledger.API.Data;
using Ledger.API.Models;

namespace Ledger.API.Repositories;

public class LoginRepository : ILoginRepository
{
    private readonly ApplicationDbContext _context;

    public LoginRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Login?> GetByIdAsync(Guid id)
    {
        return await _context.Logins.FindAsync(id);
    }

    public async Task<Login?> GetByEmailAsync(string email)
    {
        return await _context.Logins
            .FirstOrDefaultAsync(l => l.Email == email);
    }

    public async Task<Login> CreateAsync(Login login)
    {
        _context.Logins.Add(login);
        await _context.SaveChangesAsync();
        return login;
    }

    public async Task<Login> UpdateAsync(Login login)
    {
        login.UpdatedAt = DateTime.UtcNow;
        _context.Logins.Update(login);
        await _context.SaveChangesAsync();
        return login;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        return await _context.Logins
            .AnyAsync(l => l.Email == email);
    }
}

