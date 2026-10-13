# Real-data review tools

`Invoke-P1C04Walkthrough.ps1` is the Development-only P1C-04/P1C-05 HTTP walkthrough. It first characterizes real AddRows 1/2/3 FillGrid metadata and tenant-schema isolation, then starts the API and snapshot worker, runs one existing-record case and one new-state case (including at least one FillGrid-producing Set), verifies local SQL recovery after exact Redis-key deletion, and exercises the D28 StructDef-hash mismatch path.

From `AxData/`, after preparing the ignored local configuration described in `config/README.md`, run:

```powershell
pwsh -NoProfile -File .\tools\real-data-review\Invoke-P1C04Walkthrough.ps1
```

The default report is `artifacts/real-data-review/p1c-05-report.md`. It contains only safe identifiers, StructDef/FillGrid metadata and hashes, record/version identifiers, and counts. Field values, SQL text, key values, credentials, connection strings, and tokens are not emitted. On failure, process logs remain in the printed temporary directory for local inspection and must not be committed.
