namespace BuildingBlocks.Tests;

using BuildingBlocks.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

[TestClass]
public sealed class OutboxProcessorTests
{
    [TestMethod]
    public async Task ProcessBatch_DispatchesPendingMessages_Once()
    {
        await using var host = await PersistenceTestHost.CreateAsync();
        var note = await host.AddNoteAsync("hello");
        var processor = CreateProcessor(host);
        var settings = new OutboxOptions();

        Assert.AreEqual(1, await processor.ProcessBatchAsync(settings, CancellationToken.None));
        Assert.AreEqual(0, await processor.ProcessBatchAsync(settings, CancellationToken.None));

        var dispatched = (NoteCreated)host.Events.Events.Single();
        Assert.AreEqual(note.Id, dispatched.NoteId);

        var message = await SingleMessageAsync(host);
        Assert.AreEqual(host.Time.Now, message.ProcessedOnUtc);
        Assert.AreEqual(1, message.Attempts);
        Assert.IsNull(message.Error);
    }

    [TestMethod]
    public async Task ProcessBatch_RetriesAFailedMessage_AndClearsTheError()
    {
        await using var host = await PersistenceTestHost.CreateAsync();
        await host.AddNoteAsync("hello");
        host.Events.FailNext(1);
        var processor = CreateProcessor(host);
        var settings = new OutboxOptions();

        await processor.ProcessBatchAsync(settings, CancellationToken.None);
        var failed = await SingleMessageAsync(host);
        Assert.IsNull(failed.ProcessedOnUtc);
        StringAssert.Contains(failed.Error, "Handler failure");

        await processor.ProcessBatchAsync(settings, CancellationToken.None);
        var succeeded = await SingleMessageAsync(host);
        Assert.IsNotNull(succeeded.ProcessedOnUtc);
        Assert.AreEqual(2, succeeded.Attempts);
        Assert.IsNull(succeeded.Error);
        Assert.AreEqual(1, host.Events.Events.Count);
    }

    [TestMethod]
    public async Task ProcessBatch_StopsRetrying_AfterMaxAttempts()
    {
        await using var host = await PersistenceTestHost.CreateAsync();
        await host.AddNoteAsync("poison");
        host.Events.FailNext(int.MaxValue);
        var processor = CreateProcessor(host);
        var settings = new OutboxOptions { MaxAttempts = 2 };

        Assert.AreEqual(1, await processor.ProcessBatchAsync(settings, CancellationToken.None));
        Assert.AreEqual(1, await processor.ProcessBatchAsync(settings, CancellationToken.None));
        Assert.AreEqual(0, await processor.ProcessBatchAsync(settings, CancellationToken.None));

        var message = await SingleMessageAsync(host);
        Assert.AreEqual(2, message.Attempts);
        Assert.IsNull(message.ProcessedOnUtc);
        Assert.AreEqual(0, host.Events.Events.Count);
    }

    [TestMethod]
    public async Task ProcessBatch_RefusesTypesThatAreNotDomainEvents()
    {
        await using var host = await PersistenceTestHost.CreateAsync();
        await using (var scope = host.CreateScope())
        {
            var db = PersistenceTestHost.Db(scope);
            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.CreateVersion7(),
                Type = typeof(FileInfo).AssemblyQualifiedName!,
                Content = """{"originalPath":"/etc/passwd"}""",
                OccurredOnUtc = host.Time.Now,
            });
            await db.SaveChangesAsync();
        }

        await CreateProcessor(host).ProcessBatchAsync(new OutboxOptions(), CancellationToken.None);

        var message = await SingleMessageAsync(host);
        Assert.IsNull(message.ProcessedOnUtc);
        StringAssert.Contains(message.Error, "disallowed");
        Assert.AreEqual(0, host.Events.Events.Count);
    }

    private static OutboxProcessor<NotesDbContext> CreateProcessor(PersistenceTestHost host) =>
        new(host.ScopeFactory, Options.Create(new OutboxOptions()), host.Time, NullLogger<OutboxProcessor<NotesDbContext>>.Instance);

    private static async Task<OutboxMessage> SingleMessageAsync(PersistenceTestHost host)
    {
        await using var scope = host.CreateScope();
        return await PersistenceTestHost.Db(scope).OutboxMessages.AsNoTracking().SingleAsync();
    }
}
