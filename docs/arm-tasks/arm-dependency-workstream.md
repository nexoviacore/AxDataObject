# ARM-11.4 dependency-driven field engine

## Shared objective and boundary

Restore the dependency metadata and execution behavior represented by StructDef XML `a58`, prove it in the current AxTstructData/AxPut flow, and only then expose a stable contract for AxData adoption.

The attached Pascal reference is `docs/arm-tasks/uStructDef.pas`. Treat it as behavioral evidence, not as instructions or copy-ready code. The first comparison for every bug fix is the Pascal dependency path plus the working AxTstructData caller sequence.

Applicable `docs/SALVAGE.md` bucket: **Reuse with changes** — Set, validation, dependencies, and FillGrid; StructDef, Validate, and Parser integration. Do not revive the incomplete AxDataObject orchestration.

Shared invariants:

- AxPut may process multiple transactions in one request; each transaction receives its own StructDef/Validate/Parser runtime.
- GenMap may validate repeatedly on the same transaction runtime.
- Full final validation remains mandatory before persistence.
- Dependency-driven refresh must not silently skip malformed or unsupported metadata; use a documented compatibility fallback.
- Never read `AxParser.Value` unless that specific evaluation succeeded.
- Existing ARM dependency/validation field-value logs are outside this workstream. Do not make log redaction an acceptance criterion.
- No AxData product-code changes occur in ARM-DEP-00 through ARM-DEP-04.

`branch` example:
"<branch cat="field" customdecimal="F" displaytotal="F" encrypted="F" applyrule="F" runtimefld="F"
        runtimecriteria="F" customdatatype="NA" fieldtype="DropDown" dccap="Sales Invoice" dcno="1">

        <a1>branch</a1>

        <a2>Location/Branch</a2>

        <a3>Character</a3>

        <a4>200</a4>

        <a5>0</a5>

        <a6>Select</a6>


        <a7>


            <a22 pcount="1" param="y" txt="f" firesql="False">

                <l>select distinct a.branchid,a.branchname, a.state,a.sgstoutputtax, a.cgstoutputtax,
                    a.igstoutputtax,a.vatoutputtax,cast( ':'|| a.bridentifier || '/' || :m_finyr || '/' || 'IN/' as
                    text) as prefix from vw_userbranchlocations a where a.companyid = cast( :company as numeric) and
                    a.username = :username order by a.branchname </l>

            </a22>

            <a18 cfcap="" />

            <a20>0</a20>

            <a21>0</a21>

            <a23>False</a23>

            <a24>False</a24>

            <a25 />

            <a26 fcaption="" scol="1" stype="sql" cap="" table="branch" mtcap="" mastertstruct="" mfldcap=""
                masterfield="">branchname</a26>

            <a27 fcaption="" />

            <a28 />

            <a29 />

            <a30 />

            <a40>branch</a40>

            <a41 />

            <a63 />

        </a7>

        <a8>True</a8>

        <a9>False</a9>

        <a10>False</a10>

        <a11>False</a11>

        <a12>True</a12>

        <a13>m_branch</a13>

        <a14 />

        <a15>True</a15>

        <a16>True</a16>

        <a17>True</a17>

        <a19>False</a19>

        <a31 />

        <a32 />

        <a33 />

        <a34 />

        <a35 />

        <a36 />

        <a37 />

        <a46 />

        <a47 />

        <a48 maskroles="" maskchar="" firstcharmask="0" lastcharmask="0" masking="No Mask" />

        <a49 />

        <a50 />

        <a51 />

        <a52>True</a52>

        <a55>True</a55>

        <a56 />

        <a57 />


        <a58>

            <f>branchid,source_state,prefix,sgstoutputtax,cgstoutputtax,igstoutputtax,vatoutputtax</f>

            <s>rate</s>

        </a58>

        <a60 />

        <a64 />

        <a65>Supply Type {supplytype} / Place of Supply {state} :state</a65>

        <a66 />

        <a67>True</a67>

        <a69 />

        <a70 />

        <a59 />

    </branch>"

