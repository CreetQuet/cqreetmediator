namespace CQReetMediator.Abstractions;

/// <summary>
/// Main contract for the mediator that coordinates CQRS requests
/// </summary>
/// <remarks>
/// <para>
/// The mediator pattern is used to reduce coupling between components by encapsulating request/response logic.
/// This implementation supports both synchronous-friendly (ValueTask) and asynchronous (Task) operations,
/// following CQRS principles with clear separation between commands and queries.
/// </para>
/// <para>
/// <strong>Usage Guidelines:</strong>
/// <list type="bullet">
/// <item>Use <see cref="Send{TResponse}"/> for CPU-bound or fast operations</item>
/// <item>Use <see cref="SendAsync{TResponse}"/> for I/O-bound operations</item>
/// <item>Use <see cref="PublishAsync"/> for domain events and notifications</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // For commands with response
/// var result = await mediator.Send(new CreateUserCommand { Name = "John" });
/// 
/// // For queries
/// var users = await mediator.Send(new GetUsersQuery { Active = true });
/// 
/// // For domain events
/// await mediator.PublishAsync(new UserCreatedEvent { UserId = result.Id });
/// </code>
/// </example>
public interface IMediator {
    /// <summary>
    /// Sends a request with return value in an optimized way using ValueTask
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the request</typeparam>
    /// <param name="request">The request to process. Can be a command or query implementing <see cref="IRequest{TResponse}"/></param>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <returns>A ValueTask containing the response of type <typeparamref name="TResponse"/></returns>
    /// <remarks>
    /// <para>
    /// This method is optimized for synchronous or fast-completing operations. It uses ValueTask which 
    /// provides better performance for operations that complete synchronously or very quickly.
    /// </para>
    /// <para>
    /// <strong>When to use:</strong>
    /// <list type="bullet">
    /// <item>In-memory operations</item>
    /// <item>CPU-bound computations</item>
    /// <item>Fast database queries with cached data</item>
    /// <item>Operations that typically complete immediately</item>
    /// </list>
    /// </para>
    /// <para>
    /// The method will execute any configured pipeline behaviors in the order they are registered,
    /// providing cross-cutting concerns like validation, logging, or caching.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when no handler is registered for the request type</exception>
    /// <example>
    /// <code>
    /// // Synchronous-friendly query
    /// var product = await mediator.Send(new GetProductByIdQuery { Id = 123 });
    /// 
    /// // Command with response
    /// var createdId = await mediator.Send(new CreateProductCommand { Name = "Laptop" });
    /// </code>
    /// </example>
    ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);

    /// <summary>
    /// Sends a request with return value asynchronously using Task for I/O-intensive operations
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the request</typeparam>
    /// <param name="request">The request to process. Can be a command or query implementing <see cref="IRequest{TResponse}"/></param>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <returns>A Task containing the response of type <typeparamref name="TResponse"/></returns>
    /// <remarks>
    /// <para>
    /// This method is designed for I/O-bound operations that naturally involve asynchronous execution.
    /// It uses Task which is optimized for operations that require true asynchronous work.
    /// </para>
    /// <para>
    /// <strong>When to use:</strong>
    /// <list type="bullet">
    /// <item>Database operations</item>
    /// <item>HTTP API calls</item>
    /// <item>File I/O operations</item>
    /// <item>Any operation that involves waiting for external resources</item>
    /// </list>
    /// </para>
    /// <para>
    /// Like <see cref="Send{TResponse}"/>, this method executes pipeline behaviors but is specifically
    /// designed for handlers that implement <see cref="IAsyncRequestHandler{TRequest, TResponse}"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when no handler is registered for the request type</exception>
    /// <example>
    /// <code>
    /// // I/O-intensive query
    /// var users = await mediator.SendAsync(new GetUsersFromDatabaseQuery { Department = "IT" });
    /// 
    /// // Command involving external API call
    /// var result = await mediator.SendAsync(new ProcessPaymentCommand { Amount = 100.00m });
    /// </code>
    /// </example>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);

    /// <summary>
    /// Publishes a notification to all registered handlers asynchronously
    /// </summary>
    /// <param name="notification">The notification to publish, implementing <see cref="INotification"/></param>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <returns>A Task that completes when all handlers have processed the notification</returns>
    /// <remarks>
    /// <para>
    /// This method is used for implementing domain events and notifications where multiple handlers
    /// may need to react to the same event. Unlike requests, notifications can have zero to many handlers.
    /// </para>
    /// <para>
    /// <strong>Key characteristics:</strong>
    /// <list type="bullet">
    /// <item>Multiple handlers can be registered for the same notification type</item>
    /// <item>Handlers are executed in the order they are registered</item>
    /// <item>Exceptions in one handler do not stop execution of other handlers</item>
    /// <item>There is no return value from handlers</item>
    /// </list>
    /// </para>
    /// <para>
    /// Notifications are fire-and-forget by design and should not be used when a response is required.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="notification"/> is null</exception>
    /// <example>
    /// <code>
    /// // Publish a domain event
    /// await mediator.PublishAsync(new OrderShippedEvent { OrderId = orderId, ShippingDate = DateTime.UtcNow });
    /// 
    /// // Multiple handlers can process the same event
    /// await mediator.PublishAsync(new UserRegisteredEvent { UserId = userId, Email = email });
    /// </code>
    /// </example>
    Task PublishAsync(INotification notification, CancellationToken ct = default);
}