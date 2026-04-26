namespace CQReetMediator.Abstractions;

public delegate Task RequestHandlerDelegate();

public delegate Task<TResponse?> RequestHandlerDelegate<TResponse>();
