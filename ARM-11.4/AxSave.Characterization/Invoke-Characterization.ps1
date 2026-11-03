[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$axPut = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxTstructData\Service\AxPutService.cs')
$axPutModel = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxTstructData\Model\TstructData.cs')
$controller = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxTstructData\Controllers\AxPutController.cs')
$dataObject = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxTstructData\Model\AxSaveDataObject.cs')
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

try {
    Assert-Contains $axPutModel '[Newtonsoft.Json.JsonProperty("callIsFromAxSave")]' 'AxSave caller flag is missing from AxPutInput.'
    Assert-Contains $axPutModel 'public bool CallIsFromAxSave { get; set; } = false;' 'AxSave caller flag default changed.'
    Assert-Contains $axPutModel '[Newtonsoft.Json.JsonProperty("objectid")]' 'AxSave objectid is missing from AxPutInput.'
    Assert-Contains $dataObject 'Dictionary<string, Dictionary<string, Dictionary<string, string>>> Data' 'AxSave DataObject does not retain DC/row/field values.'

    $idCheck = $axPut.IndexOf('_axSaveObjectIdSupplied = putData.CallIsFromAxSave', [StringComparison]::Ordinal)
    $idCreate = $axPut.IndexOf('putData.ObjectId = Guid.NewGuid().ToString("N")', $idCheck, [StringComparison]::Ordinal)
    $saveCall = $axPut.IndexOf('result = await SaveTstructData(putData)', $idCreate, [StringComparison]::Ordinal)
    Assert-True ($idCheck -ge 0 -and $idCreate -gt $idCheck -and $saveCall -gt $idCreate) 'Missing objectid is not generated before AxPut SaveData.'

    $saveDataStart = $axPut.IndexOf('private async Task<APIResult> SaveData(', [StringComparison]::Ordinal)
    $objectLoad = $axPut.IndexOf('axSaveDataObject = await LoadAxSaveDataObjectAsync', $saveDataStart, [StringComparison]::Ordinal)
    Assert-True ($objectLoad -gt $saveDataStart) 'Supplied objectid does not load its Redis DataObject.'

    $createInitialize = $axPut.IndexOf('await InitializeStructDefFromAxSaveDataObjectAsync', $objectLoad, [StringComparison]::Ordinal)
    $createSubmit = $axPut.IndexOf('await _validate.SubmitValueDetailed(tstData)', $createInitialize, [StringComparison]::Ordinal)
    $createValidate = $axPut.IndexOf('await ValidateForPersistence(', $createSubmit, [StringComparison]::Ordinal)
    $createRedis = $axPut.IndexOf('await SaveAxSaveDataObjectAsync(putDataInput, putData)', $createValidate, [StringComparison]::Ordinal)
    $createSql = $axPut.IndexOf('GetTableWiseSQLCommandsForCreate(putData)', $createRedis, [StringComparison]::Ordinal)
    Assert-True ($createInitialize -gt $objectLoad -and $createSubmit -gt $createInitialize -and $createValidate -gt $createSubmit -and $createRedis -gt $createValidate -and $createSql -gt $createRedis) 'AxSave create order is not object→payload→refresh/validation→Redis→SQL.'

    $editStart = $axPut.IndexOf('Constants.TSTRUCT_ACTIONS.EDIT.ToString()', $createSql, [StringComparison]::Ordinal)
    $editInitialize = $axPut.IndexOf('await InitializeStructDefFromAxSaveDataObjectAsync', $editStart, [StringComparison]::Ordinal)
    $editSubmit = $axPut.IndexOf('await _validate.SubmitValueDetailed(tstData)', $editInitialize, [StringComparison]::Ordinal)
    $editValidate = $axPut.IndexOf('await ValidateForPersistence(', $editSubmit, [StringComparison]::Ordinal)
    $editRedis = $axPut.IndexOf('await SaveAxSaveDataObjectAsync(putDataInput, putData)', $editValidate, [StringComparison]::Ordinal)
    $editSql = $axPut.IndexOf('GetTableWiseSQLCommandsForEdit(putData)', $editRedis, [StringComparison]::Ordinal)
    Assert-True ($editInitialize -gt $editStart -and $editSubmit -gt $editInitialize -and $editValidate -gt $editSubmit -and $editRedis -gt $editValidate -and $editSql -gt $editRedis) 'AxSave edit order is not object→payload→refresh/validation→Redis→SQL.'

    Assert-Contains $axPut 'result.data["objectid"] = putData.ObjectId;' 'AxPut does not return objectid for AxSave calls.'
    Assert-Contains $controller 'getDataResult.data.ContainsKey("objectid")' 'AxPut HTTP errors do not preserve objectid.'
    Assert-True ($createRedis -lt $createSql -and $editRedis -lt $editSql) 'The temporary Redis-before-SQL behavior changed; the intended inconsistency demonstration would no longer be valid.'

    Write-Output "SUMMARY passed=$passed failed=0"
    exit 0
}
catch {
    Write-Error "FAIL $($_.Exception.Message)"
    exit 1
}
