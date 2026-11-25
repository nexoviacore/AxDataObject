# AxData Postman review pack

This pack reviews the M1.5 Init, Redis-only Get, Set, concurrency, and authorization contracts. It contains no credentials, connection strings, tenant secrets, or real record IDs.

## Prepare local values

1. Copy `AxData-M1.5.template.postman_environment.json` to a file whose name contains `.local.`, for example `AxData-M1.5.local.postman_environment.json`.
2. Keep the `.local.` file uncommitted. Fill in the reviewed new/existing TransIds, the existing record's key value, a live AxAuth bearer token, and a live bearer token plus ARM session ID for an unrelated user.
3. Import the collection and the local environment into Postman.

Never put tokens, session IDs, real record IDs, or environment-specific connections into the committed template or collection.

## Required local review configuration

Run the production composition locally for this review. Do not enable `AxData:M1` or `AxData:RealDataReview`; both are Development-only configured-identity modes and do not exercise production authentication.

Provide these settings through environment variables or user-secrets before startup:

- `ASPNETCORE_ENVIRONMENT=Production`
- `Jwt__Key`, `Jwt__Issuer`, and `Jwt__Audience`, matching AxAuth
- `AxData__Redis__ConnectionString`

The deployed ARM `AppSettings.ini` must be available in the location expected by `ARMCommon.AppSettingsService`; it supplies the per-application Redis and database routing used to resolve the live ARM session. The session's `APPNAME` must resolve to the reviewed tenant schema.

Keep all actual values in the existing gitignored local review configuration. Do not add them to Postman exports.

## Start the API

From `AxData/`, after loading the required local review configuration, start the API:

```powershell
dotnet run --no-restore --no-launch-profile --project src/AxData.Api --urls http://localhost:5116
```

Confirm `GET http://localhost:5116/health` returns 200 before running Postman.

## Run M1.5

Run the requests in numeric order, including `02a Get addressing contract`, but skip `07 Set active-operation conflict (manual overlap)` during the ordinary run. `02a` discovers the editable Axpert `dcId` and server-issued opaque `rowId`; later Set and selective Get requests reuse those collection variables instead of inferring a DC number or using `recordId` as row identity. Init, Get, and Set use explicit POST routes, and Get/Set carry ObjectId in the JSON body. Requests contain no `If-Match`, `expectedVersion`, or object-version fields.

Requests `01 Init new` and `02 Init existing` each run exactly one Axpert on-load pass before state is stored. A new empty non-grid DC first receives one server-addressed row so its initialization-eligible non-save/default fields can be refreshed; empty grids remain rowless. ObjectId collision retries must not repeat the pass. Later Get requests are read-only, while Set remains dependency-targeted.

Init omits `expiresAt`. Active DataObject keys have no inactivity TTL while Redis is the only state store; they remain until an explicit lifecycle removal/close, Redis eviction, or the documented exact-key manual deletion.

Request `03 Set valid` updates the existing-record object under the server-owned per-object lock and asserts that object version is absent. Request `04 Get Redis-only eviction probe` verifies the separate new object created by request 01. Then delete only that new object's exact active-state key:

```powershell
memurai-cli -h <redis-host> -p <redis-port> DEL "AXD:{TENANT-ID-UPPER}:OBJECT-ID-UPPER"
```

Replace the two uppercase placeholders with the configured tenant ID and the `newObjectId` returned by `01 Init new`. The command must report that exactly one key was deleted. Do not use a wildcard or flush Redis.

Then run `05 Get after Redis deletion (run after manual delete)`. Its tests assert `404 / AXD-NOTFOUND-001`. Redis is the only active DataObject-state store in the current scope, so no snapshot database lookup or hidden recovery attempt occurs. The separate existing-record object remains available for the later Set, selective Get, concurrency, and authorization requests.

The main numeric run finishes with four negatives: an unrelated user's client `bypassPermission` attempt (`403 / AXD-AUTH-002`), an edit that supplies only `recordId`/`rowNo` instead of `rowId` (`400 / AXD-REQ-001`), a request with no bearer token (`401 / AXD-AUTH-001`), and an unrelated authenticated user (`403 / AXD-AUTH-002`). Neither persistence identity, positional legacy row number, nor possession of an ObjectId is mutation authority.

