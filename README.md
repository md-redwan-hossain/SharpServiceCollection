| Branch | Status                                                                                                                      |
|--------|-----------------------------------------------------------------------------------------------------------------------------|
| main   | ![Dotnet 8](https://github.com/md-redwan-hossain/SharpServiceCollection/actions/workflows/dotnet.yml/badge.svg?branch=main) |

### Installation

To install, run `dotnet add package SharpServiceCollection` or
from [Nuget](https://www.nuget.org/packages/SharpServiceCollection)

### Introduction

`SharpServiceCollection` is a lightweight C# library that wraps the `IServiceCollection` interface to streamline
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
  occur. For example, `Foo` `Bar` `Baz` will be sorted as `Bar` `Baz` `Foo`. If three of them are resolved by
  `IDemoInterface`, attributes with `TryAdd = false` will register the last one, which is `Foo`. Attributes with
  `TryAdd = true` (the default) will register the first one, which is `Bar`.
- `InstanceLifetime` is an Enum with the values `Singleton` `Scoped` `Transient`

**`ResolveBy` Enum:**

- `Self` - Resolves the class by itself
- `ImplementedInterface` - Resolves by all implemented interfaces
- `MatchingInterface` - Resolves by interface with matching name (e.g., `MyService` → `IMyService`)

### Attributes

The `InjectableDependencyAttribute` provides a unified approach to dependency registration with flexible resolution
strategies:

#### Non-Generic Version

```csharp
[InjectableDependency(InstanceLifetime lifetime, ResolveBy resolveBy)]
```

Optional properties (set in attribute syntax):

```csharp
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface, TryAdd = false)]
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self, Key = "my-key")]
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface, TryAdd = false, Key = "my-key")]
```

**Optional properties:**

- `TryAdd` (default `true`) - When `true`, uses `TryAdd*` / `TryAddKeyed*` methods (skips registration if the service is already registered). When `false`, uses `Add*` / `AddKeyed*` methods.
- `Key` - For keyed services registration
- `Enumerable` - When `true` with `TryAdd = true`, registers as enumerable (multiple implementations)

#### Generic Version

```csharp
[InjectableDependency<T>(InstanceLifetime lifetime)]
```

Optional properties:

```csharp
[InjectableDependency<IUserService>(InstanceLifetime.Singleton, TryAdd = false)]
[InjectableDependency<IUserService>(InstanceLifetime.Singleton, Key = "my-key")]
[InjectableDependency<IUserService>(InstanceLifetime.Singleton, TryAdd = false, Key = "my-key")]
```

**Optional properties:**

- `TryAdd` (default `true`) - When `true`, uses `TryAdd*` / `TryAddKeyed*` methods. When `false`, uses `Add*` / `AddKeyed*` methods.
- `Key` - For keyed services registration
- `Enumerable` - When `true` with `TryAdd = true`, registers as enumerable (multiple implementations)

### Example

```csharp
public interface IUserService
{
    string GetUserInfo(int id);
}
```

```csharp
using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

// Non-generic version (TryAdd defaults to true)
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.ImplementedInterface)]
public class UserService : IUserService
{
    public string GetUserInfo(int id) => $"User {id} information";
}

// Non-generic version with Add* registration
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.MatchingInterface, TryAdd = false)]
public class PrimaryUserService : IUserService
{
    public string GetUserInfo(int id) => $"User {id} information";
}

// Generic version (TryAdd defaults to true)
[InjectableDependency<IUserService>(InstanceLifetime.Singleton)]
public class CachedUserService : IUserService
{
    public string GetUserInfo(int id) => $"Cached User {id} information";
}

// Keyed registration
[InjectableDependency<IUserService>(InstanceLifetime.Scoped, Key = "cached")]
public class KeyedUserService : IUserService
{
    public string GetUserInfo(int id) => $"Keyed User {id} information";
}

// Self registration
[InjectableDependency(InstanceLifetime.Scoped, ResolveBy.Self)]
public class ScopedDependency : IScopedDependency
{
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
