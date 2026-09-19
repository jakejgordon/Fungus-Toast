# Campaign UX observations

## Remediation workflow

- This file is the canonical issue log for the campaign UX remediation effort.
- Refer to observations by their stable three-digit identifiers (`001` through `024`).
- When work begins on an observation, record its status as `In progress`. When implementation and verification are complete, record `Fixed`, the relevant files or commit, and the verification evidence. Use `Deferred` only with a short rationale, and `Rejected` when the proposed changes are intentionally declined.
- Preserve the baseline screenshots in `screenshots/` while any observation remains unresolved so before-and-after behavior can be compared.
- **Completion cleanup:** Once every observation is either verified `Fixed`, intentionally `Deferred`, or intentionally `Rejected`, delete this `observations.md` file and remove the `Campaign UX remediation` section that links to it from the repository-root `AGENTS.md` in the same change. Decide separately whether the baseline screenshots and session README should be retained as historical evidence.

## Session setup

- Build: Beta 1.35.0
- Date: 2026-09-17
- Test posture: resume the existing campaign as if seeing all contextual tooltips for the first time; make reasonable strategic choices; complete up to two games.

## Chronological observations

### 001 — Main-menu Campaign tooltip

**Status: Rejected (2026-09-18).** No changes planned; the existing subtitle and tooltip treatment is acceptable, and the proposed P3 refinements are not worth pursuing.

Screenshot: [001-main-menu-campaign-tooltip.png](screenshots/001-main-menu-campaign-tooltip.png)

- **What happened:** Hovering/focusing Campaign reveals: “Open the campaign menu to resume an existing run or start a new one.”
- **Works well:** Clear verb, clear destination, and explicitly covers both resume and new-run intents.
- **P3 — Competing explanation:** The tooltip repeats much of the always-visible subtitle (“Play a persistent run…”), leaving two adjacent explanatory blocks with similar prominence. Consider making the subtitle benefit-oriented and the tooltip action-oriented, or hiding the subtitle while the tooltip is open.
- **P3 — Tooltip anchoring:** The tooltip starts at the button’s right edge and reads as a detached black rectangle. Add a small pointer/offset and a little more gap so its ownership is instantly legible.

### 002 — Campaign hub / resume screen

**Status: Fixed (2026-09-18).** Commit `3f059ee` — next-reward copy now reports the moldiness level reached after crossing the threshold (`MoldinessProgression.cs`, `MoldinessProgressionTests.cs`), campaign-run levels are labelled `Stage` throughout player-facing UI (`UI_CampaignPanelController.cs`, `UI_EndGamePanel.cs`, `UI_PauseMenuPanel.cs`, `MycovariantDraftController.cs`, `UI_PhaseBanner.cs`), and the hub action stack nests inside the moldiness summary card. Verified in-app by the maintainer on 2026-09-18. The opaque reward-icon concern (P2) was intentionally excluded from this pass.

Screenshot: [002-campaign-hub.png](screenshots/002-campaign-hub.png)

- **What happened:** The hub shows Moldiness Level 3, `4 / 12 to Level 4`, lifetime earnings, reward icons, and a prominent `Resume Campaign (Level 7)` action.
- **Works well:** Resume is the primary, green-highlighted action and includes the campaign level; destructive replacement is visually secondary.
- **P1 — Contradictory progression copy:** The header says the player is already Moldiness Level 3 and progressing to Level 4, while the copy below says `Next reward tier: Level 3...`. This makes it unclear whether Level 3 rewards are pending, current, or already earned. Show `Next reward tier: Level 4` and its rewards, or relabel the row as `Level 3 rewards`.
- **P2 — Two unrelated level systems need names:** `Moldiness Level 3` and `Resume Campaign (Level 7)` appear together without explaining the distinction. Rename the latter to `Resume — Stage 7`, `Run Level 7`, or another term that cannot be confused with account progression.
- **P2 — Reward icons are opaque:** Two small unlocked-reward icons have no labels. A first-time player cannot identify them without exploratory hovering. Add short labels beneath them and retain detail tooltips for depth.
- **P3 — Excess dead space:** The progression block and action block are vertically separated, weakening scan flow at this wide resolution. Consider a centered card or tighter vertical rhythm.

### 003 — Game 1 start / HUD hierarchy

**Status: Deferred (2026-09-18).** Skipped for now at the maintainer's request; revisit after the mutation-tree work in 004.

Screenshot: [003-game-1-start-hud.png](screenshots/003-game-1-start-hud.png)

