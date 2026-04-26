<div align="center">

# ⚡️ CQReetMediator

### Zero-Allocation, High-Performance CQRS Mediator for .NET 10

<br/>

[![Build](https://img.shields.io/github/actions/workflow/status/CreetQuet/CQReetMediator/ci.yml?label=Build&style=for-the-badge)]()
[![Tests](https://img.shields.io/github/actions/workflow/status/CreetQuet/CQReetMediator/tests.yml?label=Tests&style=for-the-badge)]()
[![NuGet](https://img.shields.io/nuget/v/CQReetMediator.svg?style=for-the-badge&label=NuGet)]()
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)]()
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple.svg?style=for-the-badge)]()

<br/>

**Ultra-light mediator library for high-throughput CQRS architectures.**
**0 Bytes allocation on the hot path. Zero reflection at runtime. AOT-ready.**

</div>

---

## Features

| Capability | Description |
|:---|:---|
| **Zero-Allocation Hot Path** | 0 bytes when no pipelines are present. Sealed wrappers, no virtual dispatch overhead. |
| **AOT-Ready** | No `System.Reflection` at runtime. All wrappers are pre-compiled at DI registration. `FrozenDictionary` for O(1) lookups. |
| **Unified Task API** | Single `Task`-based contract. No `ValueTask`/`Task` duality. Clean, predictable async model. |
| **Pipeline Behaviors** | Extensible interceptor chain for validation, logging, transactions, caching. Open generic support. |
| **Strict CancellationToken** | `CancellationToken` propagated through every handler, pipeline, and notification dispatch. |
| **Commands vs Queries vs Events** | `ICommand` / `ICommand<T>`, `IQuery` / `IQuery<T>`, `INotification` with clear semantic contracts. |
| **Void Requests** | `IRequest`, `ICommand`, `IQuery` without return value. Full pipeline support. |
| **Notifications** | Fire-and-forget event publishing with multiple sequential handlers. |
| **Collection Queries** | `IReadOnlyList<T>`, `List<T>`, `T[]`, `IEnumerable<T>` as TResponse with zero overhead. |

---

## Benchmarks

CQReetMediator is engineered to be as close to a direct method call as possible.

| Scenario | Throughput | Latency (Avg) | Memory |
|:---|:---|:---|:---|
| **Direct Call (Baseline)** | ~60.2M req/s | 0.016 us | 0 B |
| **CQReetMediator w/DI (Warm)** | ~5.3M req/s | 0.185 us | 64 B |
| **CQReetMediator w/DI (Cold)** | ~3.1M req/s | 0.322 us | 64 B |

> 64 bytes comes from the DI container resolution, not the mediator.

---

## Installation

```bash
dotnet add package CQReetMediator
dotnet add package CQReetMediator.DependencyInjection
```

---

## Quick Start

### 1. Define Requests

```csharp
// Command with response
public sealed record CreateUserCommand(string Name) : ICommand<Guid>;

// Void command
public sealed record DeleteUserCommand(Guid Id) : ICommand;

// Query with response
public sealed record GetUserQuery(Guid Id) : IQuery<UserDto>;

// Void query (health checks, warm-up, etc.)
public sealed record WarmUpCacheQuery : IQuery;
```

### 2. Implement Handlers

All handlers return `Task`. No `ValueTask` ambiguity.

```csharp
public sealed class CreateUserHandler : ICommandHandler<CreateUserCommand, Guid>
{
    public Task<Guid?> HandleAsync(CreateUserCommand request, CancellationToken ct)
        => Task.FromResult<Guid?>(Guid.NewGuid());
}

public sealed class DeleteUserHandler : ICommandHandler<DeleteUserCommand>
{
    public Task HandleAsync(DeleteUserCommand request, CancellationToken ct)
    {
        // delete logic
        return Task.CompletedTask;
    }
}

public sealed class GetUserHandler : IQueryHandler<GetUserQuery, UserDto>
{
    public async Task<UserDto?> HandleAsync(GetUserQuery request, CancellationToken ct)
    {
        // fetch from database
        return new UserDto(request.Id, "Garrosh");
    }
}
```

### 3. Register in DI

Automatic assembly scanning registers all handlers, pipelines, and builds the optimized registry.

```csharp
builder.Services.AddCQReetMediator(typeof(Program));
```

### 4. Use It

```csharp
app.MapPost("/users", async (IMediator mediator) =>
{
    var id = await mediator.SendAsync(new CreateUserCommand("Garrosh"));
    return Results.Ok(id);
});

app.MapDelete("/users/{id}", async (Guid id, IMediator mediator) =>
{
    await mediator.SendAsync(new DeleteUserCommand(id));
    return Results.NoContent();
});

app.MapGet("/users/{id}", async (Guid id, IMediator mediator) =>
{
    var user = await mediator.SendAsync(new GetUserQuery(id));
    return user is not null ? Results.Ok(user) : Results.NotFound();
});
```

---

## Pipeline Behaviors

Intercept requests for cross-cutting concerns. Pipelines are executed in registration order.

### With Response

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse?> InvokeAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        Console.WriteLine($"[LOG] Processing: {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"[LOG] Completed: {typeof(TRequest).Name}");
        return response;
    }
}
```

### Void Pipeline

```csharp
public sealed class AuditBehavior<TRequest>
    : IPipelineBehavior<TRequest>
    where TRequest : IRequest
{
    public async Task InvokeAsync(
        TRequest request,
        RequestHandlerDelegate next,
        CancellationToken ct)
    {
        Console.WriteLine($"[AUDIT] {typeof(TRequest).Name}");
        await next();
    }
}
```

### Registration

```csharp
// Open generic behaviors are auto-discovered by AddCQReetMediator
// Or register manually:
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<>), typeof(AuditBehavior<>));

