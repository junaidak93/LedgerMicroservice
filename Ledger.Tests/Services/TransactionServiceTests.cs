using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Ledger.API.Data;
using Ledger.API.Repositories;
using Ledger.API.Services;
using Ledger.Tests.Helpers;
using Ledger.API.DTOs;
using Ledger.API.Models;
using Xunit;
using Ledger.Infrastructure.Repositories;

namespace Ledger.Tests.Services;

public class TransactionServiceTests
{
    [Fact]
    public async Task CreateTransaction_WithIdempotencyKey_IsStoredOnce()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "tx_test_db_1")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new ApplicationDbContext(options);

        var user = TestHelpers.CreateTestUser("user1@example.com");
        context.Logins.Add(user);
        await context.SaveChangesAsync();

        var txRepo = new TransactionRepository(context);
        var loginRepo = new LoginRepository(context);
        var idemRepo = new IdempotencyRepository(context);

        var uow = new TestUnitOfWork(context);
        var service = new TransactionService(txRepo, loginRepo, uow, idemRepo);

        var dto = new TransactionCreateDto { Amount = 100, Fee = 5, Type = TransactionType.Incoming };
        var key = "idem-key-123";

        var result1 = await service.CreateTransactionAsync(user.Id, dto, key);
        var result2 = await service.CreateTransactionAsync(user.Id, dto, key);

        Assert.Equal(result1.Id, result2.Id);

        var txCount = await context.Transactions.CountAsync();
        Assert.Equal(1, txCount);
        var storedTx = await context.Transactions.SingleAsync();
        Assert.Equal(95, storedTx.CumulativeBalance);

        var updatedUser = await context.Logins.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        // original balance 1000 + (100 - 5)
        Assert.Equal(1000 + 95, updatedUser!.Balance);
    }

    [Fact]
    public async Task UpdateTransaction_InsertsReversalAndNewTransaction_UpdatesCumulativeAndUserBalance()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "tx_test_db_update")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new ApplicationDbContext(options);

        var user = TestHelpers.CreateTestUser("upduser@example.com");
        context.Logins.Add(user);
        await context.SaveChangesAsync();

        var txRepo = new TransactionRepository(context);
        var loginRepo = new LoginRepository(context);
        var idemRepo = new IdempotencyRepository(context);

        var uow = new TestUnitOfWork(context);
        var service = new TransactionService(txRepo, loginRepo, uow, idemRepo);

        // Create initial transaction: Incoming 50 fee 2 -> net = 48
        var createDto = new TransactionCreateDto { Amount = 50, Fee = 2, Type = TransactionType.Incoming };
        var created = await service.CreateTransactionAsync(user.Id, createDto);

        // Update the transaction to Incoming 20 fee 1 -> newNet = 19
        var updateDto = new TransactionUpdateDto { Amount = 20, Fee = 1, Type = TransactionType.Incoming };
        var updated = await service.UpdateTransactionAsync(created.Id, updateDto, user.Id, isAdmin: true);

        // There should be 3 transactions: original, reversal, updated
        var txs = await context.Transactions.OrderBy(t => t.CreatedAt).ToListAsync();
        Assert.Equal(3, txs.Count);

        var original = txs[0];
        var reversal = txs[1];
        var newTx = txs[2];

        // original net = 48, cumulative = 48
        Assert.Equal(48m, original.CumulativeBalance);

        // reversal should negate original: reversalNet = -(50 + 2) = -52 -> cumulative = 48 + (-52) = -4
        Assert.Equal(-4m, reversal.CumulativeBalance);

        // new transaction cumulative = -4 + 19 = 15
        Assert.Equal(15m, newTx.CumulativeBalance);

        // User balance: start 1000 -> +48 -> 1048 -> +(-52) -> 996 -> +19 -> 1015
        var updatedUser = await context.Logins.FindAsync(user.Id);
        Assert.Equal(1015m, updatedUser!.Balance);
    }

    [Fact]
    public async Task DeleteTransaction_InsertsReversal_AdjustsCumulativeAndUserBalance()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "tx_test_db_delete")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new ApplicationDbContext(options);

        var user = TestHelpers.CreateTestUser("deluser@example.com");
        context.Logins.Add(user);
        await context.SaveChangesAsync();

        var txRepo = new TransactionRepository(context);
        var loginRepo = new LoginRepository(context);
        var idemRepo = new IdempotencyRepository(context);

        var uow = new TestUnitOfWork(context);
        var service = new TransactionService(txRepo, loginRepo, uow, idemRepo);

        // Create initial transaction: Incoming 50 fee 2 -> net = 48
        var createDto = new TransactionCreateDto { Amount = 50, Fee = 2, Type = TransactionType.Incoming };
        var created = await service.CreateTransactionAsync(user.Id, createDto);

        // Delete the transaction (should insert a reversal)
        await service.DeleteTransactionAsync(created.Id);

        var txs = await context.Transactions.OrderBy(t => t.CreatedAt).ToListAsync();
        Assert.Equal(2, txs.Count);

        var original = txs[0];
        var reversal = txs[1];

        // original cumulative = 48
        Assert.Equal(48m, original.CumulativeBalance);

        // reversal cumulative = 48 + (-52) = -4
        Assert.Equal(-4m, reversal.CumulativeBalance);

        // User balance: 1000 -> +48 -> 1048 -> +(-52) -> 996
        var updatedUser = await context.Logins.FindAsync(user.Id);
        Assert.Equal(996m, updatedUser!.Balance);
    }

    [Fact]
    public async Task CreateTransaction_ConcurrentRequests_CreateOnlyOnce()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "tx_test_db_2")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new ApplicationDbContext(options);

        var user = TestHelpers.CreateTestUser("user2@example.com");
        context.Logins.Add(user);
        await context.SaveChangesAsync();

        var txRepo = new TransactionRepository(context);
        var loginRepo = new LoginRepository(context);
        var idemRepo = new IdempotencyRepository(context);

        var uow = new TestUnitOfWork(context);
        var service = new TransactionService(txRepo, loginRepo, uow, idemRepo);

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
        var storedTx = await context.Transactions.SingleAsync();
        Assert.Equal(50 - 2, storedTx.CumulativeBalance);
    }
}