- **What happened:** The game opens at Round 1 with five points, five players, a large central toast board, mutation choices at left, and scoreboard/timeline at right.
- **Works well:** `Spend 5 Points!` is the clearest call to action and the player row is unmistakably highlighted with both `#1` and `YOU`.
- **P1 — Core loop is not stated in place:** A first-time player sees nine percentage tiles, `Random Decay`, four unexplained common symbols, seven adaptation icons, a mutation→growth→decay timeline, occupancy, toxins, and two collapsed logs at once. The only imperative is to spend points, but there is no one-sentence statement of why a mutation is strategically good now or what ends the game. Add a compact first-round goal/sequence explainer anchored near the timeline.
- **P2 — Important symbols rely on recognition/hover:** Adaptations and common symbols are icon-only. Provide persistent short names in an inspect panel or a pinned “selected item” description so comparison does not require repeated hover travel.
- **P2 — Player color visibility varies on toast:** Several colonies are tiny and low-contrast against the toast photograph, especially yellow/olive. Give each live colony a subtle high-contrast outline or halo that matches the scoreboard swatch.
- **P3 — Competing bottom logs:** `Human Activity Log` and `Global Log` appear as similar collapsed controls at opposite corners. Their difference is not obvious. Consider one log with filter chips, or rename them to clarify local-versus-all-player scope.

### 004 — Mutation tree, first open

**Status: In progress (2026-09-18).** The "purchase is dangerously implicit" P1 is being addressed by making buyable cards read as buttons rather than by adding a prompt, confirmation, or tutorial — with 100–300 points spent per game, a single mislearned point is cheap, and extra teaching content would clutter the tree. First pass in `MutationNodeUI.cs`: buyable cards now carry an imperative label (`BUY` / `UPGRADE` / `ACTIVATE` for surges) where every other state stays descriptive; they lift ~3% on hover and squash ~3% while pressed; and hover fills the card with its full, saturated category hue (darkened 40% so light text stays at or above 4.5:1 — a first cut with dark text on the raw accent measured fine but read as grey against a tree that is otherwise light-on-dark) instead of a faint blend. A hand cursor is now in (2026-09-19, commits `a026b81` and `39dca1d`): `CursorManager` raycasts the pointer through the EventSystem each frame and shows the generated pointing-hand cursor only over an interactable `Selectable`, so buyable cards get the hand and locked/unaffordable/maxed cards (whose `upgradeButton.interactable` is already off) do not - the missing hand is itself the "can't buy" signal. Cursor assets and generator: `tools/cursor-gen/generate_cursors.py`, `Assets/Resources/UI/Cursors/`, `Assets/Editor/CursorTextureImporter.cs`; in-editor verification of the hand over the tree is pending the maintainer's pass. The `Store Mutation Points` jargon P2 is addressed (2026-09-19): the concept is renamed to *Bank* everywhere (identifiers, coachmark id and PlayerPrefs seen-key, scene object names, the `sfx_ui_mutation_bank_points_01.wav` clip, docs, and the Latent Polymorphism / Compound Reserve / player-inspector copy), and the button now leads with its consequence - label `Bank Points & End Turn`, hover tooltip "Ends your mutation phase now. Your unspent points carry over to next round, so you can save up for a mutation you can't afford yet.", and the coachmark body rewritten to match. Renaming the seen-key means existing profiles see the reworded coachmark once more. No confirmation step was added, for the same reason as the purchase affordance. The Time-Lapse placement P2 is addressed (2026-09-19, pending the maintainer's in-editor pass): the `Time-Lapse: On/Off` button and its header slot are gone from the tree; the control is now a `Pace: Normal` / `Pace: Time-Lapse` toggle (clock icon, selected tint when on) in a new top row of the right sidebar, with the pause-menu button at the row's right end and the phase timeline pushed to the second row so it is never clipped by corner chrome. The `TimeLapseModeIntro` coachmark hangs below that toggle from round 4 (the `TimeLapseCarriedOverIntro` variant is removed - the toggle shows its state on the board from the first frame, which is what that coachmark existed to compensate for). Built in `UI_PauseMenuPanel` (toggle, coachmark), `UI_RightSideBar.EnsureTopControlsRow`, `CoachmarkLayoutUtility.BuildCard` (shared card builder), `GameUIManager.PaceToggleButtonIcon`. Remaining bullets (choice overload, detail-pane scanning, search/pin discoverability) are not yet addressed.

Screenshot: [004-mutation-tree-first-open.png](screenshots/004-mutation-tree-first-open.png)

