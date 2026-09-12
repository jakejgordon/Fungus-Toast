# Ability Icon Overhaul Plan

Scope: the icons for Adaptations, Mycovariants, and Mycelial Surge mutations.
Goal: higher resolution, and every icon depicts what the ability does.

Status: proposed 2026-09-12. Recommended direction is Option A below; nothing
is implemented yet.

## 1. Where things stand

All three icon families are generated procedurally at runtime, not loaded from
image files:

| Family | Repository | Count | Selected by |
| --- | --- | --- | --- |
| Adaptations | `FungusToast.Unity/Assets/Scripts/Unity/UI/Campaign/AdaptationArtRepository.cs` | 31 | `AdaptationDefinition.IconId` (one `Draw*` per adaptation) |
| Mycovariants | `FungusToast.Unity/Assets/Scripts/Unity/UI/MycovariantDraft/MycovariantArtRepository.cs` | 35 (22 families; I/II/III tiers share a design) | 6 bespoke motifs; the other 29 get a category motif + hashed "identity marks" + tier pips |
| Surges | `FungusToast.Unity/Assets/Scripts/Unity/UI/MutationTree/SurgeArtRepository.cs` | 6 | `MutationIds` |

Shared rasterizer: `ProceduralIconUtility` (currently nested at the bottom of
`AdaptationArtRepository.cs`). It paints a 40x40 `Texture2D` with integer
`SetPixel` primitives (fill, border, circle, ring, Bresenham line, shield), no
anti-aliasing, no mipmaps.

Where the sprites are shown (UI units at the 1920x1080 reference resolution;
the canvas is ScaleWithScreenSize, so on a 4K display multiply by 2):

| Surface | Size | Family |
| --- | --- | --- |
| Campaign panel reward/loadout tiles | 68 | Adaptation |
| End-game details header | 56 | Adaptation, Mycovariant |
| Draft choice card (`UI_DraftChoiceCard.prefab`, `UI_DraftIcon`) | 40 | Mycovariant, Adaptation |
| Draft feed rows | 36 | Mycovariant, Adaptation |
| Campaign adaptation summary strip, moldiness rewards strip | 34 | Adaptation |
| Mutation tree card, inspector title, player inspector | 28 | Surge |
| Mold profile grids, player inspector trait rows | ~28-40 | All |

So the 40px textures are already upscaled 1.4-1.7x at 1080p and 2.8-3.4x at 4K,
which is the blur the user sees. Every consumer only ever asks a repository for
a `Sprite`; nothing reads texture size or calls `SetNativeSize`, so the
rendering method can change without touching the ~20 call sites.

## 2. Assessment

Feasible: yes, and cheaply on the integration side. The three repositories are
the only seam. No scene or prefab edits are required (which matters given the
`SampleScene.unity` churn history).

