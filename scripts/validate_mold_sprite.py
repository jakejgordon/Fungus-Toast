#!/usr/bin/env python3
"""Resize and validate transparent mold tile sprites for the 64x64 board pipeline."""

import argparse
from pathlib import Path
import sys

from PIL import Image


FOOTPRINT_BANDS = {
    "isolated": (0.45, 0.65),
    "clustered": (0.65, 0.78),
    "dense": (0.78, 0.88),
}


def parse_args():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path)
    parser.add_argument("--resize-output", type=Path, help="Write an alpha-aware 64x64 PNG before validating it.")
    parser.add_argument("--state", choices=sorted(FOOTPRINT_BANDS), required=True)
    parser.add_argument("--size", type=int, default=64)
    return parser.parse_args()


def validate(path: Path, state: str, size: int) -> bool:
    with Image.open(path) as original:
        if "A" not in original.getbands():
            print(f"FAIL {path}: no alpha channel")
            return False
        image = original.convert("RGBA")

    if image.size != (size, size):
        print(f"FAIL {path}: expected {size}x{size}, found {image.width}x{image.height}")
        return False

    alpha = image.getchannel("A")
    alpha_values = list(alpha.getdata())
    opaque_pixels = sum(value > 0 for value in alpha_values)
    if opaque_pixels == 0:
        print(f"FAIL {path}: fully transparent")
        return False
    if opaque_pixels == size * size:
        print(f"FAIL {path}: fully opaque")
        return False

    corners = [alpha.getpixel(point) for point in ((0, 0), (size - 1, 0), (0, size - 1), (size - 1, size - 1))]
    if any(value != 0 for value in corners):
        print(f"FAIL {path}: opaque corner(s): {corners}")
        return False

    border_violations = sum(
        alpha.getpixel((x, y)) > 0
        for y in range(size)
        for x in range(size)
        if x < 2 or x >= size - 2 or y < 2 or y >= size - 2
    )
    if border_violations:
        print(f"FAIL {path}: {border_violations} nontransparent pixels in the outer 2px border")
        return False

    bbox = alpha.getbbox()
    left, top, right, bottom = bbox
    footprint = ((right - left) * (bottom - top)) / float(size * size)
    minimum, maximum = FOOTPRINT_BANDS[state]
    if not minimum <= footprint <= maximum:
        print(f"FAIL {path}: {state} alpha footprint {footprint:.1%}; expected {minimum:.0%}-{maximum:.0%}")
        return False

    coverage = opaque_pixels / float(size * size)
    solid_coverage = sum(value >= 128 for value in alpha_values) / float(size * size)
    print(
        f"PASS {path}: bbox={right-left}x{bottom-top} at ({left},{top}), "
        f"footprint={footprint:.1%}, alpha>0 coverage={coverage:.1%}, alpha>=128 coverage={solid_coverage:.1%}"
    )
    return True


def main() -> int:
    args = parse_args()
    source = args.input
    if not source.is_file():
        print(f"FAIL {source}: file not found")
        return 1

    path_to_validate = source
    if args.resize_output:
        args.resize_output.parent.mkdir(parents=True, exist_ok=True)
        with Image.open(source) as image:
            rgba = image.convert("RGBA")
            resized = rgba.resize((args.size, args.size), Image.Resampling.LANCZOS)
            resized.save(args.resize_output, format="PNG")
        path_to_validate = args.resize_output

    return 0 if validate(path_to_validate, args.state, args.size) else 1


if __name__ == "__main__":
    sys.exit(main())
