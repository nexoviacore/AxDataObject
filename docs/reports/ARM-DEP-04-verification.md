# ARM-DEP-04 AxPut integration verification

Date: 2026-11-02  
Scope: current ARM AxPut create/edit/GenMap integration only; no AxData product or slice files changed.

## Result

Current ARM AxPut now performs one full initialization refresh/validation for create and new-record GenMap operations. Hydrated edits retain targeted dependency refresh/validation followed by mandatory full current-value validation. If targeted execution finds a required parent row that has not been initialized, it performs one compatibility full pass and does not scan the form again. Dependency or validation failure stops before SQL generation. The public AxPut request/response contract, persistence commands, transaction boundary, authorization, record allocation, attachments, GenMap mapping, and MdMap behavior are unchanged.

The package-free ARM-owned harness passes 32 checks. The Release project graph builds without restore as part of the regression runs, and the AxData unit, integration, and contract tests pass.

## Root cause and parity

The accepted legacy AxPut parity sequence was already intact:

1. Create a fresh `Validate` runtime for each outer transaction.
2. Load StructDef, attach Parser, register `recordid`, set `LastSavedRecordid`, and call `PrepareExps`.
3. Select the client/global date format and check `InitValidateAsync`.
4. For edit, load the baseline into the same runtime through `SubmitValueOnLoad`.
5. Submit request values or apply the selected GenMap buffer record.
6. Validate before SQL generation and persistence.

The first ARM-DEP-04 integration replaced step 6 uniformly with targeted execution. The attached failing create trace showed why that was not parity-safe: unlike a hydrated edit, a new runtime has not had its calculated/session-derived parents populated. The targeted customer path therefore reached its linked query while `company`/`m_companyid` was still effectively zero. The working trace first ran the legacy full `ValidateData` sweep and populated `company` before evaluating customer/item queries.

Step 6 is now operation-aware: create/new GenMap runs the legacy-equivalent full initialization pass exactly once; hydrated edit runs targeted execution followed by mandatory final validation. A missing required parent row is an unsafe targeted state and selects the same one-pass compatibility fallback. A normal business validation failure is returned directly and is never retried.

## Integration behavior

- Create/edit use `SubmitValueDetailed`. Successful submission entries supply exact DC/frame/row/field roots; baseline-load entries are not roots.
- Delete-only rows do not produce dependency roots.
- GenMap roots come from the value operations actually applied to the selected `GenMapBuffer` record. Delete operations are excluded, and multi-record non-grid rows are translated to target row 1 exactly as `ApplyGenMapBuffer` does.
- Create and new-record GenMap call `ValidateDataDetailed` once after submission/mapping, populating session-derived calculations before linked selections are evaluated.
- Hydrated edits call `RefreshAndValidateChangesAsync` only when roots exist. Any error stops the transaction before SQL construction.
- Submitted select roots are refreshed before targeted validation so their normalized source IDs are resolved; unchanged text can no longer be persisted as source ID zero.
- Targeted execution checks reverse ordinary dependencies using the same parent-row presence semantics as Delphi `FieldParentsBound`; an absent or unsupported parent mapping requests `AXV-DEP-W001` compatibility execution.
- A successful compatibility full pass is already the final validation and is not followed by another full scan.
- `ValidateCurrentValuesAsync(reusePreparedQueryResults: true)` always runs before SQL. It reuses an exact runtime/source/row query result prepared by targeted execution and queries any current selection without one.
- The existing parameterless `ValidateCurrentValuesAsync()` retains its previous query behavior.
- Compatibility fallback remains available for unresolved, malformed, cyclic, special, popup, unsupported cross-grid, or uninitialized-parent metadata/state. `AXV-DEP-W001` is written to ARM trace output without changing the public API.
- Repeated GenMap validation on one target runtime keeps call-local outcomes. Changed source/row queries are invalidated and replaced; unrelated valid cache entries may be reused by final validation.

## Controlled counters and persistence parity

For a repeated same-runtime sparse edit with one reachable query and one still-valid unrelated query result:

| Path | Refresh calls | Query executions | Persistence commands |
|---|---:|---:|---|
| Compatibility full scan | 3 | 2 | `persist:branch` |
| Targeted plus cache-aware final validation | 1 | 1 | `persist:branch` |

The targeted path lowers both refresh and query work while retaining the same persistence command. A cold final validation still queries any untouched selection lacking a valid runtime/source/row cache entry so untouched invalid data cannot escape validation.

## Stable ARM handoff contract

- Dependency metadata: `StructDef.DependencyMetadata`, `GetDirectDependencies(source)`, and `GetDependencyParents(target)` remain authoritative. `RequiresCompatibilityFallback` means callers must accept compatibility execution.
- Changed address: `ValidationAddress` is `{ DataContainer, FrameNo, RowNo, FieldName }`; non-grid fields use row 1.
- Submission: `SubmitValueDetailed` returns successful roots in `ProcessedFields` with category `Submission`, plus call-local warnings/errors and `LegacyMessage`.
- Targeted execution: `RefreshAndValidateChangesAsync(addresses)` validates roots and reachable ordinary dependencies in stable breadth-first order; failed or unchanged nodes stop only their downstream path.
- Final execution: `ValidateCurrentValuesAsync()` preserves compatibility behavior. The additive Boolean overload permits reuse of exact prepared query results while still validating every current field.
- Failure boundary: callers must check `ValidationInvocationResult.Success` before reading a successful outcome or constructing persistence SQL.
- Parser compatibility: decimal `abs` operands are supported. A missing direct optional variable preserves the field's pre-evaluation value, matching Delphi without reading stale `Parser.Value`; compound or other failed expressions still fail closed.
- Fallback observability: warning code `AXV-DEP-W001`; no public AxPut response change.

AxData consumption of this contract remains a separate, blocked handoff task.

## Verification

- `ARM-11.4/AxDependency.Characterization/Invoke-Characterization.ps1`: 32 passed, 0 failed.
- Release no-restore compilation passed as part of the serial test runs; only pre-existing NuGet compatibility/security warnings remain.
- AxData regression tests: 159 unit, 39 integration, and 150 contract tests passed.
- `git diff --check` passed for the ARM-DEP-04 production and harness files.
- Postman: not applicable; no external HTTP or AXD contract changed.

Existing ARM field-value logging was intentionally left unchanged.
