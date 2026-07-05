using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Canonical UI style tokens for Fungus Toast.
    ///
    /// This class is intentionally infrastructure-only in its initial rollout:
    /// adding tokens does not change visuals until consumers adopt them.
    /// </summary>
    public static class UIStyleTokens
    {
        public static class Surface
        {
            public static readonly Color Canvas = Hex("#26271F");
            public static readonly Color PanelPrimary = Hex("#34382C");
            public static readonly Color PanelSecondary = Hex("#424837");
            public static readonly Color PanelElevated = Hex("#515A45");
            public static readonly Color OverlayDim = Hex("#12161DCC");
        }

        public static class Accent
        {
            public static readonly Color Moss = Hex("#718E43");
            public static readonly Color Lichen = Hex("#90AE5B");
            public static readonly Color Spore = Hex("#BCCB88");
            public static readonly Color Hyphae = Hex("#D9DEC0");
            public static readonly Color Putrefaction = Hex("#7F6242");
        }

        public static class Text
        {
            public static readonly Color Primary = Hex("#F1F3EE");
            public static readonly Color Secondary = Hex("#D9DED3");
            public static readonly Color Muted = Hex("#B6BEAF");
            public static readonly Color Disabled = Hex("#7A8174");
            public static readonly Color OnAccent = Hex("#1B2117");
        }

        public static class State
        {
            public static readonly Color Success = Hex("#A9CC63");
            public static readonly Color Info = Hex("#7EA4A6");
            public static readonly Color Warning = Hex("#D1AE63");
            public static readonly Color Danger = Hex("#B45E5E");
            public static readonly Color Focus = Hex("#B3C77A");
        }

        public static class Category
        {
            public static readonly Color Growth = Hex("#5F8F61");
            public static readonly Color CellularResilience = Hex("#5A7289");
            public static readonly Color Fungicide = Hex("#6E5A86");
            public static readonly Color GeneticDrift = Hex("#7D6B4E");
            public static readonly Color MycelialSurges = Hex("#80607A");
        }

        public static class Button
        {
            public const float DesktopPrimaryMenuActionWidth = 500f;
            public const float DesktopCompactMenuActionWidth = 330f;
            public const float NarrowMenuActionWidth = 470f;
            public const float DesktopMenuActionHeight = 56f;
            public const float NarrowMenuActionHeight = 52f;
            public const float MinimumMenuActionHeight = 48f;

            public static readonly Color BackgroundDefault = Hex("#DFE4D4");
            public static readonly Color BackgroundHover = Hex("#EBEFE2");
            public static readonly Color BackgroundPressed = Hex("#C9D2BA");
            public static readonly Color BackgroundSelected = Hex("#98BE74");
            public static readonly Color BackgroundDisabled = Hex("#A7AE9C");

            public static readonly Color TextDefault = Hex("#202418");
            public static readonly Color TextDisabled = Hex("#666B5E");

            public static ColorBlock BuildColorBlock(float colorMultiplier = 1f, float fadeDuration = 0.1f)
            {
                return new ColorBlock
                {
                    normalColor = BackgroundDefault,
                    highlightedColor = BackgroundHover,
                    pressedColor = BackgroundPressed,
                    selectedColor = BackgroundSelected,
                    disabledColor = BackgroundDisabled,
                    colorMultiplier = colorMultiplier,
                    fadeDuration = fadeDuration
                };
            }

            public static void ApplyStyle(UnityEngine.UI.Button button, bool useSelectedAsNormal = false)
            {
                if (button == null)
                {
                    return;
                }

                var colors = BuildColorBlock();
                if (useSelectedAsNormal)
                {
                    colors.normalColor = BackgroundSelected;
                }

                button.colors = colors;
                SetButtonLabelColor(button, TextDefault);
            }

            public static void ConfigureMenuActionLayout(
                UnityEngine.UI.Button button,
                float width,
                float preferredHeight = DesktopMenuActionHeight,
                float minHeight = MinimumMenuActionHeight)
            {
                if (button == null)
                {
                    return;
                }

                var layoutElement = button.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = button.gameObject.AddComponent<LayoutElement>();
                }

                layoutElement.minWidth = width;
                layoutElement.preferredWidth = width;
                layoutElement.minHeight = minHeight;
                layoutElement.preferredHeight = preferredHeight;
                layoutElement.flexibleWidth = 0f;
                layoutElement.flexibleHeight = 0f;

                var rectTransform = button.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(width, preferredHeight);
                }
            }

            public static void ApplyPrimaryMenuAction(
                UnityEngine.UI.Button button,
                float width = DesktopPrimaryMenuActionWidth,
                bool useSelectedAsNormal = false,
                float preferredHeight = DesktopMenuActionHeight,
                float minHeight = MinimumMenuActionHeight)
            {
                ConfigureMenuActionLayout(button, width, preferredHeight, minHeight);
                ApplyStyle(button, useSelectedAsNormal);
            }

            public static void ApplyAffirmativeMenuAction(
                UnityEngine.UI.Button button,
                float width = DesktopPrimaryMenuActionWidth,
                float preferredHeight = DesktopMenuActionHeight,
                float minHeight = MinimumMenuActionHeight)
            {
                ApplyPrimaryMenuAction(button, width, useSelectedAsNormal: true, preferredHeight: preferredHeight, minHeight: minHeight);
            }

            public static void ApplyNeutralMenuAction(
                UnityEngine.UI.Button button,
                float width = DesktopPrimaryMenuActionWidth,
                float preferredHeight = DesktopMenuActionHeight,
                float minHeight = MinimumMenuActionHeight)
            {
                ApplyPrimaryMenuAction(button, width, useSelectedAsNormal: false, preferredHeight: preferredHeight, minHeight: minHeight);
            }

            public static void ApplySecondaryMenuAction(
                UnityEngine.UI.Button button,
                float width = DesktopPrimaryMenuActionWidth,
                float preferredHeight = DesktopMenuActionHeight,
                float minHeight = MinimumMenuActionHeight)
            {
                ConfigureMenuActionLayout(button, width, preferredHeight, minHeight);
                ApplyPanelSecondaryStyle(button);
            }

            public static void ApplyPanelSecondaryStyle(UnityEngine.UI.Button button)
            {
                if (button == null)
                {
                    return;
                }

                Color hoverColor = Color.Lerp(Surface.PanelElevated, Accent.Moss, 0.34f);
                Color pressedColor = Color.Lerp(Surface.PanelPrimary, Accent.Moss, 0.18f);

                button.colors = new ColorBlock
                {
                    normalColor = Surface.PanelElevated,
                    highlightedColor = hoverColor,
                    pressedColor = pressedColor,
                    selectedColor = hoverColor,
                    disabledColor = WithAlpha(Surface.PanelPrimary, Alpha.PanelDisabled),
                    colorMultiplier = 1f,
                    fadeDuration = 0.1f
                };

                SetButtonLabelColor(button, Text.Primary);
            }

            public static void SetButtonLabelColor(UnityEngine.UI.Button button, Color color)
            {
                if (button == null)
                {
                    return;
                }

                var tmpLabels = button.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int i = 0; i < tmpLabels.Length; i++)
                {
                    tmpLabels[i].color = color;
                    tmpLabels[i].fontStyle = FontStyles.Bold;
                }

                var labels = button.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i].color = color;
                    labels[i].fontStyle = FontStyle.Bold;
                }
            }
        }

        public static class Alpha
        {
            public const float PanelDisabled = 0.6f;
            public const float InactivePanel = 0.52f;
            public const float PerspectiveHighlight = 0.38f;
            public const float FocusOutline = 0.8f;
            public const float AccentOutline = 0.35f;
            public const float BadgeTint = 0.18f;
            public const float DetailsOverlay = 0.88f;
            public const float ScrollSurface = 0.22f;
            public const float SelectionFill = 0.7f;
            public const float MutedFill = 0.45f;
            public const float ViewportChrome = 0.04f;
            public const float ToggleChrome = 0.12f;
            public const float InvisibleViewport = 0.01f;
            public const float InvisibleHitbox = 0.001f;
        }

        public static void ApplyPanelSurface(GameObject panelRoot, Color color)
        {
            if (panelRoot == null)
            {
                return;
            }

            var image = panelRoot.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }

        public static void ApplyNonButtonTextPalette(GameObject root, float headingSizeThreshold = 34f)
        {
            if (root == null)
            {
                return;
            }

            var tmpLabels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < tmpLabels.Length; i++)
            {
                if (IsInsideButton(tmpLabels[i].transform))
                {
                    continue;
                }

                tmpLabels[i].color = tmpLabels[i].fontSize >= headingSizeThreshold ? Text.Primary : Text.Secondary;
            }

            var labels = root.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (IsInsideButton(labels[i].transform))
                {
                    continue;
                }

                labels[i].color = Text.Secondary;
            }
        }

        private static bool IsInsideButton(Transform target)
        {
            return target.GetComponentInParent<UnityEngine.UI.Button>() != null;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static string ToHtmlRgb(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }

        private static Color Hex(string html)
        {
            if (ColorUtility.TryParseHtmlString(html, out var color))
            {
                return color;
            }

            Debug.LogWarning($"UIStyleTokens could not parse color '{html}'. Falling back to magenta.");
            return Color.magenta;
        }
    }
}
