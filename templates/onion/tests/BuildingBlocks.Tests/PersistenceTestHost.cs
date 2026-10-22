namespace BuildingBlocks.Tests;

using System.Collections.Concurrent;
using BuildingBlocks.Application;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Events;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Events;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The real persistence pipeline (interceptors, query filters, concurrency tokens, outbox) wired through
/// <see cref="PersistenceServiceCollectionExtensions.AddAppDbContext{TDbContext}"/> over in-memory SQLite.
/// </summary>
internal sealed class PersistenceTestHost : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    private PersistenceTestHost(SqliteConnection connection)
    {
        _connection = connection;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser>(User);
        services.AddSingleton<TimeProvider>(Time);
        services.AddSingleton(Events);
        services.AddEventHandlers(typeof(PersistenceTestHost).Assembly);
        services.AddAppDbContext<NotesDbContext>(new ConfigurationBuilder().Build(), (_, options) => options.UseSqlite(connection));
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public TestUser User { get; } = new();

    public TestTimeProvider Time { get; } = new();

    public EventLog Events { get; } = new();

    public IServiceScopeFactory ScopeFactory => _provider.GetRequiredService<IServiceScopeFactory>();

    public static async Task<PersistenceTestHost> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var host = new PersistenceTestHost(connection);
        await using var scope = host.CreateScope();
        await Db(scope).Database.EnsureCreatedAsync();
        return host;
    }

    public static NotesDbContext Db(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<NotesDbContext>();

    public AsyncServiceScope CreateScope() => _provider.CreateAsyncScope();

    public async Task<Note> AddNoteAsync(string text)
    {
        await using var scope = CreateScope();
        var db = Db(scope);
        var note = new Note(Guid.CreateVersion7(), text);
        db.Notes.Add(note);
        await db.SaveChangesAsync();
        return note;
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

internal sealed class NotesDbContext(DbContextOptions<NotesDbContext> options) : AppDbContextBase(options)
{
    public DbSet<Note> Notes => Set<Note>();
}

internal sealed class Note : AggregateRoot<Guid>, IAuditable, ISoftDeletable
{
    public Note(Guid id, string text)
        : base(id)
    {
        Text = text;
        Raise(new NoteCreated(id, text));
    }

    private Note()
    {
    }

    public string Text { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public string? DeletedBy { get; private set; }

    public void Edit(string text) => Text = text;
}

internal sealed record NoteCreated(Guid NoteId, string Text) : DomainEvent;

internal sealed class NoteCreatedHandler(EventLog log) : IDomainEventHandler<NoteCreated>
{
    public Task Handle(NoteCreated domainEvent, CancellationToken cancellationToken)
    {
        if (log.TryConsumeFailure())
        {
            throw new InvalidOperationException("Handler failure");
        }

        log.Add(domainEvent);
        return Task.CompletedTask;
    }
}

internal sealed class EventLog
{
    private readonly ConcurrentQueue<IDomainEvent> _events = new();
    private int _failuresRemaining;

    public IReadOnlyCollection<IDomainEvent> Events => _events;

    /// <summary>Makes the next <paramref name="count"/> handler invocations throw.</summary>
    public void FailNext(int count) => _failuresRemaining = count;

    public bool TryConsumeFailure() => Interlocked.Decrement(ref _failuresRemaining) >= 0;

    public void Add(IDomainEvent domainEvent) => _events.Enqueue(domainEvent);
}

internal sealed class TestUser : ICurrentUser
{
    public string? UserId { get; set; }

    public bool IsAuthenticated => UserId is not null;

    public bool HasPermission(string permission) => false;
}

internal sealed class TestTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Now;
}
