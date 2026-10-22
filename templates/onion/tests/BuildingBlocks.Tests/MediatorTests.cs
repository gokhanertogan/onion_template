namespace BuildingBlocks.Tests;

using BuildingBlocks.Application;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Messaging;
using BuildingBlocks.Application.Validation;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

[TestClass]
public sealed class MediatorTests
{
    [TestMethod]
    public async Task Send_InvokesHandler_ThroughBehaviorsInOrder()
    {
        var log = new List<string>();
        await using var provider = BuildProvider(services => services
            .AddSingleton(log)
            .AddMediator(o => o
                .RegisterHandlersFromAssemblies(typeof(MediatorTests).Assembly)
                .AddOpenBehavior(typeof(OuterBehavior<,>))
                .AddOpenBehavior(typeof(InnerBehavior<,>))));

        await using var scope = provider.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new Ping("hi"));

        Assert.AreEqual("hi pong", result.Value);
        CollectionAssert.AreEqual(new[] { "outer:before", "inner:before", "handler", "inner:after", "outer:after" }, log);
    }

    [TestMethod]
    public async Task Behaviors_WithUnsatisfiedConstraints_AreSkipped()
    {
        var log = new List<string>();
        await using var provider = BuildProvider(services => services
            .AddSingleton(log)
            .AddMediator(o => o
                .RegisterHandlersFromAssemblies(typeof(MediatorTests).Assembly)
                .AddOpenBehavior(typeof(CommandOnlyBehavior<,>))));

        await using var scope = provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new Ping("hi"));

        Assert.IsFalse(log.Contains("command-only"));
    }

    [TestMethod]
    public async Task ValidationBehavior_ShortCircuits_WithValidationError()
    {
        var log = new List<string>();
        await using var provider = BuildProvider(services => services.AddSingleton(log).AddBuildingBlocksApplication(typeof(MediatorTests).Assembly));

        await using var scope = provider.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new Ping(""));

        Assert.IsTrue(result.IsFailure);
        Assert.IsInstanceOfType<ValidationError>(result.Error);
        Assert.IsFalse(log.Contains("handler"));
    }

    [TestMethod]
    public async Task CachingBehavior_CachesSuccess_ButNeverFailures()
    {
        var counter = new Counter();
        await using var provider = BuildProvider(services => services
            .AddSingleton(counter)
            .AddBuildingBlocksInfrastructure()
            .AddBuildingBlocksApplication(typeof(MediatorTests).Assembly));

        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Assert.AreEqual(1, (await sender.Send(new CachedQuery("ok"))).Value);
        Assert.AreEqual(1, (await sender.Send(new CachedQuery("ok"))).Value);
        Assert.IsTrue((await sender.Send(new CachedQuery("fail"))).IsFailure);
        Assert.IsTrue((await sender.Send(new CachedQuery("fail"))).IsFailure);
        Assert.AreEqual(3, counter.Value);
    }

    [TestMethod]
    public async Task CacheInvalidationBehavior_InvalidatesTags_OnlyAfterSuccess()
    {
        var counter = new Counter();
        await using var provider = BuildProvider(services => services
            .AddSingleton(counter)
            .AddBuildingBlocksInfrastructure()
            .AddBuildingBlocksApplication(typeof(MediatorTests).Assembly));

        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Assert.AreEqual(1, (await sender.Send(new CachedQuery("ok"))).Value);
        Assert.IsTrue((await sender.Send(new InvalidateTestTag(Fail: true))).IsFailure);
        Assert.AreEqual(1, (await sender.Send(new CachedQuery("ok"))).Value);
        Assert.IsTrue((await sender.Send(new InvalidateTestTag(Fail: false))).IsSuccess);
        Assert.AreEqual(2, (await sender.Send(new CachedQuery("ok"))).Value);
    }

    [TestMethod]
    public async Task IdempotencyBehavior_ReplaysTheFirstSuccess_PerUserAndKey()
    {
        var counter = new Counter();
        var user = new SwitchableUser { UserId = "alice" };
        await using var provider = BuildIdempotencyProvider(counter, user);

        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var key = Guid.NewGuid();

        Assert.AreEqual(1, (await sender.Send(new CreateThing(key))).Value);
        Assert.AreEqual(1, (await sender.Send(new CreateThing(key))).Value);
        Assert.AreEqual(2, (await sender.Send(new CreateThing(Guid.NewGuid()))).Value);

        user.UserId = "bob";
        Assert.AreEqual(3, (await sender.Send(new CreateThing(key))).Value);

        Assert.AreEqual(4, (await sender.Send(new CreateThing(null))).Value);
        Assert.AreEqual(5, (await sender.Send(new CreateThing(null))).Value);
    }

    [TestMethod]
    public async Task IdempotencyBehavior_DoesNotStoreFailures()
    {
        var counter = new Counter();
        await using var provider = BuildIdempotencyProvider(counter, new SwitchableUser { UserId = "alice" });

        await using var scope = provider.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var key = Guid.NewGuid();

        Assert.IsTrue((await sender.Send(new CreateThing(key, Fail: true))).IsFailure);
        Assert.IsTrue((await sender.Send(new CreateThing(key, Fail: true))).IsFailure);
        Assert.AreEqual(2, counter.Value);

        Assert.AreEqual(3, (await sender.Send(new CreateThing(key))).Value);
    }

    [TestMethod]
    public async Task IdempotencyBehavior_CoalescesConcurrentDuplicates()
    {
        var counter = new Counter();
        await using var provider = BuildIdempotencyProvider(counter, new SwitchableUser { UserId = "alice" });
        var key = Guid.NewGuid();

        async Task<Result<int>> SendInOwnScope()
        {
            await using var scope = provider.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new CreateThing(key, Delay: TimeSpan.FromMilliseconds(200)));
        }

        var results = await Task.WhenAll(SendInOwnScope(), SendInOwnScope(), SendInOwnScope());

        Assert.AreEqual(1, counter.Value);
        Assert.IsTrue(results.All(r => r.IsSuccess && r.Value == 1));
    }

    [TestMethod]
    public void AddMediator_Throws_ForDuplicateHandlers()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRequestHandler<Ping, Result<string>>, OtherPingHandler<object>>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            services.AddMediator(o => o.RegisterHandlersFromAssemblies(typeof(MediatorTests).Assembly)));
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser, AnonymousUser>();
        services.AddScoped<IUnitOfWork, NoOpUnitOfWork>();
        configure(services);
        services.TryAddSingleton(new Counter());
        services.TryAddSingleton(new List<string>());
        services.TryAddSingleton(new EventLog());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private static ServiceProvider BuildIdempotencyProvider(Counter counter, SwitchableUser user) =>
        BuildProvider(services => services
            .AddSingleton(counter)
            .Replace(ServiceDescriptor.Singleton<ICurrentUser>(user))
            .AddBuildingBlocksInfrastructure()
            .AddBuildingBlocksApplication(typeof(MediatorTests).Assembly));

    public sealed record Ping(string Message) : IQuery<string>;

    public sealed record CreateThing(Guid? IdempotencyKey, bool Fail = false, TimeSpan Delay = default) : ICommand<int>, IIdempotentCommand;

    public sealed record InvalidateTestTag(bool Fail) : ICommand, ICacheInvalidator
    {
        public IReadOnlyCollection<string> CacheTagsToInvalidate => ["test"];
    }

    public sealed record CachedQuery(string Key) : ICachedQuery<int>
    {
        public string CacheKey => $"test:{Key}";

        public IReadOnlyCollection<string> CacheTags => ["test"];

        public TimeSpan? Expiration => TimeSpan.FromMinutes(1);
    }

    public sealed record SomeCommand : ICommand;

    public sealed class Counter
    {
        private int _value;

        public int Value => _value;

        public int Increment() => Interlocked.Increment(ref _value);
    }

    internal sealed class PingHandler(List<string> log) : IQueryHandler<Ping, string>
    {
        public Task<Result<string>> Handle(Ping request, CancellationToken cancellationToken)
        {
            log.Add("handler");
            return Task.FromResult<Result<string>>($"{request.Message} pong");
        }
    }

    // Generic so assembly scanning ignores it; closed manually to simulate a second handler.
    internal sealed class OtherPingHandler<TMarker> : IRequestHandler<Ping, Result<string>>
    {
        public Task<Result<string>> Handle(Ping request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    internal sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(x => x.Message).NotEmpty();
    }

    internal sealed class CachedQueryHandler(Counter counter) : IQueryHandler<CachedQuery, int>
    {
        public Task<Result<int>> Handle(CachedQuery request, CancellationToken cancellationToken)
        {
            var calls = counter.Increment();
            return Task.FromResult<Result<int>>(request.Key == "fail" ? Error.Failure("Test.Fail", "fail") : calls);
        }
    }

    internal sealed class CreateThingHandler(Counter counter) : ICommandHandler<CreateThing, int>
    {
        public async Task<Result<int>> Handle(CreateThing request, CancellationToken cancellationToken)
        {
            await Task.Delay(request.Delay, cancellationToken);
            var calls = counter.Increment();
            return request.Fail ? Error.Failure("Test.Fail", "fail") : calls;
        }
    }

    internal sealed class InvalidateTestTagHandler : ICommandHandler<InvalidateTestTag>
    {
        public Task<Result> Handle(InvalidateTestTag request, CancellationToken cancellationToken) =>
            Task.FromResult(request.Fail ? Result.Failure(Error.Failure("Test.Fail", "fail")) : Result.Success());
    }

    internal sealed class SomeCommandHandler : ICommandHandler<SomeCommand>
    {
        public Task<Result> Handle(SomeCommand request, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
    }

    internal sealed class OuterBehavior<TRequest, TResponse>(List<string> log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            log.Add("outer:before");
            var response = await next(cancellationToken);
            log.Add("outer:after");
            return response;
        }
    }

    internal sealed class InnerBehavior<TRequest, TResponse>(List<string> log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            log.Add("inner:before");
            var response = await next(cancellationToken);
            log.Add("inner:after");
            return response;
        }
    }

    internal sealed class CommandOnlyBehavior<TRequest, TResponse>(List<string> log) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IBaseCommand
    {
        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            log.Add("command-only");
            return next(cancellationToken);
        }
    }

    private sealed class AnonymousUser : ICurrentUser
    {
        public bool IsAuthenticated => false;

        public string? UserId => null;

        public bool HasPermission(string permission) => false;
    }

    private sealed class SwitchableUser : ICurrentUser
    {
        public string? UserId { get; set; }

        public bool IsAuthenticated => UserId is not null;

        public bool HasPermission(string permission) => false;
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
