[CmdletBinding()]
param(
    [string]$ProjectPath,
    [string]$UnityPath,
    [switch]$PrintOnly,
    [switch]$Wait
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    return Split-Path -Parent $PSScriptRoot
}

function Get-ProjectUnityVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectVersionFilePath
    )

    $versionLine = Get-Content -Path $ProjectVersionFilePath | Where-Object { $_ -like 'm_EditorVersion:*' } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($versionLine)) {
        throw "Unable to determine Unity editor version from '$ProjectVersionFilePath'."
    }

    return $versionLine.Split(':', 2)[1].Trim()
}

function Resolve-UnityPath {
    param(
        [string]$ProvidedPath,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedVersion
    )

    if (-not [string]::IsNullOrWhiteSpace($ProvidedPath)) {
        return (Resolve-Path -Path $ProvidedPath).Path
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_PATH)) {
        return (Resolve-Path -Path $env:UNITY_PATH).Path
    }

    $candidatePaths = @(
        (Join-Path ${env:ProgramFiles} ("Unity\Hub\Editor\{0}\Editor\Unity.exe" -f $ExpectedVersion)),
        (Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor\6000.4.0f1\Editor\Unity.exe')
    )

    foreach ($candidate in $candidatePaths) {
        if (Test-Path -Path $candidate) {
            return $candidate
        }
    }

    $hubRoot = Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor'
    if (Test-Path -Path $hubRoot) {
        $latestInstall = Get-ChildItem -Path $hubRoot -Directory |
            Sort-Object -Property Name -Descending |
            Select-Object -First 1

        if ($null -ne $latestInstall) {
            $fallbackCandidate = Join-Path $latestInstall.FullName 'Editor\Unity.exe'
            if (Test-Path -Path $fallbackCandidate) {
                return $fallbackCandidate
            }
        }
    }

    throw 'Unable to locate Unity.exe. Pass -UnityPath or set UNITY_PATH.'
}

$repoRoot = Get-RepoRoot

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $repoRoot 'FungusToast.Unity'
}

$resolvedProjectPath = [System.IO.Path]::GetFullPath((Resolve-Path -Path $ProjectPath).Path)
$projectVersionFilePath = Join-Path $resolvedProjectPath 'ProjectSettings/ProjectVersion.txt'

if (-not (Test-Path -Path $projectVersionFilePath)) {
    throw "Unable to find Unity project version file at '$projectVersionFilePath'. Pass -ProjectPath to a valid Unity project folder."
}

$expectedUnityVersion = Get-ProjectUnityVersion -ProjectVersionFilePath $projectVersionFilePath
$resolvedUnityPath = Resolve-UnityPath -ProvidedPath $UnityPath -ExpectedVersion $expectedUnityVersion
$argumentList = @('-projectPath', $resolvedProjectPath)

Write-Host "Unity editor: $resolvedUnityPath"
Write-Host "Unity project: $resolvedProjectPath"
Write-Host "Expected Unity version: $expectedUnityVersion"

if ($PrintOnly) {
    return
}

$process = Start-Process -FilePath $resolvedUnityPath -ArgumentList $argumentList -PassThru

if ($Wait) {
    $process.WaitForExit()
}