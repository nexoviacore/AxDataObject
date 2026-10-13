## 22. Implementation entry criteria

Development can start after the source/schema discovery items below are available:

- Existing solution/projects and supported .NET version.
- StructDef, Validate, Parser, AxGet, and AxPut source or callable contracts.
- Example StructDef with non-grid, grid, dependency, FillGrid, GenMap, and MdMap behavior.
- Current authentication/user/tenant context contract.
- PEG routines plus `axactivetasks` and `axactivetaskstatus` schemas, including the decision on their persistence/recovery role.
- Supported database provider and transaction behavior.
- Redis/queue technology and deployment constraints.
- Sample request/response payloads and expected validation behavior.

Unresolved product choices are tracked only in `AxData-Clarifications.md` so implementation assumptions remain visible.
