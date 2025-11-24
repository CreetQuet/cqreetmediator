<div align="center">

# ⚡️ CQReetMediator

### A ultra-light, high-performance CQRS Mediator for .NET 9

<br/>

[![Build](https://img.shields.io/github/actions/workflow/status/CreetStack/CQReetMediator/ci.yml?label=Build&style=for-the-badge)]()
[![Tests](https://img.shields.io/github/actions/workflow/status/CreetStack/CQReetMediator/tests.yml?label=Tests&style=for-the-badge)]()
[![NuGet](https://img.shields.io/nuget/v/CQReetMediator.svg?style=for-the-badge&label=NuGet)]()
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)]()
[![Contributions Welcome](https://img.shields.io/badge/Contributions-Welcome-brightgreen.svg?style=for-the-badge)]()

<br/>

**A modular mediator library designed for CQRS architectures, with blazing-fast handler dispatch, extensible pipelines,
DI integration, and clean abstractions.**

</div>

---

## ✨ Features

- 🧩 **Commands & Queries** with separate handlers (CQRS-first design)
- ⚡ **High-performance execution** with cached compiled delegates
- 🔌 **Pipeline Behaviors**: validation, logging, metrics, etc.
- 📣 **Notifications & Event Publishing**
- 🏗️ **Zero-alloc architecture** (ValueTask-based)
- 🔄 **Full DI integration** (Microsoft.Extensions.DependencyInjection)
- 🧪 **Complete test coverage** (unit + DI integration tests)
- 🔧 Modular NuGet packages:
    - `CQReetMediator.Abstractions`
    - `CQReetMediator`
    - `CQReetMediator.DependencyInjection`

---

## 📦 Installation

### Core package

```bash
dotnet add package CQReetMediator
````

### Abstractions only

```bash
dotnet add package CQReetMediator.Abstractions
```

---

## 🚀 Quick Start

### 1. Create a Command

```csharp
public sealed record CreateUserCommand(string Name) : ICommand<Guid>;
```

### 2. Implement the Handler

```csharp
public sealed class CreateUserCommandHandler 
    : ICommandHandler<CreateUserCommand, Guid> {
    public ValueTask<Guid> HandleAsync(CreateUserCommand command, CancellationToken ct)
        => ValueTask.FromResult(Guid.NewGuid());
}
```

### 3. Register Mediator (DI)

```csharp
services.AddCQReetMediator(typeof(Program).Assembly);
```

You must pass the assemblies containing your command, query, notification handlers, and pipeline behaviors.

### 4. Use It

```csharp
var id = await mediator.Send(new CreateUserCommand("Alice"));
```

---

## 🧩 Pipeline Behaviors

Pipeline behaviors allow injecting cross-cutting logic.

```csharp
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> {
    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        Func<ValueTask<TResponse>> next,
        CancellationToken ct
    ){
        Console.WriteLine($"Start: {request.GetType().Name}");
        var result = await next();
        Console.WriteLine($"End: {request.GetType().Name}");
        return result;
    }
}
```

---

## 📣 Notifications

```csharp
public sealed record UserCreatedEvent(Guid UserId) : INotification;

public sealed class SendEmailOnUserCreated : INotificationHandler<UserCreatedEvent> {
    public Task HandleAsync(UserCreatedEvent notification, CancellationToken ct) {
        Console.WriteLine($"Email sent to user {notification.UserId}");
        return Task.CompletedTask;
    }
}
```

All `INotificationHandler<T>` implementations are automatically registered through reflection.
The mediator resolves them using `IEnumerable<INotificationHandler<T>>`, so multiple handlers are supported out of the
box.

Publish:

```csharp
await mediator.PublishAsync(new UserCreatedEvent(id));
```

`PublishAsync` will invoke *all registered notification handlers* for the event type.

---

## 🧪 Testing and Benchmark

Run ALL tests:

```bash
dotnet test
```

Run All Benchmarks:

```bash
dotnet run -c Release
```

Projects included:

* `CQReetMediator.Tests`
* `CQReetMediator.DependencyInjection.Tests`
* `CQReetMediator.Benchmarks`

---

## 📁 Repository Structure

```
/src
  CQReetMediator.Abstractions/
  CQReetMediator/
  CQReetMediator.DependencyInjection/
  CQReetMediator.Tests/
  CQReetMediator.DependencyInjection.Tests/
  CQReetMediator.Benchmarks/
```

---

<div align="center">

## 📄 License

This project is licensed under the **MIT License**.

---

## ⭐ Support the Project

If you find this library helpful,
**please consider giving it a ⭐ on GitHub!**

</div>