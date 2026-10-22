namespace BuildingBlocks.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Applies migrations (or creates the schema) before the app starts serving requests.</summary>
public sealed partial class DatabaseInitializer<TDbContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<DatabaseOptions> options,
    ILogger<DatabaseInitializer<TDbContext>> logger)
    : IHostedService
    where TDbContext : DbContext
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.ApplyMigrationsOnStartup)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        if (dbContext.Database.GetMigrations().Any())
        {
            LogMigrating(logger, typeof(TDbContext).Name);
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            LogEnsureCreated(logger, typeof(TDbContext).Name);
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 2100, Level = LogLevel.Information, Message = "Applying migrations for {DbContext}")]
    private static partial void LogMigrating(ILogger logger, string dbContext);

    [LoggerMessage(EventId = 2101, Level = LogLevel.Warning, Message = "No migrations found for {DbContext}; creating schema with EnsureCreated. Add a migration before going to production.")]
    private static partial void LogEnsureCreated(ILogger logger, string dbContext);
}
