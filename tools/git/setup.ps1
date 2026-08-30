<#
Configures this clone's local git for painless Unity collaboration:

  * core.hooksPath      -> tools/git/hooks   (cosmetic scene-churn guard)
  * merge.unityyamlmerge -> Unity's SmartMerge tool (semantic 3-way merge for
                            *.unity / *.prefab / *.asset, wired up in .gitattributes)
  * rerere.enabled      -> remember conflict resolutions

Run once per clone / worktree root:  pwsh tools/git/setup.ps1
Re-run any time after upgrading Unity.
#>
[CmdletBinding()]
param(
    [string]$UnityYamlMerge
)

$ErrorActionPreference = 'Stop'
$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

git config core.hooksPath 'tools/git/hooks'
git config rerere.enabled true
git config rerere.autoUpdate true
Write-Host "core.hooksPath  = tools/git/hooks"
Write-Host "rerere.enabled  = true"

function Resolve-UnityYamlMerge {
    param([string]$Explicit)
    if ($Explicit) { return $Explicit }

    $versionFile = Join-Path $repoRoot 'FungusToast.Unity/ProjectSettings/ProjectVersion.txt'
    $version = $null
    if (Test-Path $versionFile) {
        $line = Select-String -Path $versionFile -Pattern '^m_EditorVersion:\s*(.+)$'
        if ($line) { $version = $line.Matches[0].Groups[1].Value.Trim() }
    }

    $candidates = @()
    if ($version) {
        $candidates += "C:\Program Files\Unity\Hub\Editor\$version\Editor\Data\Tools\UnityYAMLMerge.exe"
        $candidates += "$HOME/Unity/Hub/Editor/$version/Editor/Data/Tools/UnityYAMLMerge.exe"
        $candidates += "/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/Tools/UnityYAMLMerge"
    }
    # Fall back to the newest installed editor.
    $hubRoot = "C:\Program Files\Unity\Hub\Editor"
    if (Test-Path $hubRoot) {
        Get-ChildItem $hubRoot -Directory | Sort-Object Name -Descending | ForEach-Object {
            $candidates += Join-Path $_.FullName 'Editor\Data\Tools\UnityYAMLMerge.exe'
        }
    }
    foreach ($c in $candidates) { if ($c -and (Test-Path $c)) { return $c } }
    return $null
}

$tool = Resolve-UnityYamlMerge -Explicit $UnityYamlMerge
if ($tool) {
    # %O ancestor, %B current branch, %A working/other, last arg = output path
    git config merge.unityyamlmerge.name 'Unity SmartMerge'
    git config merge.unityyamlmerge.driver ('"' + $tool + '" merge -p %O %B %A %A')
    git config merge.unityyamlmerge.recursive binary
    Write-Host "merge.unityyamlmerge -> $tool"
} else {
    Write-Warning "UnityYAMLMerge.exe not found. Pass -UnityYamlMerge '<path>' or install the editor matching ProjectVersion.txt."
    Write-Warning "Unity YAML files will fall back to the default text merge until then."
}

Write-Host ""
Write-Host "Done. See docs/UNITY_CONCURRENT_WORKFLOW.md for the playtest-while-agents-work setup."
