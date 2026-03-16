using System.IO;
using FungusToast.Unity.Grid;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class BoardMediumAssetCreator
{
    private const string DefaultSurfaceTilePath = "Assets/Tiles/Bread/BreadTileSprite_v3_0.asset";
    private const string DefaultPressedTilePath = "Assets/Tiles/Bread/BreadTileSprite_Pressed_0.asset";

    [MenuItem("Assets/Create/Configs/Toast Board Medium", priority = 305)]
    public static void CreateToastBoardMediumAsset()
    {
        string directory = GetTargetDirectory();
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(directory, "ToastBoardMedium.asset").Replace("\\", "/"));

        var asset = ScriptableObject.CreateInstance<BoardMediumConfig>();
        asset.mediumId = "toast";
        asset.overridePlayableSurface = false;
        asset.boardSurfaceTile = AssetDatabase.LoadAssetAtPath<TileBase>(DefaultSurfaceTilePath);
        asset.boardSurfaceVariantTiles = null;
        asset.surfaceVariantDensity = 0f;
        asset.renderCrust = true;
        asset.crustEdgeTile = AssetDatabase.LoadAssetAtPath<TileBase>(DefaultPressedTilePath);
        asset.crustCornerTile = AssetDatabase.LoadAssetAtPath<TileBase>(DefaultPressedTilePath);
        asset.crustThicknessRatio = 0.1f;
        asset.minCrustThickness = 1;
        asset.maxCrustThickness = 6;
        asset.useBreadSliceSilhouette = true;
        asset.topCrustRoundness = 1f;
        asset.bottomCrustRoundness = 0.35f;
        asset.crustInnerColor = new Color(0.96f, 0.8f, 0.56f, 1f);
        asset.crustMidColor = new Color(0.8f, 0.47f, 0.14f, 1f);
        asset.crustOuterColor = new Color(0.42f, 0.2f, 0.05f, 1f);
        asset.crustTopDarkening = 0.22f;
        asset.tintPerimeterTiles = false;
        asset.perimeterTintDepth = 1;
        asset.perimeterTint = new Color(0.88f, 0.78f, 0.56f, 1f);

        AssetDatabase.CreateAsset(asset, assetPath);
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }

    [MenuItem("Assets/Create/Configs/Toast Board Medium", true)]
    public static bool ValidateCreateToastBoardMediumAsset()
    {
        return !EditorApplication.isPlaying;
    }

    private static string GetTargetDirectory()
    {
        Object selectedObject = Selection.activeObject;
        if (selectedObject == null)
        {
            return "Assets";
        }

        string selectedPath = AssetDatabase.GetAssetPath(selectedObject);
        if (string.IsNullOrWhiteSpace(selectedPath))
        {
            return "Assets";
        }

        if (AssetDatabase.IsValidFolder(selectedPath))
        {
            return selectedPath;
        }

        string directory = Path.GetDirectoryName(selectedPath)?.Replace("\\", "/");
        return string.IsNullOrWhiteSpace(directory) ? "Assets" : directory;
    }
}