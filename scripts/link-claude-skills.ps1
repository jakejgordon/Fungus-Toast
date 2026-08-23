<#
.SYNOPSIS
    Mirrors .agents/skills/<name> into .claude/skills/<name> so Claude Code can
    discover the same skills used by other agent tooling in this repo.

.DESCRIPTION
    .agents/skills/ is the canonical, git-tracked source of truth for repeatable
    agent workflows. .claude/skills/ is a local-only, gitignored mirror created
    with NTFS directory junctions (no admin privilege required) so Claude Code's
    built-in skill discovery (.claude/skills/<name>/SKILL.md) picks them up
    without duplicating content.

    Safe to re-run: existing correct junctions are left alone, stale or missing
    ones are recreated.
#>

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceRoot = Join-Path $repoRoot ".agents\skills"
$targetRoot = Join-Path $repoRoot ".claude\skills"

if (-not (Test-Path $sourceRoot)) {
    Write-Error "Source directory not found: $sourceRoot"
}

New-Item -ItemType Directory -Path $targetRoot -Force | Out-Null

$skillDirs = Get-ChildItem -Path $sourceRoot -Directory
foreach ($skill in $skillDirs) {
    $targetPath = Join-Path $targetRoot $skill.Name
    $sourcePath = $skill.FullName

    $existing = Get-Item -Path $targetPath -Force -ErrorAction SilentlyContinue
    if ($existing) {
        $isJunction = $existing.Attributes -band [System.IO.FileAttributes]::ReparsePoint
        if ($isJunction -and $existing.Target -eq $sourcePath) {
            Write-Output "OK      $($skill.Name) (already linked)"
            continue
        }
        Write-Output "RELINK  $($skill.Name) (removing stale entry)"
        if ($isJunction) {
            [System.IO.Directory]::Delete($targetPath, $false)
        } else {
            Remove-Item -Path $targetPath -Recurse -Force
        }
    }

    cmd /c mklink /J "`"$targetPath`"" "`"$sourcePath`"" | Out-Null
    Write-Output "LINKED  $($skill.Name)"
}

Write-Output ""
Write-Output "Done. .claude/skills/ now mirrors .agents/skills/ via junctions."
