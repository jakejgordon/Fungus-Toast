#!/usr/bin/env python3
from __future__ import annotations

import argparse
import hashlib
import math
import re
import struct
import sys
import zlib
from dataclasses import dataclass
from pathlib import Path

import yaml


VISIBLE_ALPHA_THRESHOLD = 1.0 / 255.0
PLAYABLE_SURFACE_TILE_SCALE = 1.01


@dataclass(frozen=True)
class Rect:
    x: float
    y: float
    width: float
    height: float

    @property
    def x_min(self) -> float:
        return self.x

    @property
    def y_min(self) -> float:
        return self.y

    @property
    def x_max(self) -> float:
        return self.x + self.width

    @property
    def y_max(self) -> float:
        return self.y + self.height


@dataclass(frozen=True)
class SpriteImage:
    path: Path
    width: int
    height: int
    alpha_rows: list[list[int]]


@dataclass(frozen=True)
class SpriteMetadata:
    sprite_guid: str
    sprite_path: Path
    has_visible_alpha_bounds: bool
    visible_alpha_bounds: Rect
    has_board_bounds: bool
    board_bounds: Rect
    has_playable_ellipse: bool
    playable_ellipse_center: tuple[float, float]
    playable_ellipse_radii: tuple[float, float]
    has_playable_horizontal_span_profile: bool
    playable_horizontal_span_profile_min_y: float
    playable_horizontal_span_profile_max_y: float
    playable_horizontal_span_profile: list[tuple[float, float, float]]
    baked_blocked_tile_masks: list["BakedBlockedTileMask"]


@dataclass(frozen=True)
class BakedBlockedTileMask:
    board_width: int
    board_height: int
    bake_version: str
    sprite_content_hash: str
    blocked_tile_ids: list[int]


@dataclass(frozen=True)
class BackgroundSettings:
    name: str
    sprite_guid: str
    sprite_path: Path
    derive_blocked_tiles_from_alpha: bool
    alpha_playable_threshold: float
    min_tile_coverage: float
    max_tile_clip_fraction: float
    tile_clip_sample_resolution: int
    use_explicit_blocked_tile_ids: bool
    explicit_blocked_tile_ids: list[int]
    safe_area: Rect
    compose_safe_area_with_board_bounds_metadata: bool
    background_scale_multiplier: float
    metadata: SpriteMetadata | None
    override_description: str
    min_board_width: int
    max_board_width: int
    min_board_height: int
    max_board_height: int


@dataclass(frozen=True)
class ProbeResult:
    width: int
    height: int
    settings: BackgroundSettings
    playable_tiles: int
    blocked_tiles: int
    total_tiles: int
    effective_safe_area: Rect
    effective_area_transparency_fraction: float
    shape_source: str


@dataclass(frozen=True)
class HorizontalSpanStop:
    normalized_y: float
    min_x: float
    max_x: float


@dataclass(frozen=True)
class HorizontalSpanProfile:
    min_y: float
    max_y: float
    stops: list[HorizontalSpanStop]


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate current bread-background metadata and board-size mask behavior."
    )
    parser.add_argument(
        "--repo",
        type=Path,
        default=Path(__file__).resolve().parents[1],
        help="Repo root. Defaults to the current script's repo.",
    )
    parser.add_argument(
        "--full-square-max",
        type=int,
        default=0,
        help="If set, validate every square board from 1..N in addition to the representative probes.",
    )
    parser.add_argument(
        "--emit-baked-mask-sprite",
        type=str,
        default="",
        help="Optional sprite file name, stem, or guid to emit canonical square bounds and baked-mask YAML snippets for.",
    )
    parser.add_argument(
        "--emit-baked-mask-sizes",
        type=str,
        default="",
        help="Comma-separated board sizes for baked-mask emission, for example '85x85,90x90,95x95'.",
    )
    parser.add_argument(
        "--emit-baked-mask-version",
        type=str,
        default="contour-square-v1",
        help="Version label stored with emitted baked masks.",
    )
    args = parser.parse_args()

    repo = args.repo.resolve()
    asset_path = repo / "FungusToast.Unity/Assets/Configs/Toast Configs/ToastBoardMedium.asset"
    sprite_dir = repo / "FungusToast.Unity/Assets/Sprites/UI/Bread Backgrounds"

    sprite_guid_map = build_sprite_guid_map(sprite_dir)
    asset = load_unity_yaml(asset_path)["MonoBehaviour"]
    metadata_by_guid = build_metadata_map(asset, sprite_guid_map)
    default_settings = build_settings(
        asset,
        metadata_by_guid,
        sprite_guid_map,
        "default",
        "default background",
        1,
        10000,
        1,
        10000,
    )
    overrides = [
        build_settings(
            override_data,
            metadata_by_guid,
            sprite_guid_map,
            f"override-{index + 1}",
            describe_override(override_data),
            int(override_data.get("minBoardWidth", 1)),
            int(override_data.get("maxBoardWidth", 10000)),
            int(override_data.get("minBoardHeight", 1)),
            int(override_data.get("maxBoardHeight", 10000)),
        )
        for index, override_data in enumerate(asset.get("boardBackgroundOverrides", []))
    ]

    sprite_images = {
        guid: decode_rgba_png(path)
        for guid, path in sprite_guid_map.items()
        if guid in metadata_by_guid
        or guid == default_settings.sprite_guid
        or any(override.sprite_guid == guid for override in overrides)
    }

    errors: list[str] = []
    notes: list[str] = []

    validate_asset_text(asset_path, errors)
    validate_metadata(metadata_by_guid, sprite_images, errors)
    validate_settings(default_settings, errors)
    for override in overrides:
        validate_settings(override, errors)
    collect_override_notes(default_settings, notes)
    for override in overrides:
        collect_override_notes(override, notes)

    probes = build_probe_sizes(args.full_square_max)
    results = [
        evaluate_probe(width, height, resolve_settings(width, height, default_settings, overrides), sprite_images)
        for width, height in probes
    ]

    validate_probe_results(results, errors)

    print("Board background validation")
    print(f"Asset: {asset_path}")
    print("")
    print("Metadata summary:")
    for guid, metadata in sorted(metadata_by_guid.items(), key=lambda item: item[1].sprite_path.name):
        print(
            f"  {metadata.sprite_path.name}: "
            f"visible={format_rect(metadata.visible_alpha_bounds)} "
            f"board={'none' if not metadata.has_board_bounds else format_rect(metadata.board_bounds)} "
            f"ellipse={'none' if not metadata.has_playable_ellipse else format_ellipse(metadata.playable_ellipse_center, metadata.playable_ellipse_radii)} "
            f"profile={'none' if not metadata.has_playable_horizontal_span_profile else format_horizontal_span_profile(metadata.playable_horizontal_span_profile, metadata.playable_horizontal_span_profile_min_y, metadata.playable_horizontal_span_profile_max_y)} "
            f"baked={'none' if not metadata.baked_blocked_tile_masks else format_baked_mask_summary(metadata.baked_blocked_tile_masks)}"
        )

    print("")
    print("Probe summary:")
    for result in results:
        blocked_fraction = result.blocked_tiles / result.total_tiles if result.total_tiles else 0.0
        print(
            f"  {result.width:>3}x{result.height:<3} "
            f"{result.settings.sprite_path.name:<24} "
            f"blocked={result.blocked_tiles:>5}/{result.total_tiles:<5} "
            f"({blocked_fraction:>6.2%}) "
            f"{result.shape_source:<14} "
            f"{result.settings.override_description}"
        )

    if notes:
        print("")
        print("Notes:")
        for note in notes:
            print(f"  - {note}")

    if args.emit_baked_mask_sprite:
        try:
            requested_sizes = parse_board_sizes(args.emit_baked_mask_sizes)
        except ValueError as exc:
            errors.append(str(exc))
            requested_sizes = []

        if not requested_sizes:
            errors.append("--emit-baked-mask-sizes requires at least one board size such as 85x85,90x90,95x95.")
        else:
            print("")
            print("Baked mask emission:")
            emit_baked_mask_snippet(
                args.emit_baked_mask_sprite,
                requested_sizes,
                args.emit_baked_mask_version,
                metadata_by_guid,
                sprite_images,
                default_settings,
                overrides,
                errors,
            )

    if errors:
        print("")
        print("Errors:")
        for error in errors:
            print(f"  - {error}")
        return 1

    print("")
    print("Validation passed.")
    return 0