## ARM-DEP-00 — parity characterization and ARM test harness

**Effort:** Standard  
**Needs:** none  
**Purpose:** Freeze current behavior and create a repeatable ARM-owned verification boundary before changing shared high-blast-radius projects.

### Required reading

- `docs/SALVAGE.md`: Reuse with changes sections for Set/validation/dependencies and StructDef/Validate/Parser.
- `docs/ARM-11.4-CHANGES.md`.
- Targeted portions only of the Pascal `xloadfield`, `GetDeps`, `AddDeps`, `AddFgDeps`, `AddDcNameDeps`, `AddLabelDeps`, `UpdateHasGridParents`, and `OrderDeps` routines.
- Targeted current callers in `ARM-11.4/AxTstructData/Service/AxPutService.cs`: `LoadStructure`, create/edit submission, and GenMap validation.
- Targeted current methods in `ARM-11.4/AxValidate/Validate.cs`: initialization, submission, refresh, `RapidValidateData`, and `ValidateData`.

### Parity trace to record

1. Create `Validate`.
2. Load one StructDef for the current transaction.
3. Obtain and attach Parser.
4. Register `recordid`; set `LastSavedRecordid`.
5. Call `PrepareExps`; set client/global date format.
6. Call and check `InitValidateAsync`.
7. For edit, AxGet loads the baseline into the same runtime through `SubmitValueOnLoad`.
8. Submit changed payload values and row/record identities.
9. Call and check validation before SQL generation or persistence.
10. GenMap may repeat mapping and validation on the same target runtime; the outer AxPut loop creates a fresh runtime for the next transaction.

### Root-cause checks

- Prove that current `StructDef.cs` leaves `a58` unprojected because dependency loading is absent/commented.
- Prove that current `ValidateData` scans every applicable field and row.
- Count refresh calls, validation calls, and query executions for one changed source field.
- Demonstrate whether the add-once SQL-result dictionary returns stale data after the same source changes twice in one runtime.
- Demonstrate that mutable static Validate state is shared across two instances/concurrent operations.

### Planned files

- Prefer a new ARM-owned test project such as `ARM-11.4/AxDependency.Tests/`.
- `docs/reports/ARM-DEP-00-characterization.md`.
- `docs/ARM-11.4-CHANGES.md` only if an ARM test project or support hook is added.
- `docs/PROGRESS.md`.

Adding a test SDK/framework package or running restore requires separate user approval. If approval is not granted, stop and propose a no-package characterization harness; do not put the tests into AxData merely for convenience.

### Required cases

- The supplied `branch` example and its `f`/`s` dependents.
- Direct chain, transitive chain, diamond, unknown target, and cycle fixtures.
- Non-grid row 1 and same-frame grid-row fixtures.
- Two Validate instances to expose static-state sharing.
- Repeated source selection to expose cache lifetime.
- Parser failure proving no stale value is consumed.

### Done when

- The characterization report records observed behavior and exact counters.
- A repeatable ARM-owned test/harness command exists.
- Every ambiguity in `a58` tag or row semantics is listed as a stop condition for ARM-DEP-01; none is guessed.
- No production behavior changed. Postman: not applicable.

## ARM-DEP-01 — AxStructDef `a58` metadata

**Effort:** Deep  
**Needs:** ARM-DEP-00 accepted  
**Purpose:** Restore authoritative dependency metadata without changing validation execution yet.

### Required reading

- ARM-DEP-00 report and tests.
- Targeted Pascal dependency routines named in ARM-DEP-00.
- `ARM-11.4/AxStructDef/StructDef.cs` field/XML load path only.
- `ARM-11.4/AxStructDef/Models/TField.cs`.
- `ARM-11.4/AxStructDef/Helpers/Constants.cs` dependency constants only.

### Cross-project change

