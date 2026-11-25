# AxData local configuration

Committed files under `examples/` contain placeholders only. Copy an example into `local/` and fill in values on your machine. The entire `local/` directory is ignored by Git.

For P1C-04:

```powershell
Copy-Item .\config\examples\p1c-04.local.ps1.example .\config\local\p1c-04.local.ps1
pwsh -NoProfile -File .\tools\real-data-review\Invoke-P1C04Walkthrough.ps1
```

Fill in the copied file before running the walkthrough. Configure at least one authorized existing-record case and one authorized new-state case, with at least one case that produces FillGrid changes. Supply real sanitized FillGrid definitions covering AddRows 1, 2, and 3. The Axpert connection must use a read-only review principal. Redis is the only active DataObject-state store; no snapshot database or snapshot worker configuration is required.

For cloud deployments, store the same values in the platform secret manager and map them to environment variables. Do not deploy the Development-only RealDataReview mode as a Production configuration.

## Operational logs

API logs use structured Microsoft logging. `AxData__Logging__DetailLevel` accepts `Baseline` (the default) or `Diagnostic`. Baseline records request and stage outcomes, safe identifiers, counts, and durations. Diagnostic adds Redis hit/miss, retry, FillGrid, and other stage detail; it still never records request or response bodies, field values, tokens, session IDs, connection strings, SQL text or parameter values, or exception messages.

For local development, set `AxData__Logging__DetailLevel=Diagnostic` before starting the API and read its PowerShell console. In production, keep `Baseline` unless a time-bounded investigation needs Diagnostic, and collect stdout/stderr with the hosting platform's log agent.

