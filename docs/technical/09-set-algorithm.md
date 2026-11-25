## 9. Set processing algorithm

```text
Authenticate, resolve tenant, and load by tenant-scoped ObjectId exactly once
Fail closed on payload/key identity mismatch and require Active lifecycle plus initiator access
Load fresh transaction/DC/field/method permission, then acquire the token-owned lock
Create one Set-owned current StructDef/Validate/Parser runtime (no post-lock Redis reload)
Build one indexed request preparation and a fresh candidate state
Resolve submitted DCs and rows against the operation's StructDef metadata
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
Read the complete processed runtime into the fresh candidate once
Collect changed FillGrid parent fields and execute applicable definitions from the same loaded definition
Validate the complete FillGrid plan, then apply append/clear-and-add behavior once
Atomically verify the lock token and update persistent Redis state once
Return errors and/or dependent changes
```

The field order must be deterministic and compatible with existing Axpert semantics. If StructDef provides an evaluation order, that order is authoritative. After rejection, transitive dependents are skipped; when an independent field remains, the operation rebuilds from the last accepted baseline and replays accepted submissions before continuing. A Redis miss returns `404 / AXD-NOTFOUND-001`; no snapshot recovery is attempted.
