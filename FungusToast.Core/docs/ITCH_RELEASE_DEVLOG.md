# itch.io Release Devlog Guide

Use this guide to turn each Fungus Toast release into a short, player-facing itch.io post. It complements the build and upload procedure in [BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md); it does not replace release validation or publishing approval.

## Positioning and public voice

These rules apply to every outward-facing surface: devlogs, the itch.io page, screenshots captions, and any feedback outreach. In-game copy is governed by [GAMEPLAY_TERMINOLOGY.md](GAMEPLAY_TERMINOLOGY.md) and [UI_STYLE_GUIDE.md](UI_STYLE_GUIDE.md) section 1 instead.

- **Hook:** `A turn-based strategy roguelite where rival molds compete for every last crumb.` Use this line, or a close paraphrase, whenever the game needs a one-sentence introduction.
- **What it is:** a systems-driven, deliberately quirky simulation. The appeal is watching emergent colony behavior, spending mutation points, adapting a build, and reacting to probabilistic growth, decay, toxins, and territorial pressure.
- **Audience:** players who enjoy roguelikes/roguelites, strategy, simulations, unusual systems, and experimenting with builds. Write to them, not to a general audience.
- **Status:** Fungus Toast is a **beta**. Call it that. Never `demo`, `Early Access`, or `alpha`.
- **Price:** the game is free and is expected to stay free. Do not write copy that hints at a paid release, DLC, or a wishlist-for-launch funnel.
- **Tone:** genuine and feedback-focused, not polished or marketing-ish. No hype adjectives, no unverifiable superlatives, no urgency. It is a long-running personal passion project and should sound like one.
- **Language:** prefer fungal and ecological framing (rival molds competing for territory or scarce carbohydrates) over generic combat framing. Avoid `battle`, `attack`, `fight`, `war`, and `enemy army` when `compete`, `spread`, `colonize`, `overrun`, or `poison` say it better. `Enemy` on its own is fine; it is the in-game term for rivals.

## Goal

Help a returning player answer, in the first few seconds: **what changed, why should I care, and what should I try now?**

An itch.io devlog can also improve project discovery: itch.io says devlog posts are distributed through user email digests and other surfaces, and recommends posting about updates and new features. Keep the post accurate, specific, and useful rather than treating it as a technical changelog.

## Release workflow

1. Confirm the target version in `FungusToast.Unity/version.txt` and the exact baseline commit for the previous public release.
2. Read commits in release order:

   ```bash
   git log --reverse --format='%h%x09%s' <previous-release-commit>..HEAD
   ```

3. Inspect diffs where the subject does not make player impact clear. Do not rely only on commit titles.
4. Rank changes by player value: new or improved play first; then major clarity/usability, visuals, accessibility, performance/stability; finally notable fixes.
5. Draft the post with the template below. Verify every claim against the commits, release validation, or supplied evidence.
6. Pick one strong screenshot, GIF, or short clip if it makes the lead change easier to understand. Do not add media merely to decorate the post.
7. Present the draft for review as **raw Markdown source inside a fenced code block** (```markdown ... ```), not as rendered chat text. itch.io's devlog editor takes Markdown, and rendered output loses the heading and bold markers when copied. Also write the draft to a `.md` file so it can be copied from disk. Never publish it or claim a feature is live without explicit approval.

`FungusToast.Unity/last-deployed-version.txt` stores a version number, not the associated Git commit. Use a release tag, an explicit recorded commit, or ask for the baseline when it is unknown. Future releases should record a tag or commit SHA with the release so this remains deterministic.

## Release provenance and Git tags

Use an **annotated Git tag** as the canonical boundary for every public release. The tag gives the next devlog an unambiguous baseline without another tracking file:

```bash
git tag -a v<version> -m "Fungus Toast <version>"
git push origin v<version>
```

Create and push the tag only after the itch.io upload has succeeded and the release-record commit has been committed. Target that release record when it contains the updated `FungusToast.Unity/version.txt` and `FungusToast.Unity/last-deployed-version.txt`; otherwise target the exact commit used to produce the uploaded build. Do not move an already-pushed tag casually: correcting one requires an explicit decision because existing clones and external references may already rely on it.

