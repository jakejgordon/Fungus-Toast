using UnityEditor;

/// <summary>
/// Forces every texture under Assets/Resources/UI/Cursors/ to import as a hardware
/// cursor. Unity only honours CursorMode.Auto for textures whose importer type is
/// Cursor, so the generated PNGs (tools/cursor-gen/generate_cursors.py) must never be
/// left on the default Sprite/Default settings, and nobody should have to hand-edit
/// their .meta files after a regeneration.
/// </summary>
public class CursorTextureImporter : AssetPostprocessor
{
    private const string CursorFolderSegment = "/Resources/UI/Cursors/";

    private static bool IsCursorAsset(string path)
        => path.Replace("\\", "/").Contains(CursorFolderSegment);

    void OnPreprocessTexture()
    {
        if (!IsCursorAsset(assetPath)) return;

        TextureImporter importer = (TextureImporter)assetImporter;

        importer.textureType = TextureImporterType.Cursor;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = true;
        importer.filterMode = UnityEngine.FilterMode.Point;
        importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
