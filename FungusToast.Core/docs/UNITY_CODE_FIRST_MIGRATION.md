# Unity Code-First Migration

> **Related Documentation**: For Unity UI service and construction recipes, see
> [UI_ARCHITECTURE_HELPER.md](UI_ARCHITECTURE_HELPER.md). For layer ownership and
> runtime patterns, see [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md). For
> scene/prefab staging, churn guard, and merge rules, see
> [../../docs/UNITY_CONCURRENT_WORKFLOW.md](../../docs/UNITY_CONCURRENT_WORKFLOW.md).
> For the full documentation hierarchy, see [README.md](README.md).

This is the canonical policy for moving the Unity front end away from
Inspector-authored wiring toward code-authored construction and configuration.

## 1. Status

- **State:** Active, incremental, opportunistic. There is no big-bang rewrite and
  no deadline.
- **Migration posture:** Compatibility-first. A slice must preserve existing
  behavior unless a behavior change is explicitly called out and tested
  separately.
- **Applies to:** `FungusToast.Unity` only. `FungusToast.Core` is already
  code-first and is the model this initiative moves the Unity layer toward.

## 2. Why

Development is now overwhelmingly AI-assisted. Code-authored wiring is a better
substrate for that than Inspector-authored wiring:

- **Diffable and reviewable.** A wiring change shows up as readable C#, not as
  opaque `{fileID}` / GUID deltas in a `.unity` or `.prefab` YAML blob.
- **Greppable.** `serviceLocator.Get<GameLogPanel>()` can be followed across the
  codebase; a drag-and-drop reference cannot.
- **Mergeable.** Code wiring does not churn or corrupt under concurrent branches
  the way `SampleScene.unity` has historically (see
  [../../docs/UNITY_CONCURRENT_WORKFLOW.md](../../docs/UNITY_CONCURRENT_WORKFLOW.md)).
- **Testable.** Code-constructed objects can be exercised in play-mode or edit-mode
  tests; Inspector slots can only be eyeballed.
- **Legible to an agent.** An AI can see and safely change a `Bootstrap()` method.
  It cannot see what is dragged into a serialized field.

## 3. The Opportunistic-Refactor Policy

The repo-wide default is still *"prefer minimal, scoped changes over opportunistic
refactors"* ([.github/copilot-instructions.md](../../.github/copilot-instructions.md)).
**This initiative is the one sanctioned exception, and only within its scope
(section 4).**

When you touch a Unity area for any reason:

1. **Migrate the wiring you are already editing** in that area from Inspector to
   code, as part of the same change. If you are adding a field to a panel that
   already has ten serialized references, convert that panel's wiring while you
   are in there.
2. **Do not** expand the blast radius to untouched files, unrelated panels, or
   the whole scene. "The area I am touching" means the component, panel, or
   prefab the task already requires changing — not everything adjacent to it.
3. **If the migration would balloon the change** (large scene surgery, risky
   cross-system rewiring, a prefab that many things depend on), stop and leave a
   note in [../../docs/WORKLOG.md](../../docs/WORKLOG.md) instead of forcing it
   into an unrelated task.
4. **Never combine a wiring migration with a gameplay or behavior change in a way
   that hides one inside the other.** Same rule as the AI overhaul: parity slices
   and behavior slices stay separable.

Green-field code always follows the target patterns in section 5 — that is not a
"migration", it is just how new Unity code is written now.

## 4. Scope: What Migrates, What Stays

| Category | Direction | Notes |
|---|---|---|
| Scene GameObject graph / hierarchy | **→ code** | Target: a thin scene that holds a bootstrapper and little else; everything instantiated at runtime. |
| Serialized `MonoBehaviour` cross-references (drag-and-drop) | **→ code** | Replace with the service locator / `SetDependencies` / `Func<>` patterns in [UI_ARCHITECTURE_HELPER.md](UI_ARCHITECTURE_HELPER.md). |
| UI panel construction and layout | **→ code** | Build panels in code (the mutation inspector and dependency graph already do this). Prefabs may remain as dumb visual templates that code instantiates and populates. |
| Tuning values that encode logic (thresholds, per-mode variation) | **→ code** | Move next to their use site; obey the no-magic-constants rule and use the appropriate constants file. |
| Data ScriptableObjects that carry asset references **or are hand-edited often** | **→ code / JSON** | Board presets, campaign progression, and similar. If a `.asset` is regularly edited by hand or holds GUID references to other assets, prefer static C# or plain JSON that an agent can edit and review as text. |
| Pure-data ScriptableObjects that are rarely touched and hold no asset refs | **stays** | Only migrate if a task is already in the file. A custom editor or validation that genuinely earns its keep is a reason to keep the SO. |
| Prefabs as reusable visual templates | **stays** | Keep the visual structure and component *presence*. Strip serialized cross-references and logic tuning out of them. |
| Art / sprite / material / tile / palette assignments | **stays** | The Inspector is the right tool. Do not push these into `Resources.Load` / Addressables for this initiative. |
| Project settings, URP config, input actions | **stays** | Out of scope entirely. |

