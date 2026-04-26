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

## ✨ Features

| Capability                        | Description                                                                                                                                                 |
|:----------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Zero-Allocation Hot Path**      | 0 bytes when no pipelines are present. Sealed wrappers, no virtual dispatch overhead.                                                                       |
| **AOT-Ready**                     | No `System.Reflection` at runtime. All wrappers are pre-compiled at DI registration. A Source Generator analyzer is now used instead of `FrozenDictionary`. |
| **Unified Task API**              | Single `Task`-based contract. No `ValueTask`/`Task` duality. Clean, predictable async model.                                                                |
| **Pipeline Behaviors**            | Extensible interceptor chain for validation, logging, transactions, caching. Open generic support.                                                          |
| **Strict CancellationToken**      | `CancellationToken` propagated through every handler, pipeline, and notification dispatch.                                                                  |
| **Requests vs Events**            | `IRequest` / `IRequest<T>`, `INotification` with clear semantic contracts.                                                                                  |
| **Void Requests**                 | `IRequest` without return value. Full pipeline support.                                                                                                     |
| **Notifications**                 | Fire-and-forget event publishing with multiple sequential handlers.                                                                                         |
| **Collection Queries**            | `IReadOnlyList<T>`, `List<T>`, `T[]`, `IEnumerable<T>` as TResponse with zero overhead.                                                                     |

---

## 📊 Benchmarks

CQReetMediator is engineered to be as close to a direct method call as possible.

### BenchmarkDotNet (Without Pipeline Behaviors)

_Note: This is the raw performance of the mediator dispatching directly to the handler._

| Method              | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| DirectCall          |  3.336 ns | 0.0747 ns | 0.0623 ns |  1.00 |    0.03 |    1 | 0.0043 |      72 B |        1.00 |
| CQReetMediator_Send | 27.216 ns | 0.2946 ns | 0.2755 ns |  8.16 |    0.17 |    2 | 0.0081 |     136 B |        1.89 |
| MediatR_Send        | 51.597 ns | 0.7964 ns | 0.7450 ns | 15.47 |    0.35 |    3 | 0.0157 |     264 B |        3.67 |

### BenchmarkDotNet (With Pipeline Behaviors)

_Note: This benchmark includes the execution of Pipeline Behaviors to demonstrate performance with cross-cutting concerns enabled._

| Method              | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| DirectCall          |  3.692 ns | 0.1110 ns | 0.2293 ns |  1.00 |    0.09 |    1 | 0.0043 |      72 B |        1.00 |
| CQReetMediator_Send | 54.460 ns | 1.1152 ns | 1.8937 ns | 14.81 |    1.03 |    2 | 0.0215 |     360 B |        5.00 |
| MediatR_Send        | 96.041 ns | 1.5854 ns | 1.4054 ns | 26.11 |    1.62 |    3 | 0.0300 |     504 B |        7.00 |

### Massive Load Test (1,000,000 concurrent requests)

- **Throughput:** ~3.08M req/sec
- **Average Latency:** 0.3239 us/req

---

## 📦 Installation

```bash
dotnet add package CQReetMediator
```

---

## 🚀 Quick Start

### 1. Define Requests

```csharp
// Command with response
public sealed record CreateUserCommand(string Name) : IRequest<Guid>;

// Void command
public sealed record DeleteUserCommand(Guid Id) : IRequest;

// Query with response
public sealed record GetUserQuery(Guid Id) : IRequest<UserDto>;

// Void query (health checks, warm-up, etc.)
public sealed record WarmUpCacheQuery : IRequest;
```

### 2. Implement Handlers

All handlers return `Task`. No `ValueTask` ambiguity.

