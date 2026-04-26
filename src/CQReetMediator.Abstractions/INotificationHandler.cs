namespace CQReetMediator.Abstractions;

public interface INotificationHandler<in TNotification> where TNotification : INotification {
    Task HandleAsync(TNotification notification, CancellationToken ct);
}
