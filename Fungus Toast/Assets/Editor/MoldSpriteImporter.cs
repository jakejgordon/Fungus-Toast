using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.Tilemaps;

public class MoldSpriteImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Contains("MoldSprites")) return;

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
        if (!assetPath.Contains("MoldSprites")) return;

        string spritePath = assetPath;
        string spriteName = Path.GetFileNameWithoutExtension(spritePath);
        string spriteDir = Path.GetDirectoryName(spritePath);
        string tilePath = Path.Combine(spriteDir, spriteName + ".asset").Replace("\\", "/");

        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile != null)
        {
            // Assign the reimported sprite to the tile
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            Debug.Log($"🔄 Reassigned sprite '{spriteName}' to tile asset at {tilePath}");
        }
    }
}
