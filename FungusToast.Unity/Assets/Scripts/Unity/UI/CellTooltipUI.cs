using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Death;
using FungusToast.Core.Growth;
using System;
using System.Text;

namespace FungusToast.Unity.UI
{
    /// <summary>
    /// Cell hover-tooltip that renders ALL content in a single TextMeshProUGUI.
    ///
    /// v2 "nuclear" layout: previous versions used 9 row-group GameObjects managed by
    /// VerticalLayoutGroup + LayoutElement, which caused persistent text-overlap bugs.
    /// This version hides every prefab row group, re-parents one TMP directly under
    /// the root, builds all content as rich text, and imperatively sizes the
    /// RectTransform.  No layout groups, no ContentSizeFitters, no LayoutElements.
    /// </summary>
    public class CellTooltipUI : MonoBehaviour
    {
        // ── Serialized prefab references (kept for backward-compat) ────────
        [Header("Text Components")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI deathReasonText;
        [SerializeField] private TextMeshProUGUI ownerText;
        [SerializeField] private TextMeshProUGUI lastOwnerText;
        [SerializeField] private TextMeshProUGUI growthAgeText;
        [SerializeField] private TextMeshProUGUI expirationText;
        [SerializeField] private TextMeshProUGUI resistantText;
        [SerializeField] private TextMeshProUGUI growthSourceText;
        [SerializeField] private TextMeshProUGUI additionalInfoText;

        [Header("Icon Components")]
        [SerializeField] private Image statusIcon;
        [SerializeField] private Image ownerIcon;
        [SerializeField] private Image lastOwnerIcon;
        [SerializeField] private Image toxinIcon;
        [SerializeField] private Image resistantIcon;

        [Header("Layout Groups (hidden at runtime – kept for prefab stability)")]
        [SerializeField] private GameObject statusGroup;
        [SerializeField] private GameObject ownerGroup;
        [SerializeField] private GameObject deathReasonGroup;
        [SerializeField] private GameObject lastOwnerGroup;
        [SerializeField] private GameObject ageGroup;
        [SerializeField] private GameObject expirationGroup;
        [SerializeField] private GameObject resistantGroup;
        [SerializeField] private GameObject growthSourceGroup;
        [SerializeField] private GameObject additionalInfoGroup;

        [Header("Style")]
        [SerializeField] private Image tooltipBackgroundImage;
        [SerializeField, Range(0.5f, 1f)] private float tooltipBackgroundAlpha = 0.96f;

        // ── Constants ──────────────────────────────────────────────────────
        private const float TooltipWidth = 332f;
        private const float LeftPadding = 28f;
        private const float RightPadding = 16f;
        private const float VerticalPadding = 16f;
        private const float ContentBottomPadding = VerticalPadding;
        private const float ContentWidth = TooltipWidth - LeftPadding - RightPadding;
        private const float DefaultSectionFontSize = UIStyleTokens.Typography.CaptionMinimum;
        private const float EmphasizedSectionFontSize = 18f;
        private const float DetailSectionFontSize = UIStyleTokens.Typography.CaptionMinimum;
        private const float OwnerBadgeSize = 18f;
        private const float OwnerBadgeHorizontalOffset = 12f;

        // ── Runtime state ──────────────────────────────────────────────────
        private UI_PlayerBinder playerBinder;
        private TextMeshProUGUI bodyText;
        private Image ownerBadgeImage;
        private Image lastOwnerBadgeImage;
        private RectTransform rootRect;
        private FungalCell inspectedCell;
        private BoardTile inspectedTile;
        private GameBoard inspectedBoard;
        private bool initialized;
        private readonly StringBuilder sb = new();

        /// <summary>Raised after the tooltip's height changes, so its owner can re-clamp it.</summary>
        public event Action LayoutChanged;

        // ═══════════════════════════════════════════════════════════════════
        //  Public API
        // ═══════════════════════════════════════════════════════════════════

        public void SetPlayerBinder(UI_PlayerBinder binder) => playerBinder = binder;

        public void UpdateTooltip(
            FungalCell cell,
            GameBoard board,
            FungusToast.Unity.Grid.GridVisualizer gridVisualizer)
        {
            EnsureInitialized();

            if (bodyText == null) return;

            inspectedCell = cell;
            inspectedTile = null;
            inspectedBoard = board;
            RenderCellTooltip();
        }

        private void RenderCellTooltip()
        {
            if (bodyText == null || inspectedCell == null)
            {
                return;
            }

            FungalCell cell = inspectedCell;
            GameBoard board = inspectedBoard;

            // ── Build only the immediate, actionable inspection summary. ──
            sb.Clear();
            AppendStatus(cell);
            AppendGrowthSource(cell);
            AppendDeathReason(cell);
            AppendOwnership(cell);
            AppendAge(cell);
            AppendExpiration(cell);
            AppendResistance(cell);
            AppendAnimationFlags(cell);

            CommitRenderedContent(cell);
        }

        public void UpdateTooltip(
            BoardTile tile,
            GameBoard board,
            FungusToast.Unity.Grid.GridVisualizer gridVisualizer)
        {
            EnsureInitialized();

            if (bodyText == null || tile == null)
            {
                return;
            }

            var chemobeacon = board?.GetChemobeaconAtTile(tile.TileId);
            if (tile.NutrientPatch == null && chemobeacon == null)
            {
                return;
            }

            inspectedCell = null;
            inspectedTile = tile;
            inspectedBoard = board;
            RenderTileTooltip();
        }

        private void RenderTileTooltip()
        {
            if (bodyText == null || inspectedTile == null)
            {
                return;
            }

            BoardTile tile = inspectedTile;
            GameBoard board = inspectedBoard;
            var chemobeacon = board?.GetChemobeaconAtTile(tile.TileId);

            sb.Clear();
            if (chemobeacon != null)
            {
                AppendChemobeaconInfo(tile, board, chemobeacon);
            }
            else if (tile.NutrientPatch != null)
            {
                AppendNutrientPatch(tile.NutrientPatch);
            }

            CommitRenderedContent(null);
        }

        private void CommitRenderedContent(FungalCell cell)
        {
            bodyText.text = sb.ToString().TrimEnd('\n', '\r');
            bodyText.ForceMeshUpdate();
            SizeToContent();
            bodyText.ForceMeshUpdate();

            if (cell != null)
            {
                UpdateOwnershipIcons(cell);
            }
            else
            {
                HideOwnershipIcons();
            }

            LayoutChanged?.Invoke();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Initialisation (runs once)
        // ═══════════════════════════════════════════════════════════════════

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            rootRect = GetComponent<RectTransform>();
            if (rootRect == null)
            {
                Debug.LogWarning($"[CellTooltipUI] Disabled on non-UI object '{name}' because no RectTransform is present.");
                enabled = false;
                return;
            }

            // 1. Disable every layout-automation component on the root
            var vlg = GetComponent<VerticalLayoutGroup>();
            if (vlg != null) vlg.enabled = false;

            var hlg = GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.enabled = false;

            var csf = GetComponent<ContentSizeFitter>();
            if (csf != null) Destroy(csf);

            // 2. Hide every prefab row group
            HideGroup(statusGroup);
            HideGroup(growthSourceGroup);
            HideGroup(deathReasonGroup);
            HideGroup(ownerGroup);
            HideGroup(ageGroup);
            HideGroup(expirationGroup);
            HideGroup(resistantGroup);
            HideGroup(lastOwnerGroup);
            HideGroup(additionalInfoGroup);

            // 3. Pick a TMP to use as the single body renderer
            bodyText = ResolveBodyText();

            if (bodyText == null)
            {
                bodyText = CreateFallbackBodyText();

                if (bodyText == null)
                {
                    Debug.LogWarning($"[CellTooltipUI] Disabled on '{name}' because no TextMeshProUGUI was assigned and no fallback could be created.");
                    enabled = false;
                    return;
                }

                Debug.LogWarning("[CellTooltipUI] No TextMeshProUGUI assigned. Created a runtime fallback text renderer.");
            }

            // Re-parent directly under root so it stays visible
            bodyText.transform.SetParent(transform, false);
            bodyText.gameObject.SetActive(true);
            bodyText.transform.SetAsLastSibling();
            ConfigureBodyText(bodyText);
            ownerBadgeImage = PrepareOwnershipBadge(ownerIcon, "OwnerBadgeIcon");
            lastOwnerBadgeImage = PrepareOwnershipBadge(lastOwnerIcon, "LastOwnerBadgeIcon");

            // 4. Style the background
            ApplyBackground();
        }

        private TextMeshProUGUI ResolveBodyText()
        {
            var assignedText = additionalInfoText
                               ?? statusText
                               ?? ownerText
                               ?? growthAgeText
                               ?? deathReasonText
                               ?? lastOwnerText
                               ?? expirationText
                               ?? resistantText
                               ?? growthSourceText;

            if (assignedText != null)
            {
                return assignedText;
            }

            var availableText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (availableText != null)
            {
                return availableText;
            }

            return null;
        }

        private TextMeshProUGUI CreateFallbackBodyText()
        {
            if (rootRect == null)
            {
                return null;
            }

            var bodyTextObject = new GameObject("TooltipBodyText", typeof(RectTransform));
            bodyTextObject.transform.SetParent(transform, false);

            var fallbackText = bodyTextObject.AddComponent<TextMeshProUGUI>();
            fallbackText.raycastTarget = false;
            fallbackText.text = string.Empty;

            if (fallbackText.font == null && TMP_Settings.defaultFontAsset != null)
            {
                fallbackText.font = TMP_Settings.defaultFontAsset;
            }

            return fallbackText;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Layout helpers
        // ═══════════════════════════════════════════════════════════════════

        private static void HideGroup(GameObject g)
        {
            if (g != null) g.SetActive(false);
        }

        private static void ConfigureBodyText(TextMeshProUGUI tmp)
        {
            // Text style
            tmp.enableAutoSizing = false;
            tmp.fontSize = DefaultSectionFontSize;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            tmp.color = UIStyleTokens.Text.Primary;
            tmp.fontStyle = FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.lineSpacing = 5f;
            tmp.paragraphSpacing = 0f;
            tmp.margin = Vector4.zero;

            // Stretch to fill root with padding on all sides
            var rt = tmp.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(LeftPadding, ContentBottomPadding);
            rt.offsetMax = new Vector2(-RightPadding, -VerticalPadding);

            // Remove components that might interfere
            var le = tmp.GetComponent<LayoutElement>();
            if (le != null) Destroy(le);

            var fit = tmp.GetComponent<ContentSizeFitter>();
            if (fit != null) Destroy(fit);
        }

        private Image PrepareOwnershipBadge(Image configuredImage, string objectName)
        {
            Image badge = configuredImage;
            if (badge == null)
            {
                var badgeObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeObject.transform.SetParent(bodyText.transform, false);
                badge = badgeObject.GetComponent<Image>();
            }
            else
            {
                badge.transform.SetParent(bodyText.transform, false);
            }

            badge.gameObject.SetActive(false);
            badge.raycastTarget = false;
            badge.preserveAspect = true;

            RectTransform badgeRect = badge.rectTransform;
            badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
            badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = new Vector2(OwnerBadgeSize, OwnerBadgeSize);
            badgeRect.localScale = Vector3.one;

            return badge;
        }

        private void SizeToContent()
        {
            if (bodyText == null || rootRect == null) return;

            // TMP can report preferred height for a given width
            float textHeight = bodyText.GetPreferredValues(bodyText.text, ContentWidth, 0f).y;
            float totalHeight = textHeight + VerticalPadding + ContentBottomPadding;

            rootRect.sizeDelta = new Vector2(TooltipWidth, totalHeight);
        }

        private void ApplyBackground()
        {
            if (tooltipBackgroundImage == null)
                tooltipBackgroundImage = GetComponent<Image>();

            if (tooltipBackgroundImage != null)
            {
                var c = UIStyleTokens.Surface.PanelSecondary;
                c.a = tooltipBackgroundAlpha;
                tooltipBackgroundImage.color = c;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Content builders (each appends to shared StringBuilder)
        // ═══════════════════════════════════════════════════════════════════

        private void AppendStatus(FungalCell cell)
        {
            if (cell.IsAlive)
                sb.AppendLine(EmphasizedLine("Status", "Alive", UIStyleTokens.State.Success));
            else if (cell.IsDead)
                sb.AppendLine(EmphasizedLine("Status", "Dead", UIStyleTokens.Text.Muted));
            else if (cell.IsToxin)
                sb.AppendLine(EmphasizedLine("Status", "Toxin", UIStyleTokens.Category.Fungicide));
        }

        private void AppendGrowthSource(FungalCell cell)
        {
            if (cell.SourceOfGrowth.HasValue)
                sb.AppendLine(EmphasizedLine("Source",
                    GrowthSourceDisplayNames.GetDisplayName(cell.SourceOfGrowth.Value),
                    UIStyleTokens.State.Info));
        }

        private void AppendDeathReason(FungalCell cell)
        {
            if (cell.IsDead && cell.CauseOfDeath.HasValue)
                sb.AppendLine(EmphasizedLine("Death",
                    DeathReasonName(cell.CauseOfDeath.Value),
                    UIStyleTokens.State.Danger));
        }

        private void AppendOwnership(FungalCell cell)
        {
            if (cell.OwnerPlayerId.HasValue)
                sb.AppendLine(EmphasizedLine("Owner",
                    $"Player {cell.OwnerPlayerId.Value + 1}",
                    UIStyleTokens.Text.Primary));

            if (cell.LastOwnerPlayerId.HasValue)
                sb.AppendLine(EmphasizedLine("Last Owner",
                    $"Player {cell.LastOwnerPlayerId.Value + 1}",
                    UIStyleTokens.Text.Secondary));
        }

        private void AppendAge(FungalCell cell)
        {
            bool young = cell.IsAlive
                         && cell.GrowthCycleAge < UIEffectConstants.GrowthCycleAgeHighlightTextThreshold;
            Color ageColor = young ? UIStyleTokens.State.Success : UIStyleTokens.Text.Primary;
            sb.AppendLine(EmphasizedLine("Age",
                cell.GrowthCycleAge.ToString(),
                ageColor));
        }

        private void AppendExpiration(FungalCell cell)
        {
            if (!cell.IsToxin) return;

            int remaining = cell.ToxinExpirationAge - cell.GrowthCycleAge;
            if (remaining > 0)
                sb.AppendLine(DetailLine("Cycles Until Expiration",
                    remaining.ToString(),
                    UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));
            else
                sb.AppendLine(DetailLine("Expiration",
                    "Expires this cycle",
                    UIStyleTokens.Text.Secondary, UIStyleTokens.State.Danger));
        }

        private void AppendResistance(FungalCell cell)
        {
            if (cell.IsResistant)
            {
                sb.AppendLine(DetailLine("Resistance", "Active",
                    UIStyleTokens.Text.Secondary, UIStyleTokens.Accent.Spore));

            }
        }

        private void AppendAnimationFlags(FungalCell cell)
        {
            // IsNewlyGrown is a short-lived presentation flag that can remain set
            // until an animation completes. Tooltip copy should instead reflect the
            // player-facing definition: cells grown within the latest five cycles.
            if (IsRecentlyGrown(cell))
                sb.AppendLine(DetailBullet("Newly Grown", UIStyleTokens.State.Warning));
            if (cell.IsDying)
                sb.AppendLine(DetailBullet("Dying", UIStyleTokens.State.Danger));
            if (cell.IsReceivingToxinDrop)
                sb.AppendLine(DetailBullet("Receiving Toxin", UIStyleTokens.Category.Fungicide));
        }

        private static bool IsRecentlyGrown(FungalCell cell)
        {
            return cell.IsAlive
                   && cell.GrowthCycleAge < UIEffectConstants.GrowthCycleAgeHighlightTextThreshold;
        }

        private void AppendNutrientPatch(NutrientPatch nutrientPatch)
        {
            sb.AppendLine(EmphasizedLine("Status", nutrientPatch.DisplayName, UIStyleTokens.State.Warning));
            sb.AppendLine(DetailLine("Source", GetNutrientPatchSourceLabel(nutrientPatch.Source), UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));

            string rewardText = nutrientPatch.RewardType switch
            {
                NutrientRewardType.MutationPoints => $"+{nutrientPatch.RewardAmount} Mutation {(nutrientPatch.RewardAmount == 1 ? "Point" : "Points")}",
                NutrientRewardType.FreeGrowth => $"{nutrientPatch.RewardAmount} Free {(nutrientPatch.RewardAmount == 1 ? "Growth" : "Growths")}",
                NutrientRewardType.MycovariantDraft => nutrientPatch.RewardAmount == 1 ? "1 Mycovariant Draft" : $"{nutrientPatch.RewardAmount} Mycovariant Drafts",
                _ => nutrientPatch.Description
            };

            sb.AppendLine(DetailLine("Reward", rewardText, UIStyleTokens.Text.Secondary, UIStyleTokens.Accent.Spore));
            sb.AppendLine(DetailLine("Trigger", "First living growth onto this cluster claims it", UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));
        }

        private static string GetNutrientPatchSourceLabel(NutrientPatchSource source)
        {
            return source switch
            {
                NutrientPatchSource.NecrophyticBloom => "Necrophytic Bloom",
                _ => "Starting Board"
            };
        }

        private void AppendChemobeaconInfo(BoardTile tile, GameBoard board, GameBoard.ChemobeaconMarker chemobeacon)
        {
            sb.AppendLine(EmphasizedLine("Status", "Chemobeacon", UIStyleTokens.Accent.Spore));
            sb.AppendLine(DetailLine("Owner", $"Player {chemobeacon.PlayerId + 1}", UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));
            sb.AppendLine(DetailLine("Rounds Remaining", chemobeacon.TurnsRemaining.ToString(), UIStyleTokens.Text.Secondary, UIStyleTokens.State.Warning));
            sb.AppendLine(DetailLine("Effect", $"Projects {GameBalance.ChemotacticBeaconBaseTiles} + {GameBalance.ChemotacticBeaconTilesPerLevel}/level living cells toward the marker", UIStyleTokens.Text.Secondary, UIStyleTokens.State.Success));
            sb.AppendLine(DetailLine("Effect", "Replaces toxins, dead cells, enemy cells, and empty tiles in its path", UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));
            sb.AppendLine(DetailLine("Effect", "Skips over friendly living cells", UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Formatting helpers
        // ═══════════════════════════════════════════════════════════════════

        private static string EmphasizedLine(string label, string value, Color valueColor)
        {
            return $"<size={EmphasizedSectionFontSize}><color=#{Hex(UIStyleTokens.Text.Primary)}><b>{label}</b></color>: " +
                   $"<color=#{Hex(Contrast(valueColor))}><b>{value}</b></color></size>";
        }

        private static string DetailLine(string label, string value,
            Color labelColor, Color valueColor)
        {
            Color vc = Contrast(valueColor);
            return $"<size={DetailSectionFontSize}><color=#{Hex(labelColor)}>{label}:</color> <color=#{Hex(vc)}>{value}</color></size>";
        }

        private static string DetailBullet(string text, Color color)
        {
            return $"<size={DetailSectionFontSize}><color=#{Hex(Contrast(color))}>• {text}</color></size>";
        }

        private void UpdateOwnershipIcons(FungalCell cell)
        {
            UpdateOwnershipBadge(ownerBadgeImage, cell.OwnerPlayerId, "Owner:");
            UpdateOwnershipBadge(lastOwnerBadgeImage, cell.LastOwnerPlayerId, "Last Owner:");
        }

        private void HideOwnershipIcons()
        {
            if (ownerBadgeImage != null)
            {
                ownerBadgeImage.gameObject.SetActive(false);
            }

            if (lastOwnerBadgeImage != null)
            {
                lastOwnerBadgeImage.gameObject.SetActive(false);
            }
        }

        private void UpdateOwnershipBadge(Image badge, int? playerId, string linePrefix)
        {
            if (badge == null)
            {
                return;
            }

            if (!playerId.HasValue || playerBinder == null)
            {
                badge.gameObject.SetActive(false);
                return;
            }

            Sprite sprite = playerBinder.GetPlayerIcon(playerId.Value);
            if (sprite == null || !TryGetLineAnchor(linePrefix, out Vector3 badgePosition))
            {
                badge.gameObject.SetActive(false);
                return;
            }

            badge.sprite = sprite;
            badge.rectTransform.localPosition = badgePosition;
            badge.gameObject.SetActive(true);
        }

        private bool TryGetLineAnchor(string linePrefix, out Vector3 badgePosition)
        {
            badgePosition = Vector3.zero;

            if (bodyText == null)
            {
                return false;
            }

            TMP_TextInfo textInfo = bodyText.textInfo;
            for (int lineIndex = 0; lineIndex < textInfo.lineCount; lineIndex++)
            {
                TMP_LineInfo lineInfo = textInfo.lineInfo[lineIndex];
                string renderedLine = ExtractRenderedLine(textInfo, lineInfo).TrimStart();
                if (!renderedLine.StartsWith(linePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                int firstVisibleCharacterIndex = lineInfo.firstVisibleCharacterIndex;
                if (firstVisibleCharacterIndex < 0 || firstVisibleCharacterIndex >= textInfo.characterCount)
                {
                    return false;
                }

                TMP_CharacterInfo firstCharacter = textInfo.characterInfo[firstVisibleCharacterIndex];
                float centerY = (firstCharacter.topLeft.y + firstCharacter.bottomLeft.y) * 0.5f;
                badgePosition = new Vector3(firstCharacter.bottomLeft.x - OwnerBadgeHorizontalOffset, centerY, 0f);
                return true;
            }

            return false;
        }

        private static string ExtractRenderedLine(TMP_TextInfo textInfo, TMP_LineInfo lineInfo)
        {
            if (lineInfo.characterCount <= 0)
            {
                return string.Empty;
            }

            int lastCharacterIndex = Math.Min(lineInfo.lastCharacterIndex, textInfo.characterCount - 1);
            var lineBuilder = new StringBuilder(lineInfo.characterCount);
            for (int characterIndex = lineInfo.firstCharacterIndex; characterIndex <= lastCharacterIndex; characterIndex++)
            {
                lineBuilder.Append(textInfo.characterInfo[characterIndex].character);
            }

            return lineBuilder.ToString();
        }

        private static Color Contrast(Color c)
            => Color.Lerp(c, UIStyleTokens.Text.Primary, 0.5f);

        private static string Hex(Color c)
            => ColorUtility.ToHtmlStringRGB(c);

        // ═══════════════════════════════════════════════════════════════════
        //  Display-name look-ups
        // ═══════════════════════════════════════════════════════════════════

        private static string DeathReasonName(DeathReason r)
            => DeathReasonDisplayNames.GetDisplayName(r);

    }
}
