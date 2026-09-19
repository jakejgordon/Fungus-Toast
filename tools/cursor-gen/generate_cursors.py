#!/usr/bin/env python3
"""Generate the Fungus Toast hardware cursor set (arrow, hand, target).

Pure stdlib: shapes are signed-distance fields rasterised with 4x4 supersampling
into 32x32 RGBA PNGs, so nothing here needs Pillow or Unity. Colours are read
from UIStyleTokens.cs (same regex as tools/icon-preview/generate_style_stub.py),
so re-running this after a palette change regenerates the cursors on-palette.

    python tools/cursor-gen/generate_cursors.py            # write PNGs + preview
    python tools/cursor-gen/generate_cursors.py --preview-only

PNGs go to FungusToast.Unity/Assets/Resources/UI/Cursors/ (imported as Cursor by
Assets/Editor/CursorTextureImporter.cs). The review sheet goes to
TEMP/cursor-preview/preview.html plus an 8x sheet.png. Hotspots (pixel x, y from
the top-left) are printed at the end and must match CursorManager.cs.

It also rewrites the board hover/selection highlight sprite as an opaque white
square. That sprite is tinted at runtime by HoverEffectHelper / the selection
helpers via Tilemap.SetColor, which multiplies, so the old solid-magenta
placeholder swallowed every tint the code applied.
"""

from __future__ import annotations

import argparse
import base64
import math
import re
import struct
import sys
import zlib
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
STYLE_TOKENS = REPO_ROOT / "FungusToast.Unity/Assets/Scripts/Unity/UI/UIStyleTokens.cs"
ASSET_DIR = REPO_ROOT / "FungusToast.Unity/Assets/Resources/UI/Cursors"
PREVIEW_DIR = REPO_ROOT / "TEMP/cursor-preview"
HOVER_SPRITE = REPO_ROOT / "FungusToast.Unity/Assets/Sprites/Tiles/Bread/hover_player_icon_temp_replacement_64x64.png"
HOVER_SPRITE_SIZE = 64
SCREENSHOTS = [
    REPO_ROOT / "UX-Playtest-2026-09-17/screenshots/003-game-1-start-hud.png",
    REPO_ROOT / "UX-Playtest-2026-09-17/screenshots/004-mutation-tree-first-open.png",
]

SIZE = 32
SUPERSAMPLE = 4
OUTLINE_WIDTH = 1.0

# ---------------------------------------------------------------------------
# Palette
# ---------------------------------------------------------------------------

CLASS_RE = re.compile(r"^\s*public static class (\w+)\s*$")
COLOR_RE = re.compile(r'^\s*public static readonly Color (\w+) = Hex\("(#[0-9A-Fa-f]{6}(?:[0-9A-Fa-f]{2})?)"\);\s*(?://.*)?$')


def load_tokens(source: Path) -> dict[str, tuple[int, int, int, int]]:
    tokens: dict[str, tuple[int, int, int, int]] = {}
    current = None
    for line in source.read_text(encoding="utf-8-sig").splitlines():
        class_match = CLASS_RE.match(line)
        if class_match:
            name = class_match.group(1)
            current = None if name == "UIStyleTokens" else name
            continue
        color_match = COLOR_RE.match(line)
        if color_match and current:
            hex_value = color_match.group(2).lstrip("#")
            r, g, b = (int(hex_value[i:i + 2], 16) for i in (0, 2, 4))
            a = int(hex_value[6:8], 16) if len(hex_value) == 8 else 255
            tokens[f"{current}.{color_match.group(1)}"] = (r, g, b, a)
    return tokens


# ---------------------------------------------------------------------------
# Signed-distance primitives (pixel space, y down, negative = inside)
# ---------------------------------------------------------------------------

def sd_segment(px, py, ax, ay, bx, by):
    abx, aby = bx - ax, by - ay
    apx, apy = px - ax, py - ay
    denom = abx * abx + aby * aby
    t = 0.0 if denom == 0 else max(0.0, min(1.0, (apx * abx + apy * aby) / denom))
    dx, dy = apx - t * abx, apy - t * aby
    return math.hypot(dx, dy)


def capsule(ax, ay, bx, by, r):
    return lambda x, y: sd_segment(x, y, ax, ay, bx, by) - r


