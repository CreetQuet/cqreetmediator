namespace CQReetMediator.Abstractions;

/// <summary>
/// Represents a query in the CQRS pattern (without return value)
/// </summary>
public interface IQuery : IRequest;

/// <summary>
/// Represents a query in the CQRS pattern that returns a response
/// </summary>
/// <typeparam name="TResponse">The type of response data returned by the query</typeparam>
/// <remarks>
/// <para>
/// <strong>Collection Support:</strong> TResponse can be any type, including collection types such as:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="System.Collections.Generic.IReadOnlyList{T}"/> - Recommended for read-only collections (zero-overhead with arrays)</description></item>
///   <item><description><see cref="System.Collections.Generic.List{T}"/> - For mutable collections when modification is required</description></item>
///   <item><description><see cref="System.Collections.Generic.IEnumerable{T}"/> - For deferred enumeration scenarios</description></item>
///   <item><description><c>T[]</c> - Best for fixed-size results with minimal allocation</description></item>
/// </list>
/// <para>
/// <strong>Zero-Allocation Best Practices:</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Return arrays as <see cref="System.Collections.Generic.IReadOnlyList{T}"/> - arrays implement this interface natively</description></item>
///   <item><description>Use <see cref="System.Buffers.ArrayPool{T}"/> for high-throughput scenarios requiring pooled buffers</description></item>
///   <item><description>Avoid LINQ operators that allocate (ToList, ToArray, Select, Where) in hot paths</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Scalar query
/// public record GetUserByIdQuery(int Id) : IQuery&lt;User&gt;;
/// 
/// // Collection query returning read-only list
/// public record GetAllUsersQuery : IQuery&lt;IReadOnlyList&lt;User&gt;&gt;;
/// 
/// // Collection query returning array (zero-allocation friendly)
/// public record GetActiveUserIdsQuery : IQuery&lt;int[]&gt;;
/// </code>
/// </example>
public interface IQuery<TResponse> : IRequest<TResponse>;