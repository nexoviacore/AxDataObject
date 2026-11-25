[CmdletBinding()]
param(
    [Parameter()]
    [string] $ConfigPath = (Join-Path $PSScriptRoot "../../config/local/p1c-04.local.ps1"),

    [Parameter()]
    [string] $ReportPath = (Join-Path $PSScriptRoot "../../artifacts/real-data-review/p1c-05-report.md")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Require-Value {
    param([string] $Name, [object] $Value)

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string] $Value)) {
        throw "Required P1C-04 setting '$Name' is missing."
    }
}

function Get-EnvironmentValue {
    param([string] $Name)

    $value = [Environment]::GetEnvironmentVariable($Name)
    Require-Value $Name $value
    return $value
}

function ConvertTo-SafeCell {
    param([object] $Value)

    if ($null -eq $Value -or [string]::IsNullOrWhiteSpace([string] $Value)) {
        return "-"
    }

    return ([string] $Value).Replace("|", "\|").Replace("`r", " ").Replace("`n", " ")
}

function Invoke-RedisCli {
    param(
        [Parameter(Mandatory)] [string[]] $Arguments,
        [switch] $AllowEmpty
    )

    $result = & $script:P1C04RedisCliPath -h $script:P1C04RedisHost -p $script:P1C04RedisPort @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Redis CLI command failed."
    }

    $text = ($result | ForEach-Object { [string] $_ }) -join "`n"
    if (-not $AllowEmpty -and [string]::IsNullOrWhiteSpace($text)) {
        throw "Redis CLI returned no result."
    }

    return $text.TrimEnd()
}

function Test-MinimumVersion {
    param([string] $Actual, [version] $Minimum)

    try {
        return ([version] $Actual) -ge $Minimum
    }
    catch {
        return $false
    }
}

function Invoke-SafeHttp {
    param(
        [Parameter(Mandatory)] [ValidateSet("GET", "POST")] [string] $Method,
        [Parameter(Mandatory)] [string] $Uri,
        [object] $Body,
        [hashtable] $Headers = @{},
        [int] $ExpectedStatus = 200,
        [string] $ExpectedErrorCode
    )

    $parameters = @{
        Method = $Method
        Uri = $Uri
        Headers = $Headers
        SkipHttpErrorCheck = $true
    }
    if ($null -ne $Body) {
        $parameters.ContentType = "application/json"
        $parameters.Body = $Body | ConvertTo-Json -Depth 100 -Compress
    }

    $response = Invoke-WebRequest @parameters
    $payload = $null
    if (-not [string]::IsNullOrWhiteSpace($response.Content)) {
        try {
            $payload = $response.Content | ConvertFrom-Json -Depth 100
        }
        catch {
            throw "HTTP $Method failed with an unreadable response (status $($response.StatusCode))."
        }
    }

    if ([int] $response.StatusCode -ne $ExpectedStatus) {
        $safeCode = if ($null -ne $payload -and $null -ne $payload.code) { [string] $payload.code } else { "none" }
        $safePath = ([uri] $Uri).AbsolutePath
        throw "HTTP $Method $safePath returned status $($response.StatusCode), code $safeCode; expected $ExpectedStatus."
    }

    if (-not [string]::IsNullOrWhiteSpace($ExpectedErrorCode) -and
        ($null -eq $payload -or [string] $payload.code -ne $ExpectedErrorCode)) {
        $safeCode = if ($null -ne $payload -and $null -ne $payload.code) { [string] $payload.code } else { "none" }
        throw "HTTP $Method returned code $safeCode; expected $ExpectedErrorCode."
    }

    return $payload
}

function Wait-ForApi {
    param([System.Diagnostics.Process] $Process)

    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($script:P1C04StartupTimeoutSeconds)
    do {
        if ($Process.HasExited) {
            throw "AxData.Api exited before its health endpoint became ready."
        }

        try {
            $response = Invoke-WebRequest -Uri "$script:P1C04BaseUrl/health" -SkipHttpErrorCheck
            if ([int] $response.StatusCode -eq 200) {
                return
            }
        }
        catch {
            # Readiness is polled until the bounded deadline.
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTimeOffset]::UtcNow -lt $deadline)

    throw "AxData.Api did not become ready within $script:P1C04StartupTimeoutSeconds seconds."
}

