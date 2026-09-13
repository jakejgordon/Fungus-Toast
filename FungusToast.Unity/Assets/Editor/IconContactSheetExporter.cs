using System.Collections.Generic;
using System.IO;
using System.Linq;
using FungusToast.Core.Campaign;
using FungusToast.Core.Mutations;
using FungusToast.Core.Mycovariants;
using FungusToast.Unity.UI;
using FungusToast.Unity.UI.Icons;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ability icon tooling under the Fungus Toast/Icons menu: exports every icon to
/// TEMP/icon-sheets/unity/ as PNGs plus a grid sheet per family, and validates that every
/// adaptation, mycovariant and surge mutation has a dedicated drawing. The tools/icon-preview
/// harness renders the same drawings outside Unity (and runs the same coverage check); this is
/// the in-editor confirmation that the texture path matches.
/// </summary>
public static class IconContactSheetExporter
{
    private const string OutputRoot = "../../TEMP/icon-sheets/unity";
    private const int SheetColumns = 8;
    private const int SheetPadding = 8;

    [MenuItem("Fungus Toast/Icons/Export All Icons")]
    public static void ExportAllIcons()
    {
        ExportSurgeIcons();
        ExportMycovariantIcons();
        ExportAdaptationIcons();
    }

    [MenuItem("Fungus Toast/Icons/Export Surge Icons")]
    public static void ExportSurgeIcons()
    {
        var entries = new List<(string name, IconCanvas canvas)>();
        foreach (Mutation mutation in SurgeMutations())
        {
            var canvas = new IconCanvas();
            SurgeIcons.Draw(canvas, mutation.Id);
            entries.Add((mutation.Name, canvas));
        }

        Export("surges", entries);
    }

    [MenuItem("Fungus Toast/Icons/Export Mycovariant Icons")]
    public static void ExportMycovariantIcons()
    {
        var entries = new List<(string name, IconCanvas canvas)>();
        foreach (Mycovariant mycovariant in MycovariantRepository.All)
        {
            var canvas = new IconCanvas();
            MycovariantIcons.Draw(canvas, mycovariant);
            entries.Add((mycovariant.Name, canvas));
        }

        Export("mycovariants", entries);
    }

    [MenuItem("Fungus Toast/Icons/Export Adaptation Icons")]
    public static void ExportAdaptationIcons()
    {
        var entries = new List<(string name, IconCanvas canvas)>();
        foreach (AdaptationDefinition adaptation in AdaptationRepository.All)
        {
            var canvas = new IconCanvas();
            AdaptationIcons.Draw(canvas, adaptation.IconId);
            entries.Add((adaptation.Name, canvas));
        }

        Export("adaptations", entries);
    }

    /// <summary>Logs an error for every item that would draw the fallback icon.</summary>
    [MenuItem("Fungus Toast/Icons/Validate Icon Coverage")]
    public static void ValidateIconCoverage()
    {
        var missing = new List<string>();
        foreach (AdaptationDefinition adaptation in AdaptationRepository.All)
        {
            if (!AdaptationIcons.HasDedicatedIcon(adaptation.IconId))
            {
                missing.Add($"Adaptation '{adaptation.Name}' (IconId {adaptation.IconId}): add a drawer to AdaptationIcons");
            }
        }

        foreach (Mycovariant mycovariant in MycovariantRepository.All)
        {
            if (!MycovariantIcons.HasDedicatedIcon(mycovariant.Id))
            {
                missing.Add($"Mycovariant '{mycovariant.Name}' (Id {mycovariant.Id}): add a case to MycovariantIcons");
            }
        }

        foreach (Mutation mutation in SurgeMutations())
        {
            if (!SurgeIcons.HasDedicatedIcon(mutation.Id))
            {
                missing.Add($"Surge '{mutation.Name}' (Id {mutation.Id}): add a case to SurgeIcons");
            }
        }

        if (missing.Count == 0)
        {
            Debug.Log("Icon coverage: every adaptation, mycovariant and surge has a dedicated drawing.");
            return;
        }

        foreach (string item in missing)
        {
            Debug.LogError("Icon coverage: " + item);
        }
    }

    private static IEnumerable<Mutation> SurgeMutations() => MutationRegistry.GetAll().Where(m => m.IsSurge).OrderBy(m => m.Id);

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
