namespace CQReetMediator.Abstractions;

public interface IQuery : IRequest;

public interface IQuery<TResponse> : IRequest<TResponse>;
