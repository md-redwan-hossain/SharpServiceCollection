| Branch | Status                                                                                                                      |
|--------|-----------------------------------------------------------------------------------------------------------------------------|
| main   | ![Dotnet 8](https://github.com/md-redwan-hossain/SharpServiceCollection/actions/workflows/dotnet.yml/badge.svg?branch=main) |

# SharpServiceCollection

Attribute-driven and source-generated helpers for `Microsoft.Extensions.DependencyInjection`.

One NuGet package gives you:

1. **Attributed DI** — declare services with `[InjectableDependency]`, register via source generation or reflection
2. **Service registration orchestration** — compose many projects’ DI setup from a host at compile time

---

## Installation

```bash
dotnet add package SharpServiceCollection
```

Or from [NuGet](https://www.nuget.org/packages/SharpServiceCollection).

The package includes the runtime library **and** the Roslyn source generator (under `analyzers/dotnet/cs`).

---

## Features at a glance

| Feature | When to use | Entry point |
|---------|-------------|-------------|
| Attributed DI (source-generated) | AOT / trim-friendly; known assemblies at compile time | `AddAttributedServices()` / `AddAttributedServicesFrom{Assembly}()` |
| Attributed DI (reflection) | Runtime assembly scanning; plugins; gradual migration | `AddServicesFromCurrentAssembly()` / `AddServicesFromAssembly(...)` |
| Service registration orchestration | Modular apps: each project owns DI; host wires them | `ExecuteServiceRegistrationsAsync()` |

Typical modular app uses **both**: each project’s `ServiceRegistration` calls `AddAttributedServices()`, and the host calls `ExecuteServiceRegistrationsAsync()`.

---

## Attributed dependency injection

Annotate implementations with `InjectableDependency` or `InjectableDependency<T>`. The library maps them to the usual `Add*` / `TryAdd*` / keyed / enumerable APIs.

### Attributes

```csharp
[InjectableDependency(InstanceLifetime lifetime, ResolveBy resolveBy)]
[InjectableDependency<TService>(InstanceLifetime lifetime)]
```

Optional properties on both: `TryAdd` (default `true`), `Key`, `Enumerable`, `Order` (default `0`).

**`InstanceLifetime`:** `Singleton`, `Scoped`, `Transient`

**`ResolveBy`:**

| Value | Registers as |
|-------|----------------|
| `Self` | The concrete class |
| `MatchingInterface` | `I{ClassName}` (e.g. `MyService` → `IMyService`) |
| `ImplementedInterface` | Every implemented interface |

Use `InjectableDependency<T>` when you need an explicit service type without naming conventions.

### Registering attributed services

#### Source-generated (compile-time, AOT-friendly)

<a id="source-generated-registration-aot-friendly"></a>

The generator runs on every build of a consuming project. It emits:

| Method | Visibility | Use |
|--------|------------|-----|
| `AddAttributedServices()` | **internal** | Same assembly only (e.g. inside that project’s `ServiceRegistration`) |
| `AddAttributedServicesFrom{SanitisedAssemblyName}()` | **public** | Cross-assembly / host (avoids CS0121) |

Example: assembly `My.Module.Application` → `AddAttributedServicesFromMyModuleApplication()`.

```csharp
using SharpServiceCollection.Generated;

// Same assembly
services.AddAttributedServices();

// From another assembly
services.AddAttributedServicesFromMyModuleApplication();
```

#### Reflection (runtime scanning)

```csharp
using SharpServiceCollection.Extensions;

services.AddServicesFromCurrentAssembly();
services.AddServicesFromAssembly(assembly);
services.AddServicesFromAssemblyContaining<MyType>();
services.AddServicesFromAssemblyContaining(typeof(MyType));
```

Use reflection when the assembly is only known at runtime, or while migrating to source generation.

### Quick examples

**Self**

```csharp
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self)]
public class MyService { }
// → TryAddScoped<MyService>()
```

**Matching interface**

```csharp
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface)]
public class MyService : IMyService { }
// → TryAddScoped<IMyService, MyService>()
```

**Implemented interfaces**

```csharp
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface)]
public class MyService : IFoo, IBar { }
// → TryAddScoped for IFoo and IBar
```

**Explicit type**

```csharp
[InjectableDependency<IMyService>(InstanceLifetime.Scoped)]
public class MyService : IMyService { }
```

**Keyed**

```csharp
[InjectableDependency<IMyService>(InstanceLifetime.Scoped, Key = "key")]
public class MyService : IMyService { }
```

**Enumerable** (`Enumerable = true` requires `TryAdd = true`)

```csharp
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface, Enumerable = true)]
public class PluginA : IPlugin { }
```

**Multiple attributes on one class**

```csharp
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface)]
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self, TryAdd = false)]
public class MyService : IMyService { }
```

**`TryAdd = false`** uses `Add*` instead of `TryAdd*` (always registers).

### Duplicate registrations and `Order`

Implementations are processed by **`Order` ascending**, then class name ascending.

| `TryAdd` | Winner |
|----------|--------|
| `true` (default) | First processed (lowest `Order`) |
| `false` | Last processed (highest `Order`) |

```csharp
[InjectableDependency<IWorker>(InstanceLifetime.Scoped, Order = 1)]
public class ZebraWorker : IWorker { }

[InjectableDependency<IWorker>(InstanceLifetime.Scoped, Order = 2)]
public class AlphaWorker : IWorker { }
// TryAdd: ZebraWorker wins
```

---

## Service registration orchestration

<a id="service-registration"></a>

For solutions where **many projects** each own their DI setup, the host discovers sealed `ServiceRegistration` types in referenced assemblies and runs them at compile time — no runtime `GetTypes` / `Assembly.LoadFrom`.

### Host project

```xml
<PropertyGroup>
  <ServiceRegistrationRoot>true</ServiceRegistrationRoot>
</PropertyGroup>
```

```csharp
using SharpServiceCollection.Generated;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.ExecuteServiceRegistrationsAsync();
// or with an app-defined context:
await builder.Services.ExecuteServiceRegistrationsAsync(context);
```

NuGet consumers get `ServiceRegistrationRoot` as a compiler-visible property automatically (`buildTransitive`). For a local `ProjectReference`, add:

```xml
<ItemGroup>
  <CompilerVisibleProperty Include="ServiceRegistrationRoot" />
</ItemGroup>
```

### Per-project `ServiceRegistration`

Must be **`sealed`**, named exactly **`ServiceRegistration`**, and inherit one of the bases (enforced by diagnostics SSC005–SSC007).

**Services only:**

```csharp
using SharpServiceCollection;

public sealed class ServiceRegistration : ServiceRegistrationBase
{
    public override int Priority => 100;

    public override Task ExecuteAsync(IServiceCollection services)
    {
        services.AddAttributedServices();
        // DbContext, options, etc.
        return Task.CompletedTask;
    }
}
```

**With required context** (app defines `T` — e.g. config + environment):

```csharp
public sealed record AppContext(IConfiguration Config, IHostEnvironment Env);

public sealed class ServiceRegistration : ServiceRegistrationBase<AppContext>
{
    public override Task ExecuteAsync(IServiceCollection services, AppContext context)
    {
        services.AddAttributedServices();
        return Task.CompletedTask;
    }
}
```

| Host call | Invokes |
|-----------|---------|
| `ExecuteServiceRegistrationsAsync()` | `ServiceRegistrationBase` only |
| `ExecuteServiceRegistrationsAsync(ctx)` | `ServiceRegistrationBase<T>` where `T` matches `ctx` |

Items are sorted by **`Priority` descending**, then instantiated and `ExecuteAsync` is called. Add a project reference to the host and rebuild to include a new registration.

---

## End-to-end (ASP.NET Core)

```csharp
// Api.Host.csproj
// <ServiceRegistrationRoot>true</ServiceRegistrationRoot>

using SharpServiceCollection.Generated;

var builder = WebApplication.CreateBuilder(args);

await builder.Services.ExecuteServiceRegistrationsAsync(
    new AppContext(builder.Configuration, builder.Environment));

var app = builder.Build();
app.MapGet("/user/{id}", (IUserService users, int id) => users.GetUserInfo(id));
app.Run();
```

Each referenced module’s `ServiceRegistration.ExecuteAsync` typically calls `AddAttributedServices()` for that assembly’s `[InjectableDependency]` types.

---

## Diagnostics

| ID | Severity | Meaning |
|----|----------|---------|
| SSC001 | Error | `Enumerable=true` requires `TryAdd=true` |
| SSC002 | Error | `ResolveBy.MatchingInterface` needs `I{ClassName}` |
| SSC003 | Error | Invalid `InstanceLifetime` |
| SSC004 | Error | Invalid `ResolveBy` |
| SSC005 | Error | Type inheriting `ServiceRegistrationBase` must be named `ServiceRegistration` |
| SSC006 | Error | That type must be `sealed` |
| SSC007 | Error | `ServiceRegistration` needs an accessible parameterless constructor |
