[CmdletBinding()]
param(
    [string]$Version,

    [string]$ItchTarget,

    [string]$Channel,

    [string]$UnityPath,

    [string]$ButlerPath,

    [string]$BuildOutputPath,

    [switch]$PublishOnly,

    [switch]$BuildOnly,

    [switch]$DryRun,

    [switch]$IfChanged,

    [switch]$SkipSimulationBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    return Split-Path -Parent $PSScriptRoot
}

function Get-ReleaseVersionFilePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    return Join-Path (Join-Path $RepoRoot 'FungusToast.Unity') 'version.txt'
}

function Get-LastDeployedVersionFilePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    return Join-Path (Join-Path $RepoRoot 'FungusToast.Unity') 'last-deployed-version.txt'
}

function Test-SemanticVersion {
    param(
        [string]$Value
    )

    return -not [string]::IsNullOrWhiteSpace($Value) -and $Value -match '^\d+\.\d+\.\d+$'
}

function Read-VersionFileValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Description,
        [switch]$AllowMissingOrEmpty
    )

    if (-not (Test-Path -Path $Path)) {
        if ($AllowMissingOrEmpty) {
            return $null
        }

        throw "Unable to find $Description file at '$Path'."
    }

    $firstLine = [string](Get-Content -Path $Path -TotalCount 1 | Select-Object -First 1)
    $value = $firstLine.Trim()

    if ([string]::IsNullOrWhiteSpace($value)) {
        if ($AllowMissingOrEmpty) {
            return $null
        }

        throw "The $Description file at '$Path' is empty."
    }

    if (-not (Test-SemanticVersion -Value $value)) {
        throw "The $Description file at '$Path' must contain a semantic version in Major.Minor.BugFix format on the first line."
    }

    return $value
}

function Resolve-ReleaseVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [string]$ProvidedVersion
    )

    $releaseVersionFilePath = Get-ReleaseVersionFilePath -RepoRoot $RepoRoot
    $fileVersion = Read-VersionFileValue -Path $releaseVersionFilePath -Description 'current release version'

    if (-not [string]::IsNullOrWhiteSpace($ProvidedVersion) -and $ProvidedVersion -ne $fileVersion) {
        throw "The provided -Version '$ProvidedVersion' does not match '$fileVersion' in '$releaseVersionFilePath'. Update FungusToast.Unity/version.txt or omit -Version."
    }

    return $fileVersion
}

function Assert-ReleaseVersionIsNewerThanLastDeployment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$ReleaseVersion
    )

    $lastDeployedVersionFilePath = Get-LastDeployedVersionFilePath -RepoRoot $RepoRoot
    $lastDeployedVersion = Read-VersionFileValue -Path $lastDeployedVersionFilePath -Description 'last deployed version' -AllowMissingOrEmpty

    if ([string]::IsNullOrWhiteSpace($lastDeployedVersion)) {
        return
    }

    if (([version]$ReleaseVersion) -le ([version]$lastDeployedVersion)) {
        throw "Release version '$ReleaseVersion' is not newer than the last deployed version '$lastDeployedVersion'. Update FungusToast.Unity/version.txt before publishing to itch.io."
    }
}

function Write-LastDeployedVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,
        [Parameter(Mandatory = $true)]
        [string]$ReleaseVersion
    )

    $lastDeployedVersionFilePath = Get-LastDeployedVersionFilePath -RepoRoot $RepoRoot
    [System.IO.File]::WriteAllText($lastDeployedVersionFilePath, $ReleaseVersion + [Environment]::NewLine)
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

function Resolve-ReleaseConfig {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $config = [ordered]@{}
    $localConfigPath = Join-Path $RepoRoot 'scripts/itch-release.local.json'

    if (Test-Path -Path $localConfigPath) {
        $json = Get-Content -Path $localConfigPath -Raw | ConvertFrom-Json
        foreach ($property in $json.PSObject.Properties) {
            if (-not [string]::IsNullOrWhiteSpace($property.Value)) {
                $config[$property.Name] = $property.Value
            }
        }
    }

    return $config
}

