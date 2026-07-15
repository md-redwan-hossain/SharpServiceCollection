# SharpServiceCollection

[![Build status](https://github.com/md-redwan-hossain/SharpServiceCollection/actions/workflows/dotnet.yaml/badge.svg?branch=main)](https://github.com/md-redwan-hossain/SharpServiceCollection/actions/workflows/dotnet.yaml)

**SharpServiceCollection** is a lightweight C# package that provides a declarative, comipile-time and AOT-friendly way
to work with `IServiceCollection`.

## Table of contents

- [Installation](#installation)
- [How it works](#how-it-works)
- [Your first registration](#your-first-registration)
    - [Register a class by itself](#register-a-class-by-itself)
    - [Register by interface](#register-by-interface)
    - [Register an explicit service type](#register-an-explicit-service-type)
- [Registration options](#registration-options)
    - [Lifetimes](#lifetimes)
    - [Registration target](#registration-target)
    - [Keys, enumerable services, and ordering](#keys-enumerable-services-and-ordering)
- [Choose how services are discovered](#choose-how-services-are-discovered)
    - [Source-generated registration](#source-generated-registration)
    - [Reflection-based registration](#reflection-based-registration)
- [Register services from multiple files or projects](#register-services-from-multiple-files-or-projects)
    - [Create a module registration](#create-a-module-registration)
    - [Enable the host project](#enable-the-host-project)
    - [Use a registration context](#use-a-registration-context)
- [Opt out of source generation](#opt-out-of-source-generation)
- [Diagnostics](#diagnostics)
## Installation

Install **one** package. You do not need to install separate packages for the runtime library, dependency types, or
source generator:

```bash
dotnet add package SharpServiceCollection
```

You can also install it from [NuGet](https://www.nuget.org/packages/SharpServiceCollection).

The package contains:

- The runtime library and public attributes
- The dependency types used by the attributes
- The Roslyn source generator under `analyzers/dotnet/cs`

The source generator runs automatically when you build a project that references the package.

## How it works

The basic workflow is:

1. Install `SharpServiceCollection`.
2. Add an `InjectableDependency` attribute to each service implementation.
3. Call the generated `AddAttributedServices()` extension method.
4. Resolve the service through the normal .NET dependency injection APIs.

For example:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;
using SharpServiceCollection.Generated;

public interface IEmailSender
{
    Task SendAsync(string address, string message);
}

[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface)]
public sealed class EmailSender : IEmailSender
{
    public Task SendAsync(string address, string message)
    {
        Console.WriteLine($"Sending to {address}: {message}");
        return Task.CompletedTask;
    }
}

var builder = WebApplication.CreateBuilder(args);

// The source generator creates this method for the current assembly.
builder.Services.AddAttributedServices();

var app = builder.Build();
app.MapGet("/email", async (IEmailSender sender) =>
{
    await sender.SendAsync("user@example.com", "Welcome!");
    return Results.Ok();
});

app.Run();
```

`EmailSender` is registered as `IEmailSender`, and its lifetime is scoped. The generated code uses the normal
`Microsoft.Extensions.DependencyInjection` registration APIs; there is no runtime type scanning for source-generated
registration.

## Your first registration

### Register a class by itself

Use `ResolveBy.Self` when consumers should resolve the concrete class:

```csharp
[InjectableDependency(InstanceLifetime.Transient, ResolveBy.Self)]
public sealed class TokenGenerator
{
    public string Create() => Guid.NewGuid().ToString("N");
}
```

This is equivalent to:

```csharp
services.TryAddTransient<TokenGenerator>();
```

### Register by interface

Use `ResolveBy.MatchingInterface` when the class follows the `I{ClassName}` naming convention:

```csharp
public interface IOrderService
{
    Task PlaceAsync(int orderId);
}

[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface)]
public sealed class OrderService : IOrderService
{
    public Task PlaceAsync(int orderId) => Task.CompletedTask;
}
```

This registers:

```csharp
services.TryAddScoped<IOrderService, OrderService>();
```

The class must implement the matching interface. For example, `OrderService` must implement `IOrderService`.

### Register an explicit service type

Use the generic attribute when the service type does not follow a naming convention:

```csharp
public interface IMessageHandler
{
    Task HandleAsync(string message);
}

[InjectableDependency<IMessageHandler>(InstanceLifetime.Singleton)]
public sealed class WelcomeMessageHandler : IMessageHandler
{
    public Task HandleAsync(string message) => Task.CompletedTask;
}
```

This registers `WelcomeMessageHandler` as `IMessageHandler`.

## Registration options

### Lifetimes

`InstanceLifetime` supports the standard dependency injection lifetimes:

| Lifetime    | Behavior                                                |
|-------------|---------------------------------------------------------|
| `Singleton` | One instance for the application lifetime               |
| `Scoped`    | One instance per service scope, such as an HTTP request |
| `Transient` | A new instance each time the service is requested       |

Example:

```csharp
[InjectableDependency(InstanceLifetime.Singleton, ResolveBy.Self)]
public sealed class ApplicationClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}
```

### Registration target

`ResolveBy` determines which service type is registered:

| Value                  | Registers as                             | Example                                       |
|------------------------|------------------------------------------|-----------------------------------------------|
| `Self`                 | The concrete class                       | `TokenGenerator`                              |
| `MatchingInterface`    | `I{ClassName}`                           | `OrderService` → `IOrderService`              |
| `ImplementedInterface` | Every interface implemented by the class | `PaymentService` → `ICardPayment`, `IRefunds` |

For example:

```csharp
[InjectableDependency(
    InstanceLifetime.Scoped,
    ResolveBy.ImplementedInterface)]
public sealed class PaymentService : ICardPayment, IRefunds
{
    // Registered for both ICardPayment and IRefunds.
}
```

### Keys, enumerable services, and ordering

All registration attributes support these options:

| Option       | Default | Purpose                                                                                       |
|--------------|--------:|-----------------------------------------------------------------------------------------------|
| `TryAdd`     |  `true` | Avoid replacing an existing registration when `true`; use the regular `Add*` API when `false` |
| `Key`        |  `null` | Register a keyed service                                                                      |
| `Enumerable` | `false` | Add the implementation to an enumerable service collection                                    |
| `Order`      |     `0` | Control processing order when registrations compete                                           |

#### Keyed services

```csharp
[InjectableDependency<IMessageHandler>(
    InstanceLifetime.Transient,
    Key = "welcome")]
public sealed class WelcomeMessageHandler : IMessageHandler
{
    public Task HandleAsync(string message) => Task.CompletedTask;
}
```

The service can then be resolved using the standard keyed-service APIs:

```csharp
var handler = serviceProvider.GetRequiredKeyedService<IMessageHandler>("welcome");
```

#### Multiple implementations

Set `Enumerable = true` when you want all matching implementations available through `IEnumerable<T>`:

```csharp
public interface IValidator<T>
{
    bool IsValid(T value);
}

public sealed record Order(int Id);

[InjectableDependency<IValidator<Order>>(
    InstanceLifetime.Singleton,
    Enumerable = true)]
public sealed class PaymentValidator : IValidator<Order>
{
}

[InjectableDependency<IValidator<Order>>(
    InstanceLifetime.Singleton,
    Enumerable = true)]
public sealed class AddressValidator : IValidator<Order>
{
}
```

Inject them together:

```csharp
public sealed class OrderService(IEnumerable<IValidator<Order>> validators)
{
    public bool IsValid(Order order) => validators.All(v => v.IsValid(order));
}
```

`Enumerable = true` requires `TryAdd = true`.

#### Duplicate registrations and `Order`

Registrations are processed by `Order` ascending, followed by class name ascending.

- With `TryAdd = true`, the first matching registration wins.
- With `TryAdd = false`, the regular `Add*` method is used, so later registrations can replace the default service
  resolution.

```csharp
[InjectableDependency<IWorker>(InstanceLifetime.Scoped, Order = 1)]
public sealed class PrimaryWorker : IWorker
{
}

[InjectableDependency<IWorker>(InstanceLifetime.Scoped, Order = 2)]
public sealed class BackupWorker : IWorker
{
}
```

## Choose how services are discovered

### Source-generated registration

Source generation is the recommended option when the assemblies are known at build time. It is fast, avoids runtime
assembly scanning, and is a good fit for trimming and AOT scenarios.

Add the attribute to your services, then call:

```csharp
using SharpServiceCollection.Generated;

services.AddAttributedServices();
```

The generated method is `internal`, so it is intended for use inside the same assembly that contains the attributed
services.

When a host needs to register services from another assembly, the generator also creates a public method based on that
assembly name:

```csharp
// For an assembly named My.Module.Application:
services.AddAttributedServicesFrom_My_Module_Application();
```

The complete method name follows this pattern:

```text
AddAttributedServicesFrom_{SanitizedAssemblyName}
```

For example:

```text
Assembly name:  My.Module.Application
Generated call: services.AddAttributedServicesFrom_My_Module_Application();
```

The generator uses the consuming project's `AssemblyName`, replaces dots and other non-alphanumeric character runs
with underscores, and adds the `AddAttributedServicesFrom_` prefix. For example, the test project assembly
`SharpServiceCollection.Tests` generates:

```csharp
services.AddAttributedServicesFrom_SharpServiceCollection_Tests();
```

The generated method is public, so a host project can call the method generated in a referenced module assembly.

### Reflection-based registration

Use reflection when an assembly is only known at runtime, when you are loading plugins, or while migrating an existing
application to source generation.

```csharp
using SharpServiceCollection.Extensions;

services.AddServicesFromCurrentAssembly();
services.AddServicesFromAssembly(pluginAssembly);
services.AddServicesFromAssemblyContaining<PluginMarker>();
services.AddServicesFromAssemblyContaining(typeof(PluginMarker));
```

Reflection scans the selected assembly at runtime. It is more flexible than source generation, but source generation is
usually preferable when the set of assemblies is known during the build.

## Register services from multiple files or projects

Service registrations can be split across any number of files within a project or distributed across multiple projects. In
both cases, the source generator aggregates the registrations so they can be discovered and executed together. This is
especially useful in a modular solution where each project owns its service registrations.

For example, registrations can be organized across multiple files in one project:

```text
MyApp.Api                         # one project
├── Program.cs
└── Registrations/
    ├── DatabaseConfig.cs
    ├── AwsConfig.cs
    └── OtherConfig.cs
```

Each file can contain its own `[ServiceRegistrationItem]` class. The generator combines all registration items in the
project, regardless of which file contains them.

The same file-level organization works when registrations are split across projects. For example, a host can reference
feature modules whose registrations are also organized across multiple files:

```text
MyApp.Api                         # host project
├── Program.cs
├── DatabaseConfig.cs
├── AwsConfig.cs
└── OtherConfig.cs

MyApp.Orders                      # feature module
├── OrdersServiceRegistration.cs
├── OrdersEmailConfig.cs
└── OrdersQuartzWorkerConfig.cs

MyApp.Payments                    # feature module
└── PaymentsServiceRegistration.cs
```

Each project can split its registration items across as many meaningful files as needed. The generator combines all
registration items within each project into that project's aggregator (`RegisterAsync_{Order}_{index}` methods). The host
root then calls those aggregator methods from the host project and from referenced projects or packages, sorted by `Order`.

### Why this matters

Without orchestration, the host must know every registration class and call them one by one:

```csharp
await new DatabaseConfig().RegisterAsync(services);
await new AwsConfig().RegisterAsync(services);
await new OtherConfig().RegisterAsync(services);
await new OrdersServiceRegistration().RegisterAsync(services);
await new OrdersEmailConfig().RegisterAsync(services);
await new OrdersQuartzWorkerConfig().RegisterAsync(services);
await new PaymentsServiceRegistration().RegisterAsync(services);
```

That approach becomes a maintenance problem as files and projects are added: the host must be updated every time a new
registration item is introduced, and it must preserve the correct execution order.

With `[ServiceRegistrationItem]`, the generators discover registration items across all files in the host and referenced
projects, aggregate them, sort them by `Order`, and generate the orchestration method. The host calls one method instead:

```csharp
await builder.Services.AddServiceRegistrationItemsAsync();
```

Context-aware registrations are driven by the generic type argument. For every `[ServiceRegistrationItem]` implementation
of `IServiceRegistration<TContext>` found in the host or any referenced project, the root generator groups registrations
by `TContext` and generates an `AddServiceRegistrationItemsAsync` overload accepting that context type. The host makes
one call for each distinct context type, rather than calling each registration class individually.

For example, these registration classes:

```csharp
[ServiceRegistrationItem]
public sealed class DatabaseConfig : IServiceRegistration<AppContext>
{
    public Task RegisterAsync(IServiceCollection services, AppContext context)
    {
        services.AddDbContext<AppDbContext>(context.Configuration);
        return Task.CompletedTask;
    }
}

[ServiceRegistrationItem]
public sealed class WorkerConfig : IServiceRegistration<WorkerContext>
{
    public Task RegisterAsync(IServiceCollection services, WorkerContext context)
    {
        services.AddSingleton<WorkerOptions>(context.Options);
        return Task.CompletedTask;
    }
}
```

cause the host to receive overloads equivalent to:

```csharp
await builder.Services.AddServiceRegistrationItemsAsync(appContext);
await builder.Services.AddServiceRegistrationItemsAsync(workerContext);
```

All registration items using `AppContext` run through the first overload, and all items using `WorkerContext` run through the
second. Adding another registration file or project does not require changing the host startup code unless it introduces a
new context type that the host needs to provide.

### Create a module registration

Mark a sealed class with `[ServiceRegistrationItem]` and implement `IServiceRegistration`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Generated;
using SharpServiceCollection.Interfaces;

[ServiceRegistrationItem]
public sealed class OrdersRegistration : IServiceRegistration
{
    public Task RegisterAsync(IServiceCollection services)
    {
        services.AddAttributedServices();
        services.AddSingleton<OrderNumberGenerator>();
        return Task.CompletedTask;
    }
}
```

The generator discovers classes that:

- Have the `[ServiceRegistrationItem]` attribute
- Are `sealed`
- Implement `IServiceRegistration` or `IServiceRegistration<TContext>`
- Provide the matching `RegisterAsync` method

The class can have any name. It does not need to be named `ServiceRegistration`.

`Order` controls execution order. Higher order values run first by default. Registrations with the same order are sorted
by implementation type name. Set `ServiceRegistrationRootDescSortOrder` to `false` in the root project's `.csproj` to
make lower order values run first.

### Enable the host project

Set `ServiceRegistrationRoot` in the host project's `.csproj` file:

```xml

<PropertyGroup>
    <ServiceRegistrationRoot>true</ServiceRegistrationRoot>
    <!-- Optional; defaults to true. Set false for ascending order. -->
    <ServiceRegistrationRootDescSortOrder>false</ServiceRegistrationRootDescSortOrder>
</PropertyGroup>
```

The host can contain registrations split across any number of files and/or reference module projects or packages. The
source generator creates an `AddServiceRegistrationItemsAsync` overload for each registration context used by the
host and its referenced modules. Registrations that implement `IServiceRegistration` use the overload without a context;
registrations that implement `IServiceRegistration<TContext>` use the overload accepting that context type.

For example, if registrations use `AppContext` and `WorkerContext`, the host gets overloads equivalent to:

```csharp
await builder.Services.AddServiceRegistrationItemsAsync();
await builder.Services.AddServiceRegistrationItemsAsync(appContext);
await builder.Services.AddServiceRegistrationItemsAsync(workerContext);
```

Then execute the generated registrations during startup: 

```csharp
using SharpServiceCollection.Generated;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.AddServiceRegistrationItemsAsync();

var app = builder.Build();
app.Run();
```

The source generator creates a small public aggregator in every project that contains service registrations. A project with
`ServiceRegistrationRoot` also gets the `AddServiceRegistrationItemsAsync` orchestration extension. That method calls the
host project's own aggregator methods together with aggregators discovered through its project or package references,
sorted by `Order`. The orchestration method is `internal`, because it is intended to be called from the root project itself.

### Use a registration context

Implement `IServiceRegistration<TContext>` when a module needs configuration or environment information:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Interfaces;

public sealed record AppContext(
    IConfiguration Configuration,
    IHostEnvironment Environment);

[ServiceRegistrationItem(Order = 20)]
public sealed class PaymentsRegistration : IServiceRegistration<AppContext>
{
    public Task RegisterAsync(
        IServiceCollection services,
        AppContext context)
    {
        services.AddSingleton(context.Configuration);
        return Task.CompletedTask;
    }
}
```

Pass the matching context from the host:

```csharp
var context = new AppContext(
    builder.Configuration,
    builder.Environment);

await builder.Services.AddServiceRegistrationItemsAsync(context);
```

| Host call                                       | Executes                                                                             |
|-------------------------------------------------|--------------------------------------------------------------------------------------|
| `AddServiceRegistrationItemsAsync()`        | Registrations implementing `IServiceRegistration`                                    |
| `AddServiceRegistrationItemsAsync(context)` | Registrations implementing `IServiceRegistration<TContext>` where `TContext` matches |

## Opt out of source generation

The package contains two independent source generators. You can disable either one, or both, in a consuming project's
`.csproj` file.

### Disable attributed DI generation

Set `DisableInjectableDependencyGenerator` to `true` when you want to use the reflection-based registration APIs instead
of generated `AddAttributedServices()` methods:

```xml

<PropertyGroup>
    <DisableInjectableDependencyGenerator>true</DisableInjectableDependencyGenerator>
</PropertyGroup>
```

This disables generated attributed-service registration only. Reflection APIs such as `AddServicesFromAssembly(...)`
remain available.

### Disable service-registration orchestration

Set `DisableServiceRegistrationGenerator` to `true` when you do not use `[ServiceRegistrationItem]` and
`IServiceRegistration`:

```xml

<PropertyGroup>
    <DisableServiceRegistrationGenerator>true</DisableServiceRegistrationGenerator>
</PropertyGroup>
```

This disables generated service-registration aggregators and root methods such as
`AddServiceRegistrationItemsAsync(...)`.

To disable both generators:

```xml

<PropertyGroup>
    <DisableInjectableDependencyGenerator>true</DisableInjectableDependencyGenerator>
    <DisableServiceRegistrationGenerator>true</DisableServiceRegistrationGenerator>
</PropertyGroup>
```

## Diagnostics

The source generator reports clear diagnostics when an attribute or registration class is invalid:

| ID     | Severity | Meaning                                                                                                                  |
|--------|----------|--------------------------------------------------------------------------------------------------------------------------|
| SSC001 | Error    | `Enumerable = true` requires `TryAdd = true`                                                                             |
| SSC002 | Error    | `ResolveBy.MatchingInterface` requires an `I{ClassName}` interface                                                       |
| SSC003 | Error    | Invalid `InstanceLifetime` value                                                                                         |
| SSC004 | Error    | Invalid `ResolveBy` value                                                                                                |
| SSC005 | Error    | A type marked with `[ServiceRegistrationItem]` must be `sealed`                                                          |
| SSC006 | Error    | A type marked with `[ServiceRegistrationItem]` must implement `IServiceRegistration` or `IServiceRegistration<TContext>` |

Fix the reported source code and rebuild. The generator will run again automatically.