def circle(cx, cy, r):
    return lambda x, y: math.hypot(x - cx, y - cy) - r


def rounded_rect(x0, y0, x1, y1, r):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    hx, hy = (x1 - x0) / 2 - r, (y1 - y0) / 2 - r

    def sdf(x, y):
        qx, qy = abs(x - cx) - hx, abs(y - cy) - hy
        outside = math.hypot(max(qx, 0.0), max(qy, 0.0))
        inside = min(max(qx, qy), 0.0)
        return outside + inside - r

    return sdf


def polygon(points):
    n = len(points)

    def sdf(x, y):
        dist = float("inf")
        inside = False
        for i in range(n):
            ax, ay = points[i]
            bx, by = points[(i + 1) % n]
            dist = min(dist, sd_segment(x, y, ax, ay, bx, by))
            if (ay > y) != (by > y):
                cross_x = ax + (y - ay) * (bx - ax) / (by - ay)
                if x < cross_x:
                    inside = not inside
        return -dist if inside else dist

    return sdf


def union(*sdfs):
    return lambda x, y: min(s(x, y) for s in sdfs)


def ring(sdf, half_width):
    """The band of +-half_width around sdf's boundary."""
    return lambda x, y: abs(sdf(x, y)) - half_width


# ---------------------------------------------------------------------------
# Cursor definitions
# ---------------------------------------------------------------------------

class Layer:
    """One shape painted fill-inside-outline; `stroke` layers are a single colour."""

    def __init__(self, sdf, fill="fill", stroke=False):
        self.sdf = sdf
        self.fill = fill
        self.stroke = stroke


def arrow_layers():
    # Classic arrow silhouette scaled 1.15x from the 12x19 desktop glyph, tip on
    # pixel (2, 2) so the hotspot sits on a painted pixel with 2px padding.
    s = 1.15
    ox, oy = 2.5, 2.5
    pts = [(0, 0), (0, 17), (4.7, 13), (8, 20.5), (11.8, 19), (8.5, 12.5), (13, 12.5)]
    return [Layer(polygon([(ox + x * s, oy + y * s) for x, y in pts]))], (2, 2)


def hand_layers():
    index = capsule(11.5, 4.6, 11.5, 17.0, 2.6)          # tip at y = 2.0
    middle = capsule(16.6, 12.0, 16.6, 18.5, 2.5)
    ring_f = capsule(21.6, 13.2, 21.6, 19.0, 2.4)
    pinky = capsule(26.2, 14.8, 26.2, 19.5, 2.2)
    palm = rounded_rect(8.9, 16.0, 28.4, 28.5, 3.5)
    thumb = capsule(4.4, 16.6, 10.5, 22.5, 2.4)
    hand = union(index, middle, ring_f, pinky, palm, thumb)
    knuckles = [
        Layer(ring(capsule(14.1, 17.0, 14.1, 19.5, 0.0), 0.5), fill="outline", stroke=True),
        Layer(ring(capsule(19.1, 17.6, 19.1, 20.1, 0.0), 0.5), fill="outline", stroke=True),
        Layer(ring(capsule(24.0, 18.2, 24.0, 20.7, 0.0), 0.5), fill="outline", stroke=True),
        Layer(ring(capsule(9.9, 25.5, 27.4, 25.5, 0.0), 0.5), fill="outline", stroke=True),  # cuff
    ]
    return [Layer(hand)] + knuckles, (11, 2)


def target_layers():
    cx = cy = 16.5
    arms = [
        capsule(cx, cy - 5.0, cx, cy - 12.5, 1.5),
        capsule(cx, cy + 5.0, cx, cy + 12.5, 1.5),
        capsule(cx - 5.0, cy, cx - 12.5, cy, 1.5),
        capsule(cx + 5.0, cy, cx + 12.5, cy, 1.5),
    ]
    reticle = union(ring(circle(cx, cy, 8.5), 1.75), *arms)
    dot = circle(cx, cy, 2.6)
    return [Layer(reticle), Layer(dot, fill="accent")], (16, 16)


# ---------------------------------------------------------------------------
# Rasteriser
# ---------------------------------------------------------------------------