def build_probe_sizes(full_square_max: int) -> list[tuple[int, int]]:
    squares = list(range(1, 21))
    squares.extend([21, 25, 30, 35, 40, 41, 50, 60, 70, 80, 81, 85, 90, 95, 99, 100, 101, 120, 140, 160, 180, 200])
    if full_square_max > 0:
        squares.extend(range(1, full_square_max + 1))

    probes = {(size, size) for size in squares if size > 0}
    probes.update(
        {
            (10, 15),
            (15, 10),
            (20, 30),
            (30, 20),
            (40, 60),
            (60, 40),
            (100, 120),
            (120, 100),
            (160, 120),
            (120, 160),
        }
    )
    return sorted(probes)


def load_unity_yaml(path: Path) -> dict:
    text = path.read_text(encoding="utf-8")
    cleaned_lines = [
        line
        for line in text.splitlines()
        if not line.startswith("%") and not line.startswith("--- !u!")
    ]
    return yaml.safe_load("\n".join(cleaned_lines))


def validate_asset_text(asset_path: Path, errors: list[str]) -> None:
    text = asset_path.read_text(encoding="utf-8")
    if re.search(r"blockedTileIds:\s*\n\s*\[", text):
        errors.append(
            "ToastBoardMedium.asset stores baked blockedTileIds using an inline flow list. "
            "Use Unity-style block YAML (`blockedTileIds:` followed by `- <tileId>` lines) so Unity deserializes the masks reliably."
        )


def build_sprite_guid_map(sprite_dir: Path) -> dict[str, Path]:
    mapping: dict[str, Path] = {}
    for meta_path in sorted(sprite_dir.glob("*.png.meta")):
        guid = None
        for line in meta_path.read_text(encoding="utf-8").splitlines():
            if line.startswith("guid: "):
                guid = line.split(": ", 1)[1].strip()
                break
        if guid:
            mapping[guid] = meta_path.with_suffix("")
    return mapping


def build_metadata_map(asset: dict, sprite_guid_map: dict[str, Path]) -> dict[str, SpriteMetadata]:
    metadata_by_guid: dict[str, SpriteMetadata] = {}
    for entry in asset.get("boardBackgroundSpriteMetadata", []):
        sprite_guid = entry["backgroundSprite"]["guid"]
        sprite_path = sprite_guid_map[sprite_guid]
        metadata_by_guid[sprite_guid] = SpriteMetadata(
            sprite_guid=sprite_guid,
            sprite_path=sprite_path,
            has_visible_alpha_bounds=bool(entry.get("hasVisibleAlphaBounds", False)),
            visible_alpha_bounds=rect_from_yaml(entry.get("visibleAlphaBoundsNormalized")),
            has_board_bounds=bool(entry.get("hasBoardBounds", False)),
            board_bounds=rect_from_yaml(entry.get("boardBoundsNormalized")),
            has_playable_ellipse=bool(entry.get("hasPlayableEllipse", False)),
            playable_ellipse_center=vector2_from_yaml(entry.get("playableEllipseCenterNormalized"), (0.5, 0.5)),
            playable_ellipse_radii=vector2_from_yaml(entry.get("playableEllipseRadiiNormalized"), (0.5, 0.5)),
            has_playable_horizontal_span_profile=bool(entry.get("hasPlayableHorizontalSpanProfile", False)),
            playable_horizontal_span_profile_min_y=float(entry.get("playableHorizontalSpanProfileMinYNormalized", 0.0)),
            playable_horizontal_span_profile_max_y=float(entry.get("playableHorizontalSpanProfileMaxYNormalized", 1.0)),
            playable_horizontal_span_profile=horizontal_span_profile_from_yaml(entry.get("playableHorizontalSpanProfile")),
            baked_blocked_tile_masks=baked_blocked_tile_masks_from_yaml(entry.get("bakedBlockedTileMasks")),
        )
    return metadata_by_guid


def build_settings(
    data: dict,
    metadata_by_guid: dict[str, SpriteMetadata],
    sprite_guid_map: dict[str, Path],
    name: str,
    override_description: str,
    min_board_width: int,
    max_board_width: int,
    min_board_height: int,
    max_board_height: int,
) -> BackgroundSettings:
    sprite_guid = data["backgroundSprite"]["guid"]
    sprite_path = sprite_guid_map[sprite_guid]
    return BackgroundSettings(
        name=name,
        sprite_guid=sprite_guid,
        sprite_path=sprite_path,
        derive_blocked_tiles_from_alpha=bool(data.get("deriveBlockedTilesFromBackgroundAlpha", False)),
        alpha_playable_threshold=float(data.get("backgroundAlphaPlayableThreshold", 0.1)),
        min_tile_coverage=float(data.get("backgroundMinTileCoverage", 0.0)),
        max_tile_clip_fraction=float(data.get("backgroundMaxTileClipFraction", 0.0)),
        tile_clip_sample_resolution=int(data.get("backgroundTileClipSampleResolution", 3)),
        use_explicit_blocked_tile_ids=bool(data.get("useExplicitBlockedTileIds", False)),
        explicit_blocked_tile_ids=[int(tile_id) for tile_id in data.get("explicitBlockedTileIds", [])],
        safe_area=build_safe_area(
            float(data.get("backgroundInsetLeftNormalized", 0.0)),
            float(data.get("backgroundInsetRightNormalized", 0.0)),
            float(data.get("backgroundInsetBottomNormalized", 0.0)),
            float(data.get("backgroundInsetTopNormalized", 0.0)),
        ),
        compose_safe_area_with_board_bounds_metadata=bool(data.get("composeSafeAreaWithBoardBoundsMetadata", False)),
        background_scale_multiplier=float(data.get("backgroundScaleMultiplier", 1.0)),
        metadata=metadata_by_guid.get(sprite_guid),
        override_description=override_description,
        min_board_width=min_board_width,
        max_board_width=max_board_width,
        min_board_height=min_board_height,
        max_board_height=max_board_height,
    )


def describe_override(data: dict) -> str:
    min_width = int(data.get("minBoardWidth", 1))
    min_height = int(data.get("minBoardHeight", 1))
    max_width = int(data.get("maxBoardWidth", 10000))
    max_height = int(data.get("maxBoardHeight", 10000))
    return f"size band [{min_width}..{max_width}]x[{min_height}..{max_height}]"


def resolve_settings(
    width: int,
    height: int,
    default_settings: BackgroundSettings,
    overrides: list[BackgroundSettings],
) -> BackgroundSettings:
    for override in overrides:
        if (
            override.min_board_width <= width <= override.max_board_width
            and override.min_board_height <= height <= override.max_board_height
        ):
            return override
    return default_settings


