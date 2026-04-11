# Unity Asset Naming Conventions

Use this guide when adding new source assets under `FungusToast.Unity/Assets/`.

## Purpose

Unity asset names should be predictable in the Project view, easy to search, and stable when referenced from code, prefabs, or importer settings.

## Default Rule

For new imported source assets, use lowercase `snake_case` file names.

This applies to:
- sprites and textures
- icon sheets and button graphics
- audio files
- authored VFX source images
- other imported media files that live under `Assets/`

Examples:
- `shield_icon_64x64.png`
- `magnifying_glass_outline_metallic_gray_5px_256x256.png`
- `menu_hamburger.png`
- `skip_track_next.png`

## What This Does Not Change

Do not force this rule onto every asset type in the repo.

Keep existing conventions for:
- C# scripts and class-aligned file names, which should remain `PascalCase`
- established UI prefabs such as `UI_EndGamePanel.prefab`
- legacy ScriptableObject assets and board preset assets that already follow user-facing or inspector-friendly naming

The goal is consistency for new source media, not broad churn across existing Unity assets.

## Core Rules

### 1) Use lowercase ASCII only
- Use `a-z`, `0-9`, and underscores.
- Avoid spaces, hyphens, punctuation, and non-ASCII characters.

### 2) Prefer `snake_case`
- Separate words with underscores.
- Do not mix casing styles inside one file name.

### 3) Name for what the asset is, not where it lives
- Use semantic names that still make sense when seen in search results.
- Avoid repeating folder context unless it adds real clarity.

Good:
- `menu_hamburger.png`
- `skip_track_next.png`

Avoid:
- `ui_button_menu_hamburger.png`
- `buttons_skip_track_next_icon.png`

### 4) Add size or variant suffixes only when needed
- Include dimensions when multiple exported sizes coexist.
- Include variant descriptors when they distinguish look or state.

Good:
- `shield_icon_64x64.png`
- `bread_tile_pressed.png`
- `menu_hamburger_disabled.png`

### 5) Put the most important meaning first
- Start with the feature or object, then the variant or state.
- This keeps related assets grouped naturally in the Project view.

Prefer:
- `menu_hamburger.png`
- `menu_hamburger_hover.png`
- `skip_track_next.png`
- `skip_track_next_disabled.png`

## Folder Guidance

Recommended examples:
- `Assets/Sprites/UI/Buttons/menu_hamburger.png`
- `Assets/Sprites/UI/Buttons/skip_track_next.png`
- `Assets/Sprites/UI/Logos/fungus_toast_logo_words_1001x455.png`

If the folder already provides strong context, keep the file name short and specific.

## Decision Rule For New Files

When naming a new Unity asset:
1. Start with lowercase `snake_case`.
2. Remove redundant folder words.
3. Add variant or dimensions only if they disambiguate real siblings.
4. Keep script, prefab, and type naming on their existing conventions.

## Current Recommendation For The New Button Icons

Use:
- `menu_hamburger.png`
- `skip_track_next.png`