def rasterise(layers, palette):
    """Returns SIZE*SIZE straight-alpha RGBA tuples, row 0 at the top."""
    colours = {
        "fill": palette["Text.Primary"],
        "outline": palette["Text.OnAccent"],
        "accent": palette["Accent.Lichen"],
    }
    pixels = []
    step = 1.0 / SUPERSAMPLE
    samples = SUPERSAMPLE * SUPERSAMPLE
    for py in range(SIZE):
        for px in range(SIZE):
            acc = [0.0, 0.0, 0.0, 0.0]
            for sy in range(SUPERSAMPLE):
                for sx in range(SUPERSAMPLE):
                    x = px + (sx + 0.5) * step
                    y = py + (sy + 0.5) * step
                    colour = None
                    for layer in layers:
                        d = layer.sdf(x, y)
                        if d >= 0.0:
                            continue
                        if layer.stroke or d < -OUTLINE_WIDTH:
                            colour = colours[layer.fill]
                        else:
                            colour = colours["outline"]
                    if colour is not None:
                        r, g, b, a = colour
                        acc[0] += r * a
                        acc[1] += g * a
                        acc[2] += b * a
                        acc[3] += a
            if acc[3] == 0:
                pixels.append((0, 0, 0, 0))
            else:
                pixels.append((
                    round(acc[0] / acc[3]),
                    round(acc[1] / acc[3]),
                    round(acc[2] / acc[3]),
                    round(acc[3] / samples),
                ))
    return pixels


# ---------------------------------------------------------------------------
# PNG output
# ---------------------------------------------------------------------------

def encode_png(pixels, width, height) -> bytes:
    raw = bytearray()
    for row in range(height):
        raw.append(0)
        for col in range(width):
            raw.extend(pixels[row * width + col])

    def chunk(kind: bytes, data: bytes) -> bytes:
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

    return (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
        + chunk(b"IEND", b"")
    )