def validate_metadata(
    metadata_by_guid: dict[str, SpriteMetadata],
    sprite_images: dict[str, SpriteImage],
    errors: list[str],
) -> None:
    for guid, metadata in sorted(metadata_by_guid.items(), key=lambda item: item[1].sprite_path.name):
        image = sprite_images[guid]
        measured = measure_visible_alpha_bounds(image)
        if metadata.has_visible_alpha_bounds:
            tolerance_x = 1.0 / image.width
            tolerance_y = 1.0 / image.height
            if not rects_close(metadata.visible_alpha_bounds, measured, tolerance_x, tolerance_y):
                errors.append(
                    f"{metadata.sprite_path.name} visibleAlphaBoundsNormalized mismatch: "
                    f"serialized={format_rect(metadata.visible_alpha_bounds)} measured={format_rect(measured)}"
                )

        if metadata.has_board_bounds:
            board_bounds = sanitize_rect(metadata.board_bounds)
            if (
                board_bounds.x_min < -0.0001
                or board_bounds.y_min < -0.0001
                or board_bounds.x_max > 1.0001
                or board_bounds.y_max > 1.0001
            ):
                errors.append(
                    f"{metadata.sprite_path.name} boardBoundsNormalized escapes normalized sprite bounds: "
                    f"board={format_rect(board_bounds)}"
                )

            contour_square_masks = [mask for mask in metadata.baked_blocked_tile_masks if mask.bake_version.startswith("contour-square")]
            if contour_square_masks:
                expected_square_bounds = build_square_board_bounds(
                    metadata.visible_alpha_bounds if metadata.has_visible_alpha_bounds else measured,
                    image,
                )
                tolerance_x = 1.0 / image.width
                tolerance_y = 1.0 / image.height
                if not rects_close(board_bounds, expected_square_bounds, tolerance_x, tolerance_y):
                    errors.append(
                        f"{metadata.sprite_path.name} contour-square boardBoundsNormalized mismatch: "
                        f"serialized={format_rect(board_bounds)} expected={format_rect(expected_square_bounds)}"
                    )

        if metadata.has_playable_ellipse:
            center, radii = sanitize_ellipse(metadata.playable_ellipse_center, metadata.playable_ellipse_radii)
            ellipse_bounds = build_ellipse_bounds(center, radii)
            if ellipse_bounds.x_min < -0.0001 or ellipse_bounds.y_min < -0.0001 or ellipse_bounds.x_max > 1.0001 or ellipse_bounds.y_max > 1.0001:
                errors.append(
                    f"{metadata.sprite_path.name} playable ellipse escapes normalized sprite bounds: "
                    f"ellipse={format_ellipse(center, radii)}"
                )

        if metadata.has_playable_horizontal_span_profile:
            profile = sanitize_horizontal_span_profile(
                metadata.playable_horizontal_span_profile_min_y,
                metadata.playable_horizontal_span_profile_max_y,
                metadata.playable_horizontal_span_profile,
            )
            if profile is None or not profile.stops:
                errors.append(f"{metadata.sprite_path.name} playable horizontal span profile is enabled but empty.")
            if profile is not None and (profile.min_y < -0.0001 or profile.max_y > 1.0001 or profile.min_y > profile.max_y):
                errors.append(
                    f"{metadata.sprite_path.name} horizontal span profile has invalid y bounds: "
                    f"minY={profile.min_y:.6f} maxY={profile.max_y:.6f}"
                )
            for stop in ([] if profile is None else profile.stops):
                if stop.normalized_y < -0.0001 or stop.normalized_y > 1.0001:
                    errors.append(f"{metadata.sprite_path.name} horizontal span stop has invalid normalizedY={stop.normalized_y:.6f}.")
                if stop.min_x < -0.0001 or stop.max_x > 1.0001 or stop.min_x > stop.max_x:
                    errors.append(
                        f"{metadata.sprite_path.name} horizontal span stop is invalid: "
                        f"y={stop.normalized_y:.6f} span=({stop.min_x:.6f}, {stop.max_x:.6f})"
                    )

        baked_mask_sizes: set[tuple[int, int]] = set()
        sprite_content_hash = compute_sprite_content_hash(image)
        for baked_mask in metadata.baked_blocked_tile_masks:
            if baked_mask.board_width <= 0 or baked_mask.board_height <= 0:
                errors.append(
                    f"{metadata.sprite_path.name} baked blocked-tile mask has invalid size {baked_mask.board_width}x{baked_mask.board_height}."
                )
                continue

            size_key = (baked_mask.board_width, baked_mask.board_height)
            if size_key in baked_mask_sizes:
                errors.append(
                    f"{metadata.sprite_path.name} has duplicate baked blocked-tile masks for {baked_mask.board_width}x{baked_mask.board_height}."
                )
            baked_mask_sizes.add(size_key)
            if baked_mask.sprite_content_hash and baked_mask.sprite_content_hash != sprite_content_hash:
                errors.append(
                    f"{metadata.sprite_path.name} baked blocked-tile mask {baked_mask.board_width}x{baked_mask.board_height} "
                    f"hash mismatch: serialized={baked_mask.sprite_content_hash} expected={sprite_content_hash}"
                )


def validate_settings(settings: BackgroundSettings, errors: list[str]) -> None:
    metadata = settings.metadata
    if (
        metadata
        and metadata.has_visible_alpha_bounds
        and metadata.has_board_bounds
        and settings.derive_blocked_tiles_from_alpha
        and not settings.compose_safe_area_with_board_bounds_metadata
    ):
        visible_area = metadata.visible_alpha_bounds.width * metadata.visible_alpha_bounds.height
        board_area = metadata.board_bounds.width * metadata.board_bounds.height
        if visible_area > 0:
            retained_area_fraction = board_area / visible_area
            if retained_area_fraction < 0.8:
                errors.append(
                    f"{settings.sprite_path.name} {settings.override_description}: boardBoundsNormalized keeps only "
                    f"{retained_area_fraction:.2%} of the visible alpha area while alpha masking is enabled; "
                    "this forces a conservative inner rectangle instead of filling the bread silhouette."
                )


def collect_override_notes(settings: BackgroundSettings, notes: list[str]) -> None:
    zero_safe_area = build_safe_area(0.0, 0.0, 0.0, 0.0)
    if settings.metadata and settings.metadata.has_board_bounds and not settings.compose_safe_area_with_board_bounds_metadata:
        if settings.safe_area != zero_safe_area:
            notes.append(
                f"{settings.sprite_path.name} {settings.override_description}: boardBoundsNormalized is active, "
                "so configured insets are ignored unless safe-area composition is enabled."
            )
        if settings.metadata.has_visible_alpha_bounds and not rect_contains(
            settings.metadata.visible_alpha_bounds,
            settings.metadata.board_bounds,
            0.0,
            0.0,
        ):
            if settings.metadata.has_playable_horizontal_span_profile:
                notes.append(
                    f"{settings.sprite_path.name} {settings.override_description}: boardBoundsNormalized extends past visible alpha bounds and relies on the authored horizontal-span profile to trim the overhang."
                )
            else:
                notes.append(
                    f"{settings.sprite_path.name} {settings.override_description}: boardBoundsNormalized extends past visible alpha bounds and relies on alpha masking to trim the overhang."
                )
    if not math.isclose(settings.background_scale_multiplier, 1.0, rel_tol=0.0, abs_tol=0.0001):
        notes.append(
            f"{settings.sprite_path.name} {settings.override_description}: backgroundScaleMultiplier={settings.background_scale_multiplier:.3f}; "
            "keep visually verifying this because it changes render framing and mask derivation together."
        )
    if settings.metadata and settings.metadata.baked_blocked_tile_masks:
        notes.append(
            f"{settings.sprite_path.name} {settings.override_description}: baked blocked-tile masks are present; validation prefers those exact masks when a size match exists."
        )