- **What happened:** Clicking `Spend 5 Points!` opens a full-screen mutation tree. Mycelial Bloom is preselected; its overview, technical details, cost, current/next level, requirements, and direct unlocks appear in the left rail.
- **Works well:** The selected card and category use consistent green accents; the current-versus-next effect comparison is unusually explicit and helpful; locked cards expose their costs and prerequisites rather than hiding the future tree.
- **P1 — Purchase is dangerously implicit:** The instructional footer says purchases are immediate on mutation cards, but it is low on the far-left rail and below a long list. Cards look like selectors, not purchase buttons. Add a distinct `Buy` affordance, a first-purchase confirmation/tutorial, or at minimum strong hover text such as `Click to buy 1 level`.
- **P1 — Choice overload on first open:** Nearly the entire tree is visible before the player understands the five available roots. De-emphasize unavailable descendants by default, offer an `Available now` filter, or open in a recommended-roots view.
- **P2 — “Store Mutation Points” is jargon-heavy:** It is unclear whether this banks points across rounds, across games, or merely closes the screen without spending. Add a sublabel/tooltip explaining persistence and tradeoff.
- **P2 — Time-Lapse placement implies mutation behavior:** `Time-Lapse: On` appears alongside mutation-screen actions, although it likely controls simulation pacing. Put speed controls in the persistent HUD and use a familiar speed/pause segmented control.
- **P2 — Detail pane is hard to scan:** Long prose, technical stats, requirements, and seven direct-unlock buttons share equal visual weight. Lead with a one-line effect, then surface cost/current/next in a compact comparison; collapse technical details and downstream unlocks.
- **P3 — Search/pin discoverability:** The search field is visually faint and `Pin` does not explain what is pinned or where it remains visible. Use a search icon/placeholder (`Search mutations`) and a standard pin icon with tooltip.

### 005 — Music-skip tooltip

**Status: In progress (2026-09-19).** The P2 "simulation speed is hard to find" bullet is addressed together with 004's Time-Lapse placement: the Next Track button is removed from the gameplay HUD (it remains in the pause menu, so the only skip-forward glyph on the board is gone) and the pace toggle now sits at the top-left of the right sidebar with an explicit `Pace:` label. The track-name P2 is addressed (2026-09-19, pending the maintainer's in-editor pass): each gameplay clip now has a `MusicTrack` ScriptableObject (`Resources/Audio/MusicTracks/`) carrying a proper title and track number, and `MusicTrackCatalog` resolves the clip to `Track 5 - Break From the Mold` for the tooltip's `Current:` / `Next:` lines; a clip with no asset falls back to a title derived from the `track_XX_` filename rather than leaking the key. Metadata lives on an asset rather than being parsed from filenames because filenames cannot carry apostrophes or article casing (`fun_guses_lament` -> "Fun Gus's Lament"). Artist/collection was not added. The tooltip-placement (P3) bullet is not yet addressed.

Screenshot: [005-music-skip-tooltip.png](screenshots/005-music-skip-tooltip.png)

- **What happened:** I interpreted the top-right double-triangle icon as game speed. Its tooltip revealed that it skips music and displayed the current/next track identifiers.
- **Works well:** The tooltip immediately corrected the icon ambiguity and states the effect before repeated use.
- **P2 — Internal identifiers leak into player-facing copy:** `track_07_fungus_amongus` and `track_02_spororiffic_lounge` read like asset keys, not finished track titles. Display title-cased names (`Fungus Amongus`, `Spororiffic Lounge`) and optionally the artist/collection.
- **P2 — Simulation speed is hard to find:** The only obvious fast-forward glyph is a music control. If game speed exists, keep it visually grouped with phase/round controls and use `1× / 2× / 4×`; if not, avoid a speed-like icon in the same HUD region.
- **P3 — Tooltip blocks primary status:** The large tooltip overlays the mutation/growth/decay timeline and round/occupancy status. Place it below-left of the icon without covering the status stack, or constrain its width.

## Gameplay checkpoint

- Game 1, Round 3: growth-first opening reached Mycelial Bloom 10. Player is rank #1 with 3 living cells; the flow automatically returned to the board after spending the final point.

### 006 — Time-Lapse tutorial timing

**Status: In progress (2026-09-19).** P1 and P2 are addressed by the 004 pace-toggle move: the coachmark now appears on the board at the start of the round-4 mutation phase (after the round-2 scoreboard and round-3 adaptation hints, before the round-6 bank-points one) and is anchored directly below the toggle it describes, so it no longer waits for a tree visit or floats away from its control. Switching Time-Lapse on retires it. The responsive-state (P2) and blank-transition (P3) bullets are not yet addressed.

Screenshot: [006-time-lapse-tutorial.png](screenshots/006-time-lapse-tutorial.png)