When in doubt about a specific `.asset`, treat "is this edited by hand or by an
agent?" as the deciding question. If yes, it wants to be text.

## 5. Target Patterns

- **Thin bootstrap scene.** One `GameObject` with a bootstrapper `MonoBehaviour`.
  The startup sequence is a readable code file, not a hierarchy you reconstruct by
  clicking through the scene.
- **No serialized cross-references.** A panel gets its collaborators through
  `SetDependencies(...)` or a lightweight service locator, wired in
  `GameManager.BootstrapServices()` / the bootstrapper. Follow the
  Service Extraction, `GameUIManager` Façade, and `SetDependencies` patterns in
  [UI_ARCHITECTURE_HELPER.md](UI_ARCHITECTURE_HELPER.md).
- **Self-wiring components.** Prefer `GetComponent`, `GetComponentInChildren`, and
  runtime `AddComponent` over serialized fields that require Inspector
  assignment (this is already the rule in
  [../../docs/UNITY_CONCURRENT_WORKFLOW.md](../../docs/UNITY_CONCURRENT_WORKFLOW.md)).
- **Prefabs are dumb.** Instantiate them from code and call a `Configure(...)` /
  `SetDependencies(...)` method. A prefab should not carry references to scene
  objects or gameplay tuning.
- **Data as text.** In-scope data moves to static C# tables or JSON loaded at
  startup, validated by a repository-integrity test rather than by opening the
  Inspector.
- **Validation replaces the eyeball.** Because you lose the Inspector's visual
  "is that slot filled?" check, a migrated area needs a play-mode smoke path (or
  an edit-mode test) that boots the bootstrapper and asserts nothing critical is
  null.

## 6. Guardrails

- Keep gameplay logic deterministic and Unity-free in `FungusToast.Core`. This
  initiative does not move rules into MonoBehaviours.
- The scene/prefab churn rules still apply in full. Any residual `.unity` /
  `.prefab` edit follows
  [../../docs/UNITY_CONCURRENT_WORKFLOW.md](../../docs/UNITY_CONCURRENT_WORKFLOW.md):
  stage by path, keep the diff to exactly what the task requires, run
  `git show --stat HEAD` after committing a scene change.
- A migration slice should ideally *shrink* the scene/prefab diff over time, not
  add to it.
- Do not edit Unity-generated project files (`Assembly-CSharp.csproj`,
  `Assembly-CSharp-Editor.csproj`, `FungusToast.Unity.csproj`).
- Validate Unity compile health in the Unity environment for any slice, and
  verify the affected flow still works.

## 7. Doing a Migration Slice

1. Identify the smallest unit that matches "the area I am already touching" — one
   panel, one component, one prefab.
2. Read its serialized fields. Sort them into: cross-references (migrate), logic
   tuning (migrate to constants), asset references (leave), pure visual layout
   (leave or move to code depending on effort).
3. Introduce or reuse a wiring path (service locator entry, `SetDependencies`
   parameter, `BootstrapServices` registration).
4. Remove the now-dead `[SerializeField]` declarations and the corresponding
   YAML references from the scene/prefab.
5. Add or extend a smoke assertion covering the wiring you just moved.
6. Build Core, build Simulation if shared behavior changed, validate Unity
   compile health, run the affected flow.
7. Commit with the scene/prefab diff reduced to exactly the removed references.
   Note larger follow-on opportunities in `docs/WORKLOG.md` rather than chasing
   them now.

## 8. Cross-References

- [ARCHITECTURE_OVERVIEW.md](ARCHITECTURE_OVERVIEW.md) — "Unity Integration
  Patterns" states the code-first direction as principle.
- [UI_ARCHITECTURE_HELPER.md](UI_ARCHITECTURE_HELPER.md) — the concrete
  construction, service, and dependency recipes this policy points at.
- [../../docs/UNITY_CONCURRENT_WORKFLOW.md](../../docs/UNITY_CONCURRENT_WORKFLOW.md)
  — scene/prefab staging, churn guard, self-wiring-code rule, merge conflicts.
- [.github/copilot-instructions.md](../../.github/copilot-instructions.md) —
  top-level router and the general "minimal scoped changes" rule this initiative
  is the sanctioned exception to.
