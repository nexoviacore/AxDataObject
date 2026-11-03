# ARM-DEP-00 parity characterization

Date: 2026-10-30  
Scope: current ARM dependency/validation behavior only; no production behavior changed.  
Repeatable command: `& .\ARM-11.4\AxDependency.Characterization\Invoke-Characterization.ps1`

## Result

The package-free ARM-owned harness passes 14 source-locked characterization checks. It reads the current ARM sources and the supplied Pascal reference, exercises deterministic graph/cache models matching the identified routines, and exits nonzero when a frozen source boundary or observation changes.

The literal `branch` XML referred to by the task was not present in `uStructDef.pas`. The harness therefore labels its `branch -> f,s` case as synthetic and does not infer any additional tags or row metadata.

## Root-cause evidence

- Current C# `StructDef.SetXML` calls `XLoadField`, but `GetDeps(ref field, x)` and `UpdateHasGridParents()` are commented and `a58` is absent. Current `TField.Deps`/`Dependents` therefore remain unprojected by structure loading.
- Pascal `xloadfield` calls `getdeps(fld, x)`. `GetDeps` reads `a58`, traverses downstream field names, deduplicates them, and removes the source from the resulting closure.
- Current `ValidateData` nests frame, applicable-row, and field loops. Every visited field calls `RefreshFieldAsync` and then `RapidValidateData`; there is no changed-source or dependency filter.
- `fillSqlResultDict` is per `Validate` instance but add-once for a `field + row` key. A later linked field reads the first stored result even after the same source query runs again.
- `ErrorLog`, `LinkSearchCol`, `actualRowNo`, and `forPickList` are mutable static fields. The constructor resets shared values, so two otherwise independent instances can overwrite one another's execution state.
- `RapidValidateData` checks `AxParser.Evaluate`; the failure branch returns before reading `Parser.Value`, so the characterized parser-failure path does not consume a stale value.

Two checks distinguish the plausible competing causes:

- Stale selection: the controlled repeated-source case executes two source queries with distinct generations but observes generation 1 twice. This separates the add-once dictionary lifetime from a lower query executor that failed to run.
- Cross-instance state: the source lock identifies state declared `static` and reset by each constructor. The interference does not require a shared parser, StructDef, or Common instance.

## Pascal dependency semantics

- Structured tags recognized by `GetDeps`: `e`, `s`, `f`, `ag`, `fg`, `dc`, `lbl`, with `sf` separately assigning dependent frames.
- Ordinary tags add downstream field names. `fg` stores `#name`, `dc` stores `$name`, and `lbl` stores `~name` in compatibility `Deps`.
- The traversal is breadth-first in encounter order and deduplicates ordinary field names. Unknown targets remain in the closure; traversal stops when no matching field XML exists.
- Cycles terminate because the source starts in the visited list and every addition is deduplicated.
- Text `a58` also builds `Dependents` and one-character `DependentTypes`. A duplicate `s` can replace an earlier `e`; `d` contributes dependent-frame information and `g` is not added to ordinary closure.
- `UpdateHasGridParents` marks a dependent when source and target are in the same grid frame, or when the target frame is a popup.
- `OrderDeps` sorts dependents by frame/popup position, field order, and dependency type.

## Exact controlled counters

| Fixture | Changed sources | Rows | Refresh calls | Validation calls | Query executions |
|---|---:|---:|---:|---:|---:|
| Synthetic `branch`, `f`, `s` in non-grid row 1 | 1 | 1 | 3 | 3 | 2 |
| Same three fields in a two-row same-frame grid | 1 | 2 | 6 | 6 | 4 |

These counts are intentionally full-scan counts: `branch` is the only changed source, while `f` and `s` are the two controlled query-backed fields. The harness locks the current loop and call sites before asserting the arithmetic.

Repeated source selection in one runtime records: `queries=2`, first observed result `query-1:north`, second observed result `query-1:north`, `stale=true`.

## Graph cases

| Case | Observed downstream closure |
|---|---|
| Synthetic branch | `f,s` |
| Direct | `b` |
| Transitive | `b,c` |
| Diamond | `b,c,d` |
| Unknown target | `missing` |
| Cycle | `b` |

## AxPut parity trace

1. Create a fresh `Validate` for the current outer AxPut transaction.
2. Load and attach one `StructDef`.
3. Obtain and attach Parser.
4. Register `recordid` and set `LastSavedRecordid`.
5. Call `PrepareExps`; select the client/global date format.
6. Call and check `InitValidateAsync`.
7. For edit, AxGet loads the baseline into the same runtime through `SubmitValueOnLoad`.
8. Reconcile row/record identities and submit changed values.
9. Call and check `ValidateData` before SQL generation or persistence.
10. GenMap can apply another mapping and validate the same target runtime; the outer loop calls `LoadStructure` and creates a fresh runtime for the next transaction.

No parity step was deliberately skipped.

## ARM-DEP-01 stop conditions

The following must be resolved explicitly; ARM-DEP-01 must not guess:

- Obtain the original supplied `branch` XML and confirm whether `f` and `s` are tag names, field names, or both in that example.
- Confirm whether structured `a58` must populate `Dependents`/`DependentTypes` as text `a58` does. The Pascal structured branch builds compatibility `Deps`, but does not visibly mirror the text branch's direct-dependent/type initialization.
- Confirm whether `Deps` is the downstream transitive closure for every structured tag or only ordinary field tags; prefixed `#`, `$`, and `~` entries are not traversed as field names.
- Confirm `ag` semantics. Pascal routes it through ordinary `AddDeps`, but the tag's behavioral distinction is not represented in the compatibility list.
- Confirm whether repeated `lbl` values should deduplicate. Pascal checks the unprefixed name but stores `~name`.
- Confirm unknown-target policy. Pascal preserves unknown names in the closure, while `UpdateHasGridParents` logs and skips them and `OrderDeps` appears to assume the target exists.
- Confirm row execution semantics. Pascal dependency metadata is field-name based; `HasGridParents` marks same-frame grid relationships but does not define whether evaluation is current-row, all-row, or another mapping.
- Confirm how `sf` frame identifiers map to grid rows and popup frames.
- Confirm cycle policy beyond termination: retain the reachable closure, reject the definition, or report a diagnostic.

Existing ARM field-value logging was intentionally left unchanged and is outside this task.
