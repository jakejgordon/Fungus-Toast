using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.Onboarding
{
    /// <summary>
    /// Centered first-game coachmark that frames what the player is here to do before the
    /// round-1 Spend Points and camera coachmarks appear. Those two check <see cref="IsActive"/>
    /// so a brand-new player reads one thing at a time: this closes, then Spend Points shows
    /// right away and the camera hint follows after its own delay.
    /// </summary>
    public sealed class NewPlayerWelcomeCoachmark
    {
        // Larger than the anchored coachmarks: it sits alone in the middle of the board.
        private const float Width = 560f;
        private const float MinHeight = 150f;
        private const float TitleFontSize = 28f;
        private const float BodyFontSize = 21f;
        private const float TitleRowHeight = 56f;
        private const float BodyTopInset = TitleRowHeight + 6f;
        private const float BodyHorizontalPadding = 20f;
        private const float BodyBottomPadding = 20f;

        private readonly Func<Canvas> resolveRootCanvas;
        private readonly Func<bool> getForceFirstGameExperience;

        private RectTransform root;
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI bodyLabel;
        private bool isArmed;
        private bool hasDismissedThisGame;

        public NewPlayerWelcomeCoachmark(Func<Canvas> resolveRootCanvas, Func<bool> getForceFirstGameExperience)
        {
            this.resolveRootCanvas = resolveRootCanvas;
            this.getForceFirstGameExperience = getForceFirstGameExperience;
        }

        /// <summary>Raised only when the player closes the coachmark with its X button.</summary>
        public event Action ClosedByPlayer;

        /// <summary>
        /// True from the moment the round-1 check passes until the coachmark is acknowledged,
        /// so dependent coachmarks stay hidden through the show delay as well as while visible.
        /// </summary>
        public bool IsActive => isArmed || IsVisible;

        public bool IsVisible => root != null && root.gameObject.activeSelf;

        /// <summary>
        /// Runs the catalog rule and, if it passes, reserves the round-1 coachmark slot. The
        /// caller then shows it after the game-start title card has cleared.
        /// </summary>
        public bool TryArm(int currentRound, int humanPlayerCount, bool isFastForwarding)
        {
            if (IsActive)
            {
                return false;
            }

            if (!NewPlayerTooltipRules.ShouldShowWelcomeIntro(
                    getForceFirstGameExperience(),
                    currentRound,
                    humanPlayerCount,
                    hasDismissedThisGame,
                    isFastForwarding))
            {
                return false;
            }

            isArmed = true;
            return true;
        }

        public void ShowIfArmed()
        {
            if (!isArmed)
            {
                return;
            }

            isArmed = false;
            EnsureUi();
            if (root == null || canvasGroup == null)
            {
                return;
            }

            NewPlayerTooltipDefinition definition = NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.WelcomeIntro);
            titleLabel.text = definition.Title;
            bodyLabel.text = definition.Body;
            RefreshLayout();
            CoachmarkLayoutUtility.PrepareAttentionEntrance(root);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            CoachmarkLayoutUtility.PlayAttention(root);
        }

        /// <summary>
        /// Closes the coachmark without raising <see cref="ClosedByPlayer"/>. Used when the
        /// player moves on by themselves, e.g. by clicking Spend Points while it is open.
        /// </summary>
        public void Acknowledge()
        {
            bool wasActive = IsActive;
            isArmed = false;
            hasDismissedThisGame = true;

            if (wasActive && !getForceFirstGameExperience())
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.WelcomeIntro);
            }

            HideImmediate();
        }

        public void ResetForNewGame()
        {
            isArmed = false;
            hasDismissedThisGame = false;
            HideImmediate();
        }

        private void OnCloseClicked()
        {
            Acknowledge();
            ClosedByPlayer?.Invoke();
        }

        private void HideImmediate()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        private void EnsureUi()
        {
            if (root != null)
            {
                return;
            }

            Canvas rootCanvas = resolveRootCanvas?.Invoke();
            if (rootCanvas == null)
            {
                // Same fallback as the camera coachmark: any canvas beats not showing at all.
                Canvas anyCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                rootCanvas = anyCanvas != null ? anyCanvas.rootCanvas : null;
            }

            if (rootCanvas == null)
            {
                Debug.LogWarning("[NewPlayerWelcomeCoachmark] No canvas found; skipping the welcome coachmark.");
                return;
            }

            var rootObject = new GameObject("UI_WelcomeCoachmark", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Outline));
            rootObject.transform.SetParent(rootCanvas.transform, false);

            root = rootObject.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(Width, MinHeight);

            canvasGroup = rootObject.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var background = rootObject.GetComponent<Image>();
            var backgroundColor = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.State.Info, 0.14f);
            backgroundColor.a = 0.97f;
            background.color = backgroundColor;
            background.raycastTarget = true;

            var outline = rootObject.GetComponent<Outline>();
            outline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Focus, UIStyleTokens.Alpha.FocusOutline);
            outline.effectDistance = new Vector2(1f, -1f);

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(rootObject.transform, false);

            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(BodyHorizontalPadding, -TitleRowHeight);
            titleRect.offsetMax = new Vector2(-CoachmarkLayoutUtility.TitleRightInset, -14f);

            titleLabel = titleObject.GetComponent<TextMeshProUGUI>();
            titleLabel.text = string.Empty;
            titleLabel.color = UIStyleTokens.Text.Primary;
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.fontSize = TitleFontSize;
            titleLabel.alignment = TextAlignmentOptions.Left;
            titleLabel.textWrappingMode = TextWrappingModes.NoWrap;
            TMPOverflowUtility.SetSafeEllipsis(titleLabel);
            titleLabel.raycastTarget = false;

            var bodyObject = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObject.transform.SetParent(rootObject.transform, false);

            var bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(BodyHorizontalPadding, BodyBottomPadding);
            bodyRect.offsetMax = new Vector2(-BodyHorizontalPadding, -BodyTopInset);

            bodyLabel = bodyObject.GetComponent<TextMeshProUGUI>();
            bodyLabel.color = UIStyleTokens.Text.Primary;
            bodyLabel.fontSize = BodyFontSize;
            bodyLabel.alignment = TextAlignmentOptions.TopLeft;
            bodyLabel.textWrappingMode = TextWrappingModes.Normal;
            bodyLabel.overflowMode = TextOverflowModes.Overflow;
            bodyLabel.raycastTarget = false;

            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(rootObject.transform, false);

            var closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(CoachmarkLayoutUtility.CloseButtonSize, CoachmarkLayoutUtility.CloseButtonSize);
            closeRect.anchoredPosition = new Vector2(-CoachmarkLayoutUtility.CloseButtonInset, -CoachmarkLayoutUtility.CloseButtonInset);

            var closeImage = closeObject.GetComponent<Image>();
            closeImage.color = UIStyleTokens.Surface.PanelElevated;

            var closeButton = closeObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(closeButton);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);

            var closeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            closeLabelObject.transform.SetParent(closeObject.transform, false);

            var closeLabelRect = closeLabelObject.GetComponent<RectTransform>();
            closeLabelRect.anchorMin = Vector2.zero;
            closeLabelRect.anchorMax = Vector2.one;
            closeLabelRect.offsetMin = Vector2.zero;
            closeLabelRect.offsetMax = Vector2.zero;

            var closeLabel = closeLabelObject.GetComponent<TextMeshProUGUI>();
            closeLabel.text = "X";
            closeLabel.color = UIStyleTokens.Text.Primary;
            closeLabel.fontStyle = FontStyles.Bold;
            closeLabel.fontSize = CoachmarkLayoutUtility.CloseButtonFontSize;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                titleLabel.font = TMP_Settings.defaultFontAsset;
                bodyLabel.font = TMP_Settings.defaultFontAsset;
                closeLabel.font = TMP_Settings.defaultFontAsset;
            }

            rootObject.SetActive(false);
        }

        private void RefreshLayout()
        {
            if (root == null || bodyLabel == null)
            {
                return;
            }

            float availableBodyWidth = Mathf.Max(1f, Width - (2f * BodyHorizontalPadding));
            Vector2 bodyPreferredSize = bodyLabel.GetPreferredValues(bodyLabel.text, availableBodyWidth, 0f);
            float requiredHeight = BodyTopInset + bodyPreferredSize.y + BodyBottomPadding;

            root.sizeDelta = new Vector2(Width, Mathf.Max(MinHeight, requiredHeight));

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }
    }
}