- **What happened:** On opening mutations in Round 5, a first-time tooltip finally explains that Time-Lapse skips most animations and can be toggled for faster Growth/Decay phases.
- **Works well:** The copy answers both “what does it do?” and “when should I use it?” in three short sentences; the close button is obvious.
- **P1 — Tutorial arrives after the need:** The player has already waited through four rounds and previously encountered `Time-Lapse: On` without explanation. Show this on the first mutation-tree visit or the first time the pace control is visible.
- **P2 — Weak visual association:** The tutorial floats at the far right and does not point to or highlight the `Time-Lapse: On` control near the top center-right. Anchor the callout to the control and dim unrelated content.
- **P2 — Full-width responsive state is disorienting:** With a different selected mutation, the detail panel moves from the left rail to the right, shifting the search and tree while the tutorial is open. Keep navigation and detail placement stable between selections.
- **P3 — Brief blank transition:** Immediately after opening the mutation tree, one capture showed a near-empty dark frame for roughly one second before content appeared. Preserve the prior screen under a loading veil or fade directly between populated layouts.

### 007 — Apical Yield bonus feedback

Screenshot: [007-apical-yield-bonus-feedback.png](screenshots/007-apical-yield-bonus-feedback.png)

- **What happened:** Finishing Mutator Phenotype triggered `+2 Points!` and a centered banner, `Apical Yield grants 2 mutation points!`, leaving two spendable points instead of automatically returning to the board.
- **Works well:** The bonus is immediately attributed to its source; the remaining-points counter and still-available cards make the next action recoverable.
- **P2 — Reward interrupts the expected exit rule:** Previous visits returned to the board as soon as the normal five points were exhausted. A late bonus keeps the player in the tree, but there is no explicit `2 points remaining — spend or store` prompt. Add a short secondary action cue beneath the banner.
- **P2 — Banner obscures navigation:** The reward banner covers the category headers and central dependency path. Move transient rewards below the top bar or use a compact toast in unused space.
- **P3 — Max state is clear but visually noisy:** `MAX`, `FULLY UPGRADED`, a filled card meter, and `No further levels available` all convey the same status. Keep one strong card state and one detail-pane sentence.

### 008 — Board cell hover inspector

Screenshot: [008-board-cell-hover-inspector.png](screenshots/008-board-cell-hover-inspector.png)

- **What happened:** Hovering a live red cell shows a large magnifier ring and a translucent panel with `Status`, `Source`, `Owner`, and `Age`.
- **Works well:** The magnifier is valuable at this board scale, and the inspector exposes provenance (`Hyphal Autogrowth`) that would otherwise be invisible.
- **P1 — Inspector text fails contrast:** Pale gray text sits on a highly translucent brown panel over bright toast, making the four most important values difficult to read. Use an opaque/darker surface and WCAG-compliant foreground contrast.
- **P2 — Magnifier and panel obscure context:** The lens covers neighboring cells while the panel stretches across the same local region. Offset the panel outside the lens, and allow a compact inspect mode when only basic cell state is needed.
- **P2 — Color alone is insufficient inside the lens:** Player ownership is still conveyed mainly by mold color. Add the player number/name and a matching outline/badge inside the magnified view.

### 009 — Store Mutation Points tutorial

Screenshot: [009-store-mutation-points-tutorial.png](screenshots/009-store-mutation-points-tutorial.png)

- **What happened:** In Round 6, the tutorial finally explains that `Store Mutation Points` ends the turn immediately and banks points for the next round, specifically for unaffordable mutations.
- **Works well:** The wording resolves all three ambiguities: whether it ends the turn, whether points persist, and why the action is useful.
- **P1 — Explanation is delayed:** The control has been present since Round 1 and is strategically relevant as soon as cards cost more than the player’s current balance. Show this on first mutation-tree open, or on first selection of a card the player cannot afford.
- **P2 — Hidden cost deserves emphasis:** `Immediately end your turn` is the most consequential part of the action but appears mid-sentence. Put it in the button label/confirmation (`Bank points & end mutation phase`) or lead the tooltip with it.
- **P3 — Tutorial sequence lacks a visible cadence:** Time-Lapse appeared in Round 5 and stored points in Round 6 without a progress indicator or reason for staggered timing. If intentionally paced, show `Tip 2 of N` or trigger by first relevance instead of round count.

### 010 — Scout Your Rivals tutorial (Round 8)

Screenshot: [010-scout-your-rivals-tutorial.png](screenshots/010-scout-your-rivals-tutorial.png)

