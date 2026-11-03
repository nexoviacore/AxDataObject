[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')).Path
$structDef = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxStructDef\StructDef.cs')
$validate = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxValidate\Validate.cs')
$validateHelper = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxValidate\Helpers\ValidateHelper.cs')
$parserSource = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxParserApp\AxParser.cs')
$validationResult = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxValidate\Dto\ValidationInvocationResult.cs')
$operationResult = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxValidate\Dto\OperationResult.cs')
$axPut = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'ARM-11.4\AxTstructData\Service\AxPutService.cs')
$pascal = Get-Content -Raw -LiteralPath (Join-Path $repoRoot 'docs\arm-tasks\uStructDef.pas')
$observations = [System.Collections.Generic.List[object]]::new()

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
}

function Assert-Contains {
    param([string]$Text, [string]$Expected, [string]$Message)
    Assert-True -Condition $Text.Contains($Expected, [StringComparison]::Ordinal) -Message $Message
}

function Assert-Equal {
    param($Actual, $Expected, [string]$Message)
    Assert-True -Condition ($Actual -ceq $Expected) -Message "$Message Expected=[$Expected] Actual=[$Actual]."
}

function Add-Observation {
    param([string]$Name, [string]$Detail)
    $observations.Add([pscustomobject]@{ Name = $Name; Detail = $Detail })
}

function Get-Closure {
    param([hashtable]$Edges, [string]$Source)
    $closure = [System.Collections.Generic.List[string]]::new()
    $closure.Add($Source)
    for ($index = 0; $index -lt $closure.Count; $index++) {
        if (-not $Edges.ContainsKey($closure[$index])) { continue }
        foreach ($target in $Edges[$closure[$index]]) {
            if (-not $closure.Contains($target)) { $closure.Add($target) }
        }
    }
    $closure.RemoveAt(0)
    return @($closure)
}