function Get-StructDefReview {
    param([Parameter(Mandatory)] [string] $TransId)

    $env:AXDATA_REVIEW_TENANT = Get-EnvironmentValue "AxData__RealDataReview__TenantId"
    $env:AXDATA_REVIEW_TRANSID = $TransId
    $env:AXDATA_REVIEW_SESSION_ID = Get-EnvironmentValue "AxData__RealDataReview__SessionId"
    $env:AXDATA_REVIEW_AXPERT_SESSION_ID = Get-EnvironmentValue "AxData__RealDataReview__AxpertSessionId"
    $env:AXDATA_REVIEW_USER = Get-EnvironmentValue "AxData__RealDataReview__UserName"
    $roles = @()
    for ($index = 0; ; $index++) {
        $role = [Environment]::GetEnvironmentVariable("AxData__RealDataReview__Roles__$index")
        if ([string]::IsNullOrWhiteSpace($role)) { break }
        $roles += $role
    }
    if ($roles.Count -eq 0) { throw "At least one review role is required." }
    $env:AXDATA_REVIEW_ROLES = $roles -join ','
    $env:AXDATA_REVIEW_DB_TYPE = Get-EnvironmentValue "AxData__RealDataReview__DatabaseType"
    $env:AXDATA_REVIEW_CONNECTION_STRING = Get-EnvironmentValue "AxData__RealDataReview__ConnectionString"
    Remove-Item Env:AXDATA_REVIEW_RECORD_ID -ErrorAction SilentlyContinue
    Remove-Item Env:AXDATA_REVIEW_KEY_FIELD -ErrorAction SilentlyContinue
    Remove-Item Env:AXDATA_REVIEW_KEY_VALUE -ErrorAction SilentlyContinue

    $output = & dotnet test --nologo -v q --no-restore --no-build $script:ContractTestProject `
        --filter 'FullyQualifiedName~RealDataReviewSmokeTests.Current_structdef_and_field_runtime_load' `
        --logger 'console;verbosity=detailed' 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "The read-only StructDef/runtime review failed for TransId '$TransId'."
    }

    $text = ($output | ForEach-Object { [string] $_ }) -join "`n"
    $pattern = 'StructDef tenant=(?<tenant>[^,]+), transId=(?<transId>[^,]+), hash=(?<hash>[0-9a-f]{64}), updatedOn=(?<updatedOn>.*?), legacyVersion=.*?, runtimeStruct=.*?, runtimeMode=.*?, containers=(?<containers>\d+), fields=(?<fields>\d+), dependencies=(?<dependencies>\d+), fillGrids=(?<fillGrids>\d+)'
    $match = [regex]::Match($text, $pattern)
    if (-not $match.Success) {
        throw "The StructDef review completed without its safe summary line for TransId '$TransId'."
    }

    return [pscustomobject]@{
        Tenant = $match.Groups['tenant'].Value
        TransId = $match.Groups['transId'].Value
        Hash = $match.Groups['hash'].Value
        UpdatedOn = $match.Groups['updatedOn'].Value
        Containers = [int] $match.Groups['containers'].Value
        Fields = [int] $match.Groups['fields'].Value
        Dependencies = [int] $match.Groups['dependencies'].Value
        FillGrids = [int] $match.Groups['fillGrids'].Value
    }
}

