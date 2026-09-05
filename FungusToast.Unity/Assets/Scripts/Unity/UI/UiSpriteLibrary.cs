using UnityEngine;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Loads shared UI sprites that code-built panels need but should not carry as
    /// per-instance <c>[SerializeField]</c> assignments. Backed by
    /// <c>Assets/Resources/UI/</c> so the art selection stays a real project asset
    /// (see <see href="../../../../FungusToast.Core/docs/UNITY_CODE_FIRST_MIGRATION.md">
    /// UNITY_CODE_FIRST_MIGRATION.md</see>) while the call site stays greppable.
    /// </summary>
    public static class UiSpriteLibrary
    {
        private static Sprite titleLogoWords;
        private static Sprite backArrow;
        private static Sprite forwardArrow;

        // The scene's pre-migration wideTitleLogoSprite field pointed to
        // fungus_toast_logo_1520x651.png, a file deleted by commit d745b8c
        // ("better logo") that added this banner as its replacement — the
        // serialized reference was just never updated. This is the correct
        // asset: its aspect ratio (1914/821 = 2.331) exactly matches the
        // logo's target box (WideLogoWidth/WideLogoHeight = 520/223 = 2.332).
        public static Sprite TitleLogoWords => titleLogoWords ??= Load("fungus_toast_banner_1914x821");

        public static Sprite BackArrow => backArrow ??= Load("back_arrow_icon_256x256");

        public static Sprite ForwardArrow => forwardArrow ??= Load("forward_arrow_icon_256x256");

        private static Sprite Load(string resourceName)
        {
            string path = $"UI/{resourceName}";

            // Resources.Load<Sprite> only returns a result when the texture's
            // Sprite Mode is Single; some of these were imported as Multiple (a
            // texture holding one named sub-sprite), where the main asset at the
            // path is the Texture2D, not the Sprite.
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            Sprite[] sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }

            // Last resort: build a Sprite from the raw texture, in case neither of
            // the above resolved a Sprite sub-asset for this import configuration.
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture != null)
            {
                return Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
            }

            Debug.LogError($"UiSpriteLibrary: missing Resources/{path} (Resources.Load found nothing of any type at that path).");
            return null;
        }
    }
}
