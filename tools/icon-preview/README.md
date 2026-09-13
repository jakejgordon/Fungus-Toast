# icon-preview

Renders the ability icons (adaptations, mycovariants, surges) with the same drawing
code Unity uses, without opening the editor, and checks that every item has a
dedicated drawing.

```
cd tools/icon-preview
dotnet run
```

Writes PNGs per icon (at 128px and at the in-game slot sizes) and
`TEMP/icon-sheets/icon-review.html`, a review sheet showing each icon at every size
next to a one-line summary of the ability. Pass `--out <dir>` after `--` to change
the output folder. The process exits 1 and lists the offenders if any adaptation,
mycovariant or surge mutation would draw the fallback icon.

How it works: the icon sources under
`FungusToast.Unity/Assets/Scripts/Unity/UI/Icons/` are pure managed drawing into a
`Color[]` buffer, so they compile here against `UnityEngine.CoreModule.dll` (structs
only) plus a stub of `UIStyleTokens` generated from the real file by
`generate_style_stub.py`. Core types come from the committed
`FungusToast.Unity/Assets/Plugins/FungusToast.Core.dll`, so this never rebuilds Core.
Set `UNITY_EDITOR_DIR` if the editor is not under the default Unity Hub path.