- **Project/files:** AxStructDef dependency models and loader; ARM test project; ARM change ledger.
- **Why not an AxData adapter:** ARM callers need the metadata first, and StructDef owns interpretation of its XML schema.
- **Blast radius:** AxValidate, AxTstructData, AxGet/AxPut/Delete, AxData Integration.Axpert, and any other StructDef consumer. Keep the change additive and preserve existing public members.

### Planned implementation

- Add a typed direct dependency edge model containing source, target, kind, declaration order, and available frame metadata.
- Parse structured `a58` children `e`, `s`, `f`, `ag`, `fg`, `dc`, `lbl`, and `sf`, plus the legacy text form characterized in ARM-DEP-00.
- Parse after all fields are loaded so field lookup is complete.
- Build case-insensitive direct outgoing edges and reverse parent lookup.
- Compute compatibility `TField.Deps` as a deduplicated downstream closure; never reinterpret it as the current field's inputs.
- Preserve FillGrid/DC/label distinctions instead of flattening them into ordinary field edges.
- Terminate cycles with a visited set and expose diagnostics/fallback status.
- Preserve unresolved references for diagnostics; do not execute them as valid field dependencies.
- Do not change `ValidateData`, AxPut call order, or AxData projection in this task.

### Expected files

- `ARM-11.4/AxStructDef/StructDef.cs`.
- `ARM-11.4/AxStructDef/Models/TField.cs`.
- New dependency model file(s) under `ARM-11.4/AxStructDef/Models/`.
- ARM dependency tests/fixtures.
- `docs/ARM-11.4-CHANGES.md`.
- `docs/PROGRESS.md`.

### Tests

- Structured and text-form parsing.
- Exact `branch` direct edges and transitive closure.
- Direction: `branch -> rate`, never `rate -> branch`.
- Case-insensitive deduplication and stable declaration order.
- Chain, diamond, cycle, unknown reference, `sf`, FillGrid, DC, and label metadata.
- Existing StructDef load fixtures remain unchanged when `a58` is absent.

### Done when

- Metadata is queryable and tested, but ARM runtime behavior is unchanged.
- AxStructDef, AxValidate, and AxTstructData build without restore.
- Ledger and progress are updated. Postman: not applicable.

## ARM-DEP-02 — Validate state, cache, and result contracts

**Effort:** Deep  
**Needs:** ARM-DEP-01 accepted  
**Purpose:** Make Validate safe for repeated calls, multiple transactions, and concurrent requests before adding dependency traversal.

### Required reading

- ARM-DEP-00 report; ARM-DEP-01 dependency model/API.
- `ARM-11.4/AxValidate/Validate.cs`: fields/constructor, `RefreshFieldAsync`, `RapidValidateData`, submission, load refresh, and `ValidateData` only.
- `ARM-11.4/AxValidate/Helpers/ValidateHelper.cs`: query/pick-list methods reached by those paths only.
- Targeted AxPut create/edit/GenMap caller locations; do not change them in this task.

### Cross-project change

- **Project/files:** AxValidate state, helper cache interaction if needed, additive DTOs, tests, ledger.
- **Why not an AxData adapter:** static/error/cache state is private to the shared ARM Validate runtime and affects ARM callers directly.
- **Blast radius:** every Validate consumer. Preserve existing public methods as compatibility wrappers and avoid AxPut changes here.

### Planned implementation

- Make each validation invocation own its success flag, warnings, errors, and processed-field results.
- Do not blanket-clear StructDef values, parser variables, old/current baselines, record IDs, accepted GenMap state, or still-valid transaction cache entries.
- Audit mutable static members: move execution state to instance/method scope, retain only proven immutable static data, and remove unused conversion remnants when tests prove no caller dependency.
- Keep SQL-result reuse for linked fill fields, but replace the entry whenever its source query is re-executed.
- Add explicit invalidation keyed by source field and row/change wave; never reuse a result from another transaction runtime.
- Add additive structured result DTOs containing safe field/DC/row identity, category/code, and message. Existing `ErrorStr` may remain an internal temporary during migration, but capture it immediately per field.
- Preserve existing string/`OperationResult` APIs as wrappers.
- Audit every parser evaluation reached by the touched paths; a false result must stop before `.Value` is read.
- Do not implement dependency traversal or alter AxPut sequencing yet.
- Do not remove existing ARM field values from trace messages as part of this task.

