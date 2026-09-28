# Backdrop source art

Source images for the photographic backdrops behind each medium. The layering, realism rule, and density targets are in `FungusToast.Core/docs/NEW_BACKGROUND_HELPER.md` (Mediums And Backdrops).

Runtime copies live in `FungusToast.Unity/Assets/Resources/Backdrops/`; update both when an image changes. All four were processed with ChatGPT from the owner's own photos. They are 8-bit sRGB PNGs with no metadata.

| File | Layer | Used for | Real size |
|------|-------|----------|-----------|
| `cutting_board_surface_2048x1504.png` | Surface (alpha cutout) | Whole cutting board, seen when zoomed out | 17.25 x 12.75 in (43.8 x 32.4 cm), about 45 px/cm |
| `cutting_board_material_1024x1024.png` | Seamless tile | Setting under the plate (the plate was photographed on a larger board of the same material). Too coarse for close-zoom detail; see below. | Grain matches the board photo at about 45-52 cm per tile, so only about 20-23 px/cm |
| `plate_surface_1476x1476.png` | Surface (alpha cutout) | Plate, sits on the cutting-board material | 8.5 in (21.6 cm) diameter, about 67 px/cm |
| `countertop_material_512x512.png` | Seamless tile | Setting under the cutting board, seen only when zoomed out | Not yet set (about 15 cm per tile reads naturally) |

## Edits made after delivery

- `plate_surface`: removed a brown fleck and a hair-like mark on the right rim by replacing only the pixels darker than their local median. The rim's embossed grid is untouched.
- A second tile, `large_cutting_board_material.png`, was byte-identical to the cutting board tile and was dropped.

## Still needed

- Real sizes of each medium, so the game can place them at true physical scale.
- A close-zoom material tile for the cutting board. At default zoom the area around the medium is shown at roughly 120 px/cm, and the current tile holds about 20 px/cm. It should be made from a dedicated close-up photo of the board surface rather than from the whole-board shot.