Worth doing: yes. Beyond resolution, the mycovariant icons are mostly
non-semantic today (29 of 35 are "category motif + random dots"), so the draft
screen leans entirely on the title text. Diagrammatic icons ("here is the cell
pattern this places") are the fastest thing a player can read on a draft card
and in the player inspector.

### The medium decision

**Option A (recommended): procedural vector-style diagrams rendered at high
resolution.** Replace the 40px integer rasterizer with an anti-aliased,
float-coordinate canvas that renders at 128px with mipmaps, add a vocabulary
of board-native glyphs (living/resistant/dead/toxin/enemy cells, spore, crust,
arrows, dashed arcs), and re-author every icon as a small diagram of its
effect.

- Pros: no asset pipeline, no transparency wrangling, deterministic, colours
  stay bound to `UIStyleTokens` (so the icons track the style guide for free),
  diffs are reviewable code, and adding a new Mycovariant/Adaptation stays a
  code-only step that the `create-*` skills and helper docs can keep requiring.
  "Exact pattern of cells" is precisely what a diagram renderer is good at and
  what image models are worst at.
- Cons: flat/diagrammatic rather than painterly. ~60 drawings to author (each
  5-15 lines with a good DSL). CPU generation at first use: ~1-3 ms per icon,
  negligible.

**Option B: AI-generated PNG art** (the mold-sprite route), 256x256 PNGs under
`Assets/Resources/Icons/...` loaded by `IconId`.

- Pros: richest look.
- Cons: `docs/TEMP_MOLD_SPRITE_PROMPTS.md` records the cost: transparency
  failures, per-image post-processing, many prompt iterations per sprite. Style
  consistency across 60+ images produced over months is hard. Precise
  mechanics (a 4-cell line, a dotted arc to one tile) are unreliable from image
  models. Palette drifts from `UIStyleTokens`. Every new content item needs an
  art step outside the code loop. +5-15 MB of assets.

**Option C: SVG via `com.unity.vectorgraphics`.** Crisp vectors, but a
preview package with its own importer/tessellation quirks, and token colours
cannot flow into files. Not recommended.

Recommendation: Option A now. Design the repositories so a PNG override
(Option B) can be added later for any individual icon if a hero surface ever
wants painted art. Do not build that hook until it is wanted.

## 3. Plan (Option A)

### Phase 0 - Review tooling (first, small)

Add an Editor menu item, e.g. `Fungus Toast/Icons/Export Icon Contact Sheet`,
that renders every Adaptation, Mycovariant, and Surge icon and writes PNG
contact sheets to `TEMP/icon-sheets/` (git-ignored):

- one sheet at native texture size, labelled by `IconId`/name;
- one sheet at the real slot sizes (28, 34, 40, 56, 68) and at 2x of each, so
  the 28px surge case and the 136px campaign-tile case are both checked.

Export a "before" sheet with the current generator first. This is the review
loop for every later phase: no game flows to click through, and the sheet can
be sent back into the chat for review.

### Phase 1 - Renderer

New folder `FungusToast.Unity/Assets/Scripts/Unity/UI/Icons/`:

- `IconCanvas.cs` - replaces `ProceduralIconUtility`. Float coordinate space
  0..100 regardless of output size; default output 128x128 RGBA32, mipmaps on,
  bilinear, clamp, `Apply(updateMipmaps: true, makeNoLongerReadable: true)`.
  Anti-aliasing via analytic coverage (signed distance per primitive, 1px
  smoothstep) composited with source-over. Primitives: filled/rounded rect,
  circle, ring, arc, line with round caps, polyline, dashed line/arc, arrow
  head, shield, burst, and a 2-unit frame (the frame must stay intact at 28px,
  see commit `d47436c`).
- `IconGlyphs.cs` - the shared board vocabulary drawn with those primitives
  (see section 4). All colours from `UIStyleTokens`.
- Keep `CreateSprite(name, background, accent, draw, ...)` shape-compatible so
  each repository can be migrated independently. Move `ProceduralIconUtility`
  out of `AdaptationArtRepository.cs` as part of this.

Memory footprint: 72 icons x 128^2 x 4 bytes x 1.33 (mips) is about 4.7 MB;
fine. Texture size is one constant if 192 or 256 turns out to be wanted for
the 136px case.

### Phase 2 - Visual language

Written down once (in `FungusToast.Core/docs/UI_STYLE_GUIDE.md`, new "Ability
icons" section) and encoded in `IconGlyphs`:

- Frame: background = family/category tint as today; 2-unit accent frame.
  Family tell that does not rely on hue: Adaptations get rounded frame corners,
  Mycovariants square corners with tier pips top-right (I/II/III), Surges
  square corners with a small chevron notch at the top edge.
- Content budget: at most three "things" per icon, strokes no thinner than
  about 4/100 units, so the 28px case still reads.
- Shape-not-hue for state, per the style guide: resistant = shield notch /
  inner ring, dead = hollow with an X, toxin = diamond dot cluster, enemy =
  outlined in danger tint, empty = faint dotted square. Never draw text; use
  pips for counts.
- Directional effects use solid arrows; "lands somewhere" uses a dashed arc
  ending in the placed cell (the Surgical Inoculation shape).

### Phase 3 - Re-author icons, in three reviewed batches

Each batch: author drawings -> export contact sheet -> review -> adjust ->
commit. Order chosen so the pipeline is validated on the smallest, most
visible set first.

1. Surges (6). Also exercises the 28px tree-card path and the frame rule.
2. Mycovariants (22 designs; tiers reuse the design and differ by pips).
   Delete the hashed identity-mark system.
3. Adaptations (31).

Per-icon briefs are in section 5.

### Phase 4 - Guard rails and docs

- Fallback icon logs a warning in the Editor when hit, and an Editor validation
  (menu item, or an EditMode test since `com.unity.test-framework` is already
  in the manifest) asserts every entry of `AdaptationRepository.All`, the
  Mycovariant factories, and every `IsSurge` mutation resolves to a dedicated
  drawer. This enforces the "distinct icon per item" rule the helper docs
  already state.
- Update `FungusToast.Core/docs/ADAPTATION_HELPER.md`,
  `MYCOVARIANT_HELPER.md`, the `create-adaptation` / `create-mycovariant` /
  `create-mutation` skills, and `UI_STYLE_GUIDE.md`: the icon step now means
  "add a `Draw*` in the family's icon file using `IconGlyphs`, then export the
  contact sheet".

### Optional follow-ups (separate decisions, not part of this plan)

- Enlarge the draft card icon slot (`UI_DraftIcon` is 40x40 in a 260x400 card)
  to ~64 so the diagrams get room on the surface where they matter most. This
  is a prefab edit.
- PNG override hook for individual hero icons (Option B, per icon).

Rough effort: Phases 0-1 one session; each Phase 3 batch about one session
with a review in between; Phase 4 half a session.

## 4. Glyph vocabulary (`IconGlyphs`)

| Glyph | Drawing |
| --- | --- |
| Living cell (yours) | filled rounded square, accent |
| Resistant cell | living cell + inner shield notch / inner ring in highlight |
| Dead cell | hollow square, muted, with an X |
| Toxin | small diamond cluster (3 dots) in Fungicide tint |
| Enemy living cell | square outlined in Danger tint |
| Empty tile | faint dotted square |
| Starting spore | filled circle with a ring |
| Crust | thick band along one or more frame edges |
| Nutrient patch | cluster of small filled dots, Success tint |
| Mutation points | short stack of pips (reuse the DNA-battery motif shape) |
| Surge | chevron/bolt mark |
| Growth arrow | solid line + filled head |
| Flight / placement | dashed arc ending in a placed cell |
| Radius | dashed ring |
| Clear / kill | X stroke or sweep stroke over the target |

## 5. Per-icon briefs

Effect summaries are paraphrased from the definitions; the drawing column is
the proposal to review on the contact sheet.

### Surges

| Icon | Effect | Drawing |
| --- | --- | --- |
| Autolytic Surge | faster growth, cells die more | upward growth arrow with new cells above; bottom cell dissolving (dashed) |
| Necrotic Clearance | remove own dead cells near enemies before they reclaim | living cell, adjacent dead cell with a sweep stroke, enemy cell lurking beyond it |
| Chemotactic Beacon | line of growth from spore to a placed marker, then spirals clockwise | spore bottom-left, dashed guide line to a target ring top-right, cells along it, short clockwise spiral around the ring |
| Mimetic Resilience | copy resistant footholds near stronger rivals | enemy resistant cell (outlined + notch) with a mirrored friendly resistant cell beside it and a copy arrow |
| Competitive Antagonism | toxins focus on the biggest colony | large colony under crosshair with toxin diamonds; small colony untouched |
| Chitin Fortification | N random cells per phase become resistant permanently | 3x3 friendly block, three cells gaining shield notches, plated texture |

### Mycovariants

| Icon | Effect | Drawing |
| --- | --- | --- |
| Jetting Mycelium I/II/III | line of up to N cells in a direction, then widening toxin fan | source cell, four cells in a row, triangle of toxin diamonds fanning past the end; tier pips |
| Plasmid Bounty I/II/III | +MP | plasmid loop with MP pip stack; pips scale with tier |
| Ascus Wager | one free random Tier 5 level | ascus sac bursting, one gold pip rising to the top rung of a 5-rung ladder |
| Ascus Bait | human +MP; AI loses cells | fish hook through a spore, small dead cells falling away |
| Sporophore Decoy | human +MP; AI resistant cells lose resistance | hollow (dotted) mushroom silhouette; shield notch crossed out |
| Sporal Snare | snares cells along the human-to-AI line | two spores, line between them, loops around cells on the line |
| Perispore Crown | human +MP; AI burst around its spore | spore with a crown ring of toxin diamonds |
| Neutralizing Mantle | neutralize adjacent enemy toxins | living cell with a halo, adjacent toxin crossed out |
| Enduring Toxaphores | toxins last longer | toxin diamond with a long clock arc / hourglass |
| Ballistospore Discharge I/II/III | toxify N empty tiles | spore launching toxin diamonds along dashed arcs to empty tiles; pips |
| Cytolytic Burst | one toxin bursts in a radius | toxin at centre, dashed radius ring, poisoned cells inside |
| Chemotactic Mycotoxins | isolated toxins drift to enemies | toxin diamond, dashed drift arrow to an enemy cell |
| Perimeter Proliferator | growth multiplier near the crust | mini-board, crust band, cells along the edge with growth arrows |
| Corner Conduit I/II/III | cells placed in a corner each phase | mini-board with a corner highlighted and cells appearing there; pips |
| Hyphal Draw | pick up cells on the path to the enemy, redeploy from their side | two spores, dashed pick-up cells on the path, solid redeployed cells with a reversing arrow |
| Necrophoric Adaptation | when a cell dies, reclaim an adjacent dead tile | dying cell (X) with an arrow into an adjacent dead cell becoming living |
| Reclamation Rhizomorphs | failed reclaim gets a second try | dead cell, first arrow struck out, second root-like arrow landing |
| Mycelial Bastion I/II/III | pick up to N cells to become resistant | cluster with N cells gaining shield notches; pips |
| Surgical Inoculation | place one resistant cell anywhere | living cell, dashed arc to a distant tile, single resistant cell landing |
| Hyphal Resistance Transfer | cells near resistant cells become resistant | resistant centre with neighbours (diagonals included) gaining notches |
| Septal Alarm | neighbours of a dying cell may become resistant | dying cell (X) with alarm rays; orthogonal neighbours gaining notches |
| Septal Seal | a random share of cells becomes resistant | scattered cluster with a wax-seal stamp motif on the sealed cells |
| Aggressotropic Conduit I/II/III | cells placed toward the enemy spore each phase | spore, line of cells with an arrow toward an enemy spore; pips |

### Adaptations

| Icon | Effect | Drawing |
| --- | --- | --- |
| Conidial Relay | starting spore relocates on a round | spore, dashed arc to a new tile |
| Hyphal Economy | surges cost less | surge chevron with a minus MP pip |
| Mycotoxic Halo | toxins gain adjacent-kill chance | toxin with halo ring touching four orthogonal cells marked dying |
| Mycotoxic Lash | new toxin may kill one adjacent enemy | toxin drop with a lash stroke striking one enemy cell |
| Retrograde Bloom | lose T1 levels, gain a T5 level | rung ladder: bottom rungs losing pips, top rung blooming |
| Aegis Hyphae | first N grown cells each round are resistant | growth arrow, first cells with shield notches, later ones plain |
| Saprophage Ring | cells dying beside resistant cells leave empty tiles | dead cell beside a resistant cell being consumed into a dotted empty tile |
| Marginal Clamp | clears enemy/toxin threats bordering you on the crust | crust band, friendly cell, adjacent enemy cell and toxin with clamp brackets and X |
| Apical Yield | maxing a mutation grants MP | full mutation bar with MP pips bursting from the top |
| Crustal Callus | cells on the crust become resistant | crust band with edge cells gaining shield notches |
| Distal Spore | resistant cell lands in the far corner | spore in one corner, long dashed arc, resistant cell in the opposite corner |
| Ascus Primacy | always draft first | stack of draft cards with the top card raised and a "first" crown pip |
| Spore Salvo | toxin beside each enemy spore at game start | central spore firing three toxin diamonds landing beside three enemy spores |
| Hyphal Bridge | 4 cells in a straight line toward the nearest enemy spore | friendly and enemy spores with four evenly spaced cells between them |
| Vesicle Burst | expired toxins may pop into neighbours | toxin with burst rays into adjacent tiles |
| Rhizomorphic Hunger | growth bonus into nutrient patches, patch counts as bigger | nutrient patch with root arrows converging and a +1 dotted tile |
| Mycelial Crescendo | a random surge activates free on set rounds | surge chevron with two sparkle beats |
| Ossified Advance | resistant cells grow better orthogonally | resistant cell with four bold orthogonal arrows |
| Conidia Ascent | 3x3 block dies, 2x2 colony lands elsewhere | 3x3 block dashed/dissolving, arc to a fresh 2x2 block |
| Hyphal Priming | a random Tier 2 mutation gains free levels | tier-2 rung with two free pips stamped in |
| Tropic Lysis | after drafting, clear everything near your spore/beacon | spore with dashed radius ring; tiles inside swept clear |
| Prime Pulse | one of three MP payouts on one of three rounds | timeline with three pulse beats, one lit with MP pips |
| Hyphal Echo | surges last longer | surge chevron with echo rings |
| Oblique Filament | diagonal growth up, orthogonal down | centre cell, bold diagonal arrows, faded orthogonal arrows |
| Thanatrophic Rebound | first N deaths reclaim as resistant | dying cell (X) with a bounce arrow back into a resistant cell |
| Toxin Primacy | start with Mycotoxin Tracer levelled | tracer toxin motif with prefilled level pips |
| Centripetal Germination | spore starts closer to centre | mini-board with spore shifted inward and an arrow toward centre |
| Signal Economy | Tier 2 surges cost less | two small surge chevrons with a minus pip |
| Liminal Sporemeal | nutrient patch near the closest edge | mini-board, crust band, nutrient cluster beside the spore's edge |
| Putrefactive Resilience | less likely to be killed by putrefactive toxins | cell with a partial shield deflecting a toxin diamond |
| Compound Reserve | storing MP grants a bonus point | banked MP stack with one bonus pip on top |
