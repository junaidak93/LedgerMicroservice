using Microsoft.Extensions.DependencyInjection;
using Ledger.API.Repositories;
using Ledger.Infrastructure.Repositories;
using Ledger.Infrastructure.UnitOfWork;

namespace Ledger.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<ILoginRepository, LoginRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IStatsRepository, StatsRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
