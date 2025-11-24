namespace CQReetMediator.Abstractions;

/// <summary>
/// Represents a command in the CQRS pattern (without return value)
/// </summary>
public interface ICommand : IRequest;

/// <summary>
/// Represents a command in the CQRS pattern that returns a response
/// </summary>
/// <typeparam name="TRequest">The type of command data</typeparam>
public interface ICommand<TRequest> : IRequest<TRequest>;