function Resolve-UnityPath {
    param(
        [string]$ProvidedPath,
        [Parameter(Mandatory = $true)]
        [string]$ExpectedVersion
    )

    if (-not [string]::IsNullOrWhiteSpace($ProvidedPath)) {
        $resolvedPath = Resolve-Path -Path $ProvidedPath
        return $resolvedPath.Path
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_PATH)) {
        $resolvedPath = Resolve-Path -Path $env:UNITY_PATH
        return $resolvedPath.Path
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

function Resolve-ButlerPath {
    param(
        [string]$ProvidedPath
    )

    if (-not [string]::IsNullOrWhiteSpace($ProvidedPath)) {
        $resolvedPath = Resolve-Path -Path $ProvidedPath
        return $resolvedPath.Path
    }

    if (-not [string]::IsNullOrWhiteSpace($env:BUTLER_PATH)) {
        $resolvedPath = Resolve-Path -Path $env:BUTLER_PATH
        return $resolvedPath.Path
    }

    $command = Get-Command butler -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }

    $chosenVersionFile = Join-Path $env:APPDATA 'itch\broth\butler\.chosen-version'
    if (Test-Path -Path $chosenVersionFile) {
        $chosenVersion = (Get-Content -Path $chosenVersionFile -Raw).Trim()
        if (-not [string]::IsNullOrWhiteSpace($chosenVersion)) {
            $bundledPath = Join-Path $env:APPDATA ("itch\broth\butler\versions\{0}\butler.exe" -f $chosenVersion)
            if (Test-Path -Path $bundledPath) {
                return $bundledPath
            }
        }
    }

    throw 'Unable to locate butler.exe. Pass -ButlerPath, set BUTLER_PATH, or install butler.'
}

function Initialize-ButlerApiKey {
    if (-not [string]::IsNullOrWhiteSpace($env:BUTLER_API_KEY)) {
        return
    }

    $userValue = [Environment]::GetEnvironmentVariable('BUTLER_API_KEY', 'User')
    if (-not [string]::IsNullOrWhiteSpace($userValue)) {
        $env:BUTLER_API_KEY = $userValue
        return
    }

    $machineValue = [Environment]::GetEnvironmentVariable('BUTLER_API_KEY', 'Machine')
    if (-not [string]::IsNullOrWhiteSpace($machineValue)) {
        $env:BUTLER_API_KEY = $machineValue
    }
}

function Assert-UnityEditorIsClosed {
    $runningUnityProcesses = @(Get-Process -Name 'Unity' -ErrorAction SilentlyContinue |
        Sort-Object -Property Id)

    if ($runningUnityProcesses.Count -eq 0) {
        return
    }

    $processSummary = ($runningUnityProcesses | ForEach-Object {
        if ([string]::IsNullOrWhiteSpace($_.MainWindowTitle)) {
            return ("PID {0}" -f $_.Id)
        }

        return ("PID {0} ({1})" -f $_.Id, $_.MainWindowTitle)
    }) -join ', '

    throw ("Unity is currently running: {0}. Close all Unity editor windows and rerun scripts/publish_itch_release.ps1." -f $processSummary)
}

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Description,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host "==> $Description"
    & $Action
}

$repoRoot = Get-RepoRoot
$Version = Resolve-ReleaseVersion -RepoRoot $repoRoot -ProvidedVersion $Version
$releaseConfig = Resolve-ReleaseConfig -RepoRoot $repoRoot

if ([string]::IsNullOrWhiteSpace($ItchTarget) -and $releaseConfig.Contains('itchTarget')) {
    $ItchTarget = [string]$releaseConfig['itchTarget']
}

if ([string]::IsNullOrWhiteSpace($Channel) -and $releaseConfig.Contains('channel')) {
    $Channel = [string]$releaseConfig['channel']
}

if ([string]::IsNullOrWhiteSpace($BuildOutputPath) -and $releaseConfig.Contains('buildOutputPath')) {
    $BuildOutputPath = [string]$releaseConfig['buildOutputPath']
}

if ([string]::IsNullOrWhiteSpace($BuildOutputPath)) {
    $BuildOutputPath = 'Builds/itch/windows'
}

$fullBuildOutputPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $BuildOutputPath))
$buildRootPath = Split-Path -Parent $fullBuildOutputPath
$unityLogPath = Join-Path $buildRootPath 'unity-release.log'
$unityProjectPath = Join-Path $repoRoot 'FungusToast.Unity'
$projectVersionFilePath = Join-Path $unityProjectPath 'ProjectSettings/ProjectVersion.txt'
$expectedUnityVersion = Get-ProjectUnityVersion -ProjectVersionFilePath $projectVersionFilePath
$resolvedUnityPath = Resolve-UnityPath -ProvidedPath $UnityPath -ExpectedVersion $expectedUnityVersion

