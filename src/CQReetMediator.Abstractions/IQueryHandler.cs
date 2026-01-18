namespace CQReetMediator.Abstractions;

/// <summary>
/// Handler for queries with return values using ValueTask.
/// Optimized for synchronous or fast-completing operations.
/// </summary>
/// <typeparam name="TRequest">The type of query to handle. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of expected response. Can be scalar types or collections.</typeparam>
/// <remarks>
/// <para><strong>Collection Query Support:</strong></para>
/// <para>
/// TResponse can be any collection type. Handlers returning collections work identically to scalar handlers:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="System.Collections.Generic.IReadOnlyList{T}"/> - Recommended for read-only immutable results</description></item>
///   <item><description><see cref="System.Collections.Generic.List{T}"/> - When the caller needs to modify the collection</description></item>
///   <item><description><see cref="System.Collections.Generic.IEnumerable{T}"/> - For deferred/streaming evaluation</description></item>
///   <item><description><c>T[]</c> - Optimal for fixed-size results</description></item>
/// </list>
/// <para><strong>Zero-Allocation Guidelines:</strong></para>
/// <list type="bullet">
///   <item><description>Prefer returning arrays cast to <c>IReadOnlyList&lt;T&gt;</c> - zero interface overhead</description></item>
///   <item><description>Avoid LINQ operators (ToList, Select, Where) in hot paths - they allocate</description></item>
///   <item><description>Use <see cref="System.Buffers.ArrayPool{T}"/> for large, frequently-created collections</description></item>
///   <item><description>Consider returning <see cref="ReadOnlyMemory{T}"/> or <see cref="ReadOnlySpan{T}"/> wrappers for zero-copy scenarios</description></item>
///   <item><description>Cache empty collections using <see cref="Array.Empty{T}"/> instead of creating new empty arrays</description></item>
/// </list>
/// <para><strong>Performance Characteristics:</strong></para>
/// <list type="bullet">
///   <item><description>No additional dispatch overhead for collection types vs scalar types</description></item>
///   <item><description>ValueTask enables synchronous completion without Task allocation</description></item>
///   <item><description>Handler resolution uses O(1) dictionary lookup via pre-compiled wrappers</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Collection query handler returning IReadOnlyList (backed by array)
/// public class GetAllUsersHandler : IQueryHandler&lt;GetAllUsersQuery, IReadOnlyList&lt;User&gt;&gt;
/// {
///     private readonly IUserRepository _repository;
///     
///     public GetAllUsersHandler(IUserRepository repository) => _repository = repository;
///     
///     public ValueTask&lt;IReadOnlyList&lt;User&gt;?&gt; HandleAsync(GetAllUsersQuery query, CancellationToken ct)
///     {
///         // Return array as IReadOnlyList - zero overhead
///         User[] users = _repository.GetAllUsers();
///         return new ValueTask&lt;IReadOnlyList&lt;User&gt;?&gt;(users);
///     }
/// }
/// 
/// // Zero-allocation handler using ArrayPool
/// public class GetUserIdsHandler : IQueryHandler&lt;GetUserIdsQuery, int[]&gt;
/// {
///     public ValueTask&lt;int[]?&gt; HandleAsync(GetUserIdsQuery query, CancellationToken ct)
///     {
///         // For high-throughput: consider ArrayPool&lt;int&gt;.Shared.Rent()
///         return new ValueTask&lt;int[]?&gt;(new[] { 1, 2, 3 });
///     }
/// }
/// </code>
/// </example>
public interface IQueryHandler<in TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>;

/// <summary>
/// Async handler for queries with return values using Task.
/// Designed for I/O-bound operations such as database queries or HTTP calls.
/// </summary>
/// <typeparam name="TRequest">The type of query to handle. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of expected response. Can be scalar types or collections.</typeparam>
/// <remarks>
/// <para><strong>When to use IAsyncQueryHandler vs IQueryHandler:</strong></para>
/// <list type="bullet">
///   <item><description>Use <see cref="IAsyncQueryHandler{TRequest,TResponse}"/> for database queries, HTTP calls, file I/O</description></item>
///   <item><description>Use <see cref="IQueryHandler{TRequest,TResponse}"/> for in-memory lookups, cached data, CPU-bound computations</description></item>
/// </list>
/// <para><strong>Collection Query Support:</strong></para>
/// <para>
/// Works identically to <see cref="IQueryHandler{TRequest,TResponse}"/> for collection return types.
/// All zero-allocation guidelines apply equally.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Async collection query handler for database operations
/// public class GetOrdersByCustomerHandler : IAsyncQueryHandler&lt;GetOrdersByCustomerQuery, IReadOnlyList&lt;Order&gt;&gt;
/// {
///     private readonly IOrderRepository _repository;
///     
///     public async Task&lt;IReadOnlyList&lt;Order&gt;?&gt; HandleAsync(GetOrdersByCustomerQuery query, CancellationToken ct)
///     {
///         var orders = await _repository.GetByCustomerIdAsync(query.CustomerId, ct);
///         return orders; // Assuming repository returns IReadOnlyList or array
///     }
/// }
/// </code>
/// </example>
public interface IAsyncQueryHandler<in TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>;