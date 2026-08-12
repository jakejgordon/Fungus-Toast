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

        public static class Player
        {
            public static readonly Color Blue = Hex("#0072D1");
            public static readonly Color Orange = Hex("#FF8A00");
            public static readonly Color Sky = Hex("#00AEEF");
            public static readonly Color Purple = Hex("#8E5DFF");
            public static readonly Color Yellow = Hex("#7EA000");
            public static readonly Color Teal = Hex("#008F7A");
            public static readonly Color Vermillion = Hex("#C73E1D");

            public static Color GetByIndex(int index)
            {
                switch (Mathf.Abs(index) % 7)
                {
                    case 0: return Blue;
                    case 1: return Orange;
                    case 2: return Sky;
                    case 3: return Purple;
                    case 4: return Yellow;
                    case 5: return Teal;
                    default: return Vermillion;
                }
            }
        }

        public static class Category
        {
            public static readonly Color Growth = Hex("#5F8F61");
            public static readonly Color CellularResilience = Hex("#5A7289");
            public static readonly Color Fungicide = Hex("#6E5A86");
            public static readonly Color GeneticDrift = Hex("#7D6B4E");
            public static readonly Color MycelialSurges = Hex("#80607A");
        }

        /// <summary>
        /// Shared presentation primitives for the launch-to-game setup flow.
        /// Screen controllers remain responsible for composition and content.
        /// </summary>
        public static class Startup
        {
            public const float ContentWidth = 760f;
            public const float CardWidth = 620f;
            public const float SectionSpacing = 16f;
            public const float ControlSpacing = 12f;

            public static void ApplyCard(Image card, bool elevated = false, float alpha = 0.9f)
            {
                if (card == null)
                {
                    return;
                }

                card.color = WithAlpha(elevated ? Surface.PanelSecondary : Surface.PanelPrimary, alpha);
            }

            public static void ApplyScreenHeading(TextMeshProUGUI label)
            {
                ApplyText(label, Text.Primary, FontStyles.Bold);
            }

            public static void ApplySectionHeading(TextMeshProUGUI label)
            {
                ApplyText(label, Accent.Hyphae, FontStyles.Bold);
            }

            public static void ApplySupportingCopy(TextMeshProUGUI label)
            {
                ApplyText(label, Text.Secondary, FontStyles.Normal);
            }

            public static void ApplyMutedCopy(TextMeshProUGUI label)
            {
                ApplyText(label, Text.Muted, FontStyles.Normal);
            }

            public static void ApplyChoice(
                UnityEngine.UI.Button button,
                bool isSelected,
                bool isAvailable = true,
                Image selectionOverlay = null)
            {
                if (button == null)
                {
                    return;
                }

                button.interactable = isAvailable;
                var colors = Button.BuildChoiceColorBlock(isSelected);
                button.colors = colors;
                Button.SetButtonLabelColor(
                    button,
                    isAvailable ? Button.TextDefault : Button.TextDisabled);

                if (selectionOverlay != null)
                {
                    selectionOverlay.color = WithAlpha(Accent.Lichen, Alpha.SelectionFill);
                    selectionOverlay.enabled = isSelected;
                    selectionOverlay.gameObject.SetActive(isSelected);
                }
            }

            private static void ApplyText(TextMeshProUGUI label, Color color, FontStyles style)
            {
                if (label == null)
                {
                    return;
                }

                label.color = color;
                label.fontStyle = style;
            }
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

            public static ColorBlock BuildChoiceColorBlock(bool useSelectedAsNormal, float fadeDuration = 0.1f)
            {
                Color normal = useSelectedAsNormal ? BackgroundSelected : BackgroundDefault;
                Color hover = useSelectedAsNormal
                    ? Color.Lerp(BackgroundSelected, Accent.Lichen, 0.32f)
                    : BackgroundHover;

                return new ColorBlock
                {
                    normalColor = normal,
                    highlightedColor = hover,
                    pressedColor = BackgroundPressed,
                    selectedColor = BackgroundSelected,
                    disabledColor = BackgroundDisabled,
                    colorMultiplier = 1f,
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

            public static void ApplyStartupUtilityAction(UnityEngine.UI.Button button)
            {
                if (button == null)
                {
                    return;
                }

                Color normal = Color.Lerp(Surface.PanelSecondary, Accent.Hyphae, 0.08f);
                Color hover = Color.Lerp(normal, Accent.Spore, 0.18f);
                Color pressed = Color.Lerp(normal, Surface.PanelPrimary, 0.18f);

                button.colors = new ColorBlock
                {
                    normalColor = normal,
                    highlightedColor = hover,
                    pressedColor = pressed,
                    selectedColor = Color.Lerp(normal, Accent.Spore, 0.24f),
                    disabledColor = WithAlpha(
                        Color.Lerp(Surface.PanelSecondary, Surface.PanelPrimary, 0.5f),
                        Alpha.PanelDisabled),
                    colorMultiplier = 1f,
                    fadeDuration = 0.1f
                };

                SetButtonLabelColor(button, Text.Primary);
            }

            public static void ApplyDangerMenuAction(
                UnityEngine.UI.Button button,
                float width = DesktopPrimaryMenuActionWidth,
                float preferredHeight = DesktopMenuActionHeight,
                float minHeight = MinimumMenuActionHeight)
            {
                if (button == null)
                {
                    return;
                }

                ConfigureMenuActionLayout(button, width, preferredHeight, minHeight);
                Color normal = Color.Lerp(Surface.PanelElevated, State.Danger, 0.2f);
                Color hover = Color.Lerp(normal, State.Danger, 0.28f);
                button.colors = new ColorBlock
                {
                    normalColor = normal,
                    highlightedColor = hover,
                    pressedColor = Color.Lerp(Surface.PanelPrimary, State.Danger, 0.3f),
                    selectedColor = hover,
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