if (-not $BuildOnly) {
    Assert-ReleaseVersionIsNewerThanLastDeployment -RepoRoot $repoRoot -ReleaseVersion $Version

    if ([string]::IsNullOrWhiteSpace($ItchTarget)) {
        throw 'Itch target is required. Pass -ItchTarget or create scripts/itch-release.local.json.'
    }

    if ([string]::IsNullOrWhiteSpace($Channel)) {
        throw 'Itch channel is required. Pass -Channel or create scripts/itch-release.local.json.'
    }

    $resolvedButlerPath = Resolve-ButlerPath -ProvidedPath $ButlerPath
    Initialize-ButlerApiKey
}

if ($PublishOnly -and $BuildOnly) {
    throw 'Use either -PublishOnly or -BuildOnly, not both.'
}

if (-not $PublishOnly) {
    Assert-UnityEditorIsClosed
}

if (-not $PublishOnly) {
    Invoke-Step -Description 'Building FungusToast.Core' -Action {
        Push-Location $repoRoot
        try {
            dotnet build 'FungusToast.Core/FungusToast.Core.csproj' --no-restore
            if ($LASTEXITCODE -ne 0) {
                throw 'dotnet build for FungusToast.Core failed.'
            }
        }
        finally {
            Pop-Location
        }
    }
}

if ((-not $PublishOnly) -and (-not $SkipSimulationBuild)) {
    Invoke-Step -Description 'Building FungusToast.Simulation' -Action {
        Push-Location $repoRoot
        try {
            dotnet build 'FungusToast.Simulation/FungusToast.Simulation.csproj' --no-restore
            if ($LASTEXITCODE -ne 0) {
                throw 'dotnet build for FungusToast.Simulation failed.'
            }
        }
        finally {
            Pop-Location
        }
    }
}

if (-not $PublishOnly) {
    Invoke-Step -Description 'Cleaning previous release output' -Action {
        if (Test-Path -Path $fullBuildOutputPath) {
            Remove-Item -Path $fullBuildOutputPath -Recurse -Force
        }

        if (-not (Test-Path -Path $buildRootPath)) {
            New-Item -Path $buildRootPath -ItemType Directory | Out-Null
        }

        if (Test-Path -Path $unityLogPath) {
            Remove-Item -Path $unityLogPath -Force
        }

        New-Item -Path $fullBuildOutputPath -ItemType Directory | Out-Null
    }

    Invoke-Step -Description 'Building Windows release with Unity' -Action {
        $unityArguments = @(
            '-batchmode',
            '-quit',
            '-nographics',
            '-projectPath', $unityProjectPath,
            '-executeMethod', 'ReleaseBuildAutomation.BuildWindowsRelease',
            '-releaseOutputPath', $fullBuildOutputPath,
            '-releaseVersion', $Version,
            '-logFile', $unityLogPath
        )

        $unityProcess = Start-Process -FilePath $resolvedUnityPath -ArgumentList $unityArguments -Wait -PassThru -NoNewWindow

        if ($unityProcess.ExitCode -ne 0) {
            throw ("Unity release build failed with exit code {0}. See {1}." -f $unityProcess.ExitCode, $unityLogPath)
        }
    }
}

$builtExecutablePath = Join-Path $fullBuildOutputPath 'FungusToast.exe'
if (-not (Test-Path -Path $builtExecutablePath)) {
    if ($PublishOnly) {
        throw "Publish-only mode expected an existing Windows build at '$builtExecutablePath'. Build the game manually into '$fullBuildOutputPath' first."
    }

    throw "Expected release executable was not produced at '$builtExecutablePath'."
}

if ($BuildOnly) {
    Write-Host "Release build is ready at '$fullBuildOutputPath'."
    exit 0
}

Invoke-Step -Description 'Checking butler version' -Action {
    & $resolvedButlerPath version
    if ($LASTEXITCODE -ne 0) {
        throw 'butler version failed.'
    }
}

Invoke-Step -Description 'Pushing build to itch.io with butler' -Action {
    $arguments = @(
        'push',
        $fullBuildOutputPath,
        ("{0}:{1}" -f $ItchTarget, $Channel),
        '--userversion',
        $Version
    )

    if ($IfChanged) {
        $arguments += '--if-changed'
    }

    if ($DryRun) {
        $arguments += '--dry-run'
    }

    & $resolvedButlerPath @arguments
    if ($LASTEXITCODE -ne 0) {
        throw 'butler push failed.'
    }
}

if (-not $DryRun) {
    Invoke-Step -Description 'Recording last deployed itch.io version' -Action {
        Write-LastDeployedVersion -RepoRoot $repoRoot -ReleaseVersion $Version
    }
}

Write-Host ("Finished release flow for version {0}. Output: {1}" -f $Version, $fullBuildOutputPath)