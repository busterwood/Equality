# Equality

Declarative run-time creation of IEqualityComparer<T>.  Supports NetStandard 1.6 and .NET 4.6.2.

For example, to get (or create) an equality comparer for the `Test1` class that just compares the `Id` property:
```
IEqualityComparer<Test1> eq = Equality.Comparer<Test1>(nameof(Test1.Name));
```