def evaluate_probe(
    width: int,
    height: int,
    settings: BackgroundSettings,
    sprite_images: dict[str, SpriteImage],
) -> ProbeResult:
    image = sprite_images[settings.sprite_guid]
    effective_safe_area = get_effective_safe_area(settings, width, height)
    effective_ellipse = get_effective_playable_ellipse(settings)
    effective_horizontal_span_profile = get_effective_playable_horizontal_span_profile(settings)
    clip_offsets = build_clip_budget_sample_offsets(
        PLAYABLE_SURFACE_TILE_SCALE,
        settings.max_tile_clip_fraction,
        settings.tile_clip_sample_resolution,
    )

    blocked_tiles = 0
    explicit_blocked_tile_ids = sanitize_explicit_blocked_tile_ids(settings.explicit_blocked_tile_ids, width, height) if settings.use_explicit_blocked_tile_ids else set()
    baked_blocked_tile_ids = get_matching_baked_blocked_tile_ids(settings.metadata, width, height)
    alpha_threshold = clamp01(settings.alpha_playable_threshold)
    minimum_tile_coverage = clamp01(settings.min_tile_coverage)
    for tile_y in range(height):
        for tile_x in range(width):
            tile_id = (tile_y * width) + tile_x
            if tile_id in explicit_blocked_tile_ids or tile_id in baked_blocked_tile_ids:
                blocked_tiles += 1
                continue

            if baked_blocked_tile_ids:
                satisfies_clip_budget = True
                satisfies_coverage = True
            elif effective_horizontal_span_profile is not None:
                satisfies_clip_budget = (
                    not clip_offsets
                    or evaluate_tile_horizontal_span_profile_clip_budget(
                        effective_horizontal_span_profile,
                        width,
                        height,
                        tile_x,
                        tile_y,
                        clip_offsets,
                    )
                )
                satisfies_coverage = (
                    minimum_tile_coverage <= 0.0
                    or evaluate_tile_horizontal_span_profile_coverage(
                        effective_horizontal_span_profile,
                        width,
                        height,
                        tile_x,
                        tile_y,
                        minimum_tile_coverage,
                    )
                )
            elif effective_ellipse is not None:
                satisfies_clip_budget = (
                    not clip_offsets
                    or evaluate_tile_ellipse_clip_budget(
                        effective_ellipse,
                        width,
                        height,
                        tile_x,
                        tile_y,
                        clip_offsets,
                    )
                )
                satisfies_coverage = (
                    minimum_tile_coverage <= 0.0
                    or evaluate_tile_ellipse_coverage(
                        effective_ellipse,
                        width,
                        height,
                        tile_x,
                        tile_y,
                        minimum_tile_coverage,
                    )
                )
            else:
                satisfies_clip_budget = (
                    not clip_offsets
                    or evaluate_tile_clip_budget(
                        image,
                        effective_safe_area,
                        width,
                        height,
                        tile_x,
                        tile_y,
                        alpha_threshold,
                        clip_offsets,
                    )
                )
                satisfies_coverage = (
                    minimum_tile_coverage <= 0.0
                    or evaluate_tile_coverage(
                        image,
                        effective_safe_area,
                        width,
                        height,
                        tile_x,
                        tile_y,
                        alpha_threshold,
                        minimum_tile_coverage,
                    )
                )
            is_playable = satisfies_clip_budget and satisfies_coverage
            if not is_playable:
                blocked_tiles += 1

    total_tiles = width * height
    playable_tiles = total_tiles - blocked_tiles
    effective_area_transparency_fraction = 0.0 if effective_ellipse is not None or effective_horizontal_span_profile is not None else measure_effective_area_transparency_fraction(image, effective_safe_area, alpha_threshold)
    return ProbeResult(
        width=width,
        height=height,
        settings=settings,
        playable_tiles=playable_tiles,
        blocked_tiles=blocked_tiles,
        total_tiles=total_tiles,
        effective_safe_area=effective_safe_area,
        effective_area_transparency_fraction=effective_area_transparency_fraction,
        shape_source="baked-mask" if baked_blocked_tile_ids else ("profile-shape" if effective_horizontal_span_profile is not None else ("ellipse-shape" if effective_ellipse is not None else ("alpha-shape" if effective_area_transparency_fraction > 0.0 else "rect-safe-area"))),
    )


def sanitize_explicit_blocked_tile_ids(blocked_tile_ids: list[int], board_width: int, board_height: int) -> set[int]:
    total_tiles = board_width * board_height
    return {tile_id for tile_id in blocked_tile_ids if 0 <= tile_id < total_tiles}


def get_matching_baked_blocked_tile_ids(metadata: SpriteMetadata | None, board_width: int, board_height: int) -> set[int]:
    if metadata is None:
        return set()

    total_tiles = board_width * board_height
    for baked_mask in metadata.baked_blocked_tile_masks:
        if baked_mask.board_width != board_width or baked_mask.board_height != board_height:
            continue
        return {tile_id for tile_id in baked_mask.blocked_tile_ids if 0 <= tile_id < total_tiles}
    return set()


def validate_probe_results(results: list[ProbeResult], errors: list[str]) -> None:
    for result in results:
        if result.total_tiles > 0 and result.blocked_tiles >= result.total_tiles and max(result.width, result.height) >= 3:
            errors.append(
                f"{result.width}x{result.height} on {result.settings.sprite_path.name} fully blocked the playable board."
            )
        if (
            result.settings.derive_blocked_tiles_from_alpha
            and result.shape_source == "alpha-shape"
            and result.effective_area_transparency_fraction >= 0.02
            and max(result.width, result.height) >= 10
            and result.blocked_tiles == 0
        ):
            errors.append(
                f"{result.width}x{result.height} on {result.settings.sprite_path.name} produced a fully playable rectangle "
                f"even though {result.effective_area_transparency_fraction:.2%} of its effective safe area is transparent."
            )
        if (
            result.width == 95
            and result.height == 95
            and result.settings.name == "default"
            and result.settings.sprite_path.name == "white_bread_1024x1024.png"
            and result.blocked_tiles == 0
        ):
            errors.append(
                "95x95 default white bread produced a fully playable inner rectangle; "
                "the toast silhouette should still clip rounded-corner edge tiles at that size."
            )
        if result.shape_source == "baked-mask" and result.blocked_tiles == 0 and max(result.width, result.height) >= 10:
            errors.append(
                f"{result.width}x{result.height} on {result.settings.sprite_path.name} uses a baked mask but blocks no tiles."
            )


def get_effective_safe_area(settings: BackgroundSettings, board_width: int = 0, board_height: int = 0) -> Rect:
    safe_area = sanitize_rect(settings.safe_area)
    metadata = settings.metadata
    if metadata and metadata.has_playable_ellipse:
        ellipse = get_effective_playable_ellipse(settings)
        return build_ellipse_bounds(ellipse[0], ellipse[1]) if ellipse is not None else safe_area
    if metadata and metadata.has_board_bounds:
        board_bounds = sanitize_rect(metadata.board_bounds)
        if settings.compose_safe_area_with_board_bounds_metadata:
            return compose_rect(board_bounds, safe_area)
        return board_bounds
    if metadata and metadata.has_visible_alpha_bounds:
        return inscribe_board_aspect_ratio(compose_rect(sanitize_rect(metadata.visible_alpha_bounds), safe_area), board_width, board_height)
    return inscribe_board_aspect_ratio(safe_area, board_width, board_height)


