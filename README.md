<div align="center">

# ⚡️ CQReetMediator

### The Zero-Allocation, High-Performance CQRS Mediator for .NET 9

<br/>

[![Build](https://img.shields.io/github/actions/workflow/status/CreetQuet/CQReetMediator/ci.yml?label=Build&style=for-the-badge)]()
[![Tests](https://img.shields.io/github/actions/workflow/status/CreetQuet/CQReetMediator/tests.yml?label=Tests&style=for-the-badge)]()
[![NuGet](https://img.shields.io/nuget/v/CQReetMediator.svg?style=for-the-badge&label=NuGet)]()
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)]()
[![.NET 9](https://img.shields.io/badge/.NET-9.0-purple.svg?style=for-the-badge)]()

<br/>

**A generic, ultra-light mediator library designed for high-throughput CQRS architectures.**
**Engineered to be invisible: 0 Bytes Allocation on the hot path.**

</div>

---

## ✨ Features

- ⚡ **Zero-Allocation Architecture**: Uses `FastRequestWrapper` to achieve 0 bytes allocation on handler execution when no pipelines are present.
- 🚀 **Blazing Fast**: Capable of processing over **5.3 Million requests per second**.
- 🧠 **Smart Dispatch**: Automatically chooses between "Fast Path" (Direct execution) and "Pipeline Path" based on your DI configuration.
- 🧊 **Frozen Caching**: Uses .NET 9 `FrozenDictionary` for O(1) ultra-fast handler lookups.
- 🔄 **Hybrid Pipelines**: Seamlessly mixes `ValueTask` (Sync) and `Task` (Async) pipeline behaviors without performance penalties.
- 📣 **Notifications**: Fire-and-forget event publishing with multiple handlers.
- 💉 **DI Integration**: Native support for `Microsoft.Extensions.DependencyInjection`.

---

## 📊 Benchmarks

CQReetMediator is optimized to be as close to a direct method call as possible.

**Environment**: .NET 9.0, Core i5-6400 class CPU.

| Scenario                       | Throughput | Latency (Avg) | Memory Allocated |
|:-------------------------------| :--- | :--- |:-----------------|
| **Direct Call (Baseline)**     | ~60.2M req/s | 0.016 µs | 0 B              |
| **CQReetMediator w/DI (Warm)** | **~5.3M req/s** | **0.185 µs** | **64 B** 🏆      |
| **CQReetMediator w/DI (Cold)** | ~3.1M req/s | 0.322 µs | 64 B             |

*> "It processes 1 million messages in less than what takes a human to blink (0.18s)."*

*> "64 bytes from Dependency Injection pattern"*

---

## 📦 Installation

### Core package

```bash
dotnet add package CQReetMediator
````

### Dependency Injection Extensions (Recommended)

```bash
dotnet add package CQReetMediator.DependencyInjection
```

-----

## 🚀 Quick Start

### 1. Define a Request

Use `IRequest` for void commands or `IRequest<T>` for queries/commands with results.

```csharp
public sealed record CreateUserCommand(string Name) : IRequest<Guid>;
```

### 2. Implement the Handler

Use `ValueTask` for maximum performance.

```csharp
public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid> 
{
    public ValueTask<Guid> HandleAsync(CreateUserCommand request, CancellationToken ct)
    {
        // Your logic here...
        return ValueTask.FromResult(Guid.NewGuid());
    }
}
```

### 3. Register in DI

The library automatically scans assemblies, registers handlers, and builds the optimized registry.

```csharp
// In Program.cs
builder.Services.AddCQReetMediator(typeof(Program));
```

### 4. Use It

Inject `IMediator` and send your request.

```csharp
app.MapPost("/users", async (IMediator mediator) => 
{
    var id = await mediator.Send(new CreateUserCommand("Garrosh"));
    return Results.Ok(id);
});
```

-----

## 🧩 Pipeline Behaviors

Intercept requests for validation, logging, or transactions. You can use **Async** (`Task`) or **Sync** (`ValueTask`) behaviors; the mediator bridges them automatically.

```csharp
public sealed class LoggingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    public async ValueTask<TResponse> InvokeAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        Console.WriteLine($"[LOG] Processing: {typeof(TRequest).Name}");
        
        var response = await next(); // Execute next step
        
        Console.WriteLine($"[LOG] Completed.");
        return response;
    }
}
```

To register open generic behaviors:

```csharp
// Behaviors are executed in the order they are registered
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Add CQReetMediator AFTER registering behaviors to enable smart-detection
services.AddCQReetMediator(typeof(Program)); 
```

-----

## 📣 Notifications

Publish events to multiple listeners.

```csharp
public sealed record UserCreatedEvent(Guid UserId) : INotification;

public sealed class EmailHandler : INotificationHandler<UserCreatedEvent> 
{
    public Task HandleAsync(UserCreatedEvent notification, CancellationToken ct) 
    {
        Console.WriteLine($"Sending email to {notification.UserId}...");
        return Task.CompletedTask;
    }
}

public sealed class AnalyticsHandler : INotificationHandler<UserCreatedEvent> 
{
    public Task HandleAsync(UserCreatedEvent notification, CancellationToken ct) 
    {
        Console.WriteLine("Tracking event...");
        return Task.CompletedTask;
    }
}
```

**Publishing:**

```csharp
await mediator.PublishAsync(new UserCreatedEvent(userId));
```

-----

## 📁 Repository Structure

```
/src
  CQReetMediator.Abstractions/       # Interfaces only (Zero dependencies)
  CQReetMediator/                    # Core logic (Wrappers & Registry)
  CQReetMediator.DependencyInjection/ # DI Extension methods (Microsoft.Extensions.DI)
  CQReetMediator.Tests/              # Unit tests
  CQReetMediator.Benchmarks/         # Performance tests
```

-----

<div align="center">

## 📄 License

This project is licensed under the **MIT License**.

-----

## ⭐ Support the Project

If you find this library helpful or impressive,
**please consider giving it a ⭐ on GitHub!**

</div>
