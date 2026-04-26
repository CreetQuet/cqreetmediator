namespace CQReetMediator.Abstractions;

public interface IMediator {
    Task SendAsync(IRequest request, CancellationToken ct = default);

    Task<TResponse?> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);

    Task PublishAsync(INotification notification, CancellationToken ct = default);
}
