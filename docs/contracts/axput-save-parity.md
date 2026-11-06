# AxPut Save parity contract

This is the cached parity authority for AxPut-owned business Save. `AxTstructData.AxPutService` is authoritative; the
retired `ARM-11.4/AxDataObject` project supplied no production behavior and remains available only in Git history.

## Reuse rule

Compare the SHA-256 fingerprints below with `Get-FileHash`. When they match, reuse this trace without reopening the
source. When one changes, inspect only that file's diff and affected symbols, update the trace if behavior changed, and
refresh all fingerprints after verification.

Fingerprints were refreshed after P2-04 implementation and verification on 2026-11-06.

## Authoritative sequence

1. Validate the ARM session, restore trusted tenant/user/roles/Axpert session, and resolve the ObjectId request contract.
2. Load tenant-scoped shared state when ObjectId was supplied; check actor relation, expected version, current StructDef
   hash, and current transaction/DC/field permission.
3. Load one fresh StructDef/Validate/Parser runtime, hydrate stored values, apply an optional overlay, run targeted
   dependency work, and perform mandatory final-current-value validation. Failed computation is never consumed.
4. Preallocate required record IDs, then build create/edit main and DC commands. Main/grid/child identities are registered
   in the runtime before dependent mapping work.
5. Build MdMap commands into the same command batch and execute each batch with `commitTransaction: false` on the one
   open provider connection/transaction.
6. Preserve attachment/image/Axp-file handling at its existing position after each successful SQL batch and before the
   final database commit. Filesystem and independently executed attachment writes are not claimed to be transactional.
7. Aggregate the existing message, record ID, transaction, caption, username, tracking, and return-field response data;
   register the saved record ID and enqueue child GenMaps.
8. Process all queued input transactions and GenMap children on the same connection/transaction. A later failure prevents
   the final commit and rolls back the shared database transaction.
9. Commit once after all batches succeed, or roll back for ValidateOnly/failure. Axpert logging remains post-result and
   best-effort; connection close rolls back any dangling transaction.
10. ObjectId metadata augments, but does not replace, the existing AxPut response structure. Known versions are numeric;
    unknown versions are omitted and never serialized as null.

## Deliberately excluded

- No AxData-owned Save writer or external transaction wrapper.
- No restoration or comparison of the retired AxDataObject project.
- No claim that filesystem attachments, independently executed attachment SQL, record-ID sequences, or cross-database
  mappings are atomic with the business transaction.
- No version claim/finalization, retention/destruction, idempotency, outbox, or recovery behavior before their owning
  slices.
- No parser, validation, StructDef, ARMCommon, or provider-helper behavior change without a separately approved plan.

## Source fingerprints

| File | SHA-256 |
| --- | --- |
| `ARM-11.4/AxTstructData/Service/AxPutService.cs` | `051E25994B0BA99E139304A0772482C4A49B11BE6393E083BB39F50FBE01DA62` |
| `ARM-11.4/AxTstructData/Repositories/AxPutRepository.cs` | `08089CD9ABA01F7A1A21845CDB9ABBD4F65B241493B9E6E9D7EEF3F9EF708A54` |
| `ARM-11.4/AxTstructData/Controllers/AxPutController.cs` | `0A239E42DD7AB078D463BF9F53E8952B7923AAA000CD27241AC0C46F6D863816` |
| `ARM-11.4/AxTstructData/Service/AxPutDataObjectProjection.cs` | `2313D55291D7DCAE883DCB4346984DE5F6287EFB209EF4DD8328242D75A54051` |
