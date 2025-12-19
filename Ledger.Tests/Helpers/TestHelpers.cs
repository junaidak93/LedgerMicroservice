using Ledger.API.Models;

namespace Ledger.Tests.Helpers;

public static class TestHelpers
{
    public static Login CreateTestUser(string email = "test@example.com", Role role = Role.User)
    {
        return new Login
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "HASHED_PASSWORD",
            Role = role,
            Balance = 1000,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Transaction CreateTestTransaction(Guid userId, decimal amount = 100, TransactionType type = TransactionType.Incoming)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            Type = type,
            Fee = 5,
            Description = "Test transaction",
            Timestamp = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static RefreshToken CreateTestRefreshToken(Guid userId)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = "test_refresh_token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };
    }
}