To review concurrency, arrange a reviewed slow Set or AxPut operation against the same disposable ObjectId, then run request `07` while it is active. The response must be `409 / AXD-CONC-002` and must omit `version` and `currentVersion`. Sequential requests are intentionally new operations and last-completed wins; this pack does not claim stale-tab detection.

## P2-05 AxPut core HTTP and provider review

The separate `P2-05 AxPut core HTTP and provider review` folder is not part of the M1.5 run. Its Save-capable requests must be executed only against a reviewed disposable transaction/schema because AxPut is the real self-committing writer.

Set `axPutBaseUrl`, `armSessionId`, `unrelatedArmSessionId`, `prototypeTransId`, `prototypeKeyField`, `prototypeKeyValue`, `prototypeSubmitData`, `prototypeOverlaySubmitData`, `prototypeValidationSubmitData`, `prototypePostSaveField`, and `prototypePostSaveValue` in the uncommitted local environment. Each AxPut bearer token's session claim must match its body `ARMSessionId`; a mismatch returns `401 / AXD-AUTH-001` before Save. The unrelated bearer token and session ID must belong to the same second live user. The key must identify a record in the disposable schema. The three payload variables must be exact AxPut `SubmitData` JSON proven respectively for create, successful edit, and business-validation failure; do not invent field/DC shapes or commit real values. `prototypePostSaveField` must name an editable field in that same reviewed transaction, and `prototypePostSaveValue` must be a valid JSON scalar that produces an accepted change. Keep both values disposable and uncommitted.

Requests 01 and 01b prove create with configured values and literal `{}` respectively. Both omit `objectid` and `retainDataObject`, assert a secure ULID, retain state, and assert object-version omission. Request 02 omits both ObjectId and `Data`; it must fail with `AXD-REQ-001` before initialization and return neither ObjectId nor an object version.

Request 03 initializes an existing disposable record through AxData. Request 03b proves that another authenticated user cannot Save the known ObjectId and receives `403 / AXD-AUTH-002`. Request 04 supplies that ObjectId with no `Data`; request 04b applies a successful overlay without client concurrency metadata. Retained finalization returns the object to `Active`: request 04c verifies Get exposes the complete authorized snapshot as editable, requests 04d and 04e prove sequential Sets are accepted as new operations, and all responses omit object version. A new live session for the same resolved initiator username remains authorized; access is user-bound, not session-bound. Requests 05, 07, and 07b assert unknown ObjectId, unknown overlay metadata, and business-validation failure. Known-state failures report `dataObjectRetained=true` without version disclosure.

Request `06 AxPut active-operation conflict (manual overlap)` is skipped during the ordinary run. Start a reviewed slow Set or AxPut against `axPutNegativeObjectId`, then run request 06 concurrently; it asserts `409 / AXD-CONC-002` and that neither `version` nor `currentVersion` is disclosed. Do not automatically retry AxPut after a timeout: the business transaction may have committed. `Idempotency-Key` is not implemented in this slice.

Active DataObject state does not persist or validate a StructDef revision. Init, Get, Set, and AxPut Save remain subject to their trusted server-side contracts; there is no client-controlled schema-validation bypass field. Do not mutate Redis payloads to add a StructDef hash or version.

For request 08, set `prototypeFailureTransId` and `prototypeFailureSubmitData` to a reviewed disposable transaction that fails in a child, GenMap, MdMap, or later database command after an earlier batch has executed. Before the request, record sanitized row counts for the main and controlled target tables. After the response, verify those counts and any controlled mapping effects are unchanged, proving final rollback. If the fixture includes attachments, verify their current pre-commit behavior separately; filesystem and independently executed attachment writes are not claimed to roll back with the database. Record only pass/fail, provider, failure category, and counts—never identifiers, values, sessions, tokens, or connections.

Requests 09-11 initialize another disposable object, perform a sequential retained Save, then Save it with `retainDataObject=false`. The final response must omit object version, report `dataObjectRetained=false`, and omit `dataObjectFinalizationPending`. After request 11, confirm Get returns not found. The durable closure marker prevents the destroyed object from becoming active again; no snapshot recovery path is active. If `dataObjectFinalizationPending=true` is returned after a committed business transaction, record the Save as committed and treat DataObject cleanup as pending rather than retrying the business Save.

## Maintenance rule

Every slice that adds or changes an HTTP endpoint, request/response shape, or AXD error code must update this pack in the same slice. Add or revise the happy path and at least one negative assertion, preserve ObjectId/address variable chaining, and keep manual external steps in this README.
