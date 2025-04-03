### Installation

To install, run `dotnet add package SharpServiceCollection` or
from [Nuget](https://www.nuget.org/packages/SharpServiceCollection)

### Introduction

`SharpServiceCollection` is a lightweight C# library that wraps the `IServiceCollection interface to streamline
dependency injection through attribute-based assembly scanning.

### Usage

- `InstanceLifetime` is an Enum with the values `Singleton` `Scoped` `Transient`
- `IServiceCollection` comes with `Add*` and `TryAdd*` methods. `SharpServiceCollection` offers the same functionality.
- `SharpServiceCollection` will perform lexicographical sort on the class name to register dependency when duplicates
  occur. For example, `Foo` `Bar` `Baz` will be sorted as `Bar` `Baz` `Foo`, If three of them are resolved by
  `IDemoInterface`, Non `Try*` based attributes will register the last one, which is `Foo`, `Try*` based
  attributes will register the first one, which is `Bar`
- In general, it is better to use `Try*` based attributes.

```cs

using SharpServiceCollection.Attributes;
using SharpServiceCollection.Enums;

[TryResolveBy<IScopedDemoService>(InstanceLifetime.Scoped)]
[ResolveBy<IScopedDemoService>(InstanceLifetime.Scoped)]
public class ScopedDemoType : IScopedDemoService, IKeyedScopedDemoService
{
}

[KeyedTryResolveBy<IKeyedScopedDemoService>(InstanceLifetime.Scoped, "keyed")]
[KeyedResolveBy<IKeyedScopedDemoService>(InstanceLifetime.Scoped, "keyed")]
public class ScopedDemoType : IScopedDemoService, IKeyedScopedDemoService
{
}

[TryResolveBySelf(InstanceLifetime.Scoped)]
[ResolveBySelf(InstanceLifetime.Scoped)]
public class ScopedDependency : IScopedDependency
{
}

[TryResolveByMatchingInterface(InstanceLifetime.Scoped)]
[ResolveByMatchingInterface(InstanceLifetime.Scoped)]
public class ScopedDependency : IScopedDependency
{
}
```