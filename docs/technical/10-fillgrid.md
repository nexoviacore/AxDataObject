## 10. FillGrid behavior

FillGrid runs when the definition says a changed parent requires it, including the defined firm-bind condition or when the target grid DC is empty. The adapter must return a declarative result:

```csharp
public sealed record FillGridResult(
    string TargetDc,
    FillGridMode Mode, // Append or Replace
    IReadOnlyList<AxDataRow> Rows,
    IReadOnlyList<AxError> Errors);
```

The Set operation reuses its already loaded StructDef/query metadata. The application validates every returned operation, target, mode, field mapping, value set, and generated row identity into one complete plan before applying any operation. It then applies the plan to the fresh candidate and persists that complete object once under the Set lease.