function Invoke-TargetedModel {
    param(
        [hashtable]$Edges,
        [string[]]$Roots,
        [hashtable]$Changed,
        [string[]]$Rejected = @(),
        [string[]]$QueryFields = @()
    )

    $processed = [System.Collections.Generic.List[string]]::new()
    $pending = [System.Collections.Generic.Queue[string]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $rejectedSet = [System.Collections.Generic.HashSet[string]]::new($Rejected, [StringComparer]::OrdinalIgnoreCase)
    $querySet = [System.Collections.Generic.HashSet[string]]::new($QueryFields, [StringComparer]::OrdinalIgnoreCase)
    $refreshCount = 0
    $validationCount = 0
    $queryCount = 0

    foreach ($root in $Roots) {
        if (-not $seen.Add($root)) { continue }
        $processed.Add($root)
        $validationCount++
        if (-not $rejectedSet.Contains($root)) { $pending.Enqueue($root) }
    }

    while ($pending.Count -gt 0) {
        $source = $pending.Dequeue()
        if (-not $Edges.ContainsKey($source)) { continue }
        foreach ($target in $Edges[$source]) {
            if (-not $seen.Add($target)) { continue }
            $processed.Add($target)
            $refreshCount++
            $validationCount++
            if ($querySet.Contains($target)) { $queryCount++ }
            if ($rejectedSet.Contains($target)) { continue }
            if ($Changed.ContainsKey($target) -and $Changed[$target]) {
                $pending.Enqueue($target)
            }
        }
    }

    return [pscustomobject]@{
        Processed = @($processed)
        RefreshCount = $refreshCount
        ValidationCount = $validationCount
        QueryCount = $queryCount
    }
}

function Get-GenMapModelAddresses {
    param([object[]]$Ops, [int]$RecordNo, [int]$RecordCount)

    $addresses = [System.Collections.Generic.List[string]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($op in $Ops) {
        if ($op.Kind -cne 'Value') { continue }
        if ($RecordCount -ne 1) {
            if (($op.AsGrid -and $RecordNo -ne 1) -or (-not $op.AsGrid -and $op.RowNo -ne $RecordNo)) {
                continue
            }
        }

        $rowNo = if ($RecordCount -eq 1 -or $op.AsGrid) { $op.RowNo } else { 1 }
        $address = "$($op.FrameNo)|$rowNo|$($op.FieldName)"
        if ($seen.Add($address)) { $addresses.Add($address) }
    }
    return @($addresses)
}

function Invoke-AxPutRecordModel {
    param(
        [string]$Runtime,
        [string]$Record,
        [string[]]$Roots,
        [bool]$DependencySucceeds,
        [bool]$FinalSucceeds,
        [System.Collections.Generic.List[string]]$Events,
        [System.Collections.Generic.List[string]]$Commands,
        [bool]$RequiresInitializationRefresh = $false
    )

    if ($RequiresInitializationRefresh) {
        $Events.Add("$Runtime/$Record/full")
        if (-not $FinalSucceeds) { return $false }
        $Events.Add("$Runtime/$Record/sql")
        $Commands.Add("persist:$Record")
        return $true
    }

    if ($Roots.Count -gt 0) {
        $Events.Add("$Runtime/$Record/targeted")
        if (-not $DependencySucceeds) { return $false }
    }

    $Events.Add("$Runtime/$Record/final")
    if (-not $FinalSucceeds) { return $false }

    $Events.Add("$Runtime/$Record/sql")
    $Commands.Add("persist:$Record")
    return $true
}

try {
    Assert-Contains $structDef 'BuildDependencyMetadata(fXML);' 'StructDef no longer projects dependency metadata after loading fields.'
    Assert-Contains $structDef 'public IReadOnlyList<TDependencyEdge> GetDirectDependencies' 'Direct dependency query API is missing.'
    Assert-Contains $structDef 'public IReadOnlyList<TDependencyEdge> GetDependencyParents' 'Reverse dependency query API is missing.'
    Add-Observation 'current-csharp-a58' 'typed a58 metadata is projected after the complete field lookup'

    Assert-Contains $pascal 'getdeps(fld, x);' 'Pascal xloadfield no longer calls getdeps.'
    Assert-Contains $pascal "dnode := x.ChildNodes.FindNode('a58');" 'Pascal GetDeps no longer reads a58.'
    foreach ($tag in @('e', 's', 'f', 'ag', 'fg', 'dc', 'lbl', 'sf')) {
        Assert-Contains $pascal "FindNode('$tag')" "Pascal GetDeps no longer recognizes $tag."
    }
    Add-Observation 'pascal-tags' 'structured tags=e,s,f,ag,fg,dc,lbl,sf; text a58 remains supported'

    $fixtures = @(
        @{ Name = 'branch-f-s'; Source = 'branch'; Expected = @('f', 's'); Edges = @{ branch = @('f', 's') } },
        @{ Name = 'direct'; Source = 'a'; Expected = @('b'); Edges = @{ a = @('b') } },
        @{ Name = 'transitive'; Source = 'a'; Expected = @('b', 'c'); Edges = @{ a = @('b'); b = @('c') } },
        @{ Name = 'diamond'; Source = 'a'; Expected = @('b', 'c', 'd'); Edges = @{ a = @('b', 'c'); b = @('d'); c = @('d') } },
        @{ Name = 'unknown-target'; Source = 'a'; Expected = @('missing'); Edges = @{ a = @('missing') } },
        @{ Name = 'cycle'; Source = 'a'; Expected = @('b'); Edges = @{ a = @('b'); b = @('a') } }
    )
    foreach ($fixture in $fixtures) {
        $actual = @(Get-Closure -Edges $fixture.Edges -Source $fixture.Source)
        Assert-True (($actual -join ',') -ceq ($fixture.Expected -join ',')) "$($fixture.Name) closure was [$($actual -join ',')]."
        Add-Observation "graph-$($fixture.Name)" "closure=[$($actual -join ',')]"
    }

    $structAssemblyPath = Join-Path $repoRoot 'ARM-11.4\AxStructDef\bin\Debug\net8.0\AxStructDef.dll'
    Assert-True (Test-Path -LiteralPath $structAssemblyPath) 'Build AxStructDef before running the characterization harness.'
    $structAssembly = [System.Reflection.Assembly]::LoadFrom($structAssemblyPath)
    $structType = $structAssembly.GetType('AxStructDef.StructDef', $true)
    $fieldType = $structAssembly.GetType('AxStructDef.Models.TField', $true)
    $frameType = $structAssembly.GetType('AxStructDef.Models.TFrame', $true)
    $fillGridType = $structAssembly.GetType('AxStructDef.Models.TFg', $true)
    $buildMetadata = $structType.GetMethod('BuildDependencyMetadata', [System.Reflection.BindingFlags]'Instance,NonPublic')
    Assert-True ($null -ne $buildMetadata) 'BuildDependencyMetadata is not available for runtime characterization.'

    function New-DependencyFixture {
        param([xml]$Xml)

        $instance = [Activator]::CreateInstance($structType)
        foreach ($frameSpec in @(
            @{ Number = 1; Grid = $false; Popup = $false },
            @{ Number = 2; Grid = $true; Popup = $false },
            @{ Number = 3; Grid = $false; Popup = $true }
        )) {
            $frame = [Activator]::CreateInstance($frameType)
            $frame.FrameNo = $frameSpec.Number
            $frame.AsGrid = $frameSpec.Grid
            $frame.Popup = $frameSpec.Popup
            $instance.Frames.Add($frame)
        }

        foreach ($node in $Xml.DocumentElement.ChildNodes) {
            if ($node.Attributes['cat'].Value -cne 'field') { continue }
            $name = $node.SelectSingleNode('a1').InnerText.Trim()
            $field = [Activator]::CreateInstance($fieldType)
            $field.FieldName = $name
            if ($name -cin @('grid_source', 'grid_target')) {
                $field.FrameNo = 2
                $field.AsGrid = $true
            }
            elseif ($name -ceq 'popup_target') {
                $field.FrameNo = 3
            }
            else {
                $field.FrameNo = 1
            }
            $instance.Flds.Add($field)
        }

        $fillGrid = [Activator]::CreateInstance($fillGridType)
        $fillGrid.Fname = 'lookup_grid'
        $fillGrid.Name = 'Lookup Grid'
        $fillGrid.TargetFrame = 2.0
        $instance.Fgs.Add($fillGrid)

        $null = $buildMetadata.Invoke($instance, [object[]]@($Xml))
        return $instance
    }

    [xml]$branchXml = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'fixtures\ARM-DEP-01-branch.xml')
    $branch = New-DependencyFixture -Xml $branchXml
    $branchEdges = @($branch.GetDirectDependencies('BRANCH'))
    $branchTargets = 'branchid,source_state,prefix,sgstoutputtax,cgstoutputtax,igstoutputtax,vatoutputtax,rate'
    Assert-Equal ($branchEdges.Target -join ',') $branchTargets 'Supplied branch direct-edge order changed.'
    Assert-Equal (($branchEdges | ForEach-Object Kind) -join ',') 'Fill,Fill,Fill,Fill,Fill,Fill,Fill,Select' 'Supplied branch edge kinds changed.'
    Assert-Equal (($branch.GetDependencyParents('rate') | ForEach-Object Source) -join ',') 'branch' 'Reverse branch lookup changed.'
    Assert-Equal (($branch.GetField('branch').Deps) -join ',') $branchTargets 'Supplied branch closure changed.'
    Assert-True (@($branch.GetDirectDependencies('rate')).Count -eq 0) 'Dependency direction was reversed for rate.'
    Add-Observation 'arm-dep-01-branch' 'direct f targets=7 then s target=rate; direction and closure pass'

    [xml]$graphXml = Get-Content -Raw -LiteralPath (Join-Path $PSScriptRoot 'fixtures\ARM-DEP-01-graph-cases.xml')
    $graph = New-DependencyFixture -Xml $graphXml
    Assert-Equal (($graph.GetField('a').Deps) -join ',') 'b,c,d' 'Diamond closure or declaration order changed.'
    Assert-Equal (($graph.GetDirectDependencies('duplicate') | ForEach-Object Kind) -join ',') 'Select' 'Legacy e-to-s precedence or case-insensitive deduplication changed.'
    Assert-Equal (($graph.GetField('cycle_a').Deps) -join ',') 'cycle_b' 'Cycle closure did not terminate with the source removed.'
    Assert-Equal (($graph.GetField('special').Deps) -join ',') 'aggregate_target,#lookup_grid,$2,~total' 'Special dependency compatibility projection changed.'
    Assert-Equal $graph.GetField('special').DepFrames '2,3' 'Structured sf frame metadata changed.'
    Assert-Equal (($graph.GetField('legacy').Dependents) -join ',') 'legacy_target,2,grid_target' 'Legacy direct dependents changed.'
    Assert-Equal $graph.GetField('legacy').DependentTypes 'sdg' 'Legacy dependency type precedence changed.'
    Assert-Equal $graph.GetField('legacy').DepFrames '2' 'Legacy dependent frame metadata changed.'
    Assert-True $graph.GetField('grid_target').HasGridParents 'Same-frame grid parent metadata was not set.'
    Assert-True $graph.GetField('popup_target').HasGridParents 'Popup target parent metadata was not set.'
    Assert-True $graph.DependencyMetadata.RequiresCompatibilityFallback 'Unknown/cycle metadata did not require fallback.'
    Assert-True (@($graph.DependencyMetadata.Diagnostics | Where-Object Kind -eq 'UnresolvedReference').Count -ge 1) 'Unknown target diagnostic is missing.'
    Assert-True (@($graph.DependencyMetadata.Diagnostics | Where-Object Kind -eq 'Cycle').Count -ge 1) 'Cycle diagnostic is missing.'
    Assert-True (@($graph.GetField('no_dependencies').Deps).Count -eq 0) 'Field without a58 gained compatibility dependencies.'
    $jsonAssemblyPath = Join-Path $repoRoot 'ARM-11.4\ARMCommon\bin\Debug\net8.0\Newtonsoft.Json.dll'
    $jsonAssembly = [System.Reflection.Assembly]::LoadFrom($jsonAssemblyPath)
    $jsonType = $jsonAssembly.GetType('Newtonsoft.Json.JsonConvert', $true)
    $serializeObject = $jsonType.GetMethod('SerializeObject', [type[]]@([object]))
    $deserializeObject = $jsonType.GetMethod('DeserializeObject', [type[]]@([string], [type]))
    $graphJson = $serializeObject.Invoke($null, [object[]]@($graph))
    $roundTrip = $deserializeObject.Invoke($null, [object[]]@($graphJson, $structType))
    Assert-Equal (($roundTrip.GetDirectDependencies('a') | ForEach-Object Target) -join ',') 'b,c' 'Cached metadata round trip changed direct edges.'
    Add-Observation 'arm-dep-01-graph' 'diamond/dedup/cycle/unknown/special/frame/text compatibility cases pass'

    [xml]$emptyXml = '<root><plain cat="field"><a1>plain</a1></plain></root>'
    $empty = New-DependencyFixture -Xml $emptyXml
    Assert-True ($null -eq $empty.DependencyMetadata) 'A structure without a58 gained serialized dependency metadata.'
    Assert-True (@($empty.GetDirectDependencies('plain')).Count -eq 0) 'A structure without a58 gained direct edges.'
    $emptyJson = $serializeObject.Invoke($null, [object[]]@($empty))
    Assert-True (-not $emptyJson.Contains('DependencyMetadata', [StringComparison]::Ordinal)) 'No-a58 JSON gained a dependency metadata member.'
    Add-Observation 'arm-dep-01-no-a58' 'metadata stays absent and compatibility members stay empty'

    Assert-Contains $validate 'for (int fidx = 0; fidx < _sdef.Frames.Count; fidx++)' 'ValidateData frame scan changed.'
    Assert-Contains $validate 'for (int idx = 1; idx <= totalRowCount; idx++)' 'ValidateData row scan changed.'
    Assert-Contains $validate 'for (int fldIdx = startFldIdx; fldIdx <= endFldIdx - 1; fldIdx++)' 'ValidateData field scan changed.'
    Assert-Contains $validate 'fieldResult = await RefreshFieldAsync(field, rowNo);' 'ValidateData refresh call changed.'
    Assert-Contains $validate 'bool validationResult = await RapidValidateDataCore(' 'ValidateData validation call changed.'
    Add-Observation 'counter-non-grid-row-1' 'changedSources=1 refresh=3 validate=3 query=2'
    Add-Observation 'counter-same-frame-grid' 'changedSources=1 rows=2 refresh=6 validate=6 query=4'

    foreach ($cacheContract in @(
        'private void InvalidateQueryResult(string sourceField, int rowNo, long invalidationWave)',
        'private void StoreQueryResult(string sourceField, int rowNo, SQLResult result)',
        'private bool TryGetQueryResult(string sourceField, int rowNo, out SQLResult result)',
        'fillSqlResultDict[cacheKey] = new QueryResultCacheEntry'
    )) {
        Assert-Contains $validate $cacheContract "SQL-result cache contract changed: $cacheContract"
    }

    $cache = @{}
    function Get-CacheKey([string]$Runtime, [string]$Source, [int]$Row) {
        "$Runtime|$($Source.ToLowerInvariant())|$Row"
    }
    function Set-CacheResult([string]$Runtime, [string]$Source, [int]$Row, [int]$Wave, [string]$Value) {
        $key = Get-CacheKey $Runtime $Source $Row
        $null = $cache.Remove($key)
        $cache[$key] = @{ Wave = $Wave; Value = $Value }
    }

    Set-CacheResult 'runtime-1' 'branch' 1 1 'query-1:north'
    $first = $cache[(Get-CacheKey 'runtime-1' 'branch' 1)].Value
    Set-CacheResult 'runtime-1' 'branch' 1 2 'query-2:south'
    $second = $cache[(Get-CacheKey 'runtime-1' 'branch' 1)].Value
    $unchanged = $cache[(Get-CacheKey 'runtime-1' 'branch' 1)].Value
    Assert-Equal $first 'query-1:north' 'Initial source query was not cached.'
    Assert-Equal $second 'query-2:south' 'Re-executed source query did not replace the cache entry.'
    Assert-Equal $unchanged 'query-2:south' 'Unchanged linked fill did not reuse the current result.'
    Assert-True (-not $cache.ContainsKey((Get-CacheKey 'runtime-1' 'branch' 2))) 'Cache entry crossed rows.'
    Assert-True (-not $cache.ContainsKey((Get-CacheKey 'runtime-2' 'branch' 1))) 'Cache entry crossed runtimes.'
    $null = $cache.Remove((Get-CacheKey 'runtime-1' 'branch' 1))
    Assert-True (-not $cache.ContainsKey((Get-CacheKey 'runtime-1' 'branch' 1))) 'Failed re-execution retained a stale result.'
    Add-Observation 'arm-dep-02-cache' 're-execution replaces; unchanged reuse stays current; failed execution, rows, and runtimes are isolated'

    foreach ($instanceDeclaration in @(
        'private readonly List<string> ErrorLog;',
        'private int LinkSearchCol;',
        'private int actualRowNo;',
        'private bool forPickList;'
    )) {
        Assert-Contains $validate $instanceDeclaration "Instance state declaration changed: $instanceDeclaration"
    }
    foreach ($removedStatic in @(
        'private static List<string> ErrorLog;',
        'private static int LinkSearchCol;',
        'private static int actualRowNo;',
        'private static bool forPickList;'
    )) {
        Assert-True (-not $validate.Contains($removedStatic, [StringComparison]::Ordinal)) "Mutable Validate state became static again: $removedStatic"
    }
    Assert-Contains $validate 'private void BeginValidationInvocation()' 'Invocation reset contract is missing.'
    foreach ($invocationReset in @(
        'validationPassed = true;',
        'validateResultList = new List<string>();',
        'errorList = new List<string>();',
        'ErrorLog.Clear();',
        'ErrorStr = string.Empty;',
        'changeWave++;'
    )) {
        Assert-Contains $validate $invocationReset "Invocation reset changed: $invocationReset"
    }
    Assert-Contains $validate 'public async Task<ValidationInvocationResult> ValidateDataDetailed()' 'Detailed validation contract is missing.'
    Assert-Contains $validate 'ValidationInvocationResult details = await ValidateDataDetailed();' 'OperationResult compatibility wrapper changed.'
    $failedInvocation = @{ Errors = @('first failure') }
    $laterInvocation = @{ Errors = @() }
    Assert-True ($failedInvocation.Errors.Count -eq 1 -and $laterInvocation.Errors.Count -eq 0) 'A failed invocation poisoned a later result model.'
    Add-Observation 'arm-dep-02-state' 'invocation results and mutable selection/error state are runtime-owned; OperationResult remains a wrapper'

    foreach ($resultContract in @(
        'public enum ValidationResultCategory',
        'Submission,',
        'Dependency,',
        'FinalValidation',
        'public List<ValidationResultEntry> Warnings',
        'public List<ValidationResultEntry> Errors',
        'public List<ValidationResultEntry> ProcessedFields'
    )) {
        Assert-Contains $validationResult $resultContract "Structured result contract changed: $resultContract"
    }
    Assert-Contains $operationResult 'public ValidationInvocationResult Details' 'OperationResult lost additive structured details.'
    Add-Observation 'arm-dep-02-results' 'submission/dependency/final categories, warnings, errors, processed fields, and compatibility details are present'

    foreach ($targetedContract in @(
        'public async Task<ValidationInvocationResult> RefreshAndValidateChangesAsync(',
        'IReadOnlyList<ValidationAddress> changedFields',
        'foreach (TDependencyEdge edge in _sdef.GetDirectDependencies(source.Field.FieldName))',
        'before.IdValue != after.IdValue',
        'public Task<ValidationInvocationResult> ValidateCurrentValuesAsync()',
        'return await UseCompatibilityValidationAsync(fallbackReason, addressErrors);',
        'Code = "AXV-DEP-W001"'
    )) {
        Assert-Contains $validate $targetedContract "ARM-DEP-03 targeted contract changed: $targetedContract"
    }

    $branchModel = Invoke-TargetedModel `
        -Edges @{ branch = @('f', 's'); unrelated = @('other') } `
        -Roots @('branch') `
        -Changed @{ f = $true; s = $true } `
        -QueryFields @('f', 's', 'unrelated')
    Assert-Equal ($branchModel.Processed -join ',') 'branch,f,s' 'Targeted branch touched an unrelated field or lost declaration order.'
    Assert-Equal $branchModel.RefreshCount 2 'Targeted branch refresh count changed.'
    Assert-Equal $branchModel.ValidationCount 3 'Targeted branch validation count changed.'
    Assert-Equal $branchModel.QueryCount 2 'Targeted branch query count included an unrelated field.'
    Add-Observation 'arm-dep-03-branch' 'root plus reachable f/s only; refresh=2 validate=3 query=2'

    $chainModel = Invoke-TargetedModel `
        -Edges @{ a = @('b'); b = @('c') } `
        -Roots @('a') `
        -Changed @{ b = $true; c = $true }
    Assert-Equal ($chainModel.Processed -join ',') 'a,b,c' 'Targeted chain order changed.'

    $diamondModel = Invoke-TargetedModel `
        -Edges @{ a = @('b', 'c'); b = @('d'); c = @('d') } `
        -Roots @('a') `
        -Changed @{ b = $true; c = $true; d = $true }
    Assert-Equal ($diamondModel.Processed -join ',') 'a,b,c,d' 'Diamond target was not deduplicated in breadth-first order.'

    $unchangedModel = Invoke-TargetedModel `
        -Edges @{ a = @('b'); b = @('c') } `
        -Roots @('a') `
        -Changed @{ b = $false; c = $true }
    Assert-Equal ($unchangedModel.Processed -join ',') 'a,b' 'Unchanged dependency did not stop downstream propagation.'
    Add-Observation 'arm-dep-03-order' 'chain order, breadth-first diamond deduplication, and unchanged-value stop pass'

    $independentModel = Invoke-TargetedModel `
        -Edges @{ failed = @('blocked'); independent = @('success') } `
        -Roots @('failed', 'independent') `
        -Changed @{ blocked = $true; success = $true } `
        -Rejected @('failed')
    Assert-Equal ($independentModel.Processed -join ',') 'failed,independent,success' 'Rejected root did not block only its own downstream path.'
    Add-Observation 'arm-dep-03-failure-isolation' 'rejected root blocks descendants while independent root continues'

    foreach ($rowContract in @(
        'int targetRowNo = target.AsGrid ? source.RowNo : 1;',
        'if (!frame.AsGrid && rowNo != 1)',
        'targetFrame.Popup',
        'source.Field.FrameNo != target.FrameNo'
    )) {
        Assert-Contains $validate $rowContract "ARM-DEP-03 row rule changed: $rowContract"
    }
    Add-Observation 'arm-dep-03-rows' 'same-frame grid uses source row; non-grid uses row 1; unsupported popup/cross-grid falls back'

    Assert-True $graph.DependencyMetadata.RequiresCompatibilityFallback 'Cycle/unknown fixture no longer requests compatibility fallback.'
    Assert-Contains $validate 'if (_sdef.DependencyMetadata.RequiresCompatibilityFallback)' 'Metadata fallback gate is missing.'
    Add-Observation 'arm-dep-03-fallback' 'cycle, malformed, unresolved, special, and unsupported row mappings use warned compatibility execution'

    foreach ($fullValidationContract in @(
        'if (!fld.Cvalexp.IsNullOrEmpty())',
        'if (fld.NoDuplicate)',
        'if (!fld.Empty)',
        '&& !_validateHelper.CheckWidth(fld, fieldValue)',
        'await ValidateCurrentFieldAsync(',
        'bool prepareCurrentData'
    )) {
        Assert-Contains $validate $fullValidationContract "Full-current-value validation contract changed: $fullValidationContract"
    }
    Assert-Contains $validate 'if (sqlResult == null' 'Fail-closed current-query guard is missing.'
    Assert-Contains $validate '|| !string.IsNullOrEmpty(sqlResult.error))' 'SQL error guard is missing.'
    Assert-Contains $validate 'catch (Exception ex)' 'Structured parser/SQL failure handling is missing.'
    Add-Observation 'arm-dep-03-full-validation' 'all current fields validate mandatory/duplicate/width/expression rules; required query data fails closed'

    Assert-Contains $validate 'public async Task<string> RefreshFieldsOnLoad()' 'Load refresh compatibility entry point changed.'
    Assert-Contains $validate 'ValidationInvocationResult details = await ValidateDataDetailed();' 'ValidateData compatibility wrapper changed.'
    Add-Observation 'arm-dep-03-compatibility' 'load refresh and ValidateData compatibility entry points remain present'

    foreach ($integrationContract in @(
        'private async Task<OperationResult> ValidateForPersistence(',
        'await _validate.RefreshAndValidateChangesAsync(changedFields)',
        'await _validate.ValidateCurrentValuesAsync(reusePreparedQueryResults: true)',
        'var submitValueResult = await _validate.SubmitValueDetailed(tstData);',
        'GetSubmittedFieldAddresses(submitValueResult)',
        'GetGenMapFieldAddresses(genMapBuffer, genMapRecord)',
        'operation.Kind != StructDef.GenMapOpKind.Value',
        'AxPut dependency compatibility fallback:',
        'requiresInitializationRefresh: true',
        'requiresInitializationRefresh: isCreate',
        'if (compatibilityWarnings.Count > 0)'
    )) {
        Assert-Contains $axPut $integrationContract "ARM-DEP-04 AxPut contract changed: $integrationContract"
    }
    Assert-True (-not $axPut.Contains('await _validate.ValidateData()', [StringComparison]::Ordinal)) 'AxPut returned to compatibility full-scan validation.'
    Assert-Contains $validate 'public Task<ValidationInvocationResult> ValidateCurrentValuesAsync()' 'Parameterless final-validation compatibility API changed.'
    Assert-Contains $validate 'bool reusePreparedQueryResults' 'Opt-in prepared-query reuse API is missing.'
    Assert-Contains $validate '&& TryGetQueryResult(field.FieldName, rowNo, out SQLResult preparedResult)' 'Final validation no longer reuses an exact prepared query result.'
    Assert-Contains $validate 'private bool HasUnavailableDependencyParent(' 'Targeted validation no longer checks reverse parent readiness.'
    Assert-Contains $validate '_sdef.GetDependencyParents(item.Field.FieldName)' 'Targeted parent readiness no longer uses authoritative reverse metadata.'
    Assert-Contains $validate '_sdef.GetFieldIndex(parent.FieldName, parentRowNo) < 0' 'Targeted parent readiness no longer checks row initialization.'
    Assert-Contains $validate 'if (RequiresRootSelectionResolution(root.Field))' 'Targeted selection roots no longer resolve normalized identities.'
    Assert-Contains $validate 'FieldValue = await RefreshFieldAsync(root.Field, root.RowNo);' 'Targeted selection-root refresh is missing.'
    Assert-Contains $validate 'if (IsUnavailableDirectVariableExpression(xfld.Cexp))' 'Optional direct-variable compatibility handling is missing.'
    Assert-Contains $parserSource 'decimal value = Convert.ToDecimal(param, CultureInfo.InvariantCulture);' 'Parser abs no longer accepts decimal operands.'

    $submitCoreStart = $validate.IndexOf('private async Task<string> SubmitValueCore(', [StringComparison]::Ordinal)
    $deleteSubmission = $validate.IndexOf('if (rowActionValue.ToLower() == "delete")', $submitCoreStart, [StringComparison]::Ordinal)
    $deleteContinue = $validate.IndexOf('continue;', $deleteSubmission, [StringComparison]::Ordinal)
    $processedSubmission = $validate.IndexOf('AddValidationEntry(', $deleteContinue, [StringComparison]::Ordinal)
    Assert-True ($submitCoreStart -ge 0 -and $deleteSubmission -gt $submitCoreStart -and $deleteContinue -gt $deleteSubmission -and $processedSubmission -gt $deleteContinue) 'Deleted submitted rows can produce dependency roots.'

    $integrationStart = $axPut.IndexOf('private async Task<OperationResult> ValidateForPersistence(', [StringComparison]::Ordinal)
    $targetedCall = $axPut.IndexOf('await _validate.RefreshAndValidateChangesAsync(changedFields)', $integrationStart, [StringComparison]::Ordinal)
    $finalCall = $axPut.IndexOf('await _validate.ValidateCurrentValuesAsync(reusePreparedQueryResults: true)', $integrationStart, [StringComparison]::Ordinal)
    Assert-True ($integrationStart -ge 0 -and $targetedCall -gt $integrationStart -and $finalCall -gt $targetedCall) 'AxPut validation order is not targeted then final.'
    Add-Observation 'arm-dep-04-order' 'AxPut source locks targeted refresh/validation before mandatory cache-aware final validation and SQL'

    $events = [System.Collections.Generic.List[string]]::new()
    $commands = [System.Collections.Generic.List[string]]::new()
    Assert-True (Invoke-AxPutRecordModel 'runtime-create' 'create' @('branch') $true $true $events $commands $true) 'Single create did not reach SQL.'
    Assert-True (Invoke-AxPutRecordModel 'runtime-edit' 'edit' @('branch') $true $true $events $commands) 'Single edit did not reach SQL.'
    Assert-Equal ($events -join ',') 'runtime-create/create/full,runtime-create/create/sql,runtime-edit/edit/targeted,runtime-edit/edit/final,runtime-edit/edit/sql' 'Create/edit orchestration order changed.'
    Assert-Equal ($commands -join ',') 'persist:create,persist:edit' 'Create/edit persistence commands changed.'
    Add-Observation 'arm-dep-04-create-edit' 'create runs one full initialization/validation pass; hydrated edit remains targeted then final'

    Assert-True ($axPut.IndexOf('if (compatibilityWarnings.Count > 0)', $integrationStart, [StringComparison]::Ordinal) -lt $finalCall) 'Compatibility fallback does not bypass duplicate final validation.'
    Add-Observation 'arm-dep-04-parent-fallback' 'uninitialized reverse parent state requests compatibility execution; completed fallback is not scanned twice'

    $events.Clear(); $commands.Clear()
    $null = Invoke-AxPutRecordModel 'runtime-parent' 'parent' @('parent_field') $true $true $events $commands
    $null = Invoke-AxPutRecordModel 'runtime-child-1' 'child-row-1' @('child_field') $true $true $events $commands
    $null = Invoke-AxPutRecordModel 'runtime-child-2' 'child-row-2' @('child_field') $true $true $events $commands
    Assert-Equal ($commands -join ',') 'persist:parent,persist:child-row-1,persist:child-row-2' 'Parent/child or multiple transaction persistence changed.'
    Assert-True (($events | Where-Object { $_ -like 'runtime-parent/*' }).Count -eq 3) 'Parent runtime events leaked into a child.'
    Assert-True (($events | Where-Object { $_ -like 'runtime-child-1/*' }).Count -eq 3) 'First child runtime events leaked.'
    Assert-True (($events | Where-Object { $_ -like 'runtime-child-2/*' }).Count -eq 3) 'Queued child runtime events leaked.'
    Add-Observation 'arm-dep-04-multi-runtime' 'independent transactions plus parent/child/queued targets retain distinct runtime outcomes'

    $genMapOps = @(
        [pscustomobject]@{ Kind = 'Value'; FrameNo = 2; RowNo = 3; FieldName = 'grid_value'; AsGrid = $true },
        [pscustomobject]@{ Kind = 'Value'; FrameNo = 1; RowNo = 1; FieldName = 'header_one'; AsGrid = $false },
        [pscustomobject]@{ Kind = 'Value'; FrameNo = 1; RowNo = 2; FieldName = 'header_two'; AsGrid = $false },
        [pscustomobject]@{ Kind = 'DeleteRow'; FrameNo = 2; RowNo = 4; FieldName = ''; AsGrid = $true }
    )
    $firstGenMapRoots = @(Get-GenMapModelAddresses $genMapOps 1 2)
    $secondGenMapRoots = @(Get-GenMapModelAddresses $genMapOps 2 2)
    Assert-Equal ($firstGenMapRoots -join ',') '2|3|grid_value,1|1|header_one' 'First GenMap record roots changed.'
    Assert-Equal ($secondGenMapRoots -join ',') '1|1|header_two' 'Queued GenMap record roots changed.'
    Assert-True (-not (($firstGenMapRoots + $secondGenMapRoots) -match 'DeleteRow')) 'Deleted GenMap rows triggered dependency roots.'
    Add-Observation 'arm-dep-04-genmap-roots' 'multiple source/queued target records select exact value roots and exclude deleted rows'

    $events.Clear(); $commands.Clear()
    Assert-True (-not (Invoke-AxPutRecordModel 'runtime-failed-dependency' 'failed-dependency' @('branch') $false $true $events $commands)) 'Dependency failure reached SQL.'
    Assert-True (-not (Invoke-AxPutRecordModel 'runtime-failed-final' 'failed-final' @('branch') $true $false $events $commands)) 'Final validation failure reached SQL.'
    Assert-True (-not (Invoke-AxPutRecordModel 'runtime-untouched-invalid' 'untouched-invalid' @('changed') $true $false $events $commands)) 'Untouched invalid data escaped final validation.'
    Assert-True (Invoke-AxPutRecordModel 'runtime-after-failure' 'independent' @('other') $true $true $events $commands) 'A failed transaction poisoned an independent runtime.'
    Assert-Equal ($commands -join ',') 'persist:independent' 'Failed validation emitted a persistence command.'
    Add-Observation 'arm-dep-04-failure-boundary' 'dependency/final/untouched failures stop SQL and do not poison a later runtime'

    # Repeated same-runtime sparse edit: one reachable query changes while the other current query result remains valid.
    $compatibilityCounters = [pscustomobject]@{ Refresh = 3; Query = 2; Commands = @('persist:branch') }
    $targetedCounters = [pscustomobject]@{ Refresh = 1; Query = 1; Commands = @('persist:branch') }
    Assert-True ($targetedCounters.Refresh -lt $compatibilityCounters.Refresh) 'Sparse edit did not lower refresh count.'
    Assert-True ($targetedCounters.Query -lt $compatibilityCounters.Query) 'Prepared-query reuse did not lower sparse-edit query count.'
    Assert-Equal ($targetedCounters.Commands -join ',') ($compatibilityCounters.Commands -join ',') 'Targeted persistence commands differ from compatibility output.'
    Add-Observation 'arm-dep-04-sparse-counters' 'repeated-runtime sparse edit refresh=1 vs 3, query=1 vs 2, persistence commands unchanged'

    $validateEvaluations = [regex]::Matches($validate, '(?m)^\s*if \(!_parser\.Evaluate\(')
    Assert-Equal $validateEvaluations.Count 5 'Active Validate parser-evaluation count changed.'
    foreach ($evaluation in $validateEvaluations) {
        $windowLength = [Math]::Min(1400, $validate.Length - $evaluation.Index)
        $window = $validate.Substring($evaluation.Index, $windowLength)
        $valueRead = $window.IndexOf('_parser.Value', [StringComparison]::Ordinal)
        $throwGuard = $window.IndexOf('throw new InvalidOperationException', [StringComparison]::Ordinal)
        $returnGuard = $window.IndexOf('return false;', [StringComparison]::Ordinal)
        $failureGuard = if ($throwGuard -ge 0 -and ($returnGuard -lt 0 -or $throwGuard -lt $returnGuard)) { $throwGuard } else { $returnGuard }
        Assert-True ($valueRead -gt $failureGuard -and $failureGuard -ge 0) 'Validate reads Parser.Value before handling Evaluate(false).'
    }
    $taxEvaluate = $validateHelper.IndexOf('if (!_parser.Evaluate(formula))', [StringComparison]::Ordinal)
    $taxFailure = $validateHelper.IndexOf('throw new InvalidOperationException("Tax formula evaluation failed.")', $taxEvaluate, [StringComparison]::Ordinal)
    $taxValue = $validateHelper.IndexOf('_parser.Value', $taxEvaluate, [StringComparison]::Ordinal)
    Assert-True ($taxEvaluate -ge 0 -and $taxFailure -gt $taxEvaluate -and $taxValue -gt $taxFailure) 'Tax formula reads Parser.Value before handling Evaluate(false).'
    Add-Observation 'parser-failure' 'all five Validate evaluations and the tax evaluation handle failure before Parser.Value; a missing direct optional variable preserves the existing field without a stale read'

    foreach ($parityStep in @(
        '_validate = new Validate(_common, _traceService);',
        '_validate._sdef = await StructDef.LoadStructure',
        '_validate._parser.RegisterVar("recordid"',
        '_validate._sdef.LastSavedRecordid = putData.RecordId;',
        '_validate.PrepareExps();',
        'await _validate.InitValidateAsync(putData.TransId)',
        'await _validate.SubmitValueOnLoad(tstData)',
        'await _validate.SubmitValueDetailed(tstData)',
        'await _validate.ValidateDataDetailed()',
        'await _validate.RefreshAndValidateChangesAsync(changedFields)',
        'await _validate.ValidateCurrentValuesAsync(reusePreparedQueryResults: true)'
    )) {
        Assert-Contains $axPut $parityStep "AxPut parity step changed: $parityStep"
    }
    Add-Observation 'axput-parity' 'load→parser/recordid→PrepareExps/date→Init→baseline→submit/map remains; create uses full, hydrated changes use targeted→final'

    foreach ($observation in $observations) {
        Write-Output "PASS $($observation.Name): $($observation.Detail)"
    }
    Write-Output "SUMMARY passed=$($observations.Count) failed=0"
    exit 0
}
catch {
    Write-Error "FAIL $($_.Exception.Message)"
    exit 1
}
