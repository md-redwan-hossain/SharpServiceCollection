| Branch | Status                                                                                                                      |
|--------|-----------------------------------------------------------------------------------------------------------------------------|
| main   | ![Dotnet 8](https://github.com/md-redwan-hossain/SharpServiceCollection/actions/workflows/dotnet.yml/badge.svg?branch=main) |

### Installation

To install, run `dotnet add package SharpServiceCollection` or
from [Nuget](https://www.nuget.org/packages/SharpServiceCollection)

### Introduction

`SharpServiceCollection` is a lightweight C# library that wraps the `IServiceCollection interface to streamline
dependency injection through attribute-based assembly scanning.

### Usage

- `SharpServiceCollection` scans an assembly to automatically register services in the `IServiceCollection` container.
- Use one of the extension methods of `IServiceCollection` to perform assembly scanning.

    - `AddServicesFromCurrentAssembly()`
    - `AddServicesFromAssembly(Assembly assembly)`
    - `AddServicesFromAssemblyContaining<T>()`
    - `AddServicesFromAssemblyContaining(Type type)`

### Notes:

- `SharpServiceCollection` will perform lexicographical sort on the class name to register dependency when duplicates
  occur. For example, `Foo` `Bar` `Baz` will be sorted as `Bar` `Baz` `Foo`, If three of them are resolved by
  `IDemoInterface`, Non `Try*` based attributes will register the last one, which is `Foo`, `Try*` based
  attributes will register the first one, which is `Bar`
- `InstanceLifetime` is an Enum with the values `Singleton` `Scoped` `Transient`

### Unified Attribute Approach

The `InjectableDependencyAttribute` provides a unified approach to dependency registration with flexible resolution
strategies:

#### Non-Generic Version

```csharp
[InjectableDependency(InstanceLifetime lifetime, ResolveBy resolveBy)]
[InjectableDependency(InstanceLifetime lifetime, ResolveBy resolveBy, string key)]
```

**`ResolveBy` Enum:**

- `Self` - Resolves the class by itself
- `ImplementedInterface` - Resolves by all implemented interfaces
- `MatchingInterface` - Resolves by interface with matching name (e.g., `MyService` → `IMyService`)
- `SelfAndReplace` - Same as `Self` but uses `Add*` methods (replaces existing)
- `ImplementedInterfaceAndReplace` - Same as `ImplementedInterface` but uses `Add*` methods (replaces existing)
- `MatchingInterfaceAndReplace` - Same as `MatchingInterface` but uses `Add*` methods (replaces existing)

#### Generic Version

```csharp
[InjectableDependency<T>(InstanceLifetime lifetime)]
[InjectableDependency<T>(InstanceLifetime lifetime, bool replace)]
[InjectableDependency<T>(InstanceLifetime lifetime, string key)]
[InjectableDependency<T>(InstanceLifetime lifetime, bool replace, string key)]
```

**Parameters:**

- `replace`: `false` uses `TryAdd*` methods, `true` uses `Add*` methods
- `key`: For keyed services registration

### Example with Unified Attribute Approach

```csharp
public interface IUserService
{
    string GetUserInfo(int id);
}
```

```csharp
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

// Non-generic version
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface)]
public class UserService : IUserService
{
    public string GetUserInfo(int id) => $"User {id} information";
}

// Generic version
[InjectableDependency<IUserService>(InstanceLifetime.Singleton)]
public class CachedUserService : IUserService
{
    public string GetUserInfo(int id) => $"Cached User {id} information";
}
```

### Granular Attribute Approach

- `IServiceCollection` comes with `Add*` and `TryAdd*` methods. `SharpServiceCollection` offers the same functionality.
- In general, it is better to use `Try*` based attributes.

```csharp

using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

// The class will be resolvable by the interface specified in the generic argument
[TryResolveBy<IScopedDemoService>(InstanceLifetime.Scoped)]
[ResolveBy<IScopedDemoService>(InstanceLifetime.Scoped)]
public class ScopedDemoType : IScopedDemoService, IKeyedScopedDemoService
{
}

// The class will be resolvable by the interface specified in the generic argument and the key
[KeyedTryResolveBy<IKeyedScopedDemoService>(InstanceLifetime.Scoped, "keyed")]
[KeyedResolveBy<IKeyedScopedDemoService>(InstanceLifetime.Scoped, "keyed")]
public class ScopedDemoType : IScopedDemoService, IKeyedScopedDemoService
{
}

// The class will be resolvable by itself
[TryResolveBySelf(InstanceLifetime.Scoped)]
[ResolveBySelf(InstanceLifetime.Scoped)]
public class ScopedDependency : IScopedDependency
{
}

// This will work by convention
// The class must have to implement an interface that has the same name of the class prefixed with I
[TryResolveByMatchingInterface(InstanceLifetime.Scoped)]
[ResolveByMatchingInterface(InstanceLifetime.Scoped)]
public class ScopedDependency : IScopedDependency
{
}

// The class will be resolvable by any of the implementeed interface
[TryResolveByImplementedInterface(InstanceLifetime.Scoped)]
[ResolveByImplementedInterface(InstanceLifetime.Scoped)]
public class FooBarBaz : IFoo, IBar, IBaz
{
}
```

### Example With Granular Attribute Approach

```csharp

public interface IDemoService
{
    string Greet();
}
```

```csharp
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

[TryResolveByImplementedInterface(InstanceLifetime.Scoped)]
public class DemoService : IDemoService
{
    public string Greet()
    {
        return "Hello World!";
    }
}
```

```csharp
using SharpServiceCollection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Current assembly will be scanned by Assembly.GetCallingAssembly()
builder.Services.AddServicesFromCurrentAssembly();

var app = builder.Build();

app.MapGet("/", (IDemoService demoService) => demoService.Greet());

app.Run();
```
