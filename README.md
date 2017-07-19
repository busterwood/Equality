# Equality

[![Nuget](https://img.shields.io/nuget/v/BusterWood.Equality.svg)](https://www.nuget.org/packages/BusterWood.Equality)

Declarative run-time creation of `IEqualityComparer<T>`.  Supports NetStandard 1.6 and .NET 4.6.2.

For example, to get (or create) an equality comparer for the `Test1` class that just compares the `Id` property:
```
IEqualityComparer<Test1> eq = Equality.Comparer<Test1>(nameof(Test1.Id));
```

To compare multiple properties:
```
IEqualityComparer<Test1> eq = Equality.Comparer<Test1>(nameof(Test1.Id), nameof(Test1.Name));
```

## Implementation

A class (and assembly) are dynamically created at run-time using `System.Reflection.Emit`.
