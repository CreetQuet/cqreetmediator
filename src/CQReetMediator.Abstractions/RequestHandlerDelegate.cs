namespace CQReetMediator.Abstractions;

/// <summary>
/// Delegate representing the continuation of a pipeline.
/// Invoking this executes the next behavior or the final handler.
/// </summary>
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>();
public delegate Task<TResponse> RequestHandlerDelegateAsync<TResponse>();