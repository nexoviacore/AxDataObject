# P1D-05B operation runtime and snapshot v3 evidence

## Outcome

P1D-05B is complete. Set now owns one disposable Axpert field operation for the submitted-field sequence. The operation acquires the legacy global gate once, creates one fresh cache-bypassed StructDef/Validate/Parser runtime, hydrates the object once, evaluates fields in authoritative order, and performs one final full-state readback. No mutable Axpert runtime crosses an operation boundary.

The field model now preserves nullable positive Int64 current and original source IDs. Snapshot writes use deterministic sparse JSON schema v3; v1 and v2 remain readable. No public endpoint, request/response contract, or AXD error code changed, and no `ARM-11.4/` file was changed.

## Parity trace implemented

The adapter preserves the approved AxTstructData ordering:

1. Create Validate, load one refreshed StructDef, attach Parser, and register `recordid`.
2. Set `LastSavedRecordid` and date format before checked `InitValidateAsync`.
3. Hydrate record identity, row state, values, and `_sourceid` companions through `SubmitValueOnLoad`.
4. Evaluate submitted fields with `ValidateData`, checking every result before reading computed or current values.
5. Read accepted state and normalized source IDs from the same operation runtime.

AxPut, SQL/commit, GenMap, MdMap, and child-transaction work remain outside this slice.

## Runtime and failure evidence

- Ten accepted evaluations create one runtime, hydrate once, read back once, and complete once.
- A rejected evaluation followed by an independent field discards the contaminated runtime, creates and hydrates a fresh runtime, and replays only accepted submissions. The rejected state is never replayed.
- Rejected-source dependents remain skipped; independent accepted fields continue from the last accepted AxData checkpoint.
- A changed authoritative StructDef hash fails the operation through the existing stale-definition mapping, `AXD-CONC-003`.
- The static legacy semaphore remains held for the complete operation. A second tenant/object cannot enter its runtime factory until the first operation is disposed.

## Normalized source IDs

AxGet consumes `<field>_sourceid` companions as nullable positive Int64 values, removes the companions from the visible field set, and rejects malformed, non-positive, overflowing, or conflicting values. Hydration and final readback preserve current and original source IDs. Clients still cannot submit source IDs as mutation authority.

## Snapshot v3

- New writes use schema version 3 with repeated field names, deterministic ordering, and StructDef reference only.
- Clean fields omit the original value/source-ID pair; dirty fields retain the complete original pair.
- V1 and v2 committed fixtures recover through their existing readers.
- Default guards are 32 MiB serialized bytes, 10,000 rows per grid, and 350,000 field instances per object.
- The writer streams directly to `Utf8JsonWriter`, preserving the existing typed-value contract without building a second full DTO graph.

## Capacity and policy evidence

The exact 50 DC x 70 rows x 100 fields envelope contains 350,000 clean fields. After JIT warm-up, the final test produced 25,716,825 bytes in 426.16 ms, allocated 68,011,456 bytes on the measured thread, retained 246,936 bytes after full collection, and produced byte-identical output on a second serialization. It stays below the 32 MiB, 500 ms, 256 MiB allocation, and 10 MiB retained-memory gates.

The final policy therefore remains the P1D-05A decision: refresh/bypass the StructDef cache once for each fresh operation runtime, and retain one global operation-wide legacy gate. This slice supplies no 100-iteration isolation evidence that would justify narrowing either boundary.

## Verification

- `dotnet build --nologo -v q --no-restore -m:1 AxData.sln`: passed with the 14 existing legacy dependency warnings.
- Unit: 158 passed.
- Integration: 39 passed.
- Contract: 136 passed.
- End-to-end project: exited successfully; it currently contains no discovered tests.
- Postman: not applicable because the public HTTP and AXD contracts are unchanged.
