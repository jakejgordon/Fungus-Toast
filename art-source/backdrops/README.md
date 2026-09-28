# Backdrop source art

Source images for the photographic backdrops behind each medium. The layering, realism rule, and density targets are in `FungusToast.Core/docs/NEW_BACKGROUND_HELPER.md` (Mediums And Backdrops).

All four were processed with ChatGPT from the owner's own photos. They are 8-bit sRGB PNGs with no metadata.

| File | Layer | Used for | Real size |
|------|-------|----------|-----------|
| `cutting_board_surface_2048x1504.png` | Surface (alpha cutout) | Whole cutting board, seen when zoomed out | Not yet measured (proportions match a 44.5 x 33 cm board) |
| `cutting_board_material_1024x1024.png` | Seamless tile | Close-zoom detail on the cutting board, and the setting under the plate (the plate was photographed on a larger board of the same material) | Tile size to be set by matching its grain to the board photo |
| `plate_surface_1476x1476.png` | Surface (alpha cutout) | Plate, sits on the cutting-board material | Not yet measured (typical dinner plate is about 27 cm) |
| `countertop_material_512x512.png` | Seamless tile | Setting under the cutting board, seen only when zoomed out | Not yet set (about 15 cm per tile reads naturally) |

## Edits made after delivery

- `plate_surface`: removed a brown fleck and a hair-like mark on the right rim by replacing only the pixels darker than their local median. The rim's embossed grid is untouched.
- A second tile, `large_cutting_board_material.png`, was byte-identical to the cutting board tile and was dropped.

## Still needed

Real measurements of the cutting board, the plate, and each medium, so the game can place mediums at true physical scale.
