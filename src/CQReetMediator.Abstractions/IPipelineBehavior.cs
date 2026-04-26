namespace CQReetMediator.Abstractions;

public interface IPipelineBehavior<in TRequest> where TRequest : IRequest {
    Task InvokeAsync(TRequest request, RequestHandlerDelegate next, CancellationToken ct);
}

public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse> {
    Task<TResponse?> InvokeAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct);
}
