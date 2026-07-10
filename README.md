| Branch | Status                                                                                                                      |
|--------|-----------------------------------------------------------------------------------------------------------------------------|
| main   | ![Dotnet 8](https://github.com/md-redwan-hossain/SharpServiceCollection/actions/workflows/dotnet.yml/badge.svg?branch=main) |

### Installation

To install, run `dotnet add package SharpServiceCollection` or
from [Nuget](https://www.nuget.org/packages/SharpServiceCollection)

### Introduction

`SharpServiceCollection` is a lightweight C# library that wraps the `IServiceCollection` interface to streamline
dependency injection by providing a more attribute driven declarative approach.

Annotate a class with `InjectableDependency` or `InjectableDependency<T>` and the library calls the
equivalent `Add*` / `TryAdd*` / `AddKeyed*` / `TryAddKeyed*` / `TryAddEnumerable` methods for you.

### Source-Generator based Registration (Compile-time and AOT-friendly)

The generator emits assembly-specific registration methods based on your `InjectableDependency` attributes,
so you can register dependencies without runtime reflection scanning:

- `AddAttributedServices()` is emitted as **internal** (callable only within the same assembly).
- Each project emits a public `AddAttributedServicesFrom{SanitisedAssemblyName}()` derived from its assembly name
(for example, `My.Module.Application` becomes `AddAttributedServicesFromMyModuleApplication`).

The existing reflection methods (`AddServicesFromAssembly*`) remain supported for backward compatibility.

### Reflection based Registration (No compile-time code generation, runtime assembly scanning)

Use one of the extension methods on `IServiceCollection`:

- `AddServicesFromCurrentAssembly()`
- `AddServicesFromAssembly(Assembly assembly)`
- `AddServicesFromAssemblyContaining<T>()`
- `AddServicesFromAssemblyContaining(Type type)`

### ASP.NET Core Setup

```csharp
using SharpServiceCollection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// choose one from the 3 options below to register services:

// Source-generated registration (compile-time)
builder.Services.AddAttributedServices(); // internal access modifer
builder.Services.AddAttributedServicesFromMyApplication(); // public access modifer

// reflection based assembly scanning (runtime)
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

- `TryAdd = true` (default) — first processed wins (lowest `Order`; tie → the earliest class name)
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
