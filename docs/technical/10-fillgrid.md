## 10. FillGrid behavior

FillGrid runs when the definition says a changed parent requires it, including the defined firm-bind condition or when the target grid DC is empty. The adapter must return a declarative result:

```csharp
public sealed record FillGridResult(
    string TargetDc,
    FillGridMode Mode, // Append or Replace
    IReadOnlyList<AxDataRow> Rows,
    IReadOnlyList<AxError> Errors);
```

The application layer applies the returned mutation so it can version, audit, and persist the complete object consistently.
