[CmdletBinding()]
param(
    [string[]]$BuildProject = @(),
    [string[]]$TestProject = @(),
    [string]$TestFilter,
    [switch]$NoBuildTests,
    [switch]$Characterization,
    [switch]$Postman,
    [switch]$DiffCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path

function Resolve-RepositoryPath {
    param([Parameter(Mandatory)][string]$Path)

    $resolved = (Resolve-Path (Join-Path $repoRoot $Path)).Path
    $rootPrefix = $repoRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (!$resolved.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Path must remain inside the repository: $Path"
    }

    return $resolved
}

function Invoke-QuietCommand {
    param(
        [Parameter(Mandatory)][string]$Label,
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    $output = @(& $FilePath @Arguments 2>&1 | ForEach-Object { [string]$_ })
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        $firstFailure = $output |
            Where-Object { $_ -match '(?i)(\berror\b|\bfailed\b|exception)' } |
            Select-Object -First 1
        if ([string]::IsNullOrWhiteSpace($firstFailure)) {
            $firstFailure = $output | Where-Object { ![string]::IsNullOrWhiteSpace($_) } | Select-Object -Last 1
        }

        throw "$Label failed (exit $exitCode): $firstFailure"
    }

    Write-Output "PASS: $Label"
}

$hasAction = $BuildProject.Count -gt 0 -or
    $TestProject.Count -gt 0 -or
    $Characterization -or
    $Postman -or
    $DiffCheck
if (!$hasAction) {
    throw 'Select at least one build, test, characterization, Postman, or diff check.'
}

Push-Location $repoRoot
try {
    foreach ($project in $BuildProject) {
        $projectPath = Resolve-RepositoryPath $project
        Invoke-QuietCommand "build $project" 'dotnet' @(
            'build', '--nologo', '-v', 'q', '--no-restore', $projectPath
        )
    }

    foreach ($project in $TestProject) {
        $projectPath = Resolve-RepositoryPath $project
        $arguments = @('test', '--nologo', '-v', 'q', '--no-restore')
        if ($NoBuildTests) {
            $arguments += '--no-build'
        }
        $arguments += $projectPath
        if (![string]::IsNullOrWhiteSpace($TestFilter)) {
            $arguments += @('--filter', $TestFilter)
        }

        Invoke-QuietCommand "test $project" 'dotnet' $arguments
    }

    if ($Characterization) {
        $script = Resolve-RepositoryPath 'ARM-11.4/AxSave.Characterization/Invoke-Characterization.ps1'
        $pwsh = Join-Path $PSHOME 'pwsh.exe'
        Invoke-QuietCommand 'AxSave characterization' $pwsh @('-NoProfile', '-File', $script)
    }

    if ($Postman) {
        foreach ($jsonPath in @(
            'AxData/tools/postman/AxData-M1.5.postman_collection.json',
            'AxData/tools/postman/AxData-M1.5.template.postman_environment.json'
        )) {
            $resolvedJson = Resolve-RepositoryPath $jsonPath
            $null = Get-Content -Raw $resolvedJson | ConvertFrom-Json
        }
        Write-Output 'PASS: Postman JSON'
    }

    if ($DiffCheck) {
        Invoke-QuietCommand 'git diff --check' 'git' @('diff', '--check')
    }
}
finally {
    Pop-Location
}
