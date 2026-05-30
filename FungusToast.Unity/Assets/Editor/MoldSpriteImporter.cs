using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.Tilemaps;

public class MoldSpriteImporter : AssetPostprocessor
{
    private const string MoldTileFolderSegment = "/Sprites/Tiles/Mold/";
    private const string LegacyMoldTileFolder = "Assets/Tiles/Mold";

    private static bool IsMoldTileAsset(string path)
        => path.Replace("\\", "/").Contains(MoldTileFolderSegment);

    private static string ResolveTilePath(string spritePath)
    {
        string spriteName = Path.GetFileNameWithoutExtension(spritePath);
        string spriteDir = Path.GetDirectoryName(spritePath) ?? string.Empty;
        string sameFolderTilePath = Path.Combine(spriteDir, spriteName + ".asset").Replace("\\", "/");
        if (File.Exists(sameFolderTilePath))
        {
            return sameFolderTilePath;
        }

        return Path.Combine(LegacyMoldTileFolder, spriteName + ".asset").Replace("\\", "/");
    }

    void OnPreprocessTexture()
    {
        if (!IsMoldTileAsset(assetPath)) return;

        TextureImporter importer = (TextureImporter)assetImporter;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;

        // Apply spriteMeshType using TextureImporterSettings
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
    }

    void OnPostprocessTexture(Texture2D texture)
    {
        if (!IsMoldTileAsset(assetPath)) return;

        string spritePath = assetPath;
        string tilePath = ResolveTilePath(spritePath);

        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile != null)
        {
            // Assign the reimported sprite to the tile
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
        }
    }
}
