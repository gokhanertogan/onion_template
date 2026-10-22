namespace BuildingBlocks.Tests;

using BuildingBlocks.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

[TestClass]
public sealed class PersistenceTests
{
    [TestMethod]
    public async Task SoftDelete_KeepsTheRow_AndTheQueryFilterHidesIt()
    {
        await using var host = await PersistenceTestHost.CreateAsync();
        var note = await host.AddNoteAsync("hello");
        host.User.UserId = "deleter";

        await using (var scope = host.CreateScope())
        {
            var db = PersistenceTestHost.Db(scope);
            db.Notes.Remove(await db.Notes.SingleAsync(n => n.Id == note.Id));
            await db.SaveChangesAsync();
        }

        await using var verify = host.CreateScope();
        var verifyDb = PersistenceTestHost.Db(verify);
        Assert.IsFalse(await verifyDb.Notes.AnyAsync());

        var deleted = await verifyDb.Notes.IgnoreQueryFilters().SingleAsync(n => n.Id == note.Id);
        Assert.IsTrue(deleted.IsDeleted);
        Assert.AreEqual(host.Time.Now, deleted.DeletedAtUtc);
        Assert.AreEqual("deleter", deleted.DeletedBy);
    }

    [TestMethod]
    public async Task Auditing_StampsCreatorAndModifier_AndCreationCannotBeOverwritten()
    {
        await using var host = await PersistenceTestHost.CreateAsync();
        var createdAt = host.Time.Now;
        host.User.UserId = "alice";
        var note = await host.AddNoteAsync("v1");

        host.Time.Now = createdAt.AddHours(1);
        host.User.UserId = "bob";
        await using (var scope = host.CreateScope())
        {
            var db = PersistenceTestHost.Db(scope);
            var loaded = await db.Notes.SingleAsync(n => n.Id == note.Id);
            loaded.Edit("v2");
            db.Entry(loaded).Property(n => n.CreatedBy).CurrentValue = "mallory";
            await db.SaveChangesAsync();
        }

        await using var verify = host.CreateScope();
        var saved = await PersistenceTestHost.Db(verify).Notes.SingleAsync(n => n.Id == note.Id);
        Assert.AreEqual(createdAt, saved.CreatedAtUtc);
        Assert.AreEqual("alice", saved.CreatedBy);
        Assert.AreEqual(host.Time.Now, saved.ModifiedAtUtc);
        Assert.AreEqual("bob", saved.ModifiedBy);
    }

    [TestMethod]
    public async Task Versioning_RejectsAConcurrentUpdate()
    {
        await using var host = await PersistenceTestHost.CreateAsync();
        var note = await host.AddNoteAsync("v1");
        Assert.AreNotEqual(Guid.Empty, note.Version);

        await using var first = host.CreateScope();
        await using var second = host.CreateScope();
        var firstDb = PersistenceTestHost.Db(first);
        var secondDb = PersistenceTestHost.Db(second);
        var firstCopy = await firstDb.Notes.SingleAsync(n => n.Id == note.Id);
        var secondCopy = await secondDb.Notes.SingleAsync(n => n.Id == note.Id);

        firstCopy.Edit("first");
        await firstDb.SaveChangesAsync();
        Assert.AreNotEqual(note.Version, firstCopy.Version);

        secondCopy.Edit("second");
        await Assert.ThrowsExactlyAsync<ConcurrencyException>(() => secondDb.SaveChangesAsync());
    }

    [TestMethod]
    public async Task Outbox_StoresDomainEvents_WithTheStateChange()
    {
        await using var host = await PersistenceTestHost.CreateAsync();
        var note = await host.AddNoteAsync("hello");

        Assert.AreEqual(0, note.DomainEvents.Count);

        await using var verify = host.CreateScope();
        var message = await PersistenceTestHost.Db(verify).OutboxMessages.SingleAsync();
        StringAssert.Contains(message.Type, nameof(NoteCreated));
        StringAssert.Contains(message.Content, "hello");
        Assert.AreEqual(host.Time.Now, message.OccurredOnUtc);
        Assert.IsNull(message.ProcessedOnUtc);
    }

    [TestMethod]
    public async Task Outbox_RolledBackTransaction_LeavesNoMessage()
    {
        await using var host = await PersistenceTestHost.CreateAsync();

        await using (var scope = host.CreateScope())
        {
            var db = PersistenceTestHost.Db(scope);
            await using var transaction = await db.Database.BeginTransactionAsync();
            db.Notes.Add(new Note(Guid.CreateVersion7(), "never committed"));
            await db.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verify = host.CreateScope();
        var verifyDb = PersistenceTestHost.Db(verify);
        Assert.IsFalse(await verifyDb.Notes.IgnoreQueryFilters().AnyAsync());
        Assert.IsFalse(await verifyDb.OutboxMessages.AnyAsync());
    }
}