For the next release, draft from the previous tag:

```bash
git log --reverse v<previous-version>..HEAD
```

Keep `last-deployed-version.txt`. It remains the deployment guard used by the release script; it is not the devlog baseline. A separate latest-commit file is unnecessary while release tags are maintained. If platforms later ship different versions or different commits, add a small release manifest that records version, platform/channel, commit, publish date, and tag.

## Writing rules

- Keep the usual post to **120-250 words**. A small patch can be shorter.
- Use **ASCII characters only**, and **never an em-dash**. Em-dashes, curly quotes, and typographic ellipses are a dead giveaway that the text was AI-generated. Use a colon, comma, period, or parentheses instead of an em-dash, plain `-` for ranges and hyphens, straight `'` and `"` quotes, and three periods for an ellipsis. Check the draft before presenting it: `LC_ALL=C grep -n '[^ -~]' <draft>` must return nothing.
- Title the player benefit, not the internal work: `Fungus Toast 0.8.0: Clearer colony controls` rather than `HUD refactor complete`.
- Open with the update being live and the biggest outcome for players.
- Use 2-5 bullets, descending by player value. Each says **what changed + why it matters**.
- Translate implementation into experience: "The activity feeds now stay readable while new events arrive," not "preserved ScrollRect state."
- Name specific fixes only when players are likely to have noticed them; group low-impact fixes as "plus smaller fixes and polish."
- Use short paragraphs and ordinary words. Avoid a commit-by-commit dump, unverifiable superlatives, roadmap promises, and engine/code jargon.
- Mention a known issue only if it materially affects the released player experience, and state the practical workaround if one exists.
- End with one answerable question that relates to the release, then invite readers to follow the page for the next update.

### What feedback is worth asking for

The closing question should target one of the things the beta is trying to learn. Pick the one the release most affects:

- Is it fun? What was not fun?
- What was fun and worth expanding?
- What was confusing?
- Did any mutation, Mycovariant, or Adaptation feel weak, unfair, or hard to understand?
- Were the opening turns clear? Was there a reason to play again?

Ask about one of these concretely ("Did the new Substrate Ecology tree give you a reason to try a second run?"), not in the abstract ("Any feedback welcome").

## Template

```markdown
# Fungus Toast <version>: <strongest player benefit>

<Version> is live on itch.io. <One sentence describing the most meaningful player-facing result.>

## What's new

- **<Player-visible improvement>:** <what changed and the practical benefit.>
- **<Second meaningful change>:** <what changed and the practical benefit.>
- **<Notable fix or polish>:** <what is now clearer, smoother, or more reliable.>

<Optional screenshot/GIF with a short, descriptive caption.>

<Optional known issue and workaround, only when needed.>

I'd especially love feedback on <one specific thing a player can try or observe>. If you're enjoying Fungus Toast, follow the page to catch the next update.
```

## Final check

- Does the first sentence stand on its own in a feed or email preview?
- Are the first one or two bullets the reason a player should update?
- Does every bullet explain a player outcome, not merely an implementation task?
- Is there one real visual when the release has a visual feature worth showing?
- Is the feedback ask narrow enough to answer?
- Is every detail supported, with no speculative claims?
- Is the post pure ASCII with no em-dashes?
- Is the draft delivered as raw Markdown in a fenced code block (and saved as a `.md` file)?

## Sources and rationale

- itch.io recommends devlogs for updates and notes that they are distributed through email digests and other discovery surfaces: [Getting indexed on Search & Browse](https://itch.io/docs/creators/getting-indexed).
- itch.io recommends Butler for frequent updates because it uploads/downloads only changed data, matching the repository's release script: [How updates work](https://itch.io/docs/itch/integrating/updates.html).
- The player-outcome, visual, concise, scan-friendly structure is reinforced by this practical game-dev guide: [How to Write a Devlog for Your Indie Game](https://www.checkpointzero.net/blog/how-to-write-a-devlog-for-your-indie-game).

Treat the official itch.io guidance as authoritative for platform behavior. The third-party article informs presentation choices, not platform policy.
