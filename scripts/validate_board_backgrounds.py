#!/usr/bin/env python3
from __future__ import annotations

import argparse
import math
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

    validate_metadata(metadata_by_guid, sprite_images, errors)
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
            f"board={'none' if not metadata.has_board_bounds else format_rect(metadata.board_bounds)}"
        )

    print("")
    print("Probe summary:")
    for result in results:
        blocked_fraction = result.blocked_tiles / result.total_tiles if result.total_tiles else 0.0
        transparency = "alpha-shape" if result.effective_area_transparency_fraction > 0.0 else "rect-safe-area"
        print(
            f"  {result.width:>3}x{result.height:<3} "
            f"{result.settings.sprite_path.name:<24} "
            f"blocked={result.blocked_tiles:>5}/{result.total_tiles:<5} "
            f"({blocked_fraction:>6.2%}) "
            f"{transparency:<14} "
            f"{result.settings.override_description}"
        )

    if notes:
        print("")
        print("Notes:")
        for note in notes:
            print(f"  - {note}")

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
    squares.extend([21, 25, 30, 35, 40, 41, 50, 60, 70, 80, 81, 90, 95, 99, 100, 101, 120, 140, 160, 180, 200])
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

        if metadata.has_board_bounds and not rect_contains(metadata.visible_alpha_bounds, metadata.board_bounds, 1.0 / image.width, 1.0 / image.height):
            errors.append(
                f"{metadata.sprite_path.name} boardBoundsNormalized escapes visible alpha bounds: "
                f"visible={format_rect(metadata.visible_alpha_bounds)} board={format_rect(metadata.board_bounds)}"
            )


def collect_override_notes(settings: BackgroundSettings, notes: list[str]) -> None:
    zero_safe_area = build_safe_area(0.0, 0.0, 0.0, 0.0)
    if settings.metadata and settings.metadata.has_board_bounds and not settings.compose_safe_area_with_board_bounds_metadata:
        if settings.safe_area != zero_safe_area:
            notes.append(
                f"{settings.sprite_path.name} {settings.override_description}: boardBoundsNormalized is active, "
                "so configured insets are ignored unless safe-area composition is enabled."
            )
    if not math.isclose(settings.background_scale_multiplier, 1.0, rel_tol=0.0, abs_tol=0.0001):
        notes.append(
            f"{settings.sprite_path.name} {settings.override_description}: backgroundScaleMultiplier={settings.background_scale_multiplier:.3f}; "
            "keep visually verifying this because it changes render framing and mask derivation together."
        )


def evaluate_probe(
    width: int,
    height: int,
    settings: BackgroundSettings,
    sprite_images: dict[str, SpriteImage],
) -> ProbeResult:
    image = sprite_images[settings.sprite_guid]
    effective_safe_area = get_effective_safe_area(settings, width, height)
    clip_offsets = build_clip_budget_sample_offsets(
        PLAYABLE_SURFACE_TILE_SCALE,
        settings.max_tile_clip_fraction,
        settings.tile_clip_sample_resolution,
    )

    blocked_tiles = 0
    alpha_threshold = clamp01(settings.alpha_playable_threshold)
    minimum_tile_coverage = clamp01(settings.min_tile_coverage)
    for tile_y in range(height):
        for tile_x in range(width):
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
            if not is_playable and settings.max_tile_clip_fraction <= 0.0 and minimum_tile_coverage <= 0.0:
                is_playable = sample_tile_center_alpha(
                    image,
                    effective_safe_area,
                    width,
                    height,
                    tile_x,
                    tile_y,
                ) >= alpha_threshold
            if not is_playable:
                blocked_tiles += 1

    total_tiles = width * height
    playable_tiles = total_tiles - blocked_tiles
    return ProbeResult(
        width=width,
        height=height,
        settings=settings,
        playable_tiles=playable_tiles,
        blocked_tiles=blocked_tiles,
        total_tiles=total_tiles,
        effective_safe_area=effective_safe_area,
        effective_area_transparency_fraction=measure_effective_area_transparency_fraction(image, effective_safe_area, alpha_threshold),
    )


def validate_probe_results(results: list[ProbeResult], errors: list[str]) -> None:
    for result in results:
        if result.total_tiles > 0 and result.blocked_tiles >= result.total_tiles and max(result.width, result.height) >= 3:
            errors.append(
                f"{result.width}x{result.height} on {result.settings.sprite_path.name} fully blocked the playable board."
            )
        if (
            result.settings.derive_blocked_tiles_from_alpha
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


def get_effective_safe_area(settings: BackgroundSettings, board_width: int = 0, board_height: int = 0) -> Rect:
    safe_area = sanitize_rect(settings.safe_area)
    metadata = settings.metadata
    if metadata and metadata.has_board_bounds:
        board_bounds = sanitize_rect(metadata.board_bounds)
        if settings.compose_safe_area_with_board_bounds_metadata:
            return compose_rect(board_bounds, safe_area)
        return board_bounds
    if metadata and metadata.has_visible_alpha_bounds:
        return fit_rect_to_aspect_ratio(
            compose_rect(sanitize_rect(metadata.visible_alpha_bounds), safe_area),
            board_width,
            board_height,
        )
    return safe_area


def fit_rect_to_aspect_ratio(rect: Rect, board_width: int, board_height: int) -> Rect:
    sanitized = sanitize_rect(rect)
    if board_width <= 0 or board_height <= 0:
        return sanitized

    target_aspect = board_width / board_height
    if target_aspect <= 0:
        return sanitized

    current_aspect = sanitized.width / sanitized.height
    if math.isclose(current_aspect, target_aspect, abs_tol=0.0001):
        return sanitized

    if current_aspect > target_aspect:
        fitted_width = sanitized.height * target_aspect
        return Rect(
            sanitized.x + ((sanitized.width - fitted_width) * 0.5),
            sanitized.y,
            fitted_width,
            sanitized.height,
        )

    fitted_height = sanitized.width / target_aspect
    return Rect(
        sanitized.x,
        sanitized.y + ((sanitized.height - fitted_height) * 0.5),
        sanitized.width,
        fitted_height,
    )


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


def compose_rect(outer: Rect, inner: Rect) -> Rect:
    outer = sanitize_rect(outer)
    inner = sanitize_rect(inner)
    return Rect(
        outer.x_min + (inner.x_min * outer.width),
        outer.y_min + (inner.y_min * outer.height),
        outer.width * inner.width,
        outer.height * inner.height,
    )


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
