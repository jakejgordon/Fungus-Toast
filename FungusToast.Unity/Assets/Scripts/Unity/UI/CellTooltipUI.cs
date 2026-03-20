using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Board;
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

        // Kept so prefab fields are not orphaned; no longer used at runtime.
        [HideInInspector, SerializeField] private float rowBackgroundAlpha = 0.35f;
        [HideInInspector, SerializeField] private bool normalizeTooltipStructureOnAwake = true;

        // ── Constants ──────────────────────────────────────────────────────
        private const float TooltipWidth = 332f;
        private const float LeftPadding = 28f;
        private const float RightPadding = 16f;
        private const float VerticalPadding = 16f;
        private const float ContentWidth = TooltipWidth - LeftPadding - RightPadding;
        private const float DefaultSectionFontSize = 15.5f;
        private const float EmphasizedSectionFontSize = 18f;
        private const float DetailSectionFontSize = 14f;
        private const float OwnerBadgeSize = 18f;
        private const float OwnerBadgeHorizontalOffset = 12f;

        // ── Runtime state ──────────────────────────────────────────────────
        private UI_PlayerBinder playerBinder;
        private TextMeshProUGUI bodyText;
        private Image ownerBadgeImage;
        private Image lastOwnerBadgeImage;
        private RectTransform rootRect;
        private bool initialized;
        private readonly StringBuilder sb = new();

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

            // ── Build every line into one rich-text string ──
            sb.Clear();
            AppendStatus(cell);
            AppendGrowthSource(cell);
            AppendDeathReason(cell);
            AppendOwnership(cell);
            AppendAge(cell);
            AppendExpiration(cell);
            AppendResistance(cell);
            AppendTacticalInfo(cell, board);
            AppendAnimationFlags(cell);

            bodyText.text = sb.ToString().TrimEnd('\n', '\r');
            bodyText.ForceMeshUpdate();

            // ── Size root to fit ──
            SizeToContent();
            bodyText.ForceMeshUpdate();
            UpdateOwnershipIcons(cell);
        }

        public void UpdateTooltip(
            BoardTile tile,
            GameBoard board,
            FungusToast.Unity.Grid.GridVisualizer gridVisualizer)
        {
            EnsureInitialized();

            if (bodyText == null || tile?.NutrientPatch == null)
            {
                return;
            }

            sb.Clear();
            AppendNutrientPatch(tile.NutrientPatch);
            AppendNutrientTileInfo(tile, board);

            bodyText.text = sb.ToString().TrimEnd('\n', '\r');
            bodyText.ForceMeshUpdate();
            SizeToContent();
            bodyText.ForceMeshUpdate();
            HideOwnershipIcons();
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
            rt.offsetMin = new Vector2(LeftPadding, VerticalPadding);
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
            float totalHeight = textHeight + VerticalPadding * 2f;

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
                    GrowthSourceName(cell.SourceOfGrowth.Value),
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
            sb.AppendLine(EmphasizedLine("Growth Cycle Age",
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
                sb.AppendLine(DetailLine("Resistance", "Active",
                    UIStyleTokens.Text.Secondary, UIStyleTokens.Accent.Spore));
        }

        private void AppendTacticalInfo(FungalCell cell, GameBoard board)
        {
            // Blank line separator between core info and tactical section
            sb.AppendLine();

            var (x, y) = board.GetXYFromTileId(cell.TileId);
            sb.AppendLine(DetailLine("Tile", $"({x}, {y})",
                UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));

            bool isBorder = x == 0 || y == 0 || x == board.Width - 1 || y == board.Height - 1;
            bool nearEdge = !isBorder && (x <= 1 || y <= 1 || x >= board.Width - 2 || y >= board.Height - 2);
            string posLabel = isBorder ? "Border" : nearEdge ? "Near Edge" : "Interior";
            Color posColor = isBorder ? UIStyleTokens.State.Warning : UIStyleTokens.Text.Primary;
            sb.AppendLine(DetailLine("Position", posLabel,
                UIStyleTokens.Text.Secondary, posColor));

            int allies = 0, enemies = 0, toxins = 0, empty = 0;
            foreach (var adj in board.GetAdjacentTiles(cell.TileId))
            {
                var ac = adj.FungalCell;
                if (ac == null) { empty++; continue; }
                if (ac.IsToxin) toxins++;
                if (cell.OwnerPlayerId.HasValue && ac.OwnerPlayerId == cell.OwnerPlayerId)
                    allies++;
                else if (ac.OwnerPlayerId.HasValue)
                    enemies++;
            }

            sb.AppendLine(DetailLine("Adjacent Allies", allies.ToString(),
                UIStyleTokens.Text.Secondary, UIStyleTokens.State.Success));
            sb.AppendLine(DetailLine("Adjacent Enemies", enemies.ToString(),
                UIStyleTokens.Text.Secondary,
                enemies > 0 ? UIStyleTokens.State.Danger : UIStyleTokens.Text.Primary));
            sb.AppendLine(DetailLine("Adjacent Toxins", toxins.ToString(),
                UIStyleTokens.Text.Secondary,
                toxins > 0 ? UIStyleTokens.Category.Fungicide : UIStyleTokens.Text.Primary));
            sb.AppendLine(DetailLine("Adjacent Empty", empty.ToString(),
                UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));

            bool contested = enemies > 0 && cell.IsAlive;
            if (contested)
                sb.AppendLine(DetailLine("Local State", "Contested",
                    UIStyleTokens.Text.Secondary, UIStyleTokens.State.Danger));
            else if (cell.IsAlive)
                sb.AppendLine(DetailLine("Local State", "Stable",
                    UIStyleTokens.Text.Secondary, UIStyleTokens.State.Success));

            if (cell.ReclaimCount > 0)
                sb.AppendLine(DetailLine("Reclaimed", $"{cell.ReclaimCount}x",
                    UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));
        }

        private void AppendAnimationFlags(FungalCell cell)
        {
            if (cell.IsNewlyGrown)
                sb.AppendLine(DetailBullet("Newly Grown", UIStyleTokens.State.Warning));
            if (cell.IsDying)
                sb.AppendLine(DetailBullet("Dying", UIStyleTokens.State.Danger));
            if (cell.IsReceivingToxinDrop)
                sb.AppendLine(DetailBullet("Receiving Toxin", UIStyleTokens.Category.Fungicide));
        }

        private void AppendNutrientPatch(NutrientPatch nutrientPatch)
        {
            sb.AppendLine(EmphasizedLine("Status", nutrientPatch.DisplayName, UIStyleTokens.State.Warning));
            sb.AppendLine(DetailLine("Reward", $"+{nutrientPatch.RewardAmount} Mutation Point", UIStyleTokens.Text.Secondary, UIStyleTokens.Accent.Spore));
            sb.AppendLine(DetailLine("Trigger", "First orthogonal living growth consumes it", UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));
            sb.AppendLine();
            sb.AppendLine(DetailLine("Notes", nutrientPatch.Description, UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));
        }

        private void AppendNutrientTileInfo(BoardTile tile, GameBoard board)
        {
            sb.AppendLine();

            sb.AppendLine(DetailLine("Tile", $"({tile.X}, {tile.Y})", UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));

            int alliedNeighbors = 0;
            int enemyNeighbors = 0;
            int toxinNeighbors = 0;
            foreach (var adjacentTile in board.GetOrthogonalNeighbors(tile.TileId))
            {
                var adjacentCell = adjacentTile.FungalCell;
                if (adjacentCell == null)
                {
                    continue;
                }

                if (adjacentCell.IsToxin)
                {
                    toxinNeighbors++;
                    continue;
                }

                if (adjacentCell.OwnerPlayerId.HasValue)
                {
                    if (adjacentCell.IsAlive)
                    {
                        alliedNeighbors++;
                    }
                    else
                    {
                        enemyNeighbors++;
                    }
                }
            }

            sb.AppendLine(DetailLine("Orthogonal Living Cells", alliedNeighbors.ToString(), UIStyleTokens.Text.Secondary, alliedNeighbors > 0 ? UIStyleTokens.State.Success : UIStyleTokens.Text.Primary));
            sb.AppendLine(DetailLine("Orthogonal Dead Cells", enemyNeighbors.ToString(), UIStyleTokens.Text.Secondary, UIStyleTokens.Text.Primary));
            sb.AppendLine(DetailLine("Orthogonal Toxins", toxinNeighbors.ToString(), UIStyleTokens.Text.Secondary, toxinNeighbors > 0 ? UIStyleTokens.Category.Fungicide : UIStyleTokens.Text.Primary));
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

        private static string DeathReasonName(DeathReason r) => r switch
        {
            DeathReason.Age                       => "Old Age",
            DeathReason.Randomness                => "Random Death",
            DeathReason.PutrefactiveMycotoxin     => "Putrefactive Mycotoxin",
            DeathReason.SporicidalBloom           => "Sporicidal Bloom",
            DeathReason.MycotoxinPotentiation     => "Mycotoxin Potentiation",
            DeathReason.HyphalVectoring           => "Hyphal Vectoring",
            DeathReason.JettingMycelium           => "Jetting Mycelium",
            DeathReason.Infested                  => "Infested",
            DeathReason.Poisoned                  => "Poisoned",
            DeathReason.MycotoxicLash             => "Mycotoxic Lash",
            DeathReason.PutrefactiveCascade       => "Putrefactive Cascade",
            DeathReason.PutrefactiveCascadePoison => "Putrefactive Cascade Poison",
            DeathReason.Unknown                   => "Unknown",
            _                                     => r.ToString()
        };

        private static string GrowthSourceName(GrowthSource s) => s switch
        {
            GrowthSource.InitialSpore             => "Initial Spore",
            GrowthSource.HyphalOutgrowth          => "Hyphal Outgrowth",
            GrowthSource.TendrilOutgrowth         => "Tendril Outgrowth",
            GrowthSource.RegenerativeHyphae       => "Regenerative Hyphae",
            GrowthSource.NecrotoxicConversion     => "Necrotoxic Conversion",
            GrowthSource.HyphalSurge              => "Hyphal Surge",
            GrowthSource.JettingMycelium          => "Jetting Mycelium",
            GrowthSource.HyphalVectoring          => "Hyphal Vectoring",
            GrowthSource.SurgicalInoculation      => "Surgical Inoculation",
            GrowthSource.Necrosporulation         => "Necrosporulation",
            GrowthSource.NecrophyticBloom         => "Necrophytic Bloom",
            GrowthSource.NecrohyphalInfiltration  => "Necrohyphal Infiltration",
            GrowthSource.CreepingMold             => "Creeping Mold",
            GrowthSource.CatabolicRebirth         => "Catabolic Rebirth",
            GrowthSource.Ballistospore            => "Ballistospore",
            GrowthSource.MycotoxinTracer          => "Mycotoxin Tracers",
            GrowthSource.SporicidalBloom          => "Sporicidal Bloom",
            GrowthSource.MimeticResilience        => "Mimetic Resilience",
            GrowthSource.CornerConduit            => "Corner Conduit",
            GrowthSource.AggressotropicConduit    => "Aggressotropic Conduit",
            GrowthSource.Manual                   => "Manual",
            GrowthSource.Unknown                  => "Unknown",
            _                                     => s.ToString()
        };
    }
}