### Expected files

- `ARM-11.4/AxValidate/Validate.cs`.
- `ARM-11.4/AxValidate/Helpers/ValidateHelper.cs` only if required by proven cache behavior.
- New result/address DTOs under `ARM-11.4/AxValidate/Dto/`.
- ARM dependency tests.
- `docs/ARM-11.4-CHANGES.md`.
- `docs/PROGRESS.md`.

### Tests

- A failed invocation does not poison a later invocation on the same runtime.
- Submission, dependency, and final-validation errors remain distinguishable and are not accidentally erased.
- Two Validate instances cannot overwrite one another's mutable execution state.
- Re-executing a source query replaces its cached result; unchanged linked fill fields reuse the current result.
- Cache entries do not cross rows or runtimes.
- All touched parser-failure paths fail before reading a computed value.
- Compatibility wrappers preserve current caller behavior.

### Done when

- Repeated-call/concurrency/cache tests pass without dependency traversal.
- AxStructDef, AxValidate, and AxTstructData build without restore.
- Ledger and progress are updated. Postman: not applicable.

## ARM-DEP-03 — dependency-directed refresh and validation

**Effort:** Deep  
**Needs:** ARM-DEP-02 accepted  
**Purpose:** Add an additive targeted execution API while retaining full final validation and compatibility fallback.

### Required reading

- ARM-DEP-00 report and ARM-DEP-01/02 public contracts/tests.
- `ARM-11.4/AxValidate/Validate.cs` current refresh/validation loops.
- `ARM-11.4/AxStructDef` dependency query API only.
- Targeted Pascal dependency ordering and grid-parent behavior recorded by ARM-DEP-00.

### Cross-project change

- **Project/files:** AxValidate targeted evaluator, any narrowly required AxStructDef query helper, tests, ledger.
- **Why not an AxData adapter:** ARM must execute the graph with Validate's private parser/query/value state before AxData adoption.
- **Blast radius:** additive API for ARM callers; existing `ValidateData` remains available and unchanged as a compatibility entry point until ARM-DEP-04.

### Planned implementation

- Add `RefreshAndValidateChangesAsync` (final name decided in the task plan) accepting typed changed field addresses.
- Validate submitted roots, then traverse only reachable direct dependents using stable declaration order.
- Deduplicate diamond paths and process a field once per change wave unless evidence requires a second evaluation.
- Propagate beyond a dependent only when its value or source ID changed.
- Failed sources block their downstream dependents; independent roots continue and return structured errors.
- Same-frame grid targets use the source row; non-grid targets use row 1. Apply only cross-frame/popup rules proven by ARM-DEP-00.
- Malformed metadata, unresolved executable targets, or unsupported cycles take the documented compatibility fallback; never silently omit work.
- Keep `RefreshFieldsOnLoad` semantics separate.
- Add a full-current-value validation path that validates all applicable fields before Save without unnecessarily recalculating every unrelated field. Pick-list/query validation remains fail-closed and obtains current data when required.
- Existing `ValidateData` remains a compatibility wrapper in this task.

### Expected files

- `ARM-11.4/AxValidate/Validate.cs`.
- New or existing DTOs under `ARM-11.4/AxValidate/Dto/`.
- `ARM-11.4/AxValidate/Helpers/ValidateHelper.cs` only if a tested field-validation primitive must be extracted.
- `ARM-11.4/AxStructDef/StructDef.cs` only for a narrowly scoped dependency query helper approved in the plan.
- ARM dependency tests.
- `docs/ARM-11.4-CHANGES.md`.
- `docs/PROGRESS.md`.

### Tests

