using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ledger.API.Data;
using Ledger.API.Repositories;
using Ledger.API.Services;
using Ledger.Tests.Helpers;
using Ledger.API.DTOs;
using Ledger.API.Models;
using Xunit;

namespace Ledger.Tests.Services;

public class TransactionServiceTests
{
    [Fact]
    public async Task CreateTransaction_WithIdempotencyKey_IsStoredOnce()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "tx_test_db_1")
            .Options;

        await using var context = new ApplicationDbContext(options);

        var user = TestHelpers.CreateTestUser("user1@example.com");
        context.Logins.Add(user);
        await context.SaveChangesAsync();

        var txRepo = new TransactionRepository(context);
        var loginRepo = new LoginRepository(context);
        var idemRepo = new IdempotencyRepository(context);

        var service = new TransactionService(txRepo, loginRepo, context, idemRepo);

        var dto = new TransactionCreateDto { Amount = 100, Fee = 5, Type = TransactionType.Incoming };
        var key = "idem-key-123";

        var result1 = await service.CreateTransactionAsync(user.Id, dto, key);
        var result2 = await service.CreateTransactionAsync(user.Id, dto, key);

        Assert.Equal(result1.Id, result2.Id);

        var txCount = await context.Transactions.CountAsync();
        Assert.Equal(1, txCount);

        var updatedUser = await context.Logins.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        // original balance 1000 + (100 - 5)
        Assert.Equal(1000 + 95, updatedUser!.Balance);
    }

    [Fact]
    public async Task CreateTransaction_ConcurrentRequests_CreateOnlyOnce()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "tx_test_db_2")
            .Options;

        await using var context = new ApplicationDbContext(options);

        var user = TestHelpers.CreateTestUser("user2@example.com");
        context.Logins.Add(user);
        await context.SaveChangesAsync();

        var txRepo = new TransactionRepository(context);
        var loginRepo = new LoginRepository(context);
        var idemRepo = new IdempotencyRepository(context);

        var service = new TransactionService(txRepo, loginRepo, context, idemRepo);

        var dto = new TransactionCreateDto { Amount = 50, Fee = 2, Type = TransactionType.Incoming };
        var key = "idem-key-concurrent";

        var tasks = Enumerable.Range(0, 5).Select(_ => service.CreateTransactionAsync(user.Id, dto, key));
        var results = await Task.WhenAll(tasks);

        // All results should reference the same transaction id
        var distinctIds = results.Select(r => r.Id).Distinct().ToList();
        Assert.Single(distinctIds);

        var txCount = await context.Transactions.CountAsync();
        Assert.Equal(1, txCount);

        var updatedUser = await context.Logins.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(1000 + (50 - 2), updatedUser!.Balance);
    }
}