- **What happened:** A first-time tooltip appeared over the board explaining that scoreboard colony icons can be hovered, clicked to pin, and then inspected for adaptations and mycovariants.
- **Works well:** The instruction is concrete and explains the full hover → pin → inspect interaction chain. The tooltip appears adjacent to the scoreboard it describes.
- **P1 — High-value comparison arrives late:** This feature is introduced at Round 8, after seven spending decisions. Earlier access would make competitive decisions more informed.
- **P2 — The callout obstructs its subject:** It covers a meaningful portion of both the playfield and scoreboard. Anchor it to one highlighted rival icon or use a short guided interaction.
- **P2 — Benefit remains vague:** “What that colony has been up to” is friendly but does not preview the decision-useful categories revealed after pinning. Name or preview them in the callout.

### 011 — Round 15 mycovariant draft

Screenshot: [011-round-15-mycovariant-draft.png](screenshots/011-round-15-mycovariant-draft.png)

- **What happened:** The first draft opened automatically, showed the complete pick order in a persistent feed, and offered three cards while a tutorial explained passive versus one-time effects.
- **Works well:** `Your Turn to Draft a Mycovariant!` is unambiguous; the board and rank remain visible for context; the feed makes the changing pool and earlier choices legible.
- **P1 — Card copy can be visibly truncated:** `Chemotactic Mycotoxins` ends mid-sentence at “next to an” despite a large blank lower half of the card. This prevents an informed choice and makes a rules-heavy selection unsafe. Fix text layout/overflow and add a validation test for every draft card at supported resolutions.
- **P1 — Tutorial overlaps decision content:** The tutorial covers the draft feed and the upper-left of the first option while the player is expected to compare three dense cards. Present the explanation before the options, dock it outside the comparison area, or dismiss it automatically on first interaction.
- **P2 — Excessive empty card space:** Fixed-height cards create long blank regions, weakening scanability and making the clipped first card especially confusing. Fit cards to content or use a consistent summary/details structure.
- **P2 — No explicit selection affordance:** Cards read as informational panels; there is no `Choose` button or hover instruction and no confirmation warning. Add a clear action and, for irreversible picks, a lightweight confirmation state.

### 012 — Unity editor chrome leaks into the player UI

Evidence: [009-store-mutation-points-tutorial.png](screenshots/009-store-mutation-points-tutorial.png) and repeated live mutation-tree observations.

- **P1 — Release-quality and trust issue:** Hovering the mutation UI repeatedly exposes editor-facing tooltips such as `Asset Store — Hold Ctrl to drag and move`, `Toolbar Help`, and `Select editor layout`. These labels are unrelated to gameplay and imply that hidden Unity editor toolbar elements are receiving pointer events in the standalone build.
- **Suggestion:** Remove or disable the editor toolbar layer in player builds, confirm raycast targets and sorting order, and add a smoke test that hovers the full top/search regions in a non-development build.

### 013 — “Direct unlock” hides cross-branch prerequisites

Screenshot: [013-filament-overdrive-cross-branch-prerequisites.png](screenshots/013-filament-overdrive-cross-branch-prerequisites.png)

- **What happened:** Maxing Creeping Mold lists Filament Overdrive under `Direct unlocks`, but Filament Overdrive remains locked. Selecting it reveals three requirements, including Autolytic Surge from the distant Mycelial Surges branch; the highlighted dependency line crosses most of the screen.
- **P1 — Progression language is misleading:** `Direct unlock` reads as an immediate result, so the still-locked card feels broken. The additional prerequisite is outside the active branch and is only revealed after selecting the locked card.
- **Suggestion:** Rename this section to `Leads to` or `One requirement for`, expose the complete requirement set before the predecessor purchase, and summarize unmet requirements directly on the locked card (for example, `Needs Autolytic Surge 1`).

### 014 — Second draft confirms layout issues after the tutorial is gone

Screenshot: [014-round-20-second-draft.png](screenshots/014-round-20-second-draft.png)

- **What happened:** The Round 20 draft appears without the first-time tutorial. All three descriptions fit, confirming that the Round 15 truncation is tied to the tutorial/layout state rather than the underlying card copy.
- **Works well:** The pick order, rival choices, immediate impact summaries, board state, and standings are all visible at once.
- **P2 — Comparison density remains poor:** The cards occupy a tall fixed container but use only their upper third, while long lines and narrow columns make rule text slower to compare. Let cards fit content, widen the comparison area, or use a compact `effect / timing / target` summary.
- **P2 — One-time versus persistent is buried in prose:** This is the highest-level decision axis, but it has no visual treatment once the tutorial is gone. Add a `Passive` or `One-time` badge to each card.