def get_effective_playable_ellipse(settings: BackgroundSettings) -> tuple[tuple[float, float], tuple[float, float]] | None:
    metadata = settings.metadata
    if metadata is None or not metadata.has_playable_ellipse:
        return None

    center, radii = sanitize_ellipse(metadata.playable_ellipse_center, metadata.playable_ellipse_radii)
    bounds = compose_rect(build_ellipse_bounds(center, radii), sanitize_rect(settings.safe_area))
    return ((bounds.x + (bounds.width * 0.5), bounds.y + (bounds.height * 0.5)), (bounds.width * 0.5, bounds.height * 0.5))


def get_effective_playable_horizontal_span_profile(settings: BackgroundSettings) -> list[HorizontalSpanStop] | None:
    metadata = settings.metadata
    if metadata is None or not metadata.has_playable_horizontal_span_profile:
        return None

    return sanitize_horizontal_span_profile(
        metadata.playable_horizontal_span_profile_min_y,
        metadata.playable_horizontal_span_profile_max_y,
        metadata.playable_horizontal_span_profile,
    )


def inscribe_board_aspect_ratio(candidate: Rect, board_width: int, board_height: int) -> Rect:
    candidate = sanitize_rect(candidate)
    if board_width <= 0 or board_height <= 0:
        return candidate

    target_aspect_ratio = board_width / board_height
    if target_aspect_ratio <= 0:
        return candidate

    candidate_aspect_ratio = candidate.width / max(0.001, candidate.height)
    if math.isclose(candidate_aspect_ratio, target_aspect_ratio, rel_tol=0.0, abs_tol=0.0001):
        return candidate

    if candidate_aspect_ratio > target_aspect_ratio:
        inscribed_width = candidate.height * target_aspect_ratio
        inset_x = (candidate.width - inscribed_width) * 0.5
        return Rect(candidate.x + inset_x, candidate.y, inscribed_width, candidate.height)

    inscribed_height = candidate.width / target_aspect_ratio
    inset_y = (candidate.height - inscribed_height) * 0.5
    return Rect(candidate.x, candidate.y + inset_y, candidate.width, inscribed_height)


def measure_effective_area_transparency_fraction(image: SpriteImage, safe_area: Rect, threshold: float) -> float:
    x_start = max(0, min(image.width - 1, int(math.floor(safe_area.x_min * image.width))))
    x_end = max(0, min(image.width, int(math.ceil(safe_area.x_max * image.width))))
    y_start = max(0, min(image.height - 1, int(math.floor(safe_area.y_min * image.height))))
    y_end = max(0, min(image.height, int(math.ceil(safe_area.y_max * image.height))))
    if x_end <= x_start or y_end <= y_start:
        return 0.0

    alpha_cutoff = int(math.floor(threshold * 255.0))
    transparent_pixels = 0
    total_pixels = 0
    for y in range(y_start, y_end):
        row = image.alpha_rows[y]
        for x in range(x_start, x_end):
            total_pixels += 1
            if row[x] <= alpha_cutoff:
                transparent_pixels += 1
    return (transparent_pixels / total_pixels) if total_pixels else 0.0


def measure_visible_alpha_bounds(image: SpriteImage) -> Rect:
    min_x = image.width - 1
    max_x = 0
    min_y = image.height - 1
    max_y = 0
    found = False
    for y, row in enumerate(image.alpha_rows):
        for x, alpha in enumerate(row):
            if alpha <= 0:
                continue
            found = True
            min_x = min(min_x, x)
            max_x = max(max_x, x)
            min_y = min(min_y, y)
            max_y = max(max_y, y)

    if not found:
        return Rect(0.0, 0.0, 1.0, 1.0)

    return Rect(
        min_x / image.width,
        min_y / image.height,
        (max_x + 1 - min_x) / image.width,
        (max_y + 1 - min_y) / image.height,
    )


def evaluate_tile_coverage(
    image: SpriteImage,
    safe_area: Rect,
    board_width: int,
    board_height: int,
    tile_x: int,
    tile_y: int,
    alpha_threshold: float,
    minimum_tile_coverage: float,
) -> bool:
    sample_resolution = 5
    covered_samples = 0
    total_samples = sample_resolution * sample_resolution
    for sample_y in range(sample_resolution):
        for sample_x in range(sample_resolution):
            normalized_x = safe_area.x_min + ((tile_x + ((sample_x + 0.5) / sample_resolution)) / board_width) * safe_area.width
            normalized_y = safe_area.y_min + ((tile_y + ((sample_y + 0.5) / sample_resolution)) / board_height) * safe_area.height
            if sample_alpha_bilinear(image, normalized_x, normalized_y) >= alpha_threshold:
                covered_samples += 1
    return (covered_samples / total_samples) >= minimum_tile_coverage


def evaluate_tile_ellipse_coverage(
    ellipse: tuple[tuple[float, float], tuple[float, float]],
    board_width: int,
    board_height: int,
    tile_x: int,
    tile_y: int,
    minimum_tile_coverage: float,
) -> bool:
    sample_resolution = 5
    covered_samples = 0
    total_samples = sample_resolution * sample_resolution
    for sample_y in range(sample_resolution):
        for sample_x in range(sample_resolution):
            point = sample_ellipse_point(
                ellipse,
                board_width,
                board_height,
                tile_x,
                tile_y,
                (sample_x + 0.5) / sample_resolution,
                (sample_y + 0.5) / sample_resolution,
            )
            if point_inside_ellipse(point, ellipse):
                covered_samples += 1
    return (covered_samples / total_samples) >= minimum_tile_coverage


def evaluate_tile_horizontal_span_profile_coverage(
    profile: HorizontalSpanProfile,
    board_width: int,
    board_height: int,
    tile_x: int,
    tile_y: int,
    minimum_tile_coverage: float,
) -> bool:
    sample_resolution = 5
    covered_samples = 0
    total_samples = sample_resolution * sample_resolution
    for sample_y in range(sample_resolution):
        for sample_x in range(sample_resolution):
            point = sample_horizontal_span_profile_point(
                board_width,
                board_height,
                tile_x,
                tile_y,
                (sample_x + 0.5) / sample_resolution,
                (sample_y + 0.5) / sample_resolution,
            )
            if point_inside_horizontal_span_profile(point, profile):
                covered_samples += 1
    return (covered_samples / total_samples) >= minimum_tile_coverage


def evaluate_tile_clip_budget(
    image: SpriteImage,
    safe_area: Rect,
    board_width: int,
    board_height: int,
    tile_x: int,
    tile_y: int,
    alpha_threshold: float,
    sample_offsets: list[float],
) -> bool:
    for sample_offset_y in sample_offsets:
        for sample_offset_x in sample_offsets:
            normalized_x = safe_area.x_min + ((tile_x + 0.5 + sample_offset_x) / board_width) * safe_area.width
            normalized_y = safe_area.y_min + ((tile_y + 0.5 + sample_offset_y) / board_height) * safe_area.height
            if sample_alpha_bilinear(image, normalized_x, normalized_y) < alpha_threshold:
                return False
    return True


def evaluate_tile_ellipse_clip_budget(
    ellipse: tuple[tuple[float, float], tuple[float, float]],
    board_width: int,
    board_height: int,
    tile_x: int,
    tile_y: int,
    sample_offsets: list[float],
) -> bool:
    for sample_offset_y in sample_offsets:
        for sample_offset_x in sample_offsets:
            point = sample_ellipse_point(
                ellipse,
                board_width,
                board_height,
                tile_x,
                tile_y,
                0.5 + sample_offset_x,
                0.5 + sample_offset_y,
            )
            if not point_inside_ellipse(point, ellipse):
                return False
    return True