services.AddCQReetMediator(typeof(Program));
```

---

## Notifications

Publish domain events to multiple handlers. Handlers execute sequentially.

```csharp
public sealed record UserCreatedEvent(Guid UserId) : INotification;

public sealed class SendWelcomeEmail : INotificationHandler<UserCreatedEvent>
{
    public Task HandleAsync(UserCreatedEvent notification, CancellationToken ct)
    {
        // send email
        return Task.CompletedTask;
    }
}

public sealed class TrackAnalytics : INotificationHandler<UserCreatedEvent>
{
    public Task HandleAsync(UserCreatedEvent notification, CancellationToken ct)
    {
        // track event
        return Task.CompletedTask;
    }
}
```

```csharp
await mediator.PublishAsync(new UserCreatedEvent(userId));
```

---

## Collection Queries

TResponse can be any collection type with zero additional overhead.

```csharp
public sealed record GetAllUsersQuery : IQuery<IReadOnlyList<UserDto>>;
public sealed record GetActiveIdsQuery : IQuery<int[]>;
public sealed record GetTagsQuery : IQuery<IEnumerable<string>>;

public sealed class GetAllUsersHandler
    : IQueryHandler<GetAllUsersQuery, IReadOnlyList<UserDto>>
{
    public Task<IReadOnlyList<UserDto>?> HandleAsync(
        GetAllUsersQuery query, CancellationToken ct)
    {
        UserDto[] users = [new(Guid.NewGuid(), "Alice"), new(Guid.NewGuid(), "Bob")];
        return Task.FromResult<IReadOnlyList<UserDto>?>(users);
    }
}
```

**Zero-Allocation Tips:**
- Return arrays as `IReadOnlyList<T>` (arrays implement it natively, zero interface overhead)
- Use `Array.Empty<T>()` for empty results
- Avoid LINQ operators (`ToList`, `Select`, `Where`) in hot paths
- Consider `ArrayPool<T>` for large, high-frequency collections

---

## Architecture

```
IMediator.SendAsync(request)
    |
    v
MediatorRegistry (FrozenDictionary - O(1) lookup)
    |
    v
RequestWrapper<TRequest, TResponse> (sealed, pre-compiled at startup)
    |
    +-- Has pipelines? --> Build execution chain (reverse order) --> Execute
    |
    +-- No pipelines?  --> Direct handler call (fast path, 0 alloc)
    |
    v
IRequestHandler<TRequest, TResponse>.HandleAsync(request, ct)
```

---

## API Reference

### IMediator

```csharp
public interface IMediator
{
    Task SendAsync(IRequest request, CancellationToken ct = default);
    Task<TResponse?> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
    Task PublishAsync(INotification notification, CancellationToken ct = default);
}
```

### Contracts

| Interface | Purpose |
|:---|:---|
| `IRequest` | Void request marker |
| `IRequest<TResponse>` | Request with response |
| `ICommand` / `ICommand<T>` | Command semantics (write operations) |
| `IQuery` / `IQuery<T>` | Query semantics (read operations) |
| `INotification` | Domain event / notification |

### Handlers

| Interface | Signature |
|:---|:---|
| `IRequestHandler<TRequest>` | `Task HandleAsync(TRequest, CancellationToken)` |
| `IRequestHandler<TRequest, TResponse>` | `Task<TResponse?> HandleAsync(TRequest, CancellationToken)` |
| `ICommandHandler<TRequest>` | Alias for `IRequestHandler<TRequest>` where `TRequest : ICommand` |
| `ICommandHandler<TRequest, TResponse>` | Alias for `IRequestHandler<TRequest, TResponse>` where `TRequest : ICommand<TResponse>` |
| `IQueryHandler<TRequest>` | Alias for `IRequestHandler<TRequest>` where `TRequest : IQuery` |
| `IQueryHandler<TRequest, TResponse>` | Alias for `IRequestHandler<TRequest, TResponse>` where `TRequest : IQuery<TResponse>` |
| `INotificationHandler<TNotification>` | `Task HandleAsync(TNotification, CancellationToken)` |

### Pipeline Behaviors

| Interface | Signature |
|:---|:---|
| `IPipelineBehavior<TRequest>` | `Task InvokeAsync(TRequest, RequestHandlerDelegate, CancellationToken)` |
| `IPipelineBehavior<TRequest, TResponse>` | `Task<TResponse?> InvokeAsync(TRequest, RequestHandlerDelegate<TResponse>, CancellationToken)` |

---

## Repository Structure

```
/src
  CQReetMediator.Abstractions/            # Pure interfaces, zero dependencies
  CQReetMediator/                          # Core: Mediator, Registry, Wrappers
  CQReetMediator.DependencyInjection/      # DI extensions (Microsoft.Extensions.DI)
  CQReetMediator.Tests/                    # Unit tests
  CQReetMediator.DependencyInjection.Tests/ # Integration tests
  CQReetMediator.Benchmarks/              # Performance benchmarks
```

---

<div align="center">

## 📄 License

This project is licensed under the **MIT License**.

---

## ⭐ Support the Project

If you find this library helpful or impressive,
**please consider giving it a ⭐ on GitHub!**

</div>