def upscale(pixels, width, height, factor):
    out = []
    for row in range(height * factor):
        src_row = row // factor
        for col in range(width * factor):
            out.append(pixels[src_row * width + col // factor])
    return out


def blit(dest, dest_w, src, src_w, src_h, x, y):
    for row in range(src_h):
        for col in range(src_w):
            dest[(y + row) * dest_w + (x + col)] = src[row * src_w + col]


# ---------------------------------------------------------------------------
# Review sheet
# ---------------------------------------------------------------------------

def data_uri(png: bytes) -> str:
    return "data:image/png;base64," + base64.b64encode(png).decode("ascii")


def write_preview(cursors, palette):
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    dark = "#%02X%02X%02X" % palette["Surface.PanelPrimary"][:3]
    canvas = "#%02X%02X%02X" % palette["Surface.Canvas"][:3]
    shots = [data_uri(p.read_bytes()) for p in SCREENSHOTS if p.exists()]

    def cursor_cells(scale):
        cells = []
        for name, (png, hotspot, _) in cursors.items():
            hx, hy = hotspot
            marker = ""
            if scale >= 4:
                marker = (
                    f'<i class="hot" style="left:{hx * scale}px;top:{hy * scale}px;'
                    f'width:{scale}px;height:{scale}px"></i>'
                )
            cells.append(
                f'<span class="cur" style="width:{SIZE * scale}px;height:{SIZE * scale}px">'
                f'<img src="{data_uri(png)}" width="{SIZE * scale}" height="{SIZE * scale}" alt="{name}">{marker}</span>'
            )
        return "".join(cells)

    def scale_strip():
        return "".join(f'<div class="scale">{cursor_cells(s)}<small>{s}x</small></div>' for s in (1, 2, 4))

    rows = []
    for label, style in [
        ("Toast (light)", "background:#E8C98A"),
        ("Panel primary", f"background:{dark}"),
        ("Canvas", f"background:{canvas}"),
    ]:
        rows.append(f'<div class="row" style="{style}"><b>{label}</b>{scale_strip()}</div>')
    for i, shot in enumerate(shots):
        rows.append(f'<div class="row shot" style="background-image:url({shot})"><b>Screenshot {i + 1}</b>{scale_strip()}</div>')
    zoom = '<div class="row zoom"><b>8x with hotspot (red)</b>' + cursor_cells(8) + "</div>"
    hotspot_text = ", ".join(f"{n} {h}" for n, (_, h, _) in cursors.items())

    html = f"""<!doctype html>
<html><head><meta charset="utf-8"><title>Cursor preview</title>
<style>
 body{{font:14px system-ui,sans-serif;margin:0;padding:16px;background:#111;color:#eee}}
 .row{{display:flex;align-items:center;gap:28px;padding:18px 16px;margin-bottom:10px;border-radius:8px;flex-wrap:wrap}}
 .row b{{width:130px;color:#fff;text-shadow:0 1px 2px #000}}
 .shot{{background-size:cover;background-position:center left}}
 .scale{{display:flex;align-items:center;gap:14px}}
 .scale small{{color:#fff;opacity:.7;text-shadow:0 1px 2px #000}}
 .cur{{position:relative;display:inline-block}}
 .cur img{{display:block;image-rendering:pixelated}}
 .zoom{{background:repeating-conic-gradient(#3a3a3a 0 25%,#2a2a2a 0 50%) 0 0/16px 16px}}
 .zoom .cur{{outline:1px solid #666}}
 .hot{{position:absolute;box-sizing:border-box;border:2px solid #ff2d2d;pointer-events:none}}
</style></head><body>
<h2 style="margin:0 0 12px">Fungus Toast cursors: arrow / hand / target</h2>
<p style="margin:0 0 14px;opacity:.75">32x32 hardware cursors. Fill Text.Primary, 1px outline Text.OnAccent, accent Accent.Lichen. Hotspots: {hotspot_text}.</p>
{''.join(rows)}
{zoom}
</body></html>
"""
    (PREVIEW_DIR / "preview.html").write_text(html, encoding="utf-8")

    # 8x contact sheet on the panel colour so it can be eyeballed without a browser.
    factor = 8
    pad = 8
    bg = palette["Surface.PanelPrimary"]
    sheet_w = len(cursors) * (SIZE * factor + pad) + pad
    sheet_h = SIZE * factor + 2 * pad
    sheet = [bg] * (sheet_w * sheet_h)
    x = pad
    for _, (_, _, pixels) in cursors.items():
        composited = []
        for r, g, b, a in upscale(pixels, SIZE, SIZE, factor):
            t = a / 255
            composited.append((round(bg[0] + (r - bg[0]) * t), round(bg[1] + (g - bg[1]) * t), round(bg[2] + (b - bg[2]) * t), 255))
        blit(sheet, sheet_w, composited, SIZE * factor, SIZE * factor, x, pad)
        x += SIZE * factor + pad
    (PREVIEW_DIR / "sheet.png").write_bytes(encode_png(sheet, sheet_w, sheet_h))


# ---------------------------------------------------------------------------

def main(argv) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--preview-only", action="store_true", help="render the review sheet without touching the Unity assets")
    args = parser.parse_args(argv)

    palette = load_tokens(STYLE_TOKENS)
    cursors = {}
    for name, builder in (("cursor_arrow", arrow_layers), ("cursor_hand", hand_layers), ("cursor_target", target_layers)):
        layers, hotspot = builder()
        pixels = rasterise(layers, palette)
        cursors[name] = (encode_png(pixels, SIZE, SIZE), hotspot, pixels)

    if not args.preview_only:
        ASSET_DIR.mkdir(parents=True, exist_ok=True)
        for name, (png, _, _) in cursors.items():
            (ASSET_DIR / f"{name}.png").write_bytes(png)
        print(f"wrote {len(cursors)} cursors to {ASSET_DIR.relative_to(REPO_ROOT).as_posix()}")
        white = [(255, 255, 255, 255)] * (HOVER_SPRITE_SIZE * HOVER_SPRITE_SIZE)
        HOVER_SPRITE.write_bytes(encode_png(white, HOVER_SPRITE_SIZE, HOVER_SPRITE_SIZE))
        print(f"wrote white highlight sprite {HOVER_SPRITE.relative_to(REPO_ROOT).as_posix()}")

    write_preview(cursors, palette)
    print(f"preview: {(PREVIEW_DIR / 'preview.html').as_posix()}")
    for name, (_, hotspot, _) in cursors.items():
        print(f"  {name}: hotspot {hotspot}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
