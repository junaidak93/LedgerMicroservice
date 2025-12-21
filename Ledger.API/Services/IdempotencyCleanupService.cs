using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ledger.API.Data;
using Microsoft.EntityFrameworkCore;
using Ledger.API.Repositories;

namespace Ledger.API.Services;

public class IdempotencyCleanupOptions
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromDays(7);
}

public class IdempotencyCleanupService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<IdempotencyCleanupService> _logger;
    private readonly IdempotencyCleanupOptions _options;

    public IdempotencyCleanupService(IServiceProvider services, ILogger<IdempotencyCleanupService> logger, IOptions<IdempotencyCleanupOptions> options)
    {
        _services = services;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IdempotencyCleanupService started. Interval: {Interval}, MaxAge: {MaxAge}", _options.Interval, _options.MaxAge);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during idempotency keys cleanup");
            }

            await Task.Delay(_options.Interval, stoppingToken);
        }
    }

    private async Task CleanupOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IIdempotencyRepository>();

        var threshold = DateTime.UtcNow - _options.MaxAge;
        var deleted = await repo.DeleteExpiredAsync(threshold);

        if (deleted == 0)
        {
            _logger.LogDebug("No expired idempotency keys found.");
            return;
        }

        _logger.LogInformation("Deleted expired idempotency keys (rows affected: {Deleted}).", deleted);
    }
}