### 015 — Round 25 draft: uneven copy lengths waste comparison space

Screenshot: [015-round-25-third-draft.png](screenshots/015-round-25-third-draft.png)

- **What happened:** The third draft offered cards with very different description lengths, including a short defensive option and a much denser Aggressotropic option.
- **Works well:** The player can now read the three options without a tutorial obscuring them, and the draft feed gives specific, useful summaries of rival picks.
- **P2 — Fixed-height columns undermine comparison:** The short card leaves a very large empty region while the long card becomes a wall of text. Use a compact summary grid with expandable details, or size the choice surface to its content while keeping action buttons aligned.
- **P2 — Selection remains implicit:** Even after three drafts there is no visible `Choose` action, selected state, or reminder that clicking anywhere on a card commits the pick. Add an explicit button or a two-step selected/confirm treatment.
- **P3 — Pick-order strip is too cryptic:** Tiny portraits and arrows communicate sequence only after careful inspection. Add ordinal numbers or a short `You pick 4th` label.

### 016 — Endgame countdown tutorial

Screenshot: [016-endgame-countdown-tutorial.png](screenshots/016-endgame-countdown-tutorial.png)

- **What happened:** At 87.53% occupancy, the game announced a three-round endgame countdown and explained that the player with the most living cells wins.
- **Works well:** This is concise, timely rules copy, and the persistent `Endgame in N rounds` status creates appropriate urgency.
- **P1 — Callout is not view-aware:** The tutorial stays at the upper right when the player opens the mutation tree, obscuring an entire branch, then persists into the draft and results screen. Anchor it within the board HUD, auto-dismiss it after acknowledgment, or reposition it per view.
- **P2 — Countdown and tooltip duplicate each other:** Once the rule is understood, the large callout competes with the smaller persistent status. A one-time modal followed by the compact HUD countdown would preserve the information without ongoing obstruction.

### 017 — Round 30 draft under persistent endgame UI

Screenshot: [017-round-30-draft-endgame-overlap.png](screenshots/017-round-30-draft-endgame-overlap.png)

- **What happened:** The fourth draft opened while the endgame tutorial remained on top of the board and standings.
- **P1 — Stacked modal states compete for attention:** Draft title, pick-order strip, three cards, draft feed, standings, countdown, and the tutorial all remain simultaneously active. The user must decide whether the tutorial, countdown, or draft is the current task.
- **P2 — Draft choices can be near-duplicates:** `Ballistospore Discharge I` and `II` appear side by side with almost identical names and prose; their only meaningful difference is the target count. Emphasize the changed number and tier delta so comparison does not require rereading full sentences.
- **Suggestion:** While a draft is active, suppress nonessential board tutorials and dim secondary HUD panels; restore them after the pick resolves.

### 018 — One-time placement mode and Auto Placement tutorial

**Status: In progress (2026-09-19).** Partial fix in commits `a026b81` and `39dca1d`: entering any board tile-selection mode (`TileSelectionController`, `MultiTileSelectionController`, `MultiCellSelectionController`) now switches the hardware cursor to a target reticle and releases it on completion, cancel, auto-placement and teardown, so the mode is legible from the pointer itself. The magnifier lens and the cell inspector tooltip are intentionally left active during placement - the maintainer wants nearby toxins and mold inspectable while choosing a tile - so the P1 hover-inspection collision is addressed only by the cursor change, not by suppression. Groundwork for the P2 magenta treatment: the hover/selection highlight sprite was a solid-magenta placeholder that swallowed every runtime tint, and is now opaque white so the existing `State.Focus`->`Text.Primary` hover pulse renders as designed; the selectable-tile pulse colours in `UIEffectConstants` (`SelectableTilePulse*`) are still hard-coded magenta and remain open, as does the P1 tutorial stacking. Needs the maintainer's in-editor pass.

Screenshot: [018-auto-placement-multi-tooltip-overlap.png](screenshots/018-auto-placement-multi-tooltip-overlap.png)

- **What happened:** Choosing Ballistospore Discharge II entered a `Select 17 tiles` mode, highlighted many eligible tiles in magenta, and introduced an `Auto Placement` shortcut.
- **Works well:** The required count is explicit and Auto Placement is offered at the exact moment manual selection would be costly.
- **P1 — Interaction modes collide:** Hover inspection remains active during targeting, so a large magnifier and cell-details panel cover selectable tiles. Disable hover inspection during placement or move it behind an explicit inspect modifier.
- **P1 — Tutorial overload:** Auto Placement and Game End tutorials are visible together, on top of the targeting banner, draft feed, standings, and hundreds of magenta markers. Queue first-time messages so only one teaching layer can be active.
- **P2 — Eligible-tile treatment overwhelms the board:** The saturated magenta markers dominate the colony colors and make the board look broken at a glance. Use a subtler outline/overlay and clearly distinguish eligible, selected, and invalid states.

