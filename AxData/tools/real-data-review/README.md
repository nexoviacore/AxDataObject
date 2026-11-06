# Real-data review tools

`Invoke-P1C04Walkthrough.ps1` is the Development-only P1C-04/P1C-05 HTTP walkthrough. It first characterizes real AddRows 1/2/3 FillGrid metadata and tenant-schema isolation, then starts the API and snapshot worker, runs one existing-record case and one new-state case (including at least one FillGrid-producing Set), verifies local SQL recovery after exact Redis-key deletion, and exercises the D28 StructDef-hash mismatch path.

From `AxData/`, after preparing the ignored local configuration described in `config/README.md`, run:

```powershell
pwsh -NoProfile -File .\tools\real-data-review\Invoke-P1C04Walkthrough.ps1
```

The default report is `artifacts/real-data-review/p1c-05-report.md`. It contains only safe identifiers, StructDef/FillGrid metadata and hashes, record/version identifiers, and counts. Field values, SQL text, key values, credentials, connection strings, and tokens are not emitted. On failure, process logs remain in the printed temporary directory for local inspection and must not be committed.

## P1D-03 CI runner

`.github/workflows/axdata-e2e.yml` runs the Redis 6.2+ restart/redelivery integration cases and this HTTP walkthrough on a self-hosted Windows runner labelled `axdata-real-review`. The runner must have the repository's already-restored .NET dependencies, a reachable Redis-compatible server version 6.2 or later, and the same local Axpert/PostgreSQL prerequisites as the manual walkthrough.

Configure repository variable `AXDATA_REDIS_TEST_CONNECTION` with the runner-local Redis test endpoint. Configure `P1C04_CONFIG_PATH` with the absolute path of an ignored `p1c-04.local.ps1` on that runner. Keep credentials and connection strings in that ignored file; do not copy them into workflow YAML, repository variables, logs, or artifacts.

An existing-record case may set `RowId = "__FIRST_EXISTING_ROW__"`. The runner resolves that placeholder from the initial permission-filtered Get for the same configured DC, so CI never hard-codes a per-Init opaque row ID.