```csharp
public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
{
    public Task<Guid?> HandleAsync(CreateUserCommand request, CancellationToken ct)
        => Task.FromResult<Guid?>(Guid.NewGuid());
}

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserCommand>
{
    public Task HandleAsync(DeleteUserCommand request, CancellationToken ct)
    {
        // delete logic
        return Task.CompletedTask;
    }
}

public sealed class GetUserHandler : IRequestHandler<GetUserQuery, UserDto>
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
builder.Services.AddCQReetMediator();
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

## 🧩 Pipeline Behaviors, Pre-Processors & Post-Processors

Intercept requests for cross-cutting concerns. CQReetMediator supports 3 explicit stages:

1. **Pre-Processors** (`IPreProcessorBehavior`): Run *before* any pipeline. Ideal for validation or setup.
2. **Pipelines** (`IPipelineBehavior`): Wrap the handler execution using `next()`. Ideal for transaction scopes,
   caching, or "around" logic.
3. **Post-Processors** (`IPostProcessorBehavior`): Run *after* the pipeline and handler (if successful). Ideal for audit
   logging or cleanup.

All behaviors are executed in registration order and automatically discovered by the Source Generator.

### Pipelines (Around)

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

### Pre-Processors (Before)

```csharp
public sealed class ValidationPreProcessor<TRequest>
    : IPreProcessorBehavior<TRequest>
    where TRequest : IRequest
{
    public Task ProcessAsync(TRequest request, CancellationToken ct)
    {
        // Run validation logic here, throw exception if invalid
        return Task.CompletedTask;
    }
}
```

### Post-Processors (After)

```csharp
public sealed class AuditPostProcessor<TRequest, TResponse>
    : IPostProcessorBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task ProcessAsync(TRequest request, TResponse? response, CancellationToken ct)
    {
        // Run audit logic here
        return Task.CompletedTask;
    }
}
```

### Registration

```csharp
// Open generic behaviors are auto-discovered by AddCQReetMediator
// Or register manually:
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPreProcessorBehavior<>), typeof(ValidationPreProcessor<>));
services.AddTransient(typeof(IPostProcessorBehavior<,>), typeof(AuditPostProcessor<,>));

services.AddCQReetMediator();
```

---

## 📣 Notifications

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
public sealed record GetAllUsersQuery : IRequest<IReadOnlyList<UserDto>>;
public sealed record GetActiveIdsQuery : IRequest<int[]>;
public sealed record GetTagsQuery : IRequest<IEnumerable<string>>;

public sealed class GetAllUsersHandler
    : IRequestHandler<GetAllUsersQuery, IReadOnlyList<UserDto>>
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
MediatorRegistry (Source Generator - O(1) lookup)
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

| Interface                  | Purpose                              |
|:---------------------------|:-------------------------------------|
| `IRequest`                 | Void request marker                  |
| `IRequest<TResponse>`      | Request with response                |
| `INotification`            | Domain event / notification          |

### Handlers

| Interface                              | Signature                                                                               |
|:---------------------------------------|:----------------------------------------------------------------------------------------|
| `IRequestHandler<TRequest>`            | `Task HandleAsync(TRequest, CancellationToken)`                                         |
| `IRequestHandler<TRequest, TResponse>` | `Task<TResponse?> HandleAsync(TRequest, CancellationToken)`                             |
| `INotificationHandler<TNotification>`  | `Task HandleAsync(TNotification, CancellationToken)`                                    |

### Pipeline Behaviors, Pre-Processors & Post-Processors

| Interface                                     | Signature                                                                                      |
|:----------------------------------------------|:-----------------------------------------------------------------------------------------------|
| `IPipelineBehavior<TRequest>`                 | `Task InvokeAsync(TRequest, RequestHandlerDelegate, CancellationToken)`                        |
| `IPipelineBehavior<TRequest, TResponse>`      | `Task<TResponse?> InvokeAsync(TRequest, RequestHandlerDelegate<TResponse>, CancellationToken)` |
| `IPreProcessorBehavior<TRequest>`             | `Task ProcessAsync(TRequest, CancellationToken)`                                               |
| `IPreProcessorBehavior<TRequest, TResponse>`  | `Task ProcessAsync(TRequest, CancellationToken)`                                               |
| `IPostProcessorBehavior<TRequest>`            | `Task ProcessAsync(TRequest, CancellationToken)`                                               |
| `IPostProcessorBehavior<TRequest, TResponse>` | `Task ProcessAsync(TRequest, TResponse?, CancellationToken)`                                   |

---

## 📁 Repository Structure

```
/src
  CQReetMediator.Abstractions/            # Pure interfaces, zero dependencies
  CQReetMediator/                          # Core: Mediator, Registry, Wrappers, DI
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

---
