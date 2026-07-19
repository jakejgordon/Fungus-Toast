#!/usr/bin/env python3
"""Crop a transparent mold source to its alpha bounds and frame it for a tile."""

import argparse
from pathlib import Path

from PIL import Image


def parse_args():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--width", type=int, required=True, help="Visible target width in pixels.")
    parser.add_argument("--height", type=int, required=True, help="Visible target height in pixels.")
    parser.add_argument("--left", type=int, required=True, help="Left placement in the output tile.")
    parser.add_argument("--top", type=int, required=True, help="Top placement in the output tile.")
    parser.add_argument("--size", type=int, default=64)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    with Image.open(args.input) as source:
        rgba = source.convert("RGBA")
    bbox = rgba.getchannel("A").getbbox()
    if bbox is None:
        raise SystemExit(f"{args.input} contains no visible pixels")
    if args.left < 2 or args.top < 2 or args.left + args.width > args.size - 2 or args.top + args.height > args.size - 2:
        raise SystemExit("target placement violates the required 2px transparent outer border")

    cropped = rgba.crop(bbox)
    framed_subject = cropped.resize((args.width, args.height), Image.Resampling.LANCZOS)
    output = Image.new("RGBA", (args.size, args.size), (0, 0, 0, 0))
    output.alpha_composite(framed_subject, (args.left, args.top))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    output.save(args.output, format="PNG")
    print(f"Wrote {args.output}: source bbox={bbox}, visible target={args.width}x{args.height} at ({args.left},{args.top})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
