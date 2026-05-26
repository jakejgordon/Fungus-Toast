# Git Worktree Workflow

Use this workflow when you want to start a new bug fix or feature without disturbing local changes in the main checkout.

This repository already uses a sibling worktree folder pattern:

- main checkout: the repository root
- sibling worktrees: `..\FungusToast.worktrees\<branch-or-task-name>` relative to the repository root

## Recommended Pattern

For most new tasks, create the worktree from the main repo root:

```powershell
git fetch origin
git worktree add -b fix/tooltip-positioning ..\FungusToast.worktrees\fix-tooltip-positioning origin/main
```

This does four things:

1. updates your local view of `origin`
2. creates a new worktree folder beside the main checkout
3. creates a new local branch for the task
4. starts that branch from the latest `origin/main`

Then move into the new worktree and verify it is clean:

```powershell
Set-Location ..\FungusToast.worktrees\fix-tooltip-positioning
git branch --show-current
git status --short --branch
```

## Why Use This Instead Of Another Branch In-Place

Worktrees are useful in this repo because they let you:

- keep unrelated local edits in the main checkout untouched
- isolate a bug fix in a clean folder
- keep one task open in Unity or VS Code while another task stays parked elsewhere
- avoid repeated stash/apply cycles

## Naming Guidance

Prefer short branch names that match the task:

- `fix/tooltip-positioning`
- `fix/campaign-tooltip-overlap`
- `feature/moldiness-reward-polish`

Mirror the task name in the worktree folder when practical:

```powershell
..\FungusToast.worktrees\fix-tooltip-positioning
```

## Existing Branches

If the branch already exists locally, omit `-b`:

```powershell
git worktree add ..\FungusToast.worktrees\fix-tooltip-positioning fix/tooltip-positioning
```

If the branch exists only on the remote, create the local branch from the remote tracking branch:

```powershell
git fetch origin
git worktree add -b fix/tooltip-positioning ..\FungusToast.worktrees\fix-tooltip-positioning origin/fix/tooltip-positioning
```

## Daily Commands

From inside the worktree:

```powershell
git status
git add <files>
git commit -m "Fix tooltip positioning"
git push -u origin fix/tooltip-positioning
```

## Cleanup

After the branch is merged or no longer needed, leave the worktree directory first and remove it:

```powershell
git worktree remove ..\FungusToast.worktrees\fix-tooltip-positioning
```

If you also want to delete the local branch afterward:

```powershell
git branch -d fix/tooltip-positioning
```

## Common Gotchas

- A branch can only be checked out in one worktree at a time.
- Do not run `git worktree remove` while your shell or editor is still rooted inside that worktree.
- Uncommitted changes in the main checkout do not block creating a separate worktree unless they affect the branch creation command itself.
- Unity caches and local editor state remain per folder, so opening the worktree in a separate VS Code or Unity session is usually the cleanest option.

## Opening Unity From A Worktree

You do not need to add every worktree to Unity Hub.

Use the repo helper script from the checkout you want to test:

```powershell
.\scripts\open_unity_project.ps1
```

That script:

1. treats the current checkout as the repo root
2. opens that checkout's `FungusToast.Unity` project
3. detects the expected Unity editor version from `FungusToast.Unity/ProjectSettings/ProjectVersion.txt`
4. launches `Unity.exe` directly with `-projectPath`

Useful options:

```powershell
# Show the resolved editor and project path without launching Unity
.\scripts\open_unity_project.ps1 -PrintOnly

# Override the editor executable explicitly
.\scripts\open_unity_project.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.4.0f1\Editor\Unity.exe"

# Open a different Unity project folder if needed
.\scripts\open_unity_project.ps1 -ProjectPath .\FungusToast.Unity
```

For worktree-based bug fixes, run the helper from the worktree checkout so Unity opens the branch-specific project copy rather than the main checkout.

## Suggested Repo-Specific Flow

For the next isolated bug fix in this repository:

```powershell
git fetch origin
git worktree add -b fix/tooltip-positioning ..\FungusToast.worktrees\fix-tooltip-positioning origin/main
Set-Location ..\FungusToast.worktrees\fix-tooltip-positioning
.\scripts\open_unity_project.ps1
code .
```

That keeps the main checkout available for unrelated local edits while the tooltip fix proceeds in a clean branch-specific folder.