### 019 — Final-round signaling

Screenshot: [019-final-round-status.png](screenshots/019-final-round-status.png)

- **What happened:** The countdown transitioned to a red `Final round!` heading while the mutation-points CTA remained available.
- **Works well:** The wording and red treatment are unmistakable, and current standings remain visible so the player understands the stakes.
- **P2 — No final-action framing:** The left CTA still reads only `Spend 6 Points!`; it does not help the player understand that this is the last chance to affect living-cell rank. Add a contextual sublabel such as `Final mutation phase — spend or bank before the last growth/decay cycle`.

### 020 — Campaign loss / results screen

Screenshots: [020-game-1-results-tooltip-overlap.png](screenshots/020-game-1-results-tooltip-overlap.png) and [021-game-1-results.png](screenshots/021-game-1-results.png)

- **What happened:** Game 1 ended at Level 7. The human placed fourth with 326 alive cells; the results screen offered per-player details, Moldiness progression, board inspection, and a carry-over adaptation action.
- **Works well:** The final ranking and core per-player statistics are available in one table; `Inspect Board` preserves a route back to spatial evidence instead of immediately discarding the match.
- **P1 — Tutorial survives into results:** The endgame callout initially obscures most of Moldiness progression. Gameplay tutorials should be torn down before opening a results modal.
- **P1 — Primary carry-over CTA is truncated:** `Preserve Spores for Next R` visibly cuts off the action text despite ample surrounding space. The user cannot be sure whether this starts a new run, returns to the hub, or merely saves a choice. Allow the label to fit and use explicit copy such as `Choose 1 adaptation to preserve`.
- **P2 — Loss explanation is emotionally clear but diagnostically weak:** `You just weren't moldy enough` does not explain the 289-cell gap to first place or the player's biggest strategic deficit. Add a compact comparison to the winner and one actionable recap derived from the table.
- **P2 — Human row lacks strong identity:** The player's row no longer carries the green `YOU` treatment used throughout the match. Preserve that highlight on the results table.
- **P2 — Progression relationship remains ambiguous:** `Campaign Lost — Level 7` appears beside `Moldiness Level 3`, repeating the two-level-system ambiguity from the campaign hub. Rename one system and state clearly why no Moldiness was gained.

## Game 1 outcome

- Completed: Level 7 campaign game, 31 rounds.
- Result: 4th of 5; 326 alive, 8 resistant, 722 dead, 75 toxins, 200 spent points.
- Winner: AI Player 2 with 615 alive cells.
- Notable UX arc: excellent moment-to-moment rules copy is repeatedly weakened by late timing, persistent cross-screen callouts, implicit irreversible actions, and UI modes that remain active on top of one another.

### 021 — Carry-over adaptation picker

Screenshots: [022-carryover-adaptation-picker.png](screenshots/022-carryover-adaptation-picker.png) and [023-carryover-selected-tooltip.png](screenshots/023-carryover-selected-tooltip.png)

- **What happened:** Continuing after the loss opened a six-option adaptation picker. The screen showed `Selected: 0/1`, and selecting Apical Yield revealed its full effect in a floating tooltip before confirmation.
- **Works well:** The selection limit is explicit, the chosen icon receives a visible state, and the detailed effect is available before commitment.
- **P1 — Icon-only choices make comparison unnecessarily serial:** None of the six adaptations has a persistent name or summary, so the player must probe every icon and remember prior tooltips. Replace the icon row with labeled cards containing a one-line effect; retain hover for technical detail.
- **P2 — Confirmation looks active before the choice is valid:** The green confirmation control has strong primary-action styling even at `Selected: 0/1`. Use a visibly disabled state until exactly one adaptation is selected.
- **P2 — Tooltip obscures the choice set:** The selected adaptation's large tooltip overlaps adjacent options. Put details in a stable side panel so the grid does not move or become covered during comparison.
- **P3 — Weak use of space:** A narrow choice strip occupies the center of a very large results surface. A wider card layout could improve names, summaries, and scanning without increasing modal height.

### 022 — New-campaign setup and carry-over continuity

Screenshot: [024-new-campaign-setup.png](screenshots/024-new-campaign-setup.png)