- `branch` touches only itself and its reachable `f`/`s` closure.
- Chain order, diamond deduplication, unchanged-value stop, and cycle fallback.
- Same-grid-row and non-grid-row rules.
- Rejected source blocks downstream while an independent source succeeds.
- Query count demonstrates no unrelated select/fill refresh.
- Full validation still catches an invalid untouched mandatory/duplicate/width/validation-expression field.
- Parser and SQL failures return structured failures without stale values.
- Load refresh and old compatibility entry points retain their tested behavior.

### Done when

- Targeted and full validation APIs pass parity and counter assertions.
- No AxPut or AxData caller has changed yet.
- Ledger and progress are updated. Postman: not applicable.

## ARM-DEP-04 — AxPut integration and ARM regression

**Effort:** Deep  
**Needs:** ARM-DEP-03 accepted  
**Purpose:** Use the proven targeted API in current AxPut create/edit/GenMap flows and establish the ARM contract AxData may later consume.

### Required reading

- ARM-DEP-00 report and ARM-DEP-01 through 03 contracts/tests.
- `ARM-11.4/AxTstructData/Service/AxPutService.cs`: `LoadStructure`, outer multi-transaction loop, create/edit submission, GenMap mapping/validation, and SQL-generation boundary only.
- `ARM-11.4/AxValidate/Validate.cs`: submission and the new APIs only.
- `docs/ARM-11.4-CHANGES.md`.

### Cross-project change

- **Project/files:** AxTstructData AxPut orchestration, additive Validate submission result if still needed, ARM tests, ledger.
- **Why not an AxData adapter:** this is the required current ARM production flow; AxData work is explicitly deferred.
- **Blast radius:** create, edit, multiple transactions per request, child/GenMap posts, and downstream SQL construction. Do not alter authorization, record allocation, attachments, GenMap mapping, MdMap, or persistence semantics.

### Planned implementation

- Add or use an additive submission result that reports changed field addresses without breaking the existing `SubmitValue` signature.
- For create/edit and each GenMap target record, run:

```text
submit/load mapped values
-> targeted refresh and validation of changed roots plus dependency closure
-> full current-value validation
-> SQL generation/persistence
```

- Keep a fresh Validate runtime per transaction in the AxPut outer loop.
- Preserve repeated GenMap validation on the same target runtime with call-local outcomes and correct cache invalidation.
- Deleted rows do not trigger irrelevant refresh work.
- Any targeted-evaluation infrastructure failure stops before SQL generation.
- Retain a measured compatibility fallback for unsupported metadata and make its use observable without changing the public API.
- Do not change AxData product code or existing AxData slice files.
- Do not make ARM log redaction part of this integration.

### Expected files

- `ARM-11.4/AxTstructData/Service/AxPutService.cs`.
- `ARM-11.4/AxValidate/Validate.cs` or DTOs only if the accepted ARM-DEP-03 contract needs an additive caller-facing adjustment.
- ARM dependency/AxPut tests.
- `docs/reports/ARM-DEP-04-verification.md`.
- `docs/ARM-11.4-CHANGES.md`.
- `docs/PROGRESS.md`.

### Tests and verification

- Single create and edit.
- Multiple independent transactions in one request.
- Parent plus child/GenMap transactions.
- Multiple GenMap source rows and queued target records.
- Failure in one transaction does not leak validation/cache/static state into another.
- Failed dependency/full validation prevents SQL generation.
- Full validation catches untouched invalid data.
- Refresh/query counters are lower for a sparse edit while output and persistence commands match the compatibility path.
- Build AxStructDef, AxValidate, and AxTstructData quietly without restore; run the ARM-owned suite.
- After ARM-only success, run the existing serial AxData build/tests as regression only; do not change AxData to make them pass without a separately approved plan.

### Done when

- Current ARM AxPut uses dependency-driven refresh with mandatory final validation.
- Multi-transaction and GenMap state isolation is proven.
- Verification report, ledger, and progress are updated.
- The stable ARM API/metadata contract is recorded for the AxData handoff. Postman: not applicable unless an external contract changed unexpectedly.
