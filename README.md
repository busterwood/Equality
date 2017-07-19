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

To compare ignoring case you can pass a `StringComparer` as the first argument:
```
IEqualityComparer<Test1> eq = Equality.Comparer<Test1>(StringComparer.OrdinalIgnoreCase, nameof(Test1.Name));
```

## Implementation

A class (and assembly) are dynamically generated at run-time using `System.Reflection.Emit`.

The generated class implements:
* `IEqualityComparer<T>.Equals(x, y)`, null parameters are handled when T is a class
* `IEqualityComparer<T>.GetHashCode(x)` a null parameter returns zero when T is a class

Both classes (reference types) and structs (value types) are supported.
