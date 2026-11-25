# AxPut Save parity contract

This is the cached parity authority for AxPut-owned business Save. `AxTstructData.AxPutService` is authoritative; the
retired `ARM-11.4/AxDataObject` project supplied no production behavior and remains available only in Git history.

## Reuse rule

Compare the SHA-256 fingerprints below with `Get-FileHash`. When they match, reuse this trace without reopening the
source. When one changes, inspect only that file's diff and affected symbols, update the trace if behavior changed, and
refresh all fingerprints after verification.

Fingerprints were refreshed after AXD-STATE-03F compact field-metadata verification on 2026-11-18.

## Authoritative sequence

1. Validate the ARM session, restore trusted tenant/user/roles/Axpert session, and resolve the ObjectId request contract.
2. For an existing ObjectId, atomically establish the persistent `Saving` claim while the shared Set lock is absent,
   then load tenant-scoped current positional state, validate compact `[name,flags]` metadata against current StructDef,
   decode only source-key `value~sourceId` entries,
   derive changed fields from aligned `values`/`oldValues`, and check actor relation plus current transaction/DC/field permission
   against a freshly loaded StructDef. Business-key locators are not stored; an edit derives the trusted legacy
   `recordid` key pair from the snapshot's positive `RecordId`. Generated ObjectIds initialize new state without a prior claim.
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
10. ObjectId metadata augments, but does not replace, the existing AxPut response structure. Responses omit `version`
    and `currentVersion`. A retained successful Save returns the DataObject to `Active`; a non-retained Save durably
    closes and removes active state. Overlap returns `409 / AXD-CONC-002`; sequential requests are last-completed-wins.

## Deliberately excluded

- No AxData-owned Save writer or external transaction wrapper.
- No restoration or comparison of the retired AxDataObject project.
- No claim that filesystem attachments, independently executed attachment SQL, record-ID sequences, or cross-database
  mappings are atomic with the business transaction.
- No AxData-owned Save claim/finalization, client object version, idempotency, outbox, or recovery behavior.
- No automatic retry of timed-out AxPut requests; the business transaction outcome may be unknown.
- No persisted Init/AxGet business-key locator; ObjectId-aware edits use the server-derived `recordid` pair.
- No parser, validation, StructDef, ARMCommon, or provider-helper behavior change without a separately approved plan.

## Source fingerprints

| File | SHA-256 |
| --- | --- |
| `ARM-11.4/AxTstructData/Service/AxPutService.cs` | `7E4484D355D85A2F439001E943E293F5B725C622A2866B2038E8E71308D51062` |
| `ARM-11.4/AxTstructData/Repositories/AxPutRepository.cs` | `600041FD91D7E5F443594DCD9C9675472AF7300D6C669F2A2B7BC98C704EECE5` |
| `ARM-11.4/AxTstructData/Controllers/AxPutController.cs` | `7529429F0DC66A0285C96C8D890FACA930323B229B6317DAAF11E1E860660D52` |
| `ARM-11.4/AxTstructData/Service/AxPutDataObjectProjection.cs` | `926F157B324C27FD6A373C4BC748926A5B2863B3B2ED4B8267045EF398EB0DC2` |