def evaluate_tile_horizontal_span_profile_clip_budget(
    profile: HorizontalSpanProfile,
    board_width: int,
    board_height: int,
    tile_x: int,
    tile_y: int,
    sample_offsets: list[float],
) -> bool:
    for sample_offset_y in sample_offsets:
        for sample_offset_x in sample_offsets:
            point = sample_horizontal_span_profile_point(
                board_width,
                board_height,
                tile_x,
                tile_y,
                0.5 + sample_offset_x,
                0.5 + sample_offset_y,
            )
            if not point_inside_horizontal_span_profile(point, profile):
                return False
    return True


def sample_tile_center_alpha(
    image: SpriteImage,
    safe_area: Rect,
    board_width: int,
    board_height: int,
    tile_x: int,
    tile_y: int,
) -> float:
    normalized_x = safe_area.x_min + ((tile_x + 0.5) / board_width) * safe_area.width
    normalized_y = safe_area.y_min + ((tile_y + 0.5) / board_height) * safe_area.height
    return sample_alpha_bilinear(image, normalized_x, normalized_y)


def sample_alpha_bilinear(image: SpriteImage, normalized_x: float, normalized_y: float) -> float:
    x = clamp01(normalized_x) * (image.width - 1)
    y = clamp01(normalized_y) * (image.height - 1)
    x0 = int(math.floor(x))
    x1 = min(x0 + 1, image.width - 1)
    y0 = int(math.floor(y))
    y1 = min(y0 + 1, image.height - 1)
    tx = x - x0
    ty = y - y0
    a00 = image.alpha_rows[y0][x0] / 255.0
    a10 = image.alpha_rows[y0][x1] / 255.0
    a01 = image.alpha_rows[y1][x0] / 255.0
    a11 = image.alpha_rows[y1][x1] / 255.0
    top = a00 + ((a10 - a00) * tx)
    bottom = a01 + ((a11 - a01) * tx)
    return top + ((bottom - top) * ty)


def build_clip_budget_sample_offsets(
    playable_surface_tile_scale: float,
    maximum_tile_clip_fraction: float,
    sample_resolution: int,
) -> list[float]:
    clamped_scale = max(0.0, playable_surface_tile_scale)
    clamped_maximum_clip_fraction = min(0.49, max(0.0, maximum_tile_clip_fraction))
    half_tile_span = clamped_scale * 0.5
    required_inset_from_center = half_tile_span - clamped_maximum_clip_fraction
    if required_inset_from_center <= 0.0001:
        return []
    if required_inset_from_center > 0.5:
        required_inset_from_center = 0.5
    clamped_resolution = min(7, max(2, sample_resolution))
    if clamped_resolution == 2:
        return [-required_inset_from_center, required_inset_from_center]

    start = -required_inset_from_center
    span = required_inset_from_center * 2.0
    return [start + (span * (index / (clamped_resolution - 1))) for index in range(clamped_resolution)]


def rect_from_yaml(data: dict | None) -> Rect:
    if not data:
        return Rect(0.0, 0.0, 1.0, 1.0)
    return Rect(
        float(data.get("x", 0.0)),
        float(data.get("y", 0.0)),
        float(data.get("width", 1.0)),
        float(data.get("height", 1.0)),
    )


def vector2_from_yaml(data: dict | None, default: tuple[float, float]) -> tuple[float, float]:
    if not data:
        return default
    return (float(data.get("x", default[0])), float(data.get("y", default[1])))


def horizontal_span_profile_from_yaml(data: list[dict] | None) -> list[tuple[float, float, float]]:
    if not data:
        return []

    profile: list[tuple[float, float, float]] = []
    for entry in data:
        if not entry:
            continue
        profile.append(
            (
                float(entry.get("normalizedY", 0.0)),
                float(entry.get("minXNormalized", 0.0)),
                float(entry.get("maxXNormalized", 1.0)),
            )
        )
    return profile


def baked_blocked_tile_masks_from_yaml(data: list[dict] | None) -> list[BakedBlockedTileMask]:
    if not data:
        return []

    masks: list[BakedBlockedTileMask] = []
    for entry in data:
        if not entry:
            continue
        masks.append(
            BakedBlockedTileMask(
                board_width=int(entry.get("boardWidth", 0)),
                board_height=int(entry.get("boardHeight", 0)),
                bake_version=str(entry.get("bakeVersion", "") or ""),
                sprite_content_hash=str(entry.get("spriteContentHash", "") or ""),
                blocked_tile_ids=[int(tile_id) for tile_id in entry.get("blockedTileIds", [])],
            )
        )
    return masks


def build_safe_area(left: float, right: float, bottom: float, top: float) -> Rect:
    left = clamp01(left)
    right = clamp01(right)
    bottom = clamp01(bottom)
    top = clamp01(top)
    return Rect(
        left,
        bottom,
        max(0.01, 1.0 - left - right),
        max(0.01, 1.0 - bottom - top),
    )


def sanitize_rect(rect: Rect) -> Rect:
    x_min = clamp01(rect.x_min)
    y_min = clamp01(rect.y_min)
    x_max = min(1.0, max(x_min + 0.001, rect.x_max))
    y_max = min(1.0, max(y_min + 0.001, rect.y_max))
    return Rect(x_min, y_min, x_max - x_min, y_max - y_min)


def sanitize_ellipse(center: tuple[float, float], radii: tuple[float, float]) -> tuple[tuple[float, float], tuple[float, float]]:
    center_x = clamp01(center[0])
    center_y = clamp01(center[1])
    max_radius_x = max(0.001, min(center_x, 1.0 - center_x))
    max_radius_y = max(0.001, min(center_y, 1.0 - center_y))
    radius_x = min(max(radii[0], 0.001), max_radius_x)
    radius_y = min(max(radii[1], 0.001), max_radius_y)
    return (center_x, center_y), (radius_x, radius_y)


def sanitize_horizontal_span_profile(
    min_y: float,
    max_y: float,
    profile: list[tuple[float, float, float]],
) -> HorizontalSpanProfile | None:
    sanitized: list[HorizontalSpanStop] = []
    for normalized_y, min_x, max_x in profile:
        clamped_y = clamp01(normalized_y)
        clamped_min_x = clamp01(min_x)
        clamped_max_x = min(1.0, max(clamped_min_x, max_x))
        sanitized.append(HorizontalSpanStop(clamped_y, clamped_min_x, clamped_max_x))
    sanitized.sort(key=lambda stop: stop.normalized_y)
    if not sanitized:
        return None

    clamped_min_y = clamp01(min(min_y, max_y))
    clamped_max_y = clamp01(max(min_y, max_y))
    return HorizontalSpanProfile(clamped_min_y, clamped_max_y, sanitized)


def compose_rect(outer: Rect, inner: Rect) -> Rect:
    outer = sanitize_rect(outer)
    inner = sanitize_rect(inner)
    return Rect(
        outer.x_min + (inner.x_min * outer.width),
        outer.y_min + (inner.y_min * outer.height),
        outer.width * inner.width,
        outer.height * inner.height,
    )


def build_ellipse_bounds(center: tuple[float, float], radii: tuple[float, float]) -> Rect:
    return Rect(center[0] - radii[0], center[1] - radii[1], radii[0] * 2.0, radii[1] * 2.0)


