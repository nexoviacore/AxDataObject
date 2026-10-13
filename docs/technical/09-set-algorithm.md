## 9. Set processing algorithm

```text
Load, authorize, lock, and version-check object
Resolve submitted DCs and rows against StructDef
For each submitted row in deterministic order:
    Validate row action and row identity
    For each submitted field in dependency-safe order:
        Check field write permission
        Set candidate value
        Call Validate/Parser adapter
        If invalid:
            record structured error
            do not evaluate dependencies sourced from this field
            continue with independent submitted fields
        Else:
            apply returned refreshed/dependent values
            record changes when GetDeps=true
Collect changed FillGrid parent fields
Execute applicable FillGrid definitions
Apply append/clear-and-add behavior from definition
Update object version and Redis once
Publish one complete persistence snapshot once
Return errors and/or dependent changes
```

The field order must be deterministic and compatible with existing Axpert semantics. If StructDef provides an evaluation order, that order is authoritative.
