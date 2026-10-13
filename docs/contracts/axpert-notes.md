# Axpert discovery notes (P0-03)

Status: substantially closed. D10 answered. D11 fully answered (Utils.cs connection lifecycle + the DBHelper.cs
provider factory I already had). D12 fully answered (view/edit/create shapes + shared APIResult envelope). PEG
tables/actions documented, D14 (v1 scope) decided. D3 answered. Real StructDef export supplied. Remaining opens
are narrow: `axactivetaskstatus` DDL (D15) and a few low-priority interface shapes. P1C-05 closed the FillGrid
execution and real-data evidence gap.

## 1. Does AxPut commit internally? (D10) — ANSWERED

`AxPutService.AxPut` owns commit/rollback and closes the ambient DB connection. `AxPutRepository.ExecuteSaveDataSQLCommands(AxPut, List<SQLCommandBuilder>)` returns `Task<TransactionResult>` and calls `_common._dbHelper.ExecuteSQLsInTransactionAsync(sqlCommands, false)`. `Utils.CloseDBConnectionAsync` calls `RollbackActiveTransactions()` before closing, so any dangling transaction from AxPut is rolled back by `Utils`, not committed. AxPut remains an external, self-committing step for AxData — AxData's Save orchestrator cannot join it to AxData's own local transaction (revisit P2-04/P2-06 design).

## 2. Callable signatures — UPDATED

**StructDef load:**

```
StructDef.LoadStructure(Utils common, string transId, List<string> roles,
                         IARMLogTraceService traceService, bool refreshCache = false) -> Task<StructDef>
```

Cache key = `GenerateRedisKeyString([ GetSchemaName(), transId, "ARM-AX-STRUCTDEF" ])`.

**AxGet load** — confirmed from `AxGetService.cs`: `AxGet`, `GetTstructLoadData`, `CheckTstructViewAccess`, `LoadStructure`, `LoadData(AxGet, APIResult accessResult = null, Validate validate = null) -> Task<APIResult>`. On success: `data["message"|"recordid"|"createdby"|"createdon"|"modifiedby"|"modifiedon"]`, optional `data["attachments"]`, `data["data"]` as DC -> row -> field -> value.

**AxUserPermissionService:**

```
GetUserPermission(AxUserPermission) -> Task<APIResult>
```

Routes `VIEW` -> `AxGetService.CheckTstructViewAccess`, `CREATE` -> `AxPutService.CheckTstructCreateAccess`, `EDIT` -> `AxPutService.CheckTstructEditAccess`. Edit branch normalizes `ExcludedDCs`/`IncludedDCs`/`ExcludedFields`/`IncludedFields` to comma-separated strings.

**AxParser** (`AxParser.cs`):

```
Evaluate(string expr) -> bool
Value -> string
RegisterVar(string, string, string) -> int
GetVarValue(string) -> string
EvaluateExpression(List<string>, List<string>) -> string
FireSql(string, string) -> Task<string>
```

Uses `_common._dbHelper` and `_common.GetSQLParams`.

**Design risk — do not reproduce in AxData:** `AxParser.Evaluate` catches exceptions and returns `false`, and
several `Validate.cs` callers ignore that `false` and read `.Value` anyway — a failed evaluation can silently
look like a stale successful one. AxData's own field/dependency engine (P1-11) must always check an operation's
success result before using any value it produced. Matching hard rule added to `AGENTS.md`.

**FillGrid** — no legacy execution engine was found anywhere reviewed. `StructDef.XLoadFillGrid` only parses the
XML definition into a `TFg` object (`SQLText`, params, source/target DC, `ExecuteOnSave`, etc. — see §5's
`fillgrid1` example). AxData therefore owns the new SELECT-only, parameterized, tenant-scoped query executor;
the remaining open is real AddRows 1/2/3 behavior evidence, not legacy source reuse.

**GenMap / MdMap** — unchanged:

```
_validate._sdef.GetGenMapName(StructDef) -> string
_validate._sdef.SaveGenMap(StructDef)
_validate._sdef.MdMaps -> List<TMdMap>
await _validate._sdef.DoMDMapAsync() -> Task<List<SQLCommandBuilder>>
```

**Utils** (`Utils.cs`) — the shared context object AxTstructData/AxParserApp/AxGet/AxPut all carry:

Fields:

```
public IDbHelper _dbHelper;
public string _schemaName, _schemaNameALC, _dbType;
public Dictionary<string,string> _dbVars;               // per-DB-type date/string literals
public Dictionary<string,object> _sessionVars;           // Redis hash of session
public Dictionary<string,string> _globalVars;            // GLOBALVARS entry, case-insensitive
```

Connection lifecycle:

```
Task<string> OpenDBConnectionAsync()
Task CloseDBConnectionAsync()
void RollbackActiveTransactions()
static bool IsTransactionActive(DbTransaction)
string GetSchemaName()
Task<Dictionary<string,string>> GetDBConfigurations(string appName)
```

`OpenDBConnectionAsync` flow: load `axprops.xml` (cached in Redis) -> `GetDBConfigurations(appName)` for
`ConnectionString`/`DBType`/`DBUser`/`DBUserALC`/`Driver` -> `_dbType = config["DBType"]` -> **`new DBHelper().GetDbHelper(_dbType, _traceService)`**
(confirmed: `DBHelper.cs` line 5248, an instance method — distinct from the two static `DBHelper.CreateDbHelper(...)`
overloads at lines 5116/5132, all in `ARMCommon.Helpers`) -> set `_dbHelper.ConnectionString` -> `_dbHelper.OpenDBConnectionAsync()`
-> populate `_dbVars` from `GetDBVarsList(_dbType)` -> `_schemaName`/`_schemaNameALC` from config (split on `~` if present)
-> for Postgres, resolve `postgre_search_path` from `axprops.xml` and issue `SET search_path`.
`CloseDBConnectionAsync` -> `RollbackActiveTransactions()` (checks `_dbHelper.Transaction` via `IsTransactionActive`,
calls `RollBackTransaction()` if active) -> `_dbHelper.CloseDBConnectionAsync()` -> `CloseRedisConnectionAsync()`.

