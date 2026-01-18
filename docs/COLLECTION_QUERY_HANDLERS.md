# Collection Query Handlers

This document covers how to use CQReetMediator's query handlers with collection return types while maintaining zero-allocation principles.

## Overview

CQReetMediator fully supports queries that return collections such as `IReadOnlyList<T>`, `List<T>`, `IEnumerable<T>`, and arrays. The existing `IQueryHandler<TQuery, TResponse>` and `IAsyncQueryHandler<TQuery, TResponse>` interfaces work seamlessly with any response type, including collections.

> **Key Insight**: No special interfaces are required for collection queries. The generic `TResponse` type parameter already supports any collection type.

## Defining Collection Queries

### Query Definition

```csharp
using CQReetMediator.Abstractions;

// Scalar query - returns a single entity
public record GetUserByIdQuery(int UserId) : IQuery<User>;

// Collection query - returns multiple entities
public record GetAllUsersQuery : IQuery<IReadOnlyList<User>>;

// Collection query with filtering
public record GetActiveUsersQuery(bool IsActive) : IQuery<IReadOnlyList<User>>;

// Array-based query (optimal for fixed-size results)
public record GetUserIdsQuery : IQuery<int[]>;
```

## Implementing Collection Query Handlers

### Synchronous Handler (ValueTask)

Use `IQueryHandler<TRequest, TResponse>` for fast, in-memory operations:

```csharp
public class GetAllUsersHandler : IQueryHandler<GetAllUsersQuery, IReadOnlyList<User>>
{
    private readonly IUserRepository _repository;

    public GetAllUsersHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<IReadOnlyList<User>?> HandleAsync(GetAllUsersQuery query, CancellationToken ct)
    {
        // Return array as IReadOnlyList - arrays implement this interface natively
        User[] users = _repository.GetAllCached();
        return new ValueTask<IReadOnlyList<User>?>(users);
    }
}
```

### Asynchronous Handler (Task)

Use `IAsyncQueryHandler<TRequest, TResponse>` for I/O-bound operations:

```csharp
public class GetOrdersByCustomerHandler : IAsyncQueryHandler<GetOrdersByCustomerQuery, IReadOnlyList<Order>>
{
    private readonly IOrderRepository _repository;

    public GetOrdersByCustomerHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<Order>?> HandleAsync(GetOrdersByCustomerQuery query, CancellationToken ct)
    {
        var orders = await _repository.GetByCustomerIdAsync(query.CustomerId, ct);
        return orders;
    }
}
```

## Zero-Allocation Best Practices

### Collection Type Recommendations

| Collection Type | Allocation Profile | Recommendation |
|-----------------|-------------------|----------------|
| `T[]` (Array) | Single allocation | ✅ **Best** - Minimal overhead, implements `IReadOnlyList<T>` |
| `IReadOnlyList<T>` (array-backed) | Single allocation | ✅ **Recommended** - Clean API contract |
| `List<T>` | Single + potential resize | ⚠️ Use only when caller needs mutation |
| `IEnumerable<T>` | Depends on implementation | ⚠️ Risk of deferred execution issues |

### Code Patterns

#### ✅ Optimal: Return Array as IReadOnlyList

```csharp
public ValueTask<IReadOnlyList<User>?> HandleAsync(GetAllUsersQuery query, CancellationToken ct)
{
    User[] users = FetchUsers();
    return new ValueTask<IReadOnlyList<User>?>(users); // Zero overhead cast
}
```

#### ✅ Empty Collections: Use Array.Empty<T>

```csharp
public ValueTask<IReadOnlyList<User>?> HandleAsync(GetActiveUsersQuery query, CancellationToken ct)
{
    if (!HasActiveUsers())
    {
        return new ValueTask<IReadOnlyList<User>?>(Array.Empty<User>()); // Cached, no allocation
    }
    
    return new ValueTask<IReadOnlyList<User>?>(GetActiveUsers());
}
```

#### ✅ High-Throughput: Use ArrayPool