def sample_ellipse_point(
    ellipse: tuple[tuple[float, float], tuple[float, float]],
    board_width: int,
    board_height: int,
    tile_x: int,
    tile_y: int,
    sample_x_within_tile: float,
    sample_y_within_tile: float,
) -> tuple[float, float]:
    center, radii = ellipse
    normalized_x = center[0] + ((((tile_x + sample_x_within_tile) / board_width) * 2.0) - 1.0) * radii[0]
    normalized_y = center[1] + ((((tile_y + sample_y_within_tile) / board_height) * 2.0) - 1.0) * radii[1]
    return normalized_x, normalized_y


def sample_horizontal_span_profile_point(
    board_width: int,
    board_height: int,
    tile_x: int,
    tile_y: int,
    sample_x_within_tile: float,
    sample_y_within_tile: float,
) -> tuple[float, float]:
    return (
        clamp01((tile_x + sample_x_within_tile) / board_width),
        clamp01((tile_y + sample_y_within_tile) / board_height),
    )


def point_inside_ellipse(point: tuple[float, float], ellipse: tuple[tuple[float, float], tuple[float, float]]) -> bool:
    center, radii = ellipse
    delta_x = (point[0] - center[0]) / max(0.001, radii[0])
    delta_y = (point[1] - center[1]) / max(0.001, radii[1])
    return ((delta_x * delta_x) + (delta_y * delta_y)) <= 1.0


def point_inside_horizontal_span_profile(point: tuple[float, float], profile: HorizontalSpanProfile) -> bool:
    if point[1] < profile.min_y or point[1] > profile.max_y:
        return False

    min_x, max_x = evaluate_horizontal_span_at_y(point[1], profile.stops)
    return point[0] >= min_x and point[0] <= max_x


def evaluate_horizontal_span_at_y(normalized_y: float, profile: list[HorizontalSpanStop]) -> tuple[float, float]:
    if not profile:
        return 0.0, 1.0

    clamped_y = clamp01(normalized_y)
    if len(profile) == 1 or clamped_y <= profile[0].normalized_y:
        return profile[0].min_x, profile[0].max_x

    if clamped_y >= profile[-1].normalized_y:
        return profile[-1].min_x, profile[-1].max_x

    for index in range(len(profile) - 1):
        lower = profile[index]
        upper = profile[index + 1]
        if clamped_y < lower.normalized_y or clamped_y > upper.normalized_y:
            continue
        span = max(0.0001, upper.normalized_y - lower.normalized_y)
        t = clamp01((clamped_y - lower.normalized_y) / span)
        return (
            lower.min_x + ((upper.min_x - lower.min_x) * t),
            lower.max_x + ((upper.max_x - lower.max_x) * t),
        )

    return profile[-1].min_x, profile[-1].max_x


def rects_close(a: Rect, b: Rect, tolerance_x: float, tolerance_y: float) -> bool:
    return (
        abs(a.x - b.x) <= tolerance_x
        and abs(a.y - b.y) <= tolerance_y
        and abs(a.width - b.width) <= tolerance_x
        and abs(a.height - b.height) <= tolerance_y
    )


def rect_contains(outer: Rect, inner: Rect, tolerance_x: float, tolerance_y: float) -> bool:
    return (
        inner.x_min >= outer.x_min - tolerance_x
        and inner.y_min >= outer.y_min - tolerance_y
        and inner.x_max <= outer.x_max + tolerance_x
        and inner.y_max <= outer.y_max + tolerance_y
    )


def format_rect(rect: Rect) -> str:
    return f"({rect.x:.6f}, {rect.y:.6f}, {rect.width:.6f}, {rect.height:.6f})"


def format_ellipse(center: tuple[float, float], radii: tuple[float, float]) -> str:
    return f"center=({center[0]:.6f}, {center[1]:.6f}) radii=({radii[0]:.6f}, {radii[1]:.6f})"


def format_horizontal_span_profile(profile: list[tuple[float, float, float]], min_y: float, max_y: float) -> str:
    sanitized = sanitize_horizontal_span_profile(min_y, max_y, profile)
    if sanitized is None:
        return "empty"
    return f"{len(sanitized.stops)} stops y=({sanitized.min_y:.3f},{sanitized.max_y:.3f})"


def format_baked_mask_summary(masks: list[BakedBlockedTileMask]) -> str:
    ordered = sorted(masks, key=lambda mask: (mask.board_width, mask.board_height))
    return ", ".join(f"{mask.board_width}x{mask.board_height}" for mask in ordered)


def parse_board_sizes(raw_value: str) -> list[tuple[int, int]]:
    sizes: list[tuple[int, int]] = []
    if not raw_value.strip():
        return sizes

    for chunk in raw_value.split(","):
        token = chunk.strip().lower()
        if not token:
            continue
        width_str, separator, height_str = token.partition("x")
        if separator != "x":
            raise ValueError(f"Invalid board size '{chunk}'. Expected WIDTHxHEIGHT.")
        width = int(width_str)
        height = int(height_str)
        if width <= 0 or height <= 0:
            raise ValueError(f"Invalid board size '{chunk}'. Dimensions must be positive.")
        sizes.append((width, height))
    return sizes


def resolve_sprite_metadata(sprite_identifier: str, metadata_by_guid: dict[str, SpriteMetadata]) -> SpriteMetadata | None:
    lookup = sprite_identifier.strip().lower()
    if not lookup:
        return None

    for guid, metadata in metadata_by_guid.items():
        if guid.lower() == lookup:
            return metadata
        if metadata.sprite_path.name.lower() == lookup or metadata.sprite_path.stem.lower() == lookup:
            return metadata
    return None


def build_square_board_bounds(bounds: Rect, image: SpriteImage) -> Rect:
    sanitized = sanitize_rect(bounds)
    image_width = max(1, image.width)
    image_height = max(1, image.height)

    width_px = sanitized.width * image_width
    height_px = sanitized.height * image_height
    side_px = max(width_px, height_px)

    center_x_px = (sanitized.x * image_width) + (width_px * 0.5)
    center_y_px = (sanitized.y * image_height) + (height_px * 0.5)

    x_min_px = center_x_px - (side_px * 0.5)
    y_min_px = center_y_px - (side_px * 0.5)
    x_min_px = min(max(0.0, x_min_px), max(0.0, image_width - side_px))
    y_min_px = min(max(0.0, y_min_px), max(0.0, image_height - side_px))

    return sanitize_rect(
        Rect(
            x_min_px / image_width,
            y_min_px / image_height,
            side_px / image_width,
            side_px / image_height,
        )
    )


def build_baked_mask_from_square_bounds(
    image: SpriteImage,
    square_bounds: Rect,
    board_width: int,
    board_height: int,
    alpha_threshold: float,
    minimum_tile_coverage: float,
    max_tile_clip_fraction: float,
    tile_clip_sample_resolution: int,
) -> list[int]:
    clip_offsets = build_clip_budget_sample_offsets(
        PLAYABLE_SURFACE_TILE_SCALE,
        max_tile_clip_fraction,
        tile_clip_sample_resolution,
    )
    blocked_tile_ids: list[int] = []
    clamped_alpha_threshold = clamp01(alpha_threshold)
    clamped_minimum_tile_coverage = clamp01(minimum_tile_coverage)

    for tile_y in range(board_height):
        for tile_x in range(board_width):
            satisfies_clip_budget = (
                not clip_offsets
                or evaluate_tile_clip_budget(
                    image,
                    square_bounds,
                    board_width,
                    board_height,
                    tile_x,
                    tile_y,
                    clamped_alpha_threshold,
                    clip_offsets,
                )
            )
            satisfies_coverage = (
                clamped_minimum_tile_coverage <= 0.0
                or evaluate_tile_coverage(
                    image,
                    square_bounds,
                    board_width,
                    board_height,
                    tile_x,
                    tile_y,
                    clamped_alpha_threshold,
                    clamped_minimum_tile_coverage,
                )
            )
            if not (satisfies_clip_budget and satisfies_coverage):
                blocked_tile_ids.append((tile_y * board_width) + tile_x)

    return blocked_tile_ids