**This closes D11 fully** — I already have `DBHelper.cs` (uploaded earlier as `DBHelper.cs`, path
`ARM-11.4/ARMCommon/Helpers/DbHelper.cs`): `namespace ARMCommon.Helpers`, class `DBHelper`, three provider
implementations of `IDbHelper` — **`OracleHelper`, `PostgresHelper`, `MSSQLHelper`** — chosen by a `dbType`
string via `CreateDbHelper`/`GetDbHelper`. No further files needed for the DB-provider question.

`_dbVars` per DB type (from `GetDBVarsList`) — oracle: `sqlnull=nvl`, `dbdateformat=dd-mmm-yyyy`,
`datesql="Select sysdate as sdt from dual"`; postgres: `sqlnull=coalesce`, `dbdateformat=dd/mm/yyyy`,
`datesql="Select now() as sdt"`; mssql: `sqlnull=isnull`, `dbdateformat=mm/dd/yyyy`, `datesql="SELECT GETDATE() as sdt"`
(each also has `dbdatestring`/`dbdatestringwithms`/`forupdate`/`nullcds`).

Session/Redis on `Utils`: `GetSessionData(sessionId)` reads the whole Redis hash into `_sessionVars`; `GLOBALVARS`
deserializes separately into `_globalVars`; `ValidateSession`, `IsValidRole`, `GenerateRedisKeyString`,
`StoreAxParserObjInCache`/`GetAxParserObjFromCache`, `SetStructDefObjToCache`/`GetStructDefObjFromCache`.