- **What happened:** After preserving Apical Yield, the setup screen offered Training Level 1 and the Cineramyxa mold, whose starting adaptation is Toxin Primacy.
- **Works well:** Difficulty, opponent count, board preview, and mold identity are brought together before starting, and the primary start action is easy to locate.
- **P1 — Preserved reward is not previewed:** Apical Yield is absent from the setup summary, so the player cannot verify that the previous screen's consequential choice will carry into the next game. Add a `Carry-over adaptation` row with icon, name, and effect.
- **P2 — Difficulty progression is surprising after a Level 7 loss:** The next-run setup foregrounds Training Level 1 without explaining whether higher difficulties are locked, reset, or selectable elsewhere. State the unlock rule and distinguish campaign stage from Moldiness level.
- **P2 — Mold identity still depends on exploration:** The selected mold's strategic identity and starting adaptation should remain persistently visible, rather than relying on hover or transient detail states.

### 023 — Game 2 opening communicates inherited power poorly

Screenshot: [025-game-2-training-start.png](screenshots/025-game-2-training-start.png)

- **What happened:** Game 2 began on a small two-player toast board with Cineramyxa's Toxin Primacy and the preserved Apical Yield adaptation active.
- **Works well:** The simpler board and two-player standings materially reduce the initial visual burden, making the first spending decision easier to parse than Game 1.
- **P2 — Inherited build has no onboarding summary:** The HUD shows the adaptation icons, but it does not explain how the mold's native adaptation and the preserved adaptation combine. Add a brief start-of-run `Your build` panel with both named effects and dismiss it after acknowledgment.
- **P2 — Milestone schedule looks absolute:** `Mycovariant Draft: Round 15` is presented as a guaranteed future event, with no indication that board fill can end the match first. If the event is conditional, label it accordingly or dynamically move it earlier on shorter boards.

### 024 — Short-match milestone contradiction and Game 2 results

Screenshots: [026-game-2-endgame-countdown.png](screenshots/026-game-2-endgame-countdown.png) and [027-game-2-results.png](screenshots/027-game-2-results.png)

- **What happened:** The endgame countdown started in Round 10 and the game ended in Round 12, although the HUD had promised a mycovariant draft in Round 15 throughout the match. The human won with 47 living cells to the AI's 3.
- **Works well:** `Level 1 Cleared` gives the outcome immediate positive framing, the winner row is placed first, the Moldiness gain is quantified, and `Select an Adaptation to continue the campaign` is much clearer than Game 1's truncated carry-over CTA.
- **P1 — Advertised milestone is impossible to reach:** A Round 15 draft cannot occur in a match that ends at Round 12. This breaks planning and makes the timeline untrustworthy. Guarantee one draft before a possible endgame on short boards, calculate the draft round from expected match length, or remove/reschedule the milestone as soon as the countdown makes it unreachable.
- **P2 — Results modal does not fully isolate the completed state:** A sliver of the underlying right-side HUD and countdown text remains visible behind the results surface. Fully cover, dim, or remove gameplay HUD layers when presenting results.
- **P2 — Progress copy could connect cause and reward:** `+1 Moldiness` and `No new threshold crossed this run` are individually understandable, but the relationship between the square progress track, current level, and the next reward remains implicit. Add a direct line such as `5/12 toward Moldiness Level 4 — 7 more required`.

## Game 2 outcome

- Completed: Training Level 1, 12 rounds.
- Result: 1st of 2; 47 alive, 1 resistant, 6 dead, 0 toxins, 60 spent points.
- Opponent: AI Player 1 with 3 alive, 1 resistant, 0 dead, 0 toxins, and 60 spent points.
- Campaign reward: +1 Moldiness; progress reached 5/12 toward the next threshold.
- Notable UX arc: the reduced board and roster improve readability, but the fixed Round 15 draft promise conflicts with occupancy-driven endgame timing and can advertise content that the match can never reach.

## Cross-game priorities

1. **P1 — Enforce one active teaching/modal layer at a time.** Tutorial, draft, placement, hover-inspector, mutation-tree, and results states currently stack and obscure each other.
2. **P1 — Make irreversible actions explicit.** Mutation purchases and mycovariant picks need clear action labels, selected states, and/or lightweight confirmation.
3. **P1 — Make progression and schedules truthful.** Separate campaign stage from Moldiness level, correct reward-tier copy, and never advertise a draft after the projected end of a short match.
4. **P1 — Remove editor-facing UI from player builds.** Unity toolbar tooltips undermine trust and can intercept pointer attention.
5. **P2 — Replace icon-memory tests with persistent summaries.** Adaptations, carry-over choices, and mold starting traits need labels and stable comparison panels.
