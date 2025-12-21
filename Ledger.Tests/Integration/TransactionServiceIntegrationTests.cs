using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Ledger.API.Data;
using Ledger.API.Repositories;
using Ledger.API.Services;
using Ledger.Tests.Helpers;
using Ledger.API.DTOs;
using Ledger.API.Models;
using Xunit;

namespace Ledger.Tests.Integration;

public class TransactionServiceIntegrationTests
{
    [Fact]
    public async Task CreateTransaction_ConcurrentRequests_CreateOnlyOnce_Sqlite()
    {
        var connectionString = "DataSource=file:memdb1?mode=memory&cache=shared";

        // Keep-alive connection to hold the in-memory DB
        var keepAlive = new SqliteConnection(connectionString);
        keepAlive.Open();

        var seedOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(keepAlive)
            .Options;

        // Create schema and seed a user using the keep-alive connection
        await using (var seedCtx = new ApplicationDbContext(seedOptions))
        {
            seedCtx.Database.EnsureCreated();
            var user = TestHelpers.CreateTestUser("intuser@example.com");
            seedCtx.Logins.Add(user);
            await seedCtx.SaveChangesAsync();
        }

        var dto = new TransactionCreateDto { Amount = 50, Fee = 2, Type = TransactionType.Incoming };
        var key = "idem-key-sqlite";

        var tasks = Enumerable.Range(0, 5).Select(_ => Task.Run(async () =>
        {
            var conn = new SqliteConnection(connectionString);
            await conn.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(conn)
                .Options;

            await using var ctx = new ApplicationDbContext(options);
            var txRepo = new TransactionRepository(ctx);
            var loginRepo = new LoginRepository(ctx);
            var idemRepo = new IdempotencyRepository(ctx);
            var uow = new TestUnitOfWork(ctx);
            var service = new TransactionService(txRepo, loginRepo, uow, idemRepo);

            var user = await ctx.Logins.FirstAsync();
            var result = await service.CreateTransactionAsync(user.Id, dto, key);

            await conn.CloseAsync();
            await conn.DisposeAsync();

            return result;
        }));

        var results = await Task.WhenAll(tasks);

        var distinctIds = results.Select(r => r.Id).Distinct().ToList();
        Assert.Single(distinctIds);

        await using var verifyCtx = new ApplicationDbContext(seedOptions);
        var txCount = await verifyCtx.Transactions.CountAsync();
        Assert.Equal(1, txCount);

        var updatedUser = await verifyCtx.Logins.FirstAsync();
        Assert.Equal(1000 + (50 - 2), updatedUser.Balance);

        await keepAlive.CloseAsync();
        await keepAlive.DisposeAsync();
    }
}
