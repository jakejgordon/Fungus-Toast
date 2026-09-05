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

        public static Sprite TitleLogoWords => titleLogoWords ??= Load("fungus_toast_logo_words_1001x455");

        public static Sprite BackArrow => backArrow ??= Load("back_arrow_icon_256x256");

        public static Sprite ForwardArrow => forwardArrow ??= Load("forward_arrow_icon_256x256");

        private static Sprite Load(string resourceName)
        {
            var sprite = Resources.Load<Sprite>($"UI/{resourceName}");
            if (sprite == null)
            {
                Debug.LogError($"UiSpriteLibrary: missing Resources/UI/{resourceName}.");
            }

            return sprite;
        }
    }
}
