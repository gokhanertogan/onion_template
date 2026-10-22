namespace ServiceName.IntegrationTests;

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ServiceName.Persistence;

/// <summary>
/// Hosts the real API in memory with a throw-away SQLite database and a test JWT issuer,
/// so the full pipeline (auth, validation, persistence, outbox, caching) is exercised.
/// </summary>
public sealed class ServiceNameApiFactory : WebApplicationFactory<Program>
{
    private const string Issuer = "integration-tests";
    private const string Audience = "integration-tests";
    private static readonly SymmetricSecurityKey SigningKey = new(Encoding.UTF8.GetBytes("integration-tests-signing-key-of-at-least-256-bits!"));

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"servicename-tests-{Guid.NewGuid():N}.db");

    public async Task InitializeDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ServiceNameDbContext>().Database.EnsureCreatedAsync();
    }

    public HttpClient CreateClientWithPermissions(params string[] permissions)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(permissions));
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting($"ConnectionStrings:{DependencyInjection.ConnectionStringName}", $"Data Source={_databasePath}");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("Outbox:PollingInterval", "00:00:00.200");
        builder.UseSetting("RateLimiting:PermitLimit", "100000");
        builder.UseSetting("Logging:LogLevel:Default", "Warning");

        builder.ConfigureTestServices(services =>
        {
            ReplaceDbContextWithSqlite(services);

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.ValidAudiences = [Audience];
                options.TokenValidationParameters.IssuerSigningKey = SigningKey;
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        foreach (var file in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            File.Delete(file);
        }
    }

    private void ReplaceDbContextWithSqlite(IServiceCollection services)
    {
        var existing = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ServiceNameDbContext>) ||
                d.ServiceType == typeof(IDbContextOptionsConfiguration<ServiceNameDbContext>))
            .ToList();
        existing.ForEach(d => services.Remove(d));

        services.AddDbContext<ServiceNameDbContext>((sp, options) => options
            .UseSqlite($"Data Source={_databasePath}")
            .AddInterceptors(PersistenceServiceCollectionExtensions.GetInterceptors(sp)));
    }

    private static string CreateToken(string[] permissions)
    {
        var claims = new List<Claim> { new("sub", $"user-{Guid.NewGuid():N}"), new("name", "Integration Test") };
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(10),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
        });
    }
}
