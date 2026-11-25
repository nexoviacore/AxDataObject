[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$axPut = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxTstructData\Service\AxPutService.cs')
$axPutModel = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxTstructData\Model\TstructData.cs')
$controller = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxTstructData\Controllers\AxPutController.cs')
$project = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxTstructData\AxTstructData.csproj')
$dataObjectStore = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxTstructData\Service\AxPutDataObjectStore.cs')
$dataObjectProjection = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxTstructData\Service\AxPutDataObjectProjection.cs')
$passed = 0

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
    $script:passed++
}

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    Assert-True -Condition $Text.Contains($Expected, [StringComparison]::Ordinal) -Message $Message
}

function Assert-Excludes {
    param([string]$Text, [string]$Unexpected, [string]$Message)
    Assert-True -Condition (-not $Text.Contains($Unexpected, [StringComparison]::OrdinalIgnoreCase)) -Message $Message
}

try {
    Assert-Excludes $axPutModel ('callIsFrom' + 'AxSave') 'The disposable AxSave request flag still exists.'
    Assert-Excludes $axPut ('AX' + 'SAVE') 'The disposable Redis key path still exists.'
    Assert-Excludes $axPut ('AxSave' + 'DataObject') 'The disposable Redis projection still exists.'
    Assert-Contains $project 'AxData.Contracts.csproj' 'AxTstructData does not reference the neutral shared contract.'
    Assert-Contains $axPutModel '[Newtonsoft.Json.JsonProperty("objectid")]' 'objectid is missing from AxPutInput.'
    Assert-Contains $axPutModel '[Newtonsoft.Json.JsonProperty("retainDataObject")]' 'retainDataObject is missing from AxPutInput.'
    Assert-Excludes $axPutModel 'expectedVersion' 'AxPutInput still exposes expectedVersion.'
    Assert-Excludes $controller 'Request.Headers.IfMatch' 'AxPut still interprets If-Match.'
    Assert-Excludes $controller 'TryGetValue("version"' 'AxPut failure responses still disclose object versions.'
    Assert-Contains $controller 'TryGetValue("errorcode"' 'AxPut failure responses do not preserve AXD error codes.'
    Assert-Contains $controller 'TryGetValue("httpstatus"' 'AxPut failure responses do not preserve ObjectId HTTP status.'
    Assert-Contains $controller 'FindFirstValue("sid")' 'AxPut does not compare the bearer-token session with ARMSessionId.'

    $session = $axPut.IndexOf('await _common.ValidateSession(putData.ARMSessionId)', [StringComparison]::Ordinal)
    $identity = $axPut.IndexOf('putData.Project = _common._sessionVars', $session, [StringComparison]::Ordinal)
    $contract = $axPut.IndexOf('PrepareObjectContract(putData, objectIdWasSupplied)', $identity, [StringComparison]::Ordinal)
    $claim = $axPut.IndexOf('await ClaimExistingDataObjectAsync()', $contract, [StringComparison]::Ordinal)
    $save = $axPut.IndexOf('result = await SaveTstructData(putData)', $claim, [StringComparison]::Ordinal)
    Assert-True ($session -ge 0 -and $identity -gt $session -and $contract -gt $identity -and $claim -gt $contract -and $save -gt $claim) 'AxPut does not preserve session→trusted identity→contract→claim→Save order.'

    Assert-Contains $axPut 'AxPutObjectContractRules.Resolve(' 'AxPut does not consume the shared request rules.'
    Assert-Excludes $axPut 'knownVersion' 'AxPut still carries an object version into responses.'
    Assert-Contains $axPut 'result.data["objectid"] = putData.ObjectId;' 'AxPut does not return the supplied/generated ObjectId.'
    Assert-Contains $dataObjectStore 'AXD:{{{tenantId.Split("~")[0].ToUpperInvariant()}}}:{objectId.ToUpperInvariant()}' 'AxPut does not use the shared tenant-scoped DataObject key.'
    Assert-Contains $dataObjectStore 'HashGetAsync(key, "payload")' 'AxPut does not read the shared DataObject payload.'
    Assert-Excludes $dataObjectStore 'HashSetAsync' 'P2-03 writes shared DataObject state before P2-04 finalization.'
    Assert-Excludes $dataObjectStore 'StringSetAsync' 'P2-03 writes shared DataObject state before P2-04 finalization.'
    Assert-Contains $axPut 'AxPutDataObjectProjection.CheckLoadedState(' 'AxPut does not check the stored actor relation.'
    Assert-Excludes $axPut 'CheckExpectedVersion(' 'AxPut still rejects client object versions.'
    Assert-Excludes $axPut 'CheckStructDefVersion(' 'AxPut still binds state to a stored StructDef version.'
    Assert-Contains $axPut 'DataObject StructDef binding bypass' 'AxPut does not emit the safe current-definition diagnostic.'
    Assert-Contains $dataObjectStore "redis.call('EXISTS', KEYS[2])" 'AxPut claim does not check the shared Set lock.'
    Assert-Contains $dataObjectStore "redis.call('HGET', KEYS[1], 'payload') ~= ARGV[4]" 'AxPut claim does not verify the loaded state atomically.'
    Assert-Contains $dataObjectStore "'saveClaim', ARGV[3]" 'AxPut claim is not token-owned.'
    Assert-Contains $axPut 'await HydrateAndOverlayDataObjectAsync(putData)' 'AxPut does not hydrate and overlay stored ObjectId state.'
    Assert-Contains $axPut 'requiresInitializationRefresh: _dataObjectContext?.Snapshot is null' 'Generated ObjectIds no longer run default/autogenerated initialization.'
    Assert-Contains $dataObjectProjection '["axp_recid"]' 'DataObject hydration does not restore persisted row identity.'
    Assert-Contains $dataObjectProjection '["axrow_action"]' 'DataObject hydration does not restore row action.'
    Assert-Contains $dataObjectProjection 'container.Fields.Count' 'DataObject hydration does not consume positional field ordinals.'
    Assert-Excludes $dataObjectProjection 'OwnerUserName' 'DataObject authorization still accepts ownerUserName.'
    Assert-Excludes $dataObjectProjection 'snapshot.Version' 'DataObject projection still consumes an object version.'
    Assert-Excludes $dataObjectProjection 'KeyField =' 'DataObject projection still persists a business-key field locator.'
    Assert-Excludes $dataObjectProjection 'KeyValue =' 'DataObject projection still persists a business-key value locator.'
    Assert-Contains $axPut 'string? storedKeyField = snapshot.RecordId is > 0 ? "recordid" : null;' 'ObjectId-aware AxPut does not derive the legacy edit key field from RecordId.'
    Assert-Contains $axPut 'string? storedKeyValue = snapshot.RecordId?.ToString(CultureInfo.InvariantCulture);' 'ObjectId-aware AxPut does not derive the invariant legacy edit key value from RecordId.'
    Assert-Contains $axPut 'ObjectId runtime hydration' 'ObjectId trace no longer records hydrated state.'
    Assert-Contains $axPut 'ObjectId payload overlay' 'ObjectId trace no longer records overlay field values.'
    Assert-Contains $axPut 'result.data["dataObjectRetained"] = true;' 'Known ObjectId state does not report actual P2-03 retention.'
    Assert-Contains $controller 'TryGetValue("dataObjectRetained"' 'AxPut failure responses do not preserve retention state.'
    Assert-Contains $dataObjectProjection 'CaptureRecordIdMappings(' 'AxPut does not map returned record IDs to stable row identities.'

    $loadObject = $axPut.IndexOf('await _dataObjectStore.LoadAsync(tenantId, putData.ObjectId!)', $contract, [StringComparison]::Ordinal)
    $actorRelation = $axPut.IndexOf('AxPutDataObjectProjection.CheckLoadedState(', $loadObject, [StringComparison]::Ordinal)
    Assert-True ($loadObject -gt $contract -and $actorRelation -gt $loadObject) 'AxPut does not preserve contract→tenant load→actor relation order.'

    $createSubmit = $axPut.IndexOf('await _validate.SubmitValueDetailed(tstData)', $save, [StringComparison]::Ordinal)
    $createValidate = $axPut.IndexOf('await ValidateForPersistence(', $createSubmit, [StringComparison]::Ordinal)
    $createSql = $axPut.IndexOf('GetTableWiseSQLCommandsForCreate(putData)', $createValidate, [StringComparison]::Ordinal)
    Assert-True ($createSubmit -gt $save -and $createValidate -gt $createSubmit -and $createSql -gt $createValidate) 'AxPut create submission/validation/SQL order changed.'

    $editLoad = $axPut.IndexOf('await _getData.LoadData(getData, null, _validate)', $createSql, [StringComparison]::Ordinal)
    $editSubmit = $axPut.IndexOf('await _validate.SubmitValueDetailed(tstData)', $editLoad, [StringComparison]::Ordinal)
    $editValidate = $axPut.IndexOf('await ValidateForPersistence(', $editSubmit, [StringComparison]::Ordinal)
    $editSql = $axPut.IndexOf('GetTableWiseSQLCommandsForEdit(putData)', $editValidate, [StringComparison]::Ordinal)
    Assert-True ($editLoad -gt $createSql -and $editSubmit -gt $editLoad -and $editValidate -gt $editSubmit -and $editSql -gt $editValidate) 'AxPut edit load/submission/validation/SQL order changed.'

    $mdMap = $axPut.IndexOf('sqlCommands.AddRange(await _validate._sdef.DoMDMapAsync())', $editSql, [StringComparison]::Ordinal)
    $captureMappings = $axPut.IndexOf('AxPutDataObjectProjection.CaptureRecordIdMappings(', $mdMap, [StringComparison]::Ordinal)
    $executeSql = $axPut.IndexOf('await _repo.ExecuteSaveDataSQLCommands(putData, sqlCommands)', $captureMappings, [StringComparison]::Ordinal)
    $attachments = $axPut.IndexOf('SaveImagesAndAttachments(putDataInput, putData)', $executeSql, [StringComparison]::Ordinal)
    $queueChildren = $axPut.IndexOf('foreach (var childMap in _validate._sdef.GetChildGenMaps())', $attachments, [StringComparison]::Ordinal)
    $commit = $axPut.IndexOf('_common._dbHelper.CommitTransaction()', $queueChildren, [StringComparison]::Ordinal)
    $publishMappings = $axPut.IndexOf('_dataObjectContext.CommittedRecordIdMappings = _pendingRecordIdMappings', $commit, [StringComparison]::Ordinal)
    Assert-True ($mdMap -gt $editSql -and $captureMappings -gt $mdMap -and $executeSql -gt $captureMappings -and $attachments -gt $executeSql -and $queueChildren -gt $attachments -and $commit -gt $queueChildren -and $publishMappings -gt $commit) 'AxPut no longer preserves MdMap→mapping capture→SQL→attachments→child queue→commit→mapping publication order.'

    Write-Output "SUMMARY passed=$passed failed=0"
    exit 0
}
catch {
    Write-Error "FAIL $($_.Exception.Message)"
    exit 1
}
