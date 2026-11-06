# P2-05 core HTTP and real-provider review

Date: 2026-11-06  
Provider: PostgreSQL disposable schema

## Evidence completed

- The user confirmed the P2-03/P2-04 Postman happy, rollback, retained, and destroyed lifecycle requests pass against disposable PostgreSQL.
- The local `axdtf` existing/new-state walkthrough passes StructDef, FillGrid AddRows 1/2/3, Set, Redis publication, PostgreSQL snapshot persistence, exact-key eviction, recovery, and D28 mismatch behavior.
- Nine Redis 8.2 integration cases pass, including consumer restart and pending-entry redelivery.
- Focused AxPut request, endpoint-error, and DataObject projection contract tests pass; Postman JSON and `git diff --check` pass.
- Full contract suite: 175 passed and 2 pre-existing parser-guard failures. The first remains `Failed_validation_expression_is_not_allowed_to_read_parser_value` (expected `Fail`, actual `Success`).

## Ordered HTTP review pack

The `P2-05 AxPut core HTTP and provider review` Postman folder now contains explicit requests for create with values, create with `{}`, existing Save without `Data`, successful overlay edit with matching header/body versions, retained/destroyed state, unknown ObjectId/version omission, stale version, invalid metadata, business validation failure, StructDef mismatch, and transaction rollback.

The newly explicit `{}` create, successful overlay, validation-failure, and manual StructDef-mismatch requests still require execution with the user's uncommitted live Postman environment. Until that run is confirmed, P2-05 is review-ready rather than complete.

## Known evidence gaps

- Oracle and SQL Server business Save/provider behavior were not executed. No parity claim is made for them.
- The self-hosted CI workflow is committed, but repository Settings access is unavailable. The user approved local P1D-03 evidence as the prerequisite for continuing P2-05; CI remains required before M1.6 closure.
- Filesystem and independently executed attachment effects remain outside the proven database transaction boundary.

No credentials, sessions, connections, field values, real record identifiers, or request bodies are included in this report.
