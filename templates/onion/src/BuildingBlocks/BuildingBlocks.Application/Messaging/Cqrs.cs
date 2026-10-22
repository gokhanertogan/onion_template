namespace BuildingBlocks.Application.Messaging;

using BuildingBlocks.Domain.Results;

/// <summary>Marker shared by all commands; used by behaviors such as the unit of work.</summary>
#pragma warning disable CA1040
public interface IBaseCommand;
#pragma warning restore CA1040

/// <summary>A state-changing request without a return value.</summary>
public interface ICommand : IRequest<Result>, IBaseCommand;

/// <summary>A state-changing request that returns <typeparamref name="TResponse"/>.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;

/// <summary>A side-effect free read request.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
