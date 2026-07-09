| Branch | Status                                                                                                                      |
|--------|-----------------------------------------------------------------------------------------------------------------------------|
| main   | ![Dotnet 8](https://github.com/md-redwan-hossain/SharpServiceCollection/actions/workflows/dotnet.yml/badge.svg?branch=main) |

### Installation

To install, run `dotnet add package SharpServiceCollection` or
from [Nuget](https://www.nuget.org/packages/SharpServiceCollection)

### Introduction

`SharpServiceCollection` is a lightweight C# library that wraps the `IServiceCollection` interface to streamline
dependency injection through attribute-based assembly scanning.

Annotate a class with `InjectableDependency` or `InjectableDependency<T>`, scan the assembly, and the library calls the
equivalent `Add*` / `TryAdd*` / `AddKeyed*` / `TryAddKeyed*` / `TryAddEnumerable` methods for you.

### Assembly Scanning

Use one of the extension methods on `IServiceCollection`:

- `AddServicesFromCurrentAssembly()`
- `AddServicesFromAssembly(Assembly assembly)`
- `AddServicesFromAssemblyContaining<T>()`
- `AddServicesFromAssemblyContaining(Type type)`

### Source-Generated Registration (AOT-friendly)

`SharpServiceCollection` ships the source generator inside the main NuGet package. Install only:

```bash
dotnet add package SharpServiceCollection
```

The generator emits assembly-specific registration methods based on your `InjectableDependency` attributes,
so you can register dependencies without runtime reflection scanning:

```csharp
using SharpServiceCollection.Generated;

var builder = WebApplication.CreateBuilder(args);

// Same-assembly convenience alias (internal — not visible to referencing projects)
builder.Services.AddSourceGeneratedServices();

// Explicit per-assembly method — use from host or other assemblies
builder.Services.AddServicesFromMyApplication();
```

`AddSourceGeneratedServices()` is emitted as **internal** (callable only within the same assembly).
Each project emits a public `AddServicesFrom{SanitisedAssemblyName}()` derived from its assembly name
(for example, `My.Module.Application` becomes `AddServicesFromMyModuleApplication`).
Call the explicit method from your host or module registration code when multiple generated assemblies are referenced.

The existing reflection methods (`AddServicesFromAssembly*`) remain supported for backward compatibility.
### ASP.NET Core Setup

```csharp
using SharpServiceCollection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServicesFromCurrentAssembly();

var app = builder.Build();

app.MapGet("/user/{id}", (IUserService userService, int id) => userService.GetUserInfo(id));

app.Run();
```


### How Attributes Replace `IServiceCollection` Calls

Instead of registering services by hand, annotate the implementation class and scan the assembly.

#### Self registration

```csharp
// Manual
services.AddScoped<MyService>();
```

```csharp
// Attribute
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self, TryAdd = false)]
public class MyService { }
```

```csharp
// Manual
services.TryAddScoped<MyService>();
```

```csharp
// Attribute
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self)]
public class MyService { }
```

#### Matching interface (`IMyService` for `MyService`)

```csharp
// Manual
services.AddScoped<IMyService, MyService>();
```

```csharp
// Attribute
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface, TryAdd = false)]
public class MyService : IMyService { }
```

```csharp
// Manual
services.TryAddScoped<IMyService, MyService>();
```

```csharp
// Attribute
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface)]
public class MyService : IMyService { }
```

#### Implemented interface (each implemented interface)

```csharp
// Manual
services.AddScoped<IFoo, MyService>();
```

```csharp
// Attribute
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface, TryAdd = false)]
public class MyService : IFoo, IBar { }
```

```csharp
// Manual
services.TryAddScoped<IFoo, MyService>();
```

```csharp
// Attribute
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface)]
public class MyService : IFoo, IBar { }
```

#### Explicit interface (`InjectableDependency<T>`)

```csharp
// Manual
services.AddScoped<IMyService, MyService>();
```

```csharp
// Attribute
[InjectableDependency<IMyService>(InstanceLifetime.Scoped, TryAdd = false)]
public class MyService : IMyService { }
```

```csharp
// Manual
services.TryAddScoped<IMyService, MyService>();
```

```csharp
// Attribute
[InjectableDependency<IMyService>(InstanceLifetime.Scoped)]
public class MyService : IMyService { }
```

#### Keyed registration

```csharp
// Manual
services.AddKeyedScoped<IMyService>("key", typeof(MyService));
```

```csharp
// Attribute
[InjectableDependency<IMyService>(InstanceLifetime.Scoped, Key = "key", TryAdd = false)]
public class MyService : IMyService { }
```

```csharp
// Manual
services.TryAddKeyedScoped<IMyService>("key", typeof(MyService));
```

```csharp
// Attribute
[InjectableDependency<IMyService>(InstanceLifetime.Scoped, Key = "key")]
public class MyService : IMyService { }
```

#### Enumerable registration (multiple implementations)

```csharp
// Manual
services.TryAddEnumerable(ServiceDescriptor.Scoped<IPlugin, PluginA>());
```

```csharp
// Attribute
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface, Enumerable = true)]
public class PluginA : IPlugin { }
```

Apply the same attribute to each implementation (`PluginB`, `PluginC`, …) to register them all.

#### Multiple registrations on one class

```csharp
// Manual
services.TryAddScoped<IMyService, MyService>();
services.AddScoped<MyService>();
```

```csharp
// Attribute
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface)]
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self, TryAdd = false)]
public class MyService : IMyService { }
```

### Attributes

Two attributes cover the registration surface of `IServiceCollection`:

```csharp
[InjectableDependency(InstanceLifetime lifetime, ResolveBy resolveBy)]
[InjectableDependency<T>(InstanceLifetime lifetime)]
```

Both support optional properties: `TryAdd` (default `true`), `Key`, `Enumerable`, and `Order` (default `0`).

**`ResolveBy` enum** (`InstanceLifetime` values: `Singleton`, `Scoped`, `Transient`):

- `Self` — register as the concrete class
- `MatchingInterface` — register as `I{ClassName}` (e.g. `MyService` → `IMyService`)
- `ImplementedInterface` — register for each implemented interface

Use `InjectableDependency<T>` when you want to register as a specific interface without convention.

### Optional Properties

#### `TryAdd` (default `true`)

Controls whether `Add*` or `TryAdd*` is used.

```csharp
// Manual — always registers (overwrites existing)
services.AddScoped<IMyService, MyService>();
```

```csharp
// Attribute
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface, TryAdd = false)]
public class MyService : IMyService { }
```

```csharp
// Manual — skips if already registered
services.TryAddScoped<IMyService, MyService>();
```

```csharp
// Attribute (TryAdd defaults to true)
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface)]
public class MyService : IMyService { }
```

#### `Key`

Switches plain registration to keyed registration.

```csharp
// Manual
services.AddKeyedScoped<IMyService>("key", typeof(MyService));
```

```csharp
// Attribute
[InjectableDependency<IMyService>(InstanceLifetime.Scoped, Key = "key", TryAdd = false)]
public class MyService : IMyService { }
```

#### `Enumerable` (`TryAdd` must not be set to `false`)

Registers multiple implementations of the same service type.

```csharp
// Manual
services.TryAddEnumerable(ServiceDescriptor.Scoped<IPlugin, PluginA>());
```

```csharp
// Attribute
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface, Enumerable = true)]
public class PluginA : IPlugin { }
```

### Duplicate Registration

When multiple classes register the same service type, implementations are processed in **`Order` ascending**, then by
class name ascending. For example, with default `Order = 0`, `Foo`, `Bar`, `Baz` are processed as `Bar`, `Baz`, `Foo`.

- `TryAdd = true` (default) — first processed wins (lowest `Order`; tie → earliest class name)
- `TryAdd = false` — last processed wins (highest `Order`; tie → latest class name)

Use `Order` to control which implementation wins without renaming classes:

```csharp
// Manual — registration order is explicit
services.TryAddScoped<IWorker, ZebraWorker>();
services.TryAddScoped<IWorker, AlphaWorker>(); // ignored; ZebraWorker already registered
```

```csharp
// Attribute — ZebraWorker wins despite sorting after AlphaWorker by class name
[InjectableDependency<IWorker>(InstanceLifetime.Scoped, Order = 1)]
public class ZebraWorker : IWorker { }

[InjectableDependency<IWorker>(InstanceLifetime.Scoped, Order = 2)]
public class AlphaWorker : IWorker { }
```

`Enumerable = true` registrations follow the same sort order when resolving `IEnumerable<T>`.
