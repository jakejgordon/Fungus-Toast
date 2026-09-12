using System.Collections.Generic;
using System.IO;
using FungusToast.Core.Mutations;
using FungusToast.Unity.UI.Icons;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Writes every ability icon to TEMP/icon-sheets/unity/ as individual PNGs plus one grid sheet
/// per family, so icon changes can be reviewed without clicking through game flows. The
/// tools/icon-preview harness renders the same drawings outside Unity; this exporter is the
/// check that the in-editor path (texture creation, mipmaps) matches it.
/// </summary>
public static class IconContactSheetExporter
{
    private const string OutputRoot = "../../TEMP/icon-sheets/unity";
    private const int SheetColumns = 6;
    private const int SheetPadding = 8;

    [MenuItem("Fungus Toast/Icons/Export Surge Icons")]
    public static void ExportSurgeIcons()
    {
        var entries = new List<(string name, IconCanvas canvas)>();
        foreach (int id in SurgeIcons.SurgeMutationIds)
        {
            var canvas = new IconCanvas();
            SurgeIcons.Draw(canvas, id);
            string name = MutationRegistry.GetById(id)?.Name ?? $"mutation_{id}";
            entries.Add((name, canvas));
        }

        Export("surges", entries);
    }

    private static void Export(string family, List<(string name, IconCanvas canvas)> entries)
    {
        string directory = Path.GetFullPath(Path.Combine(Application.dataPath, OutputRoot, family));
        Directory.CreateDirectory(directory);

        foreach (var (name, canvas) in entries)
        {
            File.WriteAllBytes(Path.Combine(directory, Slug(name) + ".png"), EncodePng(canvas.Pixels, canvas.Size, canvas.Size));
        }

        if (entries.Count > 0)
        {
            int size = entries[0].canvas.Size;
            int columns = Mathf.Min(SheetColumns, entries.Count);
            int rows = Mathf.CeilToInt(entries.Count / (float)columns);
            int width = columns * (size + SheetPadding) + SheetPadding;
            int height = rows * (size + SheetPadding) + SheetPadding;
            var sheet = new Color[width * height];
            Color background = UIStyleTokens.Surface.Canvas;
            for (int i = 0; i < sheet.Length; i++)
            {
                sheet[i] = background;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                int column = index % columns;
                int row = index / columns;
                int originX = SheetPadding + column * (size + SheetPadding);
                int originY = height - SheetPadding - (row + 1) * size - row * SheetPadding;
                Color[] pixels = entries[index].canvas.Pixels;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        sheet[(originY + y) * width + originX + x] = pixels[y * size + x];
                    }
                }
            }

            File.WriteAllBytes(Path.Combine(directory, $"sheet_{family}.png"), EncodePng(sheet, width, height));
        }

        Debug.Log($"IconContactSheetExporter: wrote {entries.Count} {family} icons to {directory}");
        EditorUtility.RevealInFinder(directory);
    }

    private static byte[] EncodePng(Color[] pixels, int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
        texture.SetPixels(pixels);
        texture.Apply();
        byte[] png = texture.EncodeToPNG();
        Object.DestroyImmediate(texture);
        return png;
    }

    private static string Slug(string name)
    {
        var characters = name.ToLowerInvariant().ToCharArray();
        for (int i = 0; i < characters.Length; i++)
        {
            if (!char.IsLetterOrDigit(characters[i]))
            {
                characters[i] = '_';
            }
        }

        return new string(characters).Trim('_');
    }
}
