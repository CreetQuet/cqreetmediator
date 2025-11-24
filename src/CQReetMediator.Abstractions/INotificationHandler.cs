namespace CQReetMediator.Abstractions;

/// <summary>
/// Handler for domain notifications/events
/// </summary>
/// <typeparam name="TNotification">The type of notification to handle</typeparam>
public interface INotificationHandler<in TNotification> where TNotification : INotification {
    /// <summary>
    /// Processes a notification asynchronously
    /// </summary>
    /// <param name="notification">The notification to handle</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A Task representing the async operation</returns>
    Task HandleAsync(TNotification notification, CancellationToken ct);
}