function Get-FillGridReview {
    param([Parameter(Mandatory)] [hashtable] $Definition)

    Require-Value "FillGrid TransId" $Definition.TransId
    if (-not $Definition.ContainsKey("AddRows") -or [int] $Definition.AddRows -notin @(1, 2, 3)) {
        throw "Each P1C-05 FillGrid definition must declare AddRows 1, 2, or 3."
    }

    $env:AXDATA_REVIEW_TENANT = Get-EnvironmentValue "AxData__RealDataReview__TenantId"
    $env:AXDATA_REVIEW_TRANSID = [string] $Definition.TransId
    $env:AXDATA_REVIEW_SESSION_ID = Get-EnvironmentValue "AxData__RealDataReview__SessionId"
    $env:AXDATA_REVIEW_AXPERT_SESSION_ID = Get-EnvironmentValue "AxData__RealDataReview__AxpertSessionId"
    $env:AXDATA_REVIEW_USER = Get-EnvironmentValue "AxData__RealDataReview__UserName"
    $roles = @()
    for ($index = 0; ; $index++) {
        $role = [Environment]::GetEnvironmentVariable("AxData__RealDataReview__Roles__$index")
        if ([string]::IsNullOrWhiteSpace($role)) { break }
        $roles += $role
    }
    if ($roles.Count -eq 0) { throw "At least one review role is required." }
    $env:AXDATA_REVIEW_ROLES = $roles -join ','
    $env:AXDATA_REVIEW_DB_TYPE = Get-EnvironmentValue "AxData__RealDataReview__DatabaseType"
    $env:AXDATA_REVIEW_CONNECTION_STRING = Get-EnvironmentValue "AxData__RealDataReview__ConnectionString"
    $env:AXDATA_REVIEW_FILLGRID_ADDROWS = [string] $Definition.AddRows
    if ($Definition.ContainsKey("Name") -and -not [string]::IsNullOrWhiteSpace([string] $Definition.Name)) {
        $env:AXDATA_REVIEW_FILLGRID_NAME = [string] $Definition.Name
    }
    else {
        Remove-Item Env:AXDATA_REVIEW_FILLGRID_NAME -ErrorAction SilentlyContinue
    }

    $output = & dotnet test --nologo -v q --no-restore --no-build $script:ContractTestProject `
        --filter 'FullyQualifiedName~RealDataReviewSmokeTests.Real_FillGrid_metadata_and_tenant_schema' `
        --logger 'console;verbosity=detailed' 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "The read-only FillGrid review failed for TransId '$($Definition.TransId)' AddRows '$($Definition.AddRows)'."
    }

    $text = ($output | ForEach-Object { [string] $_ }) -join "`n"
    $pattern = 'FillGrid tenant=(?<tenant>[^,]+), transId=(?<transId>[^,]+), name=(?<name>[^,]+), addRows=(?<addRows>[123]), mode=(?<mode>Append|Replace), autoShow=(?<autoShow>True|False), firmBind=(?<firmBind>True|False), multiSelect=(?<multiSelect>True|False), parameters=(?<parameters>\d+), duplicateParameters=(?<duplicates>\d+), mappings=(?<mappings>\d+), sqlHash=(?<hash>[0-9a-f]{64}), schemaVerified=true'
    $matches = [regex]::Matches($text, $pattern)
    if ($matches.Count -eq 0) {
        throw "The FillGrid review completed without a safe summary for TransId '$($Definition.TransId)'."
    }

    return @($matches | ForEach-Object {
        [pscustomobject]@{
            Tenant = $_.Groups['tenant'].Value
            TransId = $_.Groups['transId'].Value
            Name = $_.Groups['name'].Value
            AddRows = [int] $_.Groups['addRows'].Value
            Mode = $_.Groups['mode'].Value
            AutoShow = $_.Groups['autoShow'].Value
            FirmBind = $_.Groups['firmBind'].Value
            MultiSelect = $_.Groups['multiSelect'].Value
            Parameters = [int] $_.Groups['parameters'].Value
            DuplicateParameters = [int] $_.Groups['duplicates'].Value
            Mappings = [int] $_.Groups['mappings'].Value
            SqlHash = $_.Groups['hash'].Value
        }
    })
}

function Invoke-WalkthroughCase {
    param([Parameter(Mandatory)] [hashtable] $Case)

    Require-Value "case Name" $Case.Name
    Require-Value "case TransId" $Case.TransId
    if (-not $Case.ContainsKey("Key")) { throw "Case '$($Case.Name)' must declare Key (use `$null for new state)." }
    if (-not $Case.ContainsKey("SetData") -or $null -eq $Case.SetData) { throw "Case '$($Case.Name)' must declare SetData." }

    $structDef = Get-StructDefReview -TransId ([string] $Case.TransId)
    $initBody = [ordered]@{ transId = [string] $Case.TransId; key = $Case.Key }
    $init = Invoke-SafeHttp -Method POST -Uri "$script:P1C04BaseUrl/api/v1/axdata/init" -Body $initBody
    Require-Value "Init objectId" $init.objectId
    $objectId = [string] $init.objectId
    $activeKey = "AXD:{$($structDef.Tenant.ToUpperInvariant())}:$($objectId.ToUpperInvariant())"

    $initialGet = Invoke-SafeHttp -Method POST -Uri "$script:P1C04BaseUrl/api/v1/axdata/get" `
        -Body ([ordered]@{ objectId = $objectId })
    if ($init.PSObject.Properties.Name -contains "version" -or
        $initialGet.PSObject.Properties.Name -contains "version") {
        throw "Init/Get disclosed an object version for case '$($Case.Name)'."
    }

    $resolvedSetData = $Case.SetData | ConvertTo-Json -Depth 100 | ConvertFrom-Json -Depth 100 -AsHashtable
    foreach ($dcEntry in $resolvedSetData.GetEnumerator()) {
        foreach ($row in @($dcEntry.Value.Rows)) {
            if (-not $row.ContainsKey("RowId") -or
                [string] $row.RowId -ne "__FIRST_EXISTING_ROW__") { continue }

            $container = @($initialGet.dataContainers | Where-Object { [string] $_.dcId -eq [string] $dcEntry.Key }) |
                Select-Object -First 1
            $existingRow = @($container.rows) | Select-Object -First 1
            if ($null -eq $existingRow -or [string]::IsNullOrWhiteSpace([string] $existingRow.rowId)) {
                throw "Case '$($Case.Name)' could not resolve the first existing row for DC '$($dcEntry.Key)'."
            }

            $row.RowId = [string] $existingRow.rowId
        }
    }

    $setBody = [ordered]@{
        objectId = $objectId
        getDeps = $true
        data = $resolvedSetData
    }
    $set = Invoke-SafeHttp -Method POST -Uri "$script:P1C04BaseUrl/api/v1/axdata/set" -Body $setBody
    if ($set.PSObject.Properties.Name -contains "version" -or
        $set.PSObject.Properties.Name -contains "currentVersion") {
        throw "Set disclosed object-version metadata for case '$($Case.Name)'."
    }
    $changedCount = @($set.changed).Count
    $errorCount = @($set.errors).Count
    $dependencyChangeCount = @($set.changed | Where-Object { [string] $_.source -eq "dependency" }).Count
    $fillGridChangeCount = @($set.changed | Where-Object { [string] $_.source -eq "fillGrid" }).Count
    $minimumDependencyChanges = if ($Case.ContainsKey("MinimumDependencyChanges")) {
        [int] $Case.MinimumDependencyChanges
    }
    else {
        1
    }
    if ([string] $set.status -ne "success" -or $errorCount -ne 0) {
        throw "Set field evaluation did not succeed for case '$($Case.Name)' (errors=$errorCount)."
    }
    if ($dependencyChangeCount -lt $minimumDependencyChanges) {
        throw "Set returned $dependencyChangeCount dependency change(s) for case '$($Case.Name)'; expected at least $minimumDependencyChanges."
    }
    $minimumFillGridChanges = if ($Case.ContainsKey("MinimumFillGridChanges")) {
        [int] $Case.MinimumFillGridChanges
    }
    else {
        0
    }
    if ($fillGridChangeCount -lt $minimumFillGridChanges) {
        throw "Set returned $fillGridChangeCount FillGrid change(s) for case '$($Case.Name)'; expected at least $minimumFillGridChanges."
    }
    $snapshotJson = Invoke-RedisCli -Arguments @("--raw", "HGET", $activeKey, "payload")
    $snapshot = $snapshotJson | ConvertFrom-Json -Depth 100
    if ($snapshot.PSObject.Properties.Name -contains "schemaVersion" -or
        $snapshot.PSObject.Properties.Name -contains "structDef" -or
        $snapshot.PSObject.Properties.Name -contains "version" -or
        $snapshot.PSObject.Properties.Name -contains "ownerUserName") {
        throw "Redis state contains a removed header, version, or owner field for case '$($Case.Name)'."
    }

    $deleted = [int] (Invoke-RedisCli -Arguments @("DEL", $activeKey))
    if ($deleted -ne 1) {
        throw "Expected to delete exactly one active-state key for case '$($Case.Name)'; deleted $deleted."
    }
    $null = Invoke-SafeHttp -Method POST -Uri "$script:P1C04BaseUrl/api/v1/axdata/get" `
        -Body ([ordered]@{ objectId = $objectId }) `
        -ExpectedStatus 404 -ExpectedErrorCode "AXD-NOTFOUND-001"

    return [pscustomobject]@{
        Name = [string] $Case.Name
        TransId = [string] $Case.TransId
        StatePath = if ($null -eq $Case.Key) { "new" } else { "AxGet" }
        Hash = $structDef.Hash
        UpdatedOn = $structDef.UpdatedOn
        RecordId = $init.recordId
        Changed = $changedCount
        DependencyChanges = $dependencyChangeCount
        FillGridChanges = $fillGridChangeCount
        Errors = $errorCount
        DefinedDependencies = $structDef.Dependencies
        RedisMiss = "404 AXD-NOTFOUND-001"
        CurrentDefinition = "loaded without persisted binding"
    }
}

$resolvedConfig = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ConfigPath)
if (-not (Test-Path -LiteralPath $resolvedConfig -PathType Leaf)) {
    throw "P1C-04 config was not found at '$resolvedConfig'. Copy the committed example first."
}

. $resolvedConfig

foreach ($name in @(
    "ASPNETCORE_ENVIRONMENT",
    "AxData__RealDataReview__Enabled",
    "AxData__RealDataReview__TenantId",
    "AxData__RealDataReview__UserName",
    "AxData__RealDataReview__Roles__0",
    "AxData__RealDataReview__SessionId",
    "AxData__RealDataReview__AxpertSessionId",
    "AxData__RealDataReview__DatabaseType",
    "AxData__RealDataReview__ConnectionString",
    "AxData__RealDataReview__AllowedHosts__0",
    "AxData__Redis__ConnectionString")) {
    $null = Get-EnvironmentValue $name
}

if ($env:ASPNETCORE_ENVIRONMENT -ne "Development") {
    throw "P1C-04 is restricted to ASPNETCORE_ENVIRONMENT=Development."
}
if ($env:AxData__RealDataReview__Enabled -ne "true") {
    throw "P1C-04 requires AxData RealDataReview mode."
}
Require-Value "P1C04BaseUrl" $P1C04BaseUrl
Require-Value "P1C04RedisCliPath" $P1C04RedisCliPath
Require-Value "P1C04RedisHost" $P1C04RedisHost
Require-Value "P1C04RedisPort" $P1C04RedisPort
if (-not (Get-Variable -Name P1C04Cases -Scope Script -ErrorAction SilentlyContinue)) {
    throw "Required P1C-04 setting 'P1C04Cases' is missing."
}
if ($P1C04Cases.Count -lt 2) { throw "Configure at least one AxGet case and one new-state case." }
if (@($P1C04Cases | Where-Object { $null -eq $_.Key }).Count -eq 0 -or
    @($P1C04Cases | Where-Object { $null -ne $_.Key }).Count -eq 0) {
    throw "P1C-04 cases must cover both AxGet and new-state initialization."
}
if (@($P1C04Cases | Where-Object {
    $_.ContainsKey("MinimumFillGridChanges") -and [int] $_.MinimumFillGridChanges -gt 0
}).Count -eq 0) {
    throw "P1C-05 requires at least one walkthrough case to assert FillGrid changes."
}
if (-not (Get-Variable -Name P1C05FillGridDefinitions -Scope Script -ErrorAction SilentlyContinue)) {
    throw "Required P1C-05 setting 'P1C05FillGridDefinitions' is missing."
}
$configuredAddRows = @($P1C05FillGridDefinitions | ForEach-Object { [int] $_.AddRows } | Sort-Object -Unique)
if ($configuredAddRows.Count -ne 3 -or $configuredAddRows[0] -ne 1 -or
    $configuredAddRows[1] -ne 2 -or $configuredAddRows[2] -ne 3) {
    throw "P1C-05 FillGrid definitions must cover AddRows 1, 2, and 3."
}

$script:P1C04BaseUrl = ([string] $P1C04BaseUrl).TrimEnd('/')
$script:P1C04RedisCliPath = [string] $P1C04RedisCliPath
$script:P1C04RedisHost = [string] $P1C04RedisHost
$script:P1C04RedisPort = [int] $P1C04RedisPort
$startupTimeout = Get-Variable -Name P1C04StartupTimeoutSeconds -Scope Script -ValueOnly -ErrorAction SilentlyContinue
$script:P1C04StartupTimeoutSeconds = if ($null -ne $startupTimeout) { [int] $startupTimeout } else { 45 }
# The non-admin Windows review runner uses the structured console sink. Prevent the
# default Event Log provider from throwing and truncating otherwise valid HTTP responses.
$env:Logging__EventLog__LogLevel__Default = "None"
$axDataRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$script:ContractTestProject = Join-Path $axDataRoot "tests/AxData.ContractTests/AxData.ContractTests.csproj"
$apiProject = Join-Path $axDataRoot "src/AxData.Api/AxData.Api.csproj"
$resolvedReport = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ReportPath)
$fillGridCharacterizations = @($P1C05FillGridDefinitions | ForEach-Object {
    Get-FillGridReview -Definition $_
})
if (@($fillGridCharacterizations | Where-Object { $_.Parameters -gt 0 }).Count -eq 0) {
    throw "P1C-05 requires at least one real parameterized FillGrid definition."
}
if (@($fillGridCharacterizations | Where-Object { $_.DuplicateParameters -gt 0 }).Count -eq 0) {
    throw "P1C-05 requires at least one real FillGrid definition with a duplicate named parameter occurrence."
}

$serverInfo = Invoke-RedisCli -Arguments @("INFO", "server")
$redisVersionMatch = [regex]::Match($serverInfo, '(?m)^redis_version:(?<version>[^\r\n]+)')
$memuraiVersionMatch = [regex]::Match($serverInfo, '(?m)^memurai_version:(?<version>[^\r\n]+)')
if (-not $redisVersionMatch.Success) {
    throw "P1C-04 could not determine the Redis server version."
}
$redisVersion = $redisVersionMatch.Groups['version'].Value.Trim()
$memuraiVersion = if ($memuraiVersionMatch.Success) { $memuraiVersionMatch.Groups['version'].Value.Trim() } else { "not reported" }

$runDirectory = Join-Path ([IO.Path]::GetTempPath()) ("axdata-p1c04-" + [guid]::NewGuid().ToString("N"))
$null = New-Item -ItemType Directory -Path $runDirectory
$apiOut = Join-Path $runDirectory "api.out.log"
$apiErr = Join-Path $runDirectory "api.err.log"
$api = $null
$succeeded = $false

try {
    $baseUrlAlreadyInUse = $false
    try {
        $existingResponse = Invoke-WebRequest -Uri "$script:P1C04BaseUrl/health" -SkipHttpErrorCheck -ConnectionTimeoutSeconds 1
        $baseUrlAlreadyInUse = $null -ne $existingResponse
    }
    catch {
        $baseUrlAlreadyInUse = $false
    }
    if ($baseUrlAlreadyInUse) {
        throw "The configured API base URL is already serving a response. Stop that process or select another local URL."
    }

    $api = Start-Process dotnet -ArgumentList @("run", "--no-restore", "--no-build", "--no-launch-profile", "--project", $apiProject, "--urls", $script:P1C04BaseUrl) `
        -WorkingDirectory $axDataRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $apiOut -RedirectStandardError $apiErr

    Wait-ForApi -Process $api
    $results = foreach ($case in $P1C04Cases) {
        Invoke-WalkthroughCase -Case $case
    }

    $reportDirectory = Split-Path -Parent $resolvedReport
    $null = New-Item -ItemType Directory -Path $reportDirectory -Force
    $report = @(
        "# P1C-05 AxData-native FillGrid and P1C-04 HTTP walkthrough"
        ""
        "Generated: $([DateTimeOffset]::UtcNow.ToString('u'))"
        ""
        "Redis version: $(ConvertTo-SafeCell $redisVersion); Memurai version: $(ConvertTo-SafeCell $memuraiVersion)."
        ""
        "## FillGrid characterization"
        ""
        "| Tenant | TransId | Name | AddRows | Mode | Auto show | Firm bind | Multiselect | Parameters | Duplicate parameters | Mappings | SQL hash | Schema verified |"
        "|---|---|---|---:|---|---|---|---|---:|---:|---:|---|---|"
    )
    foreach ($item in $fillGridCharacterizations) {
        $report += "| $(ConvertTo-SafeCell $item.Tenant) | $(ConvertTo-SafeCell $item.TransId) | $(ConvertTo-SafeCell $item.Name) | $($item.AddRows) | $($item.Mode) | $($item.AutoShow) | $($item.FirmBind) | $($item.MultiSelect) | $($item.Parameters) | $($item.DuplicateParameters) | $($item.Mappings) | $($item.SqlHash) | true |"
    }
    $report += @(
        ""
        "## HTTP walkthrough"
        ""
        "| Case | TransId | State path | StructDef hash | updatedon | RecordId | Changed | Dependency changes | FillGrid changes | Errors | Defined dependencies | Redis miss | Current definition |"
        "|---|---|---|---|---|---:|---:|---:|---:|---:|---:|---|---|"
    )
    foreach ($result in $results) {
        $report += "| $(ConvertTo-SafeCell $result.Name) | $(ConvertTo-SafeCell $result.TransId) | $($result.StatePath) | $($result.Hash) | $(ConvertTo-SafeCell $result.UpdatedOn) | $(ConvertTo-SafeCell $result.RecordId) | $($result.Changed) | $($result.DependencyChanges) | $($result.FillGridChanges) | $($result.Errors) | $($result.DefinedDependencies) | $($result.RedisMiss) | $($result.CurrentDefinition) |"
    }
    $report += @(
        ""
        "Result: PASS. Real AddRows 1/2/3 metadata, multiple applicable definitions, duplicate-parameter normalization, parameterized PostgreSQL FillGrid execution, tenant-schema isolation, trusted identity, authorization, current StructDef, AxGet/new state, Init, Get, Set, field evaluation, dependencies, version-free positional Redis state, exact-key deletion, and the documented Redis-miss response completed."
        ""
        "No field values, key values, credentials, connection strings, or tokens are included in this report."
    )
    Set-Content -LiteralPath $resolvedReport -Value $report -Encoding utf8
    $succeeded = $true
    Write-Host "P1C-05 PASS. Safe report: $resolvedReport"
}
finally {
    foreach ($process in @($api)) {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -ErrorAction SilentlyContinue
            $process.WaitForExit(5000) | Out-Null
        }
    }

    if ($succeeded) {
        $resolvedRunDirectory = [IO.Path]::GetFullPath($runDirectory)
        $resolvedTempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
        if ($resolvedRunDirectory.StartsWith($resolvedTempRoot, [StringComparison]::OrdinalIgnoreCase) -and
            (Split-Path -Leaf $resolvedRunDirectory).StartsWith("axdata-p1c04-", [StringComparison]::OrdinalIgnoreCase)) {
            Remove-Item -LiteralPath $resolvedRunDirectory -Recurse -Force
        }
    }
    else {
        Write-Warning "P1C-04 failed. Process logs were retained at '$runDirectory'; inspect them locally because they may contain environment-specific data."
    }
}