Permission shaping on `Utils` (feeds §4's permission section):

```
Task<DataTable> HandleIncludeExcludeCols(DataTable, List<string> excludeCols, List<string> includeCols, List<string> maskedFields)
Task<DataTable> HandleIncludeExcludeCols(DataTable, APIResult accessResult)
Task<Dictionary<string,Dictionary<string,Dictionary<string,string>>>> HandleIncludeExcludeCols(data, APIResult accessResult, Dictionary<string,string> FieldWiseDCs)
string ApplyFieldMask(string input, string pattern)   // supports 999.., 99..99, prefix 99***, suffix ***99, mixed
```

Include list `"all"` expands to all columns; exclude `"none"` clears; `axrnum_forpagination` always excluded;
system columns (`transid`, `recordid`, `modifiedby`, `modifiedon`, `createdon`, `createdby`, `rno`, `pageno`)
always included.

**Design note for D13 (AxData's own DB access):** `Utils` bundles DB connection + Redis + session + logging +
permission-shaping into one object — more surface than AxData's own SQL tables need. Recommend
`AxData.Infrastructure` depend on `IDbHelper` + `DBHelper.CreateDbHelper(dbType, connectionString)` directly,
not the whole `Utils` class, to keep the coupling narrow (see D13 in DECISIONS.md).

**AxGetRepository / AxPutRepository** — unchanged: `AxGetRepository` exposes view permission, DC-wise SQL
load/cache, `ExecuteDCWiseSQLs`. `AxPutRepository` exposes edit/create permission, child record lookup,
DC-wise SQL, `ExecuteSaveDataSQLCommands`.

## 3. axactivetasks / axactivetaskstatus / PEG routines — ANSWERED, with caveats

Tables/columns seen: `axactivetasks`, `axactivetaskstatus`, `AxActiveTaskData`, `AxActiveTaskParams`,
`axprocessdef`, `axprocessdefv2`, `axpdef_peg_processmaster`, `axprocess`, `Axusers`. PEG task IDs are
`Guid.NewGuid().ToString("N")`. Actions: open connection, read task, begin transaction, update `axactivetasks`
where `status='Active'`, insert `axactivetaskstatus`, commit, publish to `peg_notifications`; otherwise rollback.
Statuses seen: `Active`, `Approved`, `Checked`, `Rejected`, `Forwarded`, `Returned`, `Skipped`,
`AmendmentApproved`, `AmendmentDiscarded`.

**D14 decided:** v1 builds only Initiate/Approve/Reject/Return/Recall (what Phase 3's slices already cover).
`Forwarded`/`Skipped`/`AmendmentApproved`/`AmendmentDiscarded`/`Checked` are backlog — new slices after v1 ships
(see the Backlog note at the end of `docs/slices/phase-3-peg.md`).

Caveat still open (D15): `SkipAndUpdateAllOtherTasksWithSameIndexAsync` inserts into
`axactivetaskstatus (taskid, status, statusdate)`, while the main insert path uses `taskstatus`/`eventdatetime`.
Need real DDL to confirm which is right.

## 4. Tenant/user context, DB provider, permission source

**Tenant/user context:** session-based. `Project` from `APPNAME`, `UserName` from `USERNAME`, `AxSessionId`
from `AXPERT_SESSIONID`, roles default to `USER_ROLES`, then `_common.IsValidRole(...)`. Tenant = schema name
via `_common.GetSchemaName()`.

**DB provider (D11) — fully answered.** See §2's `Utils`/`DBHelper` writeup above.

**Permission source (D12) — fully answered.**

`AxUserPermissionService.GetUserPermission` routes to:

- `VIEW` -> `AxGetService.CheckTstructViewAccess`. Response: `ExcludedDCs`, `IncludedDCs`, `ExcludedFields`,
  `IncludedFields`, `MaskedFields`, `EncryptedFields`, `RecordId` — consumed by `Utils.HandleIncludeExcludeCols`
  (see §2).
- `CREATE` -> `AxPutService.CheckTstructCreateAccess`. Confirmed: coarser than view/edit. Loads permission rows
  via `_repo.GetCreatePermissionFromDB`, prefers `permissiontype == "User"` rows over role rows when present,
  then scans `allowcreate` ("YES") / `fullcontrol` ("T") columns — any matching row sets a transaction-level
  `AllowCreate = "T"` flag. No DC/field-level granularity for create (unlike view/edit).
- `EDIT` -> `AxPutService.CheckTstructEditAccess`. `AxUserPermissionService` expects `ExcludedDCs`,
  `IncludedDCs`, `ExcludedFields`, `IncludedFields` as `List<string>` and normalizes them to comma-separated
  strings. Masked/encrypted fields are not normalized on the edit branch. Exact underlying DataTable column
  names (pre-normalization) are not confirmed, but the output shape used by AxData is.

**Common response envelope**, used across AxGet/AxPut/Permission calls:

```
APIResult { bool success; string error; Dictionary<string,object> data }   // data keys case-insensitive
```

D12 fully answered.

## 5. Sample StructDef — ANSWERED

Real export for `TransId = empin` ("Increment Employee wise").
Key shape:

- `Struct`: `SaveControl = savecontrol_chk`, `DelControl = deletecontrol_chk`, `Track = true`, `TrackChange = true`, `TrackAllFields = true`, `PrimaryField = ""`, `Attach = false`, `HasWorkFlow = false`, `VersionNo = 11.4`.
- Frames:
  - `dc1`: `FrameNo = 1`, table `empina`, caption "Header", `AsGrid = false`, `FieldCount = 42`, `DcSQL` selects from `empina` joined to `cm_corp_hdr`, `compa`, `cm_branch_hdr`, `empma`, `ppdetail`. `DcSQLGroup = ["dc1"]`.
  - `dc2`: `FrameNo = 2`, table `empinb`, caption "Increment Details", `AsGrid = true`, `FieldCount = 21`, `DcSQL` selects from `empinb` joined to `cpaycode`, ordered by `empinbrow`. `DcSQLGroup = ["dc2"]`.
- Representative fields:
  - `docid`: `autogenerate`, `NoDuplicate = true`, `SaveValue = true`, hidden.
  - `corpcode`: `select`, `SourceKey = true`, SQL over `cm_corp_hdr`, `Pickfields = ["cm_corp_hdrid"]`.
  - `compcode`: `select`, `SourceKey = true`, SQL over `compa`, depends on `:corpcode`, `Pickfields = ["a1.compaid"]`.
  - `branch`: `select`, `SourceKey = true`, SQL over `cm_branch_hdr`, depends on `:username` and `:compcode`.
  - `empid`: `select`, `SourceKey = true`, SQL over `EMPMA`, depends on `:compid` and `:branchid`.
  - `csalmonth`: `select`, `SourceKey = true`, SQL over `PPDETAIL`, depends on `:compid`, `:branchid`, `:recordid`.
  - `npaycat`: `select`, SQL over `PAYSTRA`/`bandmast`, depends on `:compid`.
  - Grid fields on `dc2`: `paycode` (`select`, `SourceKey = true`, `NoDuplicate = true`), `frequency` (`select`, `FromList = true`, values `Annual,Monthly`), `freq_amt`, `revamount`, `mamount`, `wamount`, `amountpa`, `oamount`, `amount`, `view_diffamt`, `diffamt`, `rvar`, `prdocid`.
- Dependencies/fill fields: many `ModeofEntry = fill` fields link to a select field, e.g. `corpname` links `corpcode`, `compname`/`compid`/`payfreq`/`paytype`/`applyctc` link `compcode`, `branchid` links `branch`, `empmaid`/`empname`/`paycat`/`doj` link `empid`, `migrationreq`/`salmonthstdt`/`salmonthendsdt`/`csalmonthn`/`salpayseqno`/`payseqno`/`csmstatus` link `csalmonth`.
- Calculations: `pmctc = tctc/12`, `noofmonths = GetInteger((DaysElapsed(effectfrom,finyrdt)+1)/30)`, `duplicatecheck = COMPCODE + BRANCH + EMPID + csalmonth + DTOC(effectfrom)`, `postcf = iif(epayseq=salpayseqno,{F},{T})`, `savecontrol_chk` and `deletecontrol_chk` use `IIF` over `migrationreq` and `csmstatus`.
- Validation expression: `effectfrom` has `Cvalexp = iif(CTOD(effectfrom) < CTOD(doj), {Effective Date should be Greater than Date of Join}, iif(CTOD(effectfrom) > CTOD(salmonthendsdt), { should be less than or equal to Salary Month End date},{T}))`.
- FillGrid: `Fgs` has one entry `fillgrid1`, caption "Fill Details", `TargetFrame = 2`, `TargetDcName = "dc2"`, `ParamNames = "compid,npaycatid"`, `Map = ["paycode=paycode","paydesc=paydesc","formula=formula","rvar=execution_order"]`, `AddRows = 2`, `MultiSelect = true`, `ValidateRow = true`.
- MdMaps: one map `TransId = empin`, `Table = empma`, `MasterSearchField = empmaid`, `DetailSearchField = empmaid`, mapping `npaycatid -> paycat`, operation `7`.
- GenMaps: one map `empin2`, target `epays`, source `dc1`, mappings include `mxdt <- revdate`, `newjoiner <- F`, `corpcode <- corpcode`, `compcode <- compcode`, `branch <- branch`, `paycat <- npaycat`, `tctc <- tctc`, `effdate <- effectfrom`, `empid <- empid`, plus grid mappings for `mamount`, `wamount`, `frequency`, `freq_amt`, `paycode`.
- Scripts/buttons: includes OnDataLoad script setting read-only/disable controls when `csmstatus # {OPEN}`, preview form tasks, `sqlpost`, CSV/Excel import tasks, toolbar buttons, and two iframes (`dc:dc1`, `dc:dc2`).

## §6 AxPut crash-recovery facts

Investigation scope: `AxTstructData`'s `AxPutService.SaveData`, its repository/DB-helper boundary, record-ID
allocation, validation/autogeneration, GenMap/MdMap, attachments, and `axpertlog`. These are characterization
facts for D30; no `ARM-11.4` code change is proposed.

### 6.1 Transaction boundary and partial commits

**Main data, additional input transactions, and GenMap child transactions share one database transaction.**
Each transaction builds its main/DC SQL and MdMap SQL, then calls
`ExecuteSaveDataSQLCommands` (`AxPutService.cs:664-853`). Successful parents enqueue GenMap child TransIds and
the same loop processes them before the final commit (`AxPutService.cs:909-933`). The repository calls
`ExecuteSQLsInTransactionAsync(sqlCommands, false)` (`AxPutRepository.cs:183-189`); each provider begins a
transaction only when none exists and attaches every command to it (Oracle `DBHelper.cs:1352-1424`, PostgreSQL
`DBHelper.cs:3029-3098`, SQL Server `DBHelper.cs:4853-4923`). Thus successive batches reuse the one open helper
transaction. `SaveData` commits only after the full loop succeeds, at `AxPutService.cs:951-967`. A validation-only
call rolls that transaction back at lines 953-957. A command exception rolls it back inside the provider
(Oracle `DBHelper.cs:1433-1438`, PostgreSQL `DBHelper.cs:3107-3113`, SQL Server `DBHelper.cs:4932-4938`), and
`AxPut` also rolls back any still-active transaction on an unsuccessful result before closing the connection
(`AxPutService.cs:135-142`; `Utils.cs:2638-2656`).

Step placement:

- Record IDs are requested before the main SQL batches: `SaveData` preallocates them at
  `AxPutService.cs:570,642-643`; `StructDef.GetRecordIdsFromDB` invokes provider-specific
  `FN_AXDBPUT_GETRECID` SQL/stored-procedure forms at `StructDef.cs:6929-6969`, whose names are defined at
  `Constants_SQL.cs:442-444`. The database function body is not in this repository, so whether its allocation
  is transactional, sequence-backed, or gap-free is unproven.
- Autogenerated business fields are evaluated before their row SQL is built. Validation calls
  `GetLastNo(field, false)` (`Validate.cs:810-815`); it reads the configured `Sequence` table and calls
  `UpdateLastNo` (`StructDef.cs:2957-3080`). `UpdateLastNo` starts/reuses the same helper transaction with
  `commitTransaction: false` (`StructDef.cs:2829-2889`), so its update is included in the final AxPut
  commit/rollback, although the preceding read is not locked by evidence in this code.
- GenMap value projection is in memory (`StructDef.SaveGenMap`, `StructDef.cs:4972-5012`). Its generated child
  inserts/updates are sent through the shared transaction (`AxPutService.cs:747-840,853`).
- MdMap builds SQL commands and appends them to the same batch before execution
  (`AxPutService.cs:843-853`; `StructDef.cs:5200-5229`), so the reviewed same-database MdMap path is inside the
  transaction. This evidence does not make a cross-database MdMap atomic.
- Images, header attachments, and Axp files are attempted **after each successful SQL batch but before the
  final commit** (`AxPutService.cs:853-857,951-957`). Folder variants directly create/delete/overwrite files
  (`AxAttachmentsService.cs:511-577,590-642,672-741`) and are outside the database transaction. DB-backed
  attachment routines use `ExecuteSQLAsync`, not the transaction command list
  (`AxAttachmentsRepository.cs:33-82,99-151`); those commands do not explicitly attach the active helper
  transaction (`DBHelper.cs:1009-1074,2642-2709,4286-4355`), so atomic participation is not established.
  `SaveImagesAndAttachments` returns a Boolean, but its caller ignores it (`AxPutService.cs:982-1028` versus
  line 857).
- The only explicit after-commit action in this path is `axpertlog`: `AxPut` logs in `finally` after `SaveData`
  returns (`AxPutService.cs:135-142,2327-2348`). `InsertIntoAxpertLog` first rolls back any active transaction,
  then independently inserts the log row and swallows failures (`Utils.cs:1804-1832`). No separate generic
  post-save action dispatcher is called by this AxPut path.

Partial-commit/partial-side-effect scenarios visible from this boundary are:

1. AxPut commits, then the process/network fails before the caller receives or durably records the response:
   the business save succeeded but D21 remains `InProgress` with an unknown outcome.
2. AxPut commits, then the process fails before `axpertlog`, or the log insert fails and is swallowed: business
   data exists without a success log.
3. A folder image/attachment/Axp file is written, overwritten, or deleted; then a later child/input batch fails
   or the final database commit fails: the database rolls back but the filesystem change remains.
4. `ValidateOnly` still reaches attachment handling before explicitly rolling back the database transaction,
   so it can leave filesystem effects even though business SQL is rolled back.
5. An attachment operation fails or only partly completes: its Boolean is ignored, so the business database
   transaction can still commit without all requested attachments.
6. DB-backed attachment commands are outside the proven `ExecuteSQLsInTransactionAsync` command boundary;
   depending on provider behavior they may fail while the failure is treated as success, or may not share the
   business transaction. The code does not prove atomicity.
7. Record-ID reservation happens before the proven business transaction. A later failure can consume/strand
   allocated IDs even though no business record commits. The allocator implementation is unavailable, so no
   stronger durability claim is possible.

There is no scenario in the reviewed command-list path where a successful main-record SQL batch is committed
before a later child/GenMap/MdMap SQL batch: those batches commit together. The non-atomic cases are the
preallocated IDs, attachment/file side effects, post-commit log, and the caller-observation window.

### 6.2 New records and keys

`AxPut`'s request model exposes `AxPut.RecordId` (`TstructData.cs:31-57`). For create, the primary record receives
an allocated ID only when `putData.RecordId == 0`; a nonzero caller value is retained and passed into the insert
(`AxPutService.cs:1821-1828,1863-1871`). Therefore a caller **can** supply the main record ID. Child/grid IDs are
still allocated separately. The current pre-count still requests IDs for all create rows before SQL generation
(`AxPutService.cs:570,642-643,2275-2305`), so a caller-supplied main ID can leave one reserved ID unused; it does
not overwrite the supplied ID. The normal allocator is neither an identity column nor client-side `max+1` in the
reviewed code: it calls the provider-specific `FN_AXDBPUT_GETRECID` database routine and consumes its returned
`recordid` values (`AxPutService.cs:2243-2272`; `StructDef.cs:6929-6969`). Its internal mechanism is unknown.

On success, `SaveData` includes each `recordid` in its result list (`AxPutService.cs:855-884`), the final response
returns that list after commit (`AxPutService.cs:951-967`), and the controller copies the response data to HTTP
output (`AxPutController.cs:89-110`).

There is no mandatory universal business key in `AxPut`. StructDef fields may be marked `NoDuplicate`; validation
checks non-grid values with a pre-save query (`Validate.cs:1096-1126`; `ValidateHelper.cs:1811-1889`). Fields with
`ModeofEntry == "autogenerate"` read and increment the configured sequence-table row
(`Validate.cs:810-815`; `StructDef.cs:2957-3080`). These are transaction-specific metadata behaviors, not proof
of a database unique constraint, and not every transaction has such a field. A generated value is also not known
to a caller that lost the AxPut response.

### 6.3 Idempotency and replay behavior

AxPut has no request/idempotency key in `AxPutInput` or `AxPut` (`TstructData.cs:31-60`). Repeating a create with
`RecordId == 0` allocates fresh IDs again (`AxPutService.cs:570,642-643,1824-1828`) and can create a second record.
A `NoDuplicate` field may reject a replay, but that is optional metadata and a check-then-write query rather than
a generic idempotency guarantee. Repeating a create with the same caller-supplied nonzero record ID should meet
the table's record-ID constraint rather than create a second main row, but AxPut exposes no “already succeeded”
outcome and this investigation did not inspect transaction DDL.

Edits are not generally safe to reapply. They have no version/compare-and-set guard. Ordinary field edits issue
updates and also refresh `username`/`modifiedon` (`AxPutService.cs:1289-1316`); submitted grid/DC row actions can
delete rows, allocate and insert new rows, or update existing rows (`AxPutService.cs:1905-2059,2065-2168`). A
replayed edit containing `CREATE` child rows can therefore add duplicates/new IDs, and GenMap/MdMap effects can
run again. Only a narrowly constrained update-only payload may be value-idempotent, while still changing audit
timestamps and rerunning mappings.

### 6.4 Correlation evidence

AxPut writes `axpertlog` only after its business transaction outcome. The row schema includes session ID,
username, called/finished timestamps, struct name, record ID, service/result, timing, and call details
(`Constants_SQL.cs:482-485`; `AxpertLog.cs:4-27`). For AxPut specifically, the service sets session ID, username,
success/error, service name, and on success a `transid=recordid,...` call-details string
(`AxPutService.cs:2327-2348`), but it does **not** set `AxpertLog.StructName` or `AxpertLog.RecordId`. There is no
request ID, idempotency key, or trace ID column populated by this call. Because insertion is post-commit and its
exceptions are swallowed (`Utils.cs:1804-1832`), a matching row can be supporting evidence, but absence cannot
prove AxPut did not commit and the row cannot uniquely correlate concurrent identical calls.

### 6.5 D30 option ranking and recommendation

Ranked for safe reconciliation without changing `ARM-11.4`:

1. **(a) Caller-supplied record ID, then AxGet — recommended.** It is supported by the current request model and
   create SQL path. AxData should reserve an Axpert-compatible ID through the existing
   `FN_AXDBPUT_GETRECID` provider path, durably store that ID with the `InProgress` idempotency entry before
   calling AxPut, pass it as `RecordId`, and reconcile through authorized AxGet with `KeyField = "recordid"` and
   `KeyValue = reservedId`. AxGet passes the key pair through its permission lookup and then loads the resolved
   record (`AxGetRepository.cs:21-52`; `AxGetService.cs:112-135`). An authoritative successful read that finds the
   record proves it committed; an authoritative not-found result proves that reserved main record did not commit.
2. **(b) Business/auto-number lookup — conditionally feasible.** Use only when the transaction has a stable,
   caller-known, uniquely enforced key. Server-generated auto-number values do not solve a lost-response case
   because the caller does not know the generated value, and metadata duplicate checks do not prove a DB unique
   constraint.
3. **(d) NeedsReview fallback only — always feasible and safe, but operationally weak.** Use when ID reservation failed,
   the transaction cannot be read back under the authorized context, or verification is ambiguous. Never blind
   retry.
4. **(c) Existing log/audit correlation — not reliable for automatic reconciliation.** `axpertlog` is
   post-commit, best-effort, non-unique, and lacks a request/idempotency/trace identifier. It can corroborate but
   cannot prove absence or safely authorize a retry.

**D30 recommendation:** adopt (a) as the default NEW-record reconciliation mechanism, allow (b) only for an
explicitly configured and proven unique caller-known key, never use (c) as authoritative proof, and retain (d)
as the fail-closed fallback. This requires an AxData-side reservation/reconciliation adapter, not an
`ARM-11.4` change.

## 7. Authentication

| AxData value            | AxAuth/AxTstructData source                                                                     | Proposed AxData mapping                                                                                                                                                                                                                |
| ----------------------- | ----------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --- |
| `SessionId`             | JWT `sid`; duplicated in `ClaimTypes.NameIdentifier`; Redis hash key is the same `ARMSessionId` | Derive from the validated token. Require all supplied session claims to agree. Resolve it through the ARM session adapter on every request. If a legacy body contains `ARMSessionId`, require equality but never trust it as identity. |
| `UserName`              | JWT `ClaimTypes.Name`; authoritative session value `USERNAME`                                   | Use Redis `USERNAME` after liveness validation and require it to equal the JWT username. Do not use `NameIdentifier`, because AxAuth puts the session ID there.                                                                        |
| App/access code         | Redis `APPNAME`                                                                                 | Keep as adapter-private routing data. Use existing ARM configuration to select the tenant Redis/application connection.                                                                                                                |
| `TenantId`              | `APPNAME` resolves through ARM app connection configuration; `DBUser` is the database schema    | Use the resolved schema as canonical `TenantId`. Validate that the live session’s `APPNAME` agrees with the application selected while resolving the session.                                                                          |
| `Roles`                 | `USER_ROLES`, populated at login from `AXUSERLEVELGROUPS`                                       | Split, trim, remove empty values, and deduplicate case-insensitively. Never accept roles from the request or JWT.                                                                                                                      |
| `IsAdministrator`       | none                                                                                            | Not derived. Authorization never uses an admin flag (P1-07); leave false.                                                                                                                                                              |     |
| Axpert internal session | Redis `AXPERT_SESSIONID`                                                                        | Keep inside `AxData.Integration.Axpert`; pass it only to concrete Axpert adapters that need it. It is not the AxData tenant or credential.                                                                                             |
| Site number             | Redis `SITENO` (from AxProps `siteno` at login)                                                 | Adapter-private. Used only by the record-ID reservation adapter; never exposed to Application or accepted from requests.                                                                                                               |

## §8 Create-with-RecordId and probe

This is the P2-06/D30 spike plan. It records the repository evidence available before the disposable-schema
characterization. It does not treat code inspection as proof of the database constraint or of live replay
behavior. The live steps below must use a disposable tenant/schema only; review data remains read-only.

### 8.1 What selects create versus edit

`AxPutInput.Data` contains one or more `AxPut` requests. Each request independently carries `TransId`, `RecordId`,
`Action`, `KeyField`, `KeyValue`, and `SubmitData` (`AxTstructData/Model/TstructData.cs:31-60`). The top-level
`Action` is authoritative for selecting the save path:

- `Action = CREATE` performs create permission checking and then calls the create validation/SQL-building path
  (`AxTstructData/Service/AxPutService.cs:587-605,668-695`). The create access-cache key omits `KeyField` and
  `KeyValue` (`AxPutService.cs:587`), so those values do not turn a create into an edit.
- `Action = EDIT` performs edit permission lookup using `KeyField` and `KeyValue`; the repository passes both to
  the provider-specific permission routine (`AxTstructData/Repositories/AxPutRepository.cs:20-52`). The resolved
  record ID returned by that lookup replaces the request's `RecordId` before edit validation and loading
  (`AxPutService.cs:645-659`; resolution is produced at `AxPutService.cs:363-380,473-478`). A caller-supplied
  `RecordId` is therefore not the edit-record authority.
- Within an EDIT payload, each submitted row may carry `axrow_action = CREATE`, `EDIT`, or `DELETE`. CREATE rows
  are counted for ID allocation (`AxPutService.cs:2275-2305`), and the edit SQL builder dispatches those actions
  to insert/update/delete operations (`AxPutService.cs:1905-2059,2065-2168`). These row flags do not change the
  top-level transaction action.

For a top-level CREATE, the main-row builder allocates an ID only when `putData.RecordId == 0`. When it is
nonzero, the supplied value is retained and passed to the insert (`AxPutService.cs:1821-1828,1863-1871`). The
insert builder binds that value as `@recordid` into the main table's `<table-name>id` column
(`AxPutService.cs:1066-1125,1273-1286`). Therefore, by code inspection, a caller-supplied nonzero RecordId on a
new transaction is treated as **create**, provided `Action = CREATE`; RecordId does not select edit. The
disposable-schema probe must still confirm the live behavior.

The existing pre-count includes every submitted CREATE row even when the main ID was supplied
(`AxPutService.cs:570,642-643,2275-2305`). Consequently, the current implementation may reserve one unused ID;
it does not replace the supplied main ID.

### 8.2 Physical key and uniqueness evidence

AxPut uses a semantic `RecordId`, but the physical main-table key column is not literally `recordid`. For the
main table, `GenerateInsertStatement` writes the ID to `<table-name>id` (`AxPutService.cs:1084-1125`), and the
update/delete builders address rows through `<table-name>id` (`AxPutService.cs:1415-1425,1438-1455`). Child and
grid tables similarly use their own `<child-table>id` plus the main `<main-table>id` foreign-key value
(`AxPutService.cs:1139-1198`).

No Axpert Builder transaction-table DDL generator is present in this `ARM-11.4` repository. Targeted searches
found no code here that declares `<table-name>id` as a primary key or creates a unique constraint/index for it.
Accordingly, this note cannot truthfully cite builder code proving uniqueness. The AxPut code cited above shows
the assumed key column and always issues a plain insert; it does not itself enforce uniqueness. Before D30 can
be considered characterized, one of these sources must establish the constraint for every supported provider:

1. the matching Axpert Builder source/generated DDL, showing `<table-name>id` in a primary-key or unique
   constraint/index; or
2. the disposable table's PostgreSQL, Oracle, and SQL Server catalogs, captured after the table is generated by
   the matching Builder release.

If the live table has neither a primary key nor a unique constraint/index on `<main-table>id`, same-ID replay is
unsafe and the D30 automatic-retry premise fails. The outcome must be `NeedsReview`; application-side
check-then-insert is not an acceptable substitute for a database uniqueness constraint.

### 8.3 Main-table resolution and SQL-only existence probe

StructDef loads each DC's table from its XML `table` node into `TFrame.TableName`; when `FrameNo == 1`, it assigns
that value to `StructDef.PrimaryTable` (`AxStructDef/StructDef.cs:690-700,723-734`; the frame property is declared
at `AxStructDef/Models/TFrame.cs:13-18`). AxPut independently follows the same first-frame convention when
building create/edit SQL (`AxPutService.cs:1752-1769,1880-1897`). The D30 probe should therefore use the exact
loaded `PrimaryTable`, and derive the physical key column as `PrimaryTable + "id"`.

The tenant schema must come from trusted server-side tenant/database context, never from an API request and
never by accepting a schema-qualified client identifier. Treat the StructDef table as one identifier, reject an
empty or malformed value, and quote schema, table, and key column separately. Do not parameterize identifiers;
validate them against the Builder-supported identifier grammar and then apply provider quoting:

| Provider   | Qualified target shape                                                                                |
| ---------- | ----------------------------------------------------------------------------------------------------- |
| PostgreSQL | `"<trusted-schema>"."<main-table>"`, key `"<main-table>id"`                                           |
| Oracle     | `"<TRUSTED-SCHEMA>"."<MAIN-TABLE>"`, key `"<MAIN-TABLE>ID"`; use the canonical catalog/Builder casing |
| SQL Server | `[<trusted-schema>].[<main-table>]`, key `[<main-table>id]`                                           |

Issue a scalar, read-only, parameterized count:

```sql
SELECT COUNT(*)
FROM <provider-quoted trusted-schema>.<provider-quoted main-table>
WHERE <provider-quoted main-table-id-column> = @recordid
```

`IDbHelper.ExecuteScalarAsync` accepts parameter names, `DbType`s, and values on the open single connection
(`ARMCommon/Interface/IDbHelper.cs:39-43`). Oracle rewrites `@` placeholders to `:` and binds by name
(`ARMCommon/Helpers/DbHelper.cs:487-525`); PostgreSQL and SQL Server bind the supplied parameters
(`DbHelper.cs:2219-2262,3719-3761`). Always check `ScalarResult.error` before reading `ScalarResult.result`
(`ARMCommon/Model/ARMResult.cs:89-93`). Interpret an exact count of zero as absent, one as present, more than one
as ambiguous, and any execution/conversion error as a probe error. The reconciliation port exposes only the
successful existence conclusion, not row data or field values; ambiguous/error results become `NeedsReview`.

### 8.4 Disposable-schema characterization test plan

Every step below marked **USER MUST RUN** requires Builder/database/session access and is intentionally not
implemented as an automated test until separately approved. Use opaque placeholders in notes and logs; do not
print IDs, field values, credentials, tokens, connection strings, or generated payload contents.

1. **Provision and constraint proof — USER MUST RUN.** For each supported provider, create a disposable tenant
   schema and a minimal transaction with a main table using the production-matching Axpert Builder release.
   Prefer a definition with one child/grid table and controlled GenMap/MdMap targets so replay side effects are
   observable. Capture sanitized generated DDL or query the provider catalog. Assert that the main
   `<main-table>id` column is covered by a primary key or unique constraint/index. Record only provider,
   constraint kind, pass/fail, and source-code/DDL location; do not record actual schema, table, constraint, or ID
   values. Stop and mark D30 unproven if uniqueness is absent or ambiguous.
2. **Create with a reserved ID — USER MUST RUN.** Reserve one Axpert-compatible ID through the same
   `FN_AXDBPUT_GETRECID` provider path intended for P2-04. Submit a minimal AxPut request with top-level
   `Action = CREATE`, the reserved nonzero `RecordId`, and no edit key pair. Assert success, assert the returned
   main mapping corresponds to the reserved ID without printing it, and use the SQL-only probe to assert an
   exact count of one. Verify expected child/GenMap/MdMap effects once.
3. **Same-ID create replay — USER MUST RUN.** Replay the byte-equivalent create payload with the same reserved
   ID. Assert AxPut reports failure caused by the main-key uniqueness boundary, the main-row count remains
   exactly one, the original row is unchanged, and child/GenMap/MdMap counts did not increase. If a second main
   row or any committed replay side effect appears, D30 automatic retry fails and the case goes to
   `NeedsReview`.
4. **Timeout with late commit — USER MUST RUN.** Reserve a different opaque ID. Dispatch CREATE through a client
   whose observation timeout is shorter than the AxPut command timeout, then stop waiting without terminating
   the server process. Allow the server call to finish and do not retry during the configured command-timeout
   plus reconciliation margin. After that boundary, run the SQL-only probe. Characterize both controlled
   outcomes: present means complete the existing InProgress operation without rerunning AxPut; authoritatively
   absent means `FailedRetryable`, whose later retry must reuse the same reserved ID; probe error or count greater
   than one means `NeedsReview`.
5. **Edit replay — USER MUST RUN.** Create a disposable baseline record, then submit EDIT using the semantic
   key pair (`KeyField = recordid`, opaque `KeyValue`) and an update plus a child row marked
   `axrow_action = CREATE`; include controlled GenMap/MdMap effects where available. Replay the identical EDIT.
   Record whether the main update runs again, audit timestamps change, another child ID/row is created, and
   mappings execute again. The expected safety conclusion is independent of the exact observed combination:
   EDIT has no version guard and must transition stale InProgress work to `NeedsReview`, never automatic retry.
6. **Sanitized evidence review — USER MUST RUN.** Retain only pass/fail outcomes, provider name, exception class
   or constraint category, counts, timings relative to configured timeouts, and source locations. Remove all real
   identifiers and values before adding results to this note. Only after review should automated contract or
   integration tests be proposed; this planning task writes no tests.

### 8.5 fn_axdbput_getrecid (PostgreSQL)

Signature `(psiteno integer, pnoofrows integer)` returns one varchar `recordid` per requested row:
`rpad(psiteno, 3, '0') || lpad(nextval('seq_axdb_recordid'), 12, '0')`.

#### AxPut caller chain

The active AxPut allocation path is:

1. `SaveData` counts top-level CREATE rows and EDIT rows whose `axrow_action` is CREATE
   (`AxTstructData/Service/AxPutService.cs:570,2275-2305`).
2. Before building the first transaction, it preallocates that count through `GetNewRecordIds`
   (`AxPutService.cs:642-643`). `GetRecordIdForCreate` uses the same method for an on-demand refill
   (`AxPutService.cs:2243-2272`).
3. GenMap-generated INSERT commands also obtain IDs through `GetNewRecordIds` before their placeholder values
   are replaced (`AxPutService.cs:805-835`).
4. `GetNewRecordIds` calls the loaded StructDef's `GetRecordIdsFromDB`, checks the returned error before reading
   data, converts every returned `recordid` to `Int64`, and returns `List<Int64>`
   (`AxPutService.cs:2243-2255`).
5. MdMap's `StructDef.BuildInsertSql` additionally calls `GetRecordIdsFromDB(1)` directly when it must populate
   the target table's numeric `<table>id` column, then converts that result to `Int64`
   (`AxStructDef/StructDef.cs:6588-6595,6642-6659`).

The older repository allocation methods visible at `AxPutService.cs:1467-1750` and
`AxPutRepository.cs:162-181` are commented out and are not active callers. `AxDataObject` contains its own copy
of the allocation routine; it is not the `AxTstructData`/`StructDef` path described above.

#### Source, default, and scope of `psiteno`

The value originates in the application AxProps XML, not in the AxPut request. During login, AxAuth reads the
current application's AxProps XML from its selected database and parses `<props><envvar><siteno>` into the
string property `EnvironmentVars.SiteNo` (`AxAuth/Repositories/LoginRepository.cs:23-43`;
`ARMCommon/Helpers/AxPropsManager.cs:25-53,146-150`). AxAuth then copies that value into the top-level `SITENO`
field of the newly created ARM session hash (`AxAuth/Service/LoginService.cs:306-337`). Session values are
written to Redis under the session ID (`ARMCommon/Helpers/Utils.cs:2155-2168`).

At AxPut entry, `ValidateSession` loads that Redis hash into the request-scoped `Utils._sessionVars`
(`ARMCommon/Helpers/Utils.cs:684-710,2940-2954`; `AxTstructData/Program.cs:60-64`). The same `Utils` instance is
passed into `StructDef.LoadStructure` (`AxTstructData/Service/AxPutService.cs:500-502`).
`StructDef.GetRecordIdsFromDB` reads `SESSION_DATA.SITENO` directly from `_sessionVars`, converts it to `Int32`,
and binds it as `DbType.Int32` to the provider call (`AxStructDef/StructDef.cs:6929-6969`). Although
`GetSessionData` separately deserializes the session's `GLOBALVARS` value into `_globalVars`, the allocator does
not read `siteno` from that dictionary (`ARMCommon/Helpers/Utils.cs:696-706`).

The effective fallback is:

- `siteNo` starts at `0`.
- A missing key, null/empty value, nonnumeric value, or conversion overflow is swallowed by the empty catch and
  leaves it at `0`.
- A successfully parsed negative value is changed to `1`.
- A configured value of `0` remains `0`.

This follows directly from `StructDef.cs:6934-6943`. `EnvVars.SiteNo` itself defaults to an empty string when
the XML value is absent (`AxPropsManager.cs:146-150`), which consequently reaches the allocator fallback of
zero after session storage and failed conversion.

The authoritative source is therefore **per tenant/application**, because it comes from that application's
AxProps XML/database selection. Operationally it is **copied per session at login** and AxPut consumes that
session-cached copy. It is not calculated from the username and is not per-user, though an existing session may
retain its copied value until the session is recreated after an AxProps change.

#### RecordId type and 15-digit compatibility

`AxPut.RecordId` is `System.Int64` (`long`), not `int` or `string`, and defaults to zero
(`AxTstructData/Model/TstructData.cs:45-57`). The allocation parameter `psiteno` and requested row count are
`Int32`, but returned database `recordid` values are converted to `Int64` both in AxPut and the MdMap insert path
(`AxPutService.cs:2252-2255`; `AxStructDef/StructDef.cs:6653-6658`). Insert parameters for main IDs are likewise
typed `DbType.Int64` (`AxPutService.cs:1084-1137`).

A 15-digit decimal ID is compatible with `Int64`: even the largest 15-digit value is below `Int64.MaxValue`.
It is also below JavaScript's maximum exactly representable integer, so the 15-digit format itself does not
lose integer precision in ordinary JSON/JavaScript transport. The database function returns `recordid` as text,
but AxPut's public request model and internal save path use the converted numeric `Int64` value.

- nextval is non-transactional: IDs are never reissued after rollback or failure; gaps are expected.
- Uniqueness comes from the shared sequence; different site numbers can produce the same 3-character prefix (1, 10, 100), which does not affect ID uniqueness.
- The sequence is unqualified and resolves via the trusted tenant search_path.
- It writes (sequence state), so it cannot run on a read-only connection.
- Oracle and SQL Server bodies: not yet reviewed. Open: confirm the same never-reissued property.
- PostgreSQL: <main-table>id confirmed as primary key and unique (catalog query, pass).
- psiteno: per tenant/application, parsed from AxProps <envvar><siteno> at login, copied into the ARM session hash as SITENO. GetRecordIdsFromDB reads it from the session; missing/invalid -> 0, negative -> 1.
- RecordId is Int64 end to end in AxPut and MdMap insert; the function's text result is converted to Int64 (leading zeros are lost when siteno = 0).
- Allocation uses one ID per top-level CREATE row plus EDIT rows marked CREATE; a supplied main ID leaves one spare reserved ID.

## 9. D3: Set validation failure behavior — ANSWERED

`Validate.cs`'s `RefreshField()`/`ValidateData()` iterate frames -> rows -> fields. A `RapidValidateData` failure
sets `validationPassed = false`, appends to `validateResultList`/`errorList`, and continues to the next field.
Field-level exceptions are caught, logged, and also don't stop the batch. `ValidateData()` returns
`OperationResult { Result = "Fail"|"Success", Message = joined validateResultList }`. `RefreshFieldsOnLoad`
short-circuits only on a frame-level exception. Design decision recorded in D3: apply valid independent fields,
block dependents of an invalid field, block final Save until all validation errors are resolved.

## Open — what to bring next

1. Real DDL for `axactivetaskstatus` (D15) — code paths disagree on column names.
2. Exact `CheckTstructEditAccess` DataTable column names pre-normalization (low priority — the four-list output
   shape AxData actually consumes is already known).
3. Low priority, only if a later slice needs to mirror them: `IRedisCacheHelper`, `IAppSettingsHelper`,
   `DataContext`, `Constants_SQL`, `Constants.AXPERT_PROPERTIES`/`SESSION_DATA` enum shapes,
   `DBHelper.GetDBTimeTaken()`/`GetDBConnectionTime()` semantics.

```

```
