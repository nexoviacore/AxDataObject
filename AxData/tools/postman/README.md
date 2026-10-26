# AxData Postman review pack

This pack reviews the M1.5 Init, Get/recovery, Set, Query, concurrency, and authorization contracts. It contains no credentials, connection strings, tenant secrets, or real record IDs.

## Prepare local values

1. Copy `AxData-M1.5.template.postman_environment.json` to a file whose name contains `.local.`, for example `AxData-M1.5.local.postman_environment.json`.
2. Keep the `.local.` file uncommitted. Fill in the reviewed new/existing TransIds, the existing record's key value, a live AxAuth bearer token, and a live bearer token for an unrelated user.
3. Import the collection and the local environment into Postman.

Never put tokens, session IDs, real record IDs, or environment-specific connections into the committed template or collection.

## Required local review configuration

Run the production composition locally for this review. Do not enable `AxData:M1` or `AxData:RealDataReview`; both are Development-only configured-identity modes and do not exercise production authentication.

Provide these settings through environment variables or user-secrets before startup:

- `ASPNETCORE_ENVIRONMENT=Production`
- `Jwt__Key`, `Jwt__Issuer`, and `Jwt__Audience`, matching AxAuth
- `AxData__Redis__ConnectionString`
- `AxData__Postgres__ConnectionString`
- worker settings `AXDATA_REDIS_CONNECTION`, `AXDATA_POSTGRES_CONNECTION`, and `AXDATA_TENANT_ID`

The deployed ARM `AppSettings.ini` must be available in the location expected by `ARMCommon.AppSettingsService`; it supplies the per-application Redis and database routing used to resolve the live ARM session. The session's `APPNAME` must resolve to the reviewed tenant schema.

The API and worker tenant IDs, Redis connections, and snapshot-store connections must match. The Redis server must support Redis 6.2 or later. Apply `src/AxData.Infrastructure/Sql/Migrations/Postgres/001_AxDataObjectInstance.sql` to the local snapshot database before the review.

Keep all actual values in the existing gitignored local review configuration. Do not add them to Postman exports.

## Start the worker and API

From `AxData/`, after loading the required local review configuration, start the worker in one PowerShell window:

```powershell
dotnet run --no-restore --no-launch-profile --project src/AxData.Workers
```

Start the API in another PowerShell window:

```powershell
dotnet run --no-restore --no-launch-profile --project src/AxData.Api --urls http://localhost:5116
```

Confirm `GET http://localhost:5116/health` returns 200 before running Postman.

## Run M1.5

Run the requests in numeric order, including `02a Get addressing contract`. That request discovers the editable Axpert `dcId` and server-issued opaque `rowId`; all later Set and selective Query requests reuse those collection variables instead of inferring a DC number or using `recordId` as row identity. Set requests send the D4 header form `If-Match: "{{version}}"`. The stale requests deliberately send `If-Match: "{{preUpdateVersion}}"`, where `preUpdateVersion` is the version saved immediately before the successful update.

Request `03 Set valid` advances the object to version 2 and publishes its snapshot. Wait until the worker reports a newly acknowledged snapshot. Run `04 Get before eviction`, then delete only the exact active-state key:

```powershell
memurai-cli -h <redis-host> -p <redis-port> DEL "AXD:{TENANT-ID-UPPER}:OBJECT-ID-UPPER"
```

Replace the two uppercase placeholders with the configured tenant ID and the `objectId` returned by `02 Init existing`. The command must report that exactly one key was deleted. Do not use a wildcard or flush Redis.

Then run `05 Get after eviction (run after manual evict)`. Its tests assert that recovery returns the same version, `dcId`, and `rowId` as request 04. The expected walkthrough version is 2, and the enforced invariant is equality with the state captured immediately before eviction.

The main numeric run finishes with four negatives: a client `bypassPermission` attempt using the saved stale version (`409 / AXD-CONC-001`), an edit that supplies only `recordId`/`rowNo` instead of `rowId` (`400 / AXD-REQ-001`), a request with no bearer token (`401 / AXD-AUTH-001`), and an unrelated authenticated user (`403 / AXD-AUTH-002`). Neither persistence identity, positional legacy row number, nor possession of an ObjectId is mutation authority.

## Maintenance rule

Every slice that adds or changes an HTTP endpoint, request/response shape, or AXD error code must update this pack in the same slice. Add or revise the happy path and at least one negative assertion, preserve variable chaining for `objectId` and `version`, and keep manual external steps in this README.