def compute_sprite_content_hash(image: SpriteImage) -> str:
    digest = hashlib.sha256()
    digest.update(f"{image.width}x{image.height}|".encode("utf-8"))
    for row in image.alpha_rows:
        digest.update(bytes(row))
    return digest.hexdigest()[:16]


def resolve_settings_for_sprite_and_size(
    sprite_guid: str,
    board_width: int,
    board_height: int,
    default_settings: BackgroundSettings,
    overrides: list[BackgroundSettings],
) -> BackgroundSettings | None:
    for override in overrides:
        if override.sprite_guid != sprite_guid:
            continue
        if override.min_board_width <= board_width <= override.max_board_width and override.min_board_height <= board_height <= override.max_board_height:
            return override
    if default_settings.sprite_guid == sprite_guid:
        return default_settings
    return None


def emit_baked_mask_snippet(
    sprite_identifier: str,
    board_sizes: list[tuple[int, int]],
    bake_version: str,
    metadata_by_guid: dict[str, SpriteMetadata],
    sprite_images: dict[str, SpriteImage],
    default_settings: BackgroundSettings,
    overrides: list[BackgroundSettings],
    errors: list[str],
) -> None:
    metadata = resolve_sprite_metadata(sprite_identifier, metadata_by_guid)
    if metadata is None:
        errors.append(f"No sprite metadata entry matched '{sprite_identifier}'.")
        return

    image = sprite_images.get(metadata.sprite_guid)
    if image is None:
        errors.append(f"No readable sprite image found for {metadata.sprite_path.name}.")
        return

    source_bounds = metadata.visible_alpha_bounds if metadata.has_visible_alpha_bounds else measure_visible_alpha_bounds(image)
    square_bounds = build_square_board_bounds(source_bounds, image)
    sprite_content_hash = compute_sprite_content_hash(image)

    print(f"  Sprite: {metadata.sprite_path.name}")
    print(f"  Source visible bounds: {format_rect(source_bounds)}")
    print(f"  Recommended pixel-square boardBoundsNormalized: {format_rect(square_bounds)}")
    print("  YAML snippet:")
    print("    hasBoardBounds: 1")
    print("    boardBoundsNormalized:")
    print("      serializedVersion: 2")
    print(f"      x: {square_bounds.x:.6f}")
    print(f"      y: {square_bounds.y:.6f}")
    print(f"      width: {square_bounds.width:.6f}")
    print(f"      height: {square_bounds.height:.6f}")
    print("    bakedBlockedTileMasks:")

    for board_width, board_height in board_sizes:
        settings = resolve_settings_for_sprite_and_size(metadata.sprite_guid, board_width, board_height, default_settings, overrides)
        if settings is None:
            errors.append(
                f"No board background override/default settings matched {metadata.sprite_path.name} at {board_width}x{board_height}."
            )
            continue

        blocked_tile_ids = build_baked_mask_from_square_bounds(
            image,
            square_bounds,
            board_width,
            board_height,
            settings.alpha_playable_threshold,
            settings.min_tile_coverage,
            settings.max_tile_clip_fraction,
            settings.tile_clip_sample_resolution,
        )
        print(f"    - boardWidth: {board_width}")
        print(f"      boardHeight: {board_height}")
        print(f"      bakeVersion: {bake_version}")
        print(f"      spriteContentHash: {sprite_content_hash}")
        print("      blockedTileIds:")
        if blocked_tile_ids:
            for tile_id in blocked_tile_ids:
                print(f"        - {tile_id}")
        else:
            print("        []")


def clamp01(value: float) -> float:
    return min(1.0, max(0.0, value))


def decode_rgba_png(path: Path) -> SpriteImage:
    with path.open("rb") as handle:
        raw = handle.read()

    if raw[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"{path} is not a PNG file")

    width = height = bit_depth = color_type = interlace_method = None
    idat_parts: list[bytes] = []
    offset = 8
    while offset < len(raw):
        chunk_length = struct.unpack(">I", raw[offset:offset + 4])[0]
        chunk_type = raw[offset + 4:offset + 8]
        chunk_data = raw[offset + 8:offset + 8 + chunk_length]
        offset += chunk_length + 12

        if chunk_type == b"IHDR":
            width, height, bit_depth, color_type, _compression, _filter_method, interlace_method = struct.unpack(">IIBBBBB", chunk_data)
        elif chunk_type == b"IDAT":
            idat_parts.append(chunk_data)
        elif chunk_type == b"IEND":
            break

    if color_type != 6 or bit_depth != 8 or interlace_method != 0:
        raise ValueError(
            f"{path} must be non-interlaced RGBA8 PNG for this validator. "
            f"Found color_type={color_type}, bit_depth={bit_depth}, interlace={interlace_method}."
        )

    decompressed = zlib.decompress(b"".join(idat_parts))
    stride = width * 4
    expected_size = (stride + 1) * height
    if len(decompressed) != expected_size:
        raise ValueError(f"{path} decompressed to {len(decompressed)} bytes; expected {expected_size}.")

    alpha_rows: list[list[int]] = []
    previous_row = [0] * stride
    cursor = 0
    for _ in range(height):
        filter_type = decompressed[cursor]
        cursor += 1
        row = list(decompressed[cursor:cursor + stride])
        cursor += stride
        row = unfilter_scanline(filter_type, row, previous_row, 4)
        alpha_rows.append([row[(pixel_index * 4) + 3] for pixel_index in range(width)])
        previous_row = row

    return SpriteImage(path=path, width=width, height=height, alpha_rows=alpha_rows)


def unfilter_scanline(filter_type: int, row: list[int], previous_row: list[int], bytes_per_pixel: int) -> list[int]:
    if filter_type == 0:
        return row

    output = row[:]
    if filter_type == 1:
        for index in range(len(output)):
            left = output[index - bytes_per_pixel] if index >= bytes_per_pixel else 0
            output[index] = (output[index] + left) & 0xFF
        return output

    if filter_type == 2:
        for index in range(len(output)):
            up = previous_row[index]
            output[index] = (output[index] + up) & 0xFF
        return output

    if filter_type == 3:
        for index in range(len(output)):
            left = output[index - bytes_per_pixel] if index >= bytes_per_pixel else 0
            up = previous_row[index]
            output[index] = (output[index] + ((left + up) // 2)) & 0xFF
        return output

    if filter_type == 4:
        for index in range(len(output)):
            left = output[index - bytes_per_pixel] if index >= bytes_per_pixel else 0
            up = previous_row[index]
            upper_left = previous_row[index - bytes_per_pixel] if index >= bytes_per_pixel else 0
            output[index] = (output[index] + paeth_predictor(left, up, upper_left)) & 0xFF
        return output

    raise ValueError(f"Unsupported PNG filter type: {filter_type}")


def paeth_predictor(left: int, up: int, upper_left: int) -> int:
    predictor = left + up - upper_left
    distance_left = abs(predictor - left)
    distance_up = abs(predictor - up)
    distance_upper_left = abs(predictor - upper_left)
    if distance_left <= distance_up and distance_left <= distance_upper_left:
        return left
    if distance_up <= distance_upper_left:
        return up
    return upper_left


if __name__ == "__main__":
    sys.exit(main())