```csharp
public ValueTask<int[]?> HandleAsync(GetUserIdsQuery query, CancellationToken ct)
{
    int count = GetExpectedCount();
    int[] buffer = ArrayPool<int>.Shared.Rent(count);
    
    try
    {
        int actualCount = FillUserIds(buffer);
        int[] result = buffer.AsSpan(0, actualCount).ToArray();
        return new ValueTask<int[]?>(result);
    }
    finally
    {
        ArrayPool<int>.Shared.Return(buffer);
    }
}
```

#### ❌ Avoid: LINQ in Hot Paths

```csharp
// BAD - Multiple allocations
public ValueTask<IReadOnlyList<User>?> HandleAsync(GetActiveUsersQuery query, CancellationToken ct)
{
    var users = _repository.GetAll()
        .Where(u => u.IsActive)     // Allocates iterator
        .Select(u => MapToDto(u))   // Allocates iterator
        .ToList();                  // Allocates List<T>
    
    return new ValueTask<IReadOnlyList<User>?>(users);
}
```

```csharp
// GOOD - Single allocation
public ValueTask<IReadOnlyList<User>?> HandleAsync(GetActiveUsersQuery query, CancellationToken ct)
{
    User[] allUsers = _repository.GetAllCached();
    int count = 0;
    
    // Count first
    for (int i = 0; i < allUsers.Length; i++)
    {
        if (allUsers[i].IsActive) count++;
    }
    
    if (count == 0) return new ValueTask<IReadOnlyList<User>?>(Array.Empty<User>());
    
    // Single allocation
    User[] result = new User[count];
    int index = 0;
    
    for (int i = 0; i < allUsers.Length; i++)
    {
        if (allUsers[i].IsActive)
        {
            result[index++] = allUsers[i];
        }
    }
    
    return new ValueTask<IReadOnlyList<User>?>(result);
}
```

## When to Use Collection vs Scalar Handlers

| Scenario | Return Type | Reason |
|----------|-------------|--------|
| Find single entity by ID | `User` | Single result expected |
| Check if entity exists | `bool` | Simple boolean result |
| Get all entities | `IReadOnlyList<User>` | Multiple results |
| Get paginated results | `IReadOnlyList<User>` | Subset of entities |
| Get entity count | `int` | Scalar aggregation |
| Get grouped data | `IReadOnlyList<GroupResult>` | Aggregated collection |

## Performance Characteristics

### Handler Dispatch

- **O(1) lookup**: Handler resolution uses pre-compiled wrapper dictionaries (`FrozenDictionary`)
- **No reflection at runtime**: All type resolution happens during application startup
- **No virtual dispatch penalty**: Collection types have identical dispatch cost to scalar types

### Memory Characteristics

- **ValueTask**: Enables synchronous completion without `Task` allocation
- **No boxing**: Generic constraints prevent interface boxing
- **No iterator allocation**: Direct array access preferred over `IEnumerable<T>`

## Registration

Collection query handlers are registered automatically via assembly scanning:

```csharp
services.AddCQReetMediator(typeof(GetAllUsersHandler));
```

The DI extension scans for all `IQueryHandler<,>` and `IAsyncQueryHandler<,>` implementations and registers them appropriately.

## Usage in Application Code

```csharp
public class UserController
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Scalar query
    public async Task<User?> GetUser(int id)
    {
        return await _mediator.Send(new GetUserByIdQuery(id));
    }

    // Collection query (sync-friendly)
    public async Task<IReadOnlyList<User>?> GetAllUsers()
    {
        return await _mediator.Send(new GetAllUsersQuery());
    }

    // Collection query (async/I/O)
    public async Task<IReadOnlyList<Order>?> GetOrders(int customerId)
    {
        return await _mediator.SendAsync(new GetOrdersByCustomerQuery(customerId));
    }
}
```

## Summary

- **No new interfaces needed**: Use existing `IQueryHandler<TQuery, TResult>` with any collection type
- **Zero-allocation focus**: Return arrays as `IReadOnlyList<T>`, avoid LINQ, use `ArrayPool<T>`
- **Same dispatch performance**: Collections have identical handler resolution cost to scalars
- **Full backward compatibility**: Existing scalar handlers continue working unchanged
