using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.UI.Onboarding;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;

namespace FungusToast.Unity.UI
{
    public class UI_PauseMenuPanel : MonoBehaviour, ITooltipContentProvider
    {
        private enum PendingAction
        {
            None,
            RestartLevel,
            ReturnToMainMenu,
            ExitGame
        }

        private const float HudButtonWidth = UIStyleTokens.Interaction.MinimumTargetSize;
        private const float HudButtonHeight = UIStyleTokens.Interaction.MinimumTargetSize;
        private const float HudButtonIconSize = 20f;
        private const float HudButtonGap = 8f;
        private const float HudButtonRightInset = 8f;
        private const float PaceToggleHorizontalPadding = 12f;
        private const float PaceToggleContentSpacing = 8f;
        private const float PaceToggleFontSize = 18f;
        private const float PaceToggleFallbackWidth = 200f;
        private static readonly Vector2 PaceCoachmarkSize = new Vector2(340f, 172f);
        private static readonly Vector2 PaceCoachmarkOffset = new Vector2(0f, -8f);
        private const string PaceNormalLabel = "Pace: Normal";
        private const string PaceTimeLapseLabel = "Pace: Time-Lapse";
        private const string PaceNormalTooltip = "Growth and Decay play out with their full animations.\nClick to switch to Time-Lapse and skip most of them.";
        private const string PaceTimeLapseTooltip = "Time-Lapse skips most Growth and Decay animations so rounds go by faster.\nClick to switch back to Normal.";
        private const float ActionButtonIconSize = 28f;
        private const float ActionButtonContentSpacing = 12f;
        private const float CardWidth = 420f;
        private const float CardPadding = 24f;
        private const float ContentSpacing = 14f;
        private const float CardMinimumHeight = 520f;
        private const float CardViewportMargin = 64f;
        private const float PauseMenuPrimaryButtonWidth = CardWidth - (CardPadding * 2f);
        private const float PauseMenuConfirmationButtonWidth = 180f;

        private GameUIManager gameUI;
        private Action onOpenRequested;
        private Action onResumeRequested;
        private Action onReturnToMainMenuRequested;
        private Action onRestartLevelRequested;
        private Action onExitRequested;
        private Action onNextTrackRequested;
        private Action onReplayTutorialTipsRequested;
        private Func<string> getCurrentTrackName;
        private Func<string> getNextTrackName;

        private Canvas rootCanvas;
        private TMP_FontAsset sharedFont;

        private RectTransform hudControlsRow;
        private GameObject hudButtonRoot;
        private Button hudMenuButton;
        private GameObject paceToggleRoot;
        private Button paceToggleButton;
        private TextMeshProUGUI paceToggleLabel;
        private Image paceToggleIcon;
        private TooltipTrigger paceToggleTooltip;
        private CoachmarkLayoutUtility.CoachmarkCard paceCoachmark;
        private bool hasDismissedPaceCoachmarkThisGame;

        private GameObject overlayRoot;
        private CanvasGroup overlayCanvasGroup;
        private RectTransform cardRect;
        private RectTransform contentRect;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI subtitleLabel;
        private GameObject primaryActionsRoot;
        private GameObject tutorialHelpRoot;
        private GameObject soundSettingsRoot;
        private GameObject confirmationRoot;
        private TextMeshProUGUI confirmationLabel;
        private Button soundEffectsToggleButton;
        private Button soundEffectsVolumeButton;
        private Button musicVolumeButton;
        private Button nextTrackMenuButton;
        private Button tutorialReplayButton;
        private TextMeshProUGUI tutorialStatusLabel;

        private PendingAction pendingAction;
        private bool gameplayVisible;
        private bool hudControlsSuppressed;

        public bool IsOpen { get; private set; }
        public bool IsConfirming => pendingAction != PendingAction.None;

        public void SetDependencies(
            GameUIManager ui,
            Action openRequested,
            Action resumeRequested,
            Action returnToMainMenuRequested,
            Action restartLevelRequested,
            Action exitRequested,
            Action nextTrackRequested,
            Action replayTutorialTipsRequested,
            Func<string> currentTrackNameProvider,
            Func<string> nextTrackNameProvider)
        {
            gameUI = ui;
            onOpenRequested = openRequested;
            onResumeRequested = resumeRequested;
            onReturnToMainMenuRequested = returnToMainMenuRequested;
            onRestartLevelRequested = restartLevelRequested;
            onExitRequested = exitRequested;
            onNextTrackRequested = nextTrackRequested;
            onReplayTutorialTipsRequested = replayTutorialTipsRequested;
            getCurrentTrackName = currentTrackNameProvider;
            getNextTrackName = nextTrackNameProvider;

            EnsureBuilt();
        }

        public void SetGameplayVisibility(bool isVisible)
        {
            gameplayVisible = isVisible;
            EnsureBuilt();
            RefreshHudControlsVisibility();

            if (!isVisible)
            {
                Hide();
            }
        }

        public void SetHudControlsSuppressed(bool suppressed)
        {
            hudControlsSuppressed = suppressed;
            EnsureBuilt();
            RefreshHudControlsVisibility();
        }

        public void Show()
        {
            if (!gameplayVisible)
            {
                return;
            }

            EnsureBuilt();
            if (overlayRoot == null || overlayCanvasGroup == null)
            {
                return;
            }

            pendingAction = PendingAction.None;
            ApplyPanelState();

            overlayRoot.transform.SetAsLastSibling();
            overlayRoot.SetActive(true);
            overlayCanvasGroup.alpha = 1f;
            overlayCanvasGroup.interactable = true;
            overlayCanvasGroup.blocksRaycasts = true;
            RefreshTutorialControls(clearStatus: true);
            RefreshSoundSettingsButtons();
            IsOpen = true;
        }

        public void Hide()
        {
            pendingAction = PendingAction.None;
            ApplyPanelState();

            if (overlayCanvasGroup != null)
            {
                overlayCanvasGroup.alpha = 0f;
                overlayCanvasGroup.interactable = false;
                overlayCanvasGroup.blocksRaycasts = false;
            }

            if (overlayRoot != null)
            {
                overlayRoot.SetActive(false);
            }

            RefreshCardLayout();

            IsOpen = false;
        }

        public void CancelPendingAction()
        {
            pendingAction = PendingAction.None;
            ApplyPanelState();
        }

        public string GetTooltipText()
        {
            string currentTrack = FormatTrackName(getCurrentTrackName?.Invoke(), "Waiting to start");
            string nextTrack = FormatTrackName(getNextTrackName?.Invoke(), "Unavailable");
            return $"Skip to the next track immediately.\nCurrent: {currentTrack}\nNext: {nextTrack}";
        }

        private void EnsureBuilt()
        {
            if (overlayRoot != null && hudButtonRoot != null && paceToggleRoot != null)
            {
                return;
            }

            rootCanvas = ResolveRootCanvas();
            if (rootCanvas == null)
            {
                Debug.LogError("UI_PauseMenuPanel: Could not find a root Canvas to attach runtime UI.");
                return;
            }

            sharedFont = ResolveSharedFont();

            // The persistent HUD controls live in the sidebar's top row when it exists (pace on the
            // left, menu on the right, phase tracker beneath); otherwise they fall back to the root
            // canvas corner so a scene without the sidebar still gets a working pause button.
            EnsureHudControlsRow();
            Transform hudParent = hudControlsRow != null ? hudControlsRow : rootCanvas.transform;

            if (paceToggleRoot == null)
            {
                BuildPaceToggle(hudParent);
            }

            if (hudControlsRow != null && hudControlsRow.Find("Spacer") == null)
            {
                GameObject spacer = CreateUiObject("Spacer", hudControlsRow);
                LayoutElement spacerLayout = spacer.AddComponent<LayoutElement>();
                spacerLayout.flexibleWidth = 1f;
                spacerLayout.minWidth = HudButtonGap;
            }

            if (hudButtonRoot == null)
            {
                BuildHudButton(hudParent);
            }

            RefreshPaceToggle();

            if (overlayRoot == null)
            {
                BuildOverlay(rootCanvas.transform);
            }

            RefreshHudControlsVisibility();

            ApplyPanelState();
            Hide();
        }

        private void RefreshHudControlsVisibility()
        {
            bool shouldShow = gameplayVisible && !hudControlsSuppressed;

            if (hudButtonRoot != null)
            {
                hudButtonRoot.SetActive(shouldShow);
            }

            if (paceToggleRoot != null)
            {
                paceToggleRoot.SetActive(shouldShow);
            }

            if (!shouldShow)
            {
                paceCoachmark?.HideImmediate();
            }
        }

        private void EnsureHudControlsRow()
        {
            if (hudControlsRow != null)
            {
                return;
            }

            UI_RightSidebar sidebar = gameUI != null ? gameUI.RightSidebar : null;
            hudControlsRow = sidebar != null ? sidebar.EnsureTopControlsRow() : null;
        }

        private Canvas ResolveRootCanvas()
        {
            if (gameUI != null)
            {
                Canvas uiCanvas = gameUI.GetComponentInParent<Canvas>();
                if (uiCanvas != null)
                {
                    return uiCanvas.rootCanvas;
                }
            }

            Canvas anyCanvas = FindAnyObjectByType<Canvas>();
            return anyCanvas != null ? anyCanvas.rootCanvas : null;
        }

        private TMP_FontAsset ResolveSharedFont()
        {
            if (gameUI != null)
            {
                TextMeshProUGUI sampleLabel = gameUI.GetComponentInChildren<TextMeshProUGUI>(true);
                if (sampleLabel != null)
                {
                    return sampleLabel.font;
                }
            }

            return TMP_Settings.defaultFontAsset;
        }

        private void BuildHudButton(Transform parent)
        {
            hudButtonRoot = CreateUiObject("PauseMenuHudButton", parent);
            RectTransform rootRect = hudButtonRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.sizeDelta = new Vector2(HudButtonWidth, HudButtonHeight);
            rootRect.anchoredPosition = new Vector2(-HudButtonRightInset, -6f);

            LayoutElement rootLayout = hudButtonRoot.AddComponent<LayoutElement>();
            rootLayout.minWidth = HudButtonWidth;
            rootLayout.preferredWidth = HudButtonWidth;
            rootLayout.minHeight = HudButtonHeight;
            rootLayout.preferredHeight = HudButtonHeight;
            rootLayout.flexibleWidth = 0f;
            rootLayout.flexibleHeight = 0f;

            Image background = hudButtonRoot.AddComponent<Image>();
            background.color = UIStyleTokens.Button.BackgroundDefault;

            hudMenuButton = hudButtonRoot.AddComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(hudMenuButton);
            hudMenuButton.onClick.AddListener(() => onOpenRequested?.Invoke());

            TooltipTrigger tooltip = hudButtonRoot.AddComponent<TooltipTrigger>();
            tooltip.SetStaticText("Open the pause menu.");

            Sprite pauseMenuIcon = gameUI != null ? gameUI.PauseMenuButtonIcon : null;
            if (pauseMenuIcon != null)
            {
                CreateIconImage(hudButtonRoot.transform, "PauseMenuIcon", pauseMenuIcon, HudButtonIconSize, Vector2.zero);
            }
            else
            {
                if (gameUI == null)
                {
                    Debug.LogWarning("UI_PauseMenuPanel: Building pause HUD button without a GameUIManager. Using hamburger fallback.");
                }

                CreateHamburgerIcon(hudButtonRoot.transform);
            }
        }

        private void BuildPaceToggle(Transform parent)
        {
            paceToggleRoot = CreateUiObject("PaceToggleHudButton", parent);
            RectTransform rootRect = paceToggleRoot.GetComponent<RectTransform>();
            // Only meaningful on the root-canvas fallback; the sidebar row lays the toggle out itself.
            rootRect.anchorMin = new Vector2(1f, 1f);
            rootRect.anchorMax = new Vector2(1f, 1f);
            rootRect.pivot = new Vector2(1f, 1f);
            rootRect.sizeDelta = new Vector2(PaceToggleFallbackWidth, HudButtonHeight);
            rootRect.anchoredPosition = new Vector2(
                -(HudButtonRightInset + HudButtonWidth + HudButtonGap),
                -6f);

            HorizontalLayoutGroup contentLayout = paceToggleRoot.AddComponent<HorizontalLayoutGroup>();
            int horizontalPadding = Mathf.RoundToInt(PaceToggleHorizontalPadding);
            contentLayout.padding = new RectOffset(horizontalPadding, horizontalPadding, 0, 0);
            contentLayout.spacing = PaceToggleContentSpacing;
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;

            LayoutElement rootLayout = paceToggleRoot.AddComponent<LayoutElement>();
            rootLayout.minHeight = HudButtonHeight;
            rootLayout.preferredHeight = HudButtonHeight;
            rootLayout.flexibleWidth = 0f;
            rootLayout.flexibleHeight = 0f;

            Image background = paceToggleRoot.AddComponent<Image>();
            background.color = UIStyleTokens.Button.BackgroundDefault;

            paceToggleButton = paceToggleRoot.AddComponent<Button>();
            paceToggleButton.onClick.AddListener(OnPaceToggleClicked);

            paceToggleTooltip = paceToggleRoot.AddComponent<TooltipTrigger>();

            Sprite paceIcon = gameUI != null ? gameUI.PaceToggleButtonIcon : null;
            if (paceIcon != null)
            {
                paceToggleIcon = CreateIconLayoutImage(paceToggleRoot.transform, "PaceIcon", paceIcon, HudButtonIconSize, UIStyleTokens.Button.TextDefault);
            }
            else if (gameUI == null)
            {
                Debug.LogWarning("UI_PauseMenuPanel: Building pace toggle without a GameUIManager. Omitting its icon.");
            }

            paceToggleLabel = CreateLabel(paceToggleRoot.transform, PaceNormalLabel, PaceToggleFontSize, FontStyles.Bold);
            paceToggleLabel.alignment = TextAlignmentOptions.MidlineLeft;
            paceToggleLabel.color = UIStyleTokens.Button.TextDefault;
            LayoutElement labelLayout = paceToggleLabel.GetComponent<LayoutElement>();
            labelLayout.minHeight = HudButtonHeight;
            labelLayout.preferredHeight = HudButtonHeight;
        }

        private void OnPaceToggleClicked()
        {
            GameManager.Instance?.CycleRoundPresentationSpeedMode();
            RefreshPaceToggle();
        }

        /// <summary>
        /// Syncs the toggle label, tint and tooltip with the current presentation speed.
        /// Called by <see cref="GameManager.SetRoundPresentationSpeedMode"/> so the toggle
        /// stays right no matter where the mode was changed from.
        /// </summary>
        public void RefreshPaceToggle()
        {
            if (paceToggleButton == null)
            {
                return;
            }

            bool isTimeLapse = GameManager.Instance != null && GameManager.Instance.IsFastRoundPresentationMode;

            // The "on" state is tinted so the button reads at a glance, but it does not take the
            // light-green CTA fill: that is a saturated mid-tone, and dark text on it drops from
            // 9.6:1 to 5.9:1, which is the drop that makes the on-state look muddy beside the off
            // one. A dark accent-blended fill with a light label holds ~9:1 in both states
            // (UI_STYLE_GUIDE.md section 5.12).
            UIStyleTokens.Button.ApplyStyle(paceToggleButton);

            var fill = isTimeLapse
                ? UIStyleTokens.Badge.Fill(UIStyleTokens.Accent.Lichen)
                : UIStyleTokens.Button.BackgroundDefault;
            var content = isTimeLapse ? UIStyleTokens.Badge.Label : UIStyleTokens.Button.TextDefault;

            if (paceToggleButton.image != null)
            {
                var colors = paceToggleButton.colors;
                colors.normalColor = fill;
                if (isTimeLapse)
                {
                    colors.highlightedColor = UIStyleTokens.Badge.Fill(UIStyleTokens.Accent.Spore);
                    colors.pressedColor = UIStyleTokens.Badge.Fill(UIStyleTokens.Accent.Moss);
                    colors.selectedColor = fill;
                }

                paceToggleButton.colors = colors;
            }

            if (paceToggleIcon != null)
            {
                paceToggleIcon.color = content;
            }

            if (paceToggleLabel != null)
            {
                paceToggleLabel.text = isTimeLapse ? PaceTimeLapseLabel : PaceNormalLabel;
                paceToggleLabel.color = content;
            }

            paceToggleTooltip?.SetStaticText(isTimeLapse ? PaceTimeLapseTooltip : PaceNormalTooltip);

            if (isTimeLapse && paceCoachmark != null && paceCoachmark.IsVisible)
            {
                // Turning Time-Lapse on is the behaviour the coachmark teaches, so retire it.
                OnPaceCoachmarkDismissed();
            }
        }

        public void ResetForNewGame()
        {
            hasDismissedPaceCoachmarkThisGame = false;
            paceCoachmark?.HideImmediate();
        }

        public void TryShowPaceCoachmark(int currentRound)
        {
            EnsureBuilt();
            if (paceToggleRoot == null || !paceToggleRoot.activeInHierarchy || rootCanvas == null)
            {
                return;
            }

            var gameManager = GameManager.Instance;
            bool forceFirstGame = gameManager != null && gameManager.ShouldForceFirstGameExperience;
            bool isFastForwarding = gameManager != null && gameManager.IsFastForwarding;
            if (!NewPlayerTooltipRules.ShouldShowTimeLapseModeIntro(
                    forceFirstGame,
                    currentRound,
                    hasDismissedPaceCoachmarkThisGame,
                    isFastForwarding))
            {
                return;
            }

            paceCoachmark ??= CoachmarkLayoutUtility.BuildCard(
                "UI_PaceCoachmark",
                rootCanvas.transform,
                PaceCoachmarkSize,
                OnPaceCoachmarkDismissed);

            paceCoachmark.Show(NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.TimeLapseModeIntro));
            PositionPaceCoachmark();
        }

        private void PositionPaceCoachmark()
        {
            if (paceCoachmark == null || paceCoachmark.Root == null || paceToggleRoot == null || rootCanvas == null)
            {
                return;
            }

            RectTransform anchorRect = paceToggleRoot.GetComponent<RectTransform>();
            RectTransform boundsRect = paceCoachmark.Root.parent as RectTransform;
            if (anchorRect == null || boundsRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            Vector3[] corners = new Vector3[4];
            anchorRect.GetWorldCorners(corners);
            // corners[0] is bottom-left: the card hangs below the toggle, aligned to its left edge.
            CoachmarkLayoutUtility.TryPlaceAtWorldPoint(
                paceCoachmark.Root,
                boundsRect,
                rootCanvas,
                corners[0],
                PaceCoachmarkOffset,
                CoachmarkLayoutUtility.DefaultScreenPadding);
        }

        private void OnPaceCoachmarkDismissed()
        {
            hasDismissedPaceCoachmarkThisGame = true;
            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (!forceFirstGame)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.TimeLapseModeIntro);
            }

            paceCoachmark?.HideImmediate();
        }

        private void BuildOverlay(Transform parent)
        {
            overlayRoot = CreateUiObject("PauseMenuOverlay", parent);
            RectTransform overlayRect = overlayRoot.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            Image overlayImage = overlayRoot.AddComponent<Image>();
            overlayImage.color = UIStyleTokens.Surface.OverlayDim;
            overlayImage.raycastTarget = true;

            overlayCanvasGroup = overlayRoot.AddComponent<CanvasGroup>();

            GameObject card = CreateUiObject("PauseMenuCard", overlayRoot.transform);
            cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(CardWidth, CardMinimumHeight);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardImage = card.AddComponent<Image>();
            cardImage.color = UIStyleTokens.Surface.PanelPrimary;

            GameObject contentRoot = CreateUiObject("PauseMenuContent", card.transform);
            contentRect = contentRoot.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 1f);
            contentRect.anchorMax = new Vector2(0.5f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = new Vector2(0f, -CardPadding);
            contentRect.sizeDelta = new Vector2(CardWidth - (CardPadding * 2f), 0f);

            VerticalLayoutGroup contentLayout = contentRoot.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = ContentSpacing;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentFitter = contentRoot.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement contentElement = contentRoot.AddComponent<LayoutElement>();
            contentElement.minWidth = CardWidth - (CardPadding * 2f);
            contentElement.preferredWidth = CardWidth - (CardPadding * 2f);

            titleLabel = CreateLabel(contentRoot.transform, "Pause Menu", 38f, FontStyles.Bold);
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.color = UIStyleTokens.Text.Primary;

            subtitleLabel = CreateLabel(contentRoot.transform, string.Empty, 22f, FontStyles.Normal);
            subtitleLabel.alignment = TextAlignmentOptions.Center;
            subtitleLabel.color = UIStyleTokens.Text.Secondary;
            ConfigureDynamicWrappedLabel(subtitleLabel);

            primaryActionsRoot = CreateVerticalSection(contentRoot.transform, "PrimaryActions", 10f);

            Button resumeButton = CreateActionButton(primaryActionsRoot.transform, "Resume", width: PauseMenuPrimaryButtonWidth, useSelectedAsNormal: true);
            resumeButton.onClick.AddListener(() => onResumeRequested?.Invoke());
            EnsureTooltip(resumeButton, "Close the pause menu and return to the current game.");

            string runUnit = GetCurrentRunUnitName();
            Button restartButton = CreateActionButton(primaryActionsRoot.transform, $"Restart {runUnit}", width: PauseMenuPrimaryButtonWidth);
            restartButton.onClick.AddListener(RequestRestartConfirmation);
            EnsureTooltip(restartButton, $"Restart the current {runUnit.ToLowerInvariant()} from its original seed after confirmation.");

            Button mainMenuButton = CreateActionButton(primaryActionsRoot.transform, "Main Menu", gameUI != null ? gameUI.PauseMenuButtonIcon : null, PauseMenuPrimaryButtonWidth);
            mainMenuButton.onClick.AddListener(RequestMainMenuConfirmation);
            EnsureTooltip(mainMenuButton, "Return to the main menu after saving the current run.");

            Button exitButton = CreateActionButton(primaryActionsRoot.transform, "Exit Game", width: PauseMenuPrimaryButtonWidth);
            exitButton.onClick.AddListener(RequestExitConfirmation);
            EnsureTooltip(exitButton, "Exit the game after saving the current run.");

            tutorialHelpRoot = CreateVerticalSection(contentRoot.transform, "TutorialHelp", 8f);

            TextMeshProUGUI tutorialLabel = CreateLabel(tutorialHelpRoot.transform, "Tutorial Tips", 22f, FontStyles.Bold);
            tutorialLabel.alignment = TextAlignmentOptions.Center;
            tutorialLabel.color = UIStyleTokens.Text.Primary;

            tutorialReplayButton = CreateActionButton(tutorialHelpRoot.transform, "Replay Tutorial Tips", width: PauseMenuPrimaryButtonWidth, secondaryStyle: true);
            tutorialReplayButton.onClick.AddListener(OnReplayTutorialTipsClicked);
            EnsureTooltip(tutorialReplayButton, "Re-enable tutorial hints for the current profile.");

            tutorialStatusLabel = CreateLabel(tutorialHelpRoot.transform, string.Empty, 18f, FontStyles.Normal);
            tutorialStatusLabel.alignment = TextAlignmentOptions.Center;
            tutorialStatusLabel.color = UIStyleTokens.Text.Secondary;
            ConfigureDynamicWrappedLabel(tutorialStatusLabel);

            soundSettingsRoot = CreateVerticalSection(contentRoot.transform, "SoundSettings", 10f);

            TextMeshProUGUI soundLabel = CreateLabel(soundSettingsRoot.transform, "Audio", 22f, FontStyles.Bold);
            soundLabel.alignment = TextAlignmentOptions.Center;
            soundLabel.color = UIStyleTokens.Text.Primary;

            soundEffectsToggleButton = CreateActionButton(soundSettingsRoot.transform, string.Empty, width: PauseMenuPrimaryButtonWidth, secondaryStyle: true);
            soundEffectsToggleButton.onClick.AddListener(OnSoundEffectsToggleClicked);
            EnsureTooltip(soundEffectsToggleButton, "Turn sound effects on or off.");

            soundEffectsVolumeButton = CreateActionButton(soundSettingsRoot.transform, string.Empty, width: PauseMenuPrimaryButtonWidth, secondaryStyle: true);
            soundEffectsVolumeButton.onClick.AddListener(OnSoundEffectsVolumeClicked);
            EnsureTooltip(soundEffectsVolumeButton, "Cycle the sound effects volume to the next preset.");

            musicVolumeButton = CreateActionButton(soundSettingsRoot.transform, string.Empty, width: PauseMenuPrimaryButtonWidth, secondaryStyle: true);
            musicVolumeButton.onClick.AddListener(OnMusicVolumeClicked);
            EnsureTooltip(musicVolumeButton, "Cycle the music volume to the next preset.");

            nextTrackMenuButton = CreateActionButton(
                soundSettingsRoot.transform,
                "Next Track",
                gameUI != null ? gameUI.NextTrackMenuButtonIcon : null,
                PauseMenuPrimaryButtonWidth,
                secondaryStyle: true,
                iconColor: UIStyleTokens.Text.Primary);
            nextTrackMenuButton.onClick.AddListener(OnNextTrackClicked);

            TooltipTrigger nextTrackTooltip = nextTrackMenuButton.gameObject.AddComponent<TooltipTrigger>();
            nextTrackTooltip.SetDynamicProvider(this);

            confirmationRoot = CreateVerticalSection(contentRoot.transform, "Confirmation", 12f);

            confirmationLabel = CreateLabel(confirmationRoot.transform, string.Empty, 21f, FontStyles.Normal);
            confirmationLabel.alignment = TextAlignmentOptions.Center;
            confirmationLabel.color = UIStyleTokens.Text.Secondary;
            ConfigureDynamicWrappedLabel(confirmationLabel);

            GameObject confirmationButtons = CreateUiObject("ConfirmationButtons", confirmationRoot.transform);
            HorizontalLayoutGroup buttonRow = confirmationButtons.AddComponent<HorizontalLayoutGroup>();
            buttonRow.spacing = 12f;
            buttonRow.childAlignment = TextAnchor.MiddleCenter;
            buttonRow.childControlWidth = true;
            buttonRow.childControlHeight = false;
            buttonRow.childForceExpandWidth = true;
            buttonRow.childForceExpandHeight = false;

            Button confirmButton = CreateActionButton(confirmationButtons.transform, "Confirm", width: PauseMenuConfirmationButtonWidth, useSelectedAsNormal: true);
            confirmButton.onClick.AddListener(ConfirmPendingAction);
            EnsureTooltip(confirmButton, GetConfirmTooltipText);

            Button cancelButton = CreateActionButton(confirmationButtons.transform, "Cancel", width: PauseMenuConfirmationButtonWidth, secondaryStyle: true);
            cancelButton.onClick.AddListener(CancelPendingAction);
            EnsureTooltip(cancelButton, "Cancel this confirmation and return to the pause menu.");

            RefreshTutorialControls(clearStatus: true);
            RefreshSoundSettingsButtons();
            RefreshCardLayout();
        }

        private void ApplyPanelState()
        {
            if (primaryActionsRoot == null || soundSettingsRoot == null || confirmationRoot == null || confirmationLabel == null || subtitleLabel == null)
            {
                return;
            }

            bool showConfirmation = pendingAction != PendingAction.None;
            primaryActionsRoot.SetActive(!showConfirmation);
            tutorialHelpRoot.SetActive(!showConfirmation);
            soundSettingsRoot.SetActive(!showConfirmation);
            confirmationRoot.SetActive(showConfirmation);

            if (!showConfirmation)
            {
                subtitleLabel.text = string.Empty;
                RefreshCardLayout();
                return;
            }

            switch (pendingAction)
            {
                case PendingAction.RestartLevel:
                    string runUnit = GetCurrentRunUnitName().ToLowerInvariant();
                    subtitleLabel.text = $"Restart this {runUnit}?";
                    confirmationLabel.text = $"Restart the current {runUnit} from its original seed and setup? Progress since the {runUnit} began will be discarded.";
                    break;
                case PendingAction.ReturnToMainMenu:
                    subtitleLabel.text = "Leave the current run?";
                    confirmationLabel.text = "Return to the main menu? Your current game will be saved and can be resumed later.";
                    break;
                case PendingAction.ExitGame:
                    subtitleLabel.text = "Close Fungus Toast?";
                    confirmationLabel.text = "Exit the game now? Your current game will be saved and can be resumed later.";
                    break;
            }

            RefreshCardLayout();
        }

        private void RequestMainMenuConfirmation()
        {
            pendingAction = PendingAction.ReturnToMainMenu;
            ApplyPanelState();
        }

        private void RequestRestartConfirmation()
        {
            pendingAction = PendingAction.RestartLevel;
            ApplyPanelState();
        }

        private void RequestExitConfirmation()
        {
            pendingAction = PendingAction.ExitGame;
            ApplyPanelState();
        }

        private void ConfirmPendingAction()
        {
            switch (pendingAction)
            {
                case PendingAction.RestartLevel:
                    onRestartLevelRequested?.Invoke();
                    break;
                case PendingAction.ReturnToMainMenu:
                    onReturnToMainMenuRequested?.Invoke();
                    break;
                case PendingAction.ExitGame:
                    onExitRequested?.Invoke();
                    break;
            }
        }

        private void OnSoundEffectsToggleClicked()
        {
            SoundEffectsSettings.ToggleEnabled();
            RefreshSoundSettingsButtons();
        }

        private void OnSoundEffectsVolumeClicked()
        {
            SoundEffectsSettings.CycleVolumeForward();
            RefreshSoundSettingsButtons();
        }

        private void OnMusicVolumeClicked()
        {
            MusicSettings.CycleVolumeForward();
            GameManager.Instance?.RefreshMusicVolume();
            RefreshSoundSettingsButtons();
        }

        private void OnNextTrackClicked()
        {
            onNextTrackRequested?.Invoke();
        }

        private void OnReplayTutorialTipsClicked()
        {
            if (onReplayTutorialTipsRequested == null)
            {
                SetTutorialStatus("Tutorial tips are unavailable right now.", UIStyleTokens.State.Warning);
                return;
            }

            onReplayTutorialTipsRequested.Invoke();
            SetTutorialStatus("Tutorial tips re-enabled.", UIStyleTokens.State.Success);
        }

        private void RefreshTutorialControls(bool clearStatus)
        {
            SetButtonLabel(tutorialReplayButton, "Replay Tutorial Tips");

            if (clearStatus)
            {
                SetTutorialStatus(string.Empty, UIStyleTokens.Text.Secondary);
            }
        }

        private void SetTutorialStatus(string statusText, Color statusColor)
        {
            if (tutorialStatusLabel == null)
            {
                return;
            }

            tutorialStatusLabel.text = statusText;
            tutorialStatusLabel.color = statusColor;
            tutorialStatusLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(statusText));
            RefreshCardLayout();
        }

        private void RefreshSoundSettingsButtons()
        {
            SetButtonLabel(soundEffectsToggleButton, $"Sound Effects: {(SoundEffectsSettings.Enabled ? "On" : "Off")}");
            SetButtonLabel(soundEffectsVolumeButton, $"SFX Volume: {Mathf.RoundToInt(SoundEffectsSettings.Volume * 100f)}%");
            SetButtonLabel(musicVolumeButton, $"Music Volume: {Mathf.RoundToInt(MusicSettings.Volume * 100f)}%");
            SetButtonLabel(nextTrackMenuButton, "Next Track");
        }

        private static string FormatTrackName(string trackName, string fallback)
        {
            return string.IsNullOrWhiteSpace(trackName) ? fallback : trackName;
        }

        private static void EnsureTooltip(Button button, string text)
        {
            EnsureTooltip(button, () => text);
        }

        private static void EnsureTooltip(Button button, Func<string> resolver)
        {
            if (button == null || resolver == null)
            {
                return;
            }

            var provider = button.GetComponent<MoldButtonTooltipProvider>();
            if (provider == null)
            {
                provider = button.gameObject.AddComponent<MoldButtonTooltipProvider>();
            }

            provider.Initialize(resolver);

            var trigger = button.GetComponent<TooltipTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<TooltipTrigger>();
            }

            trigger.SetDynamicProvider(provider);
        }

        private string GetConfirmTooltipText()
        {
            return pendingAction switch
            {
                PendingAction.RestartLevel => $"Confirm restarting this {GetCurrentRunUnitName().ToLowerInvariant()} from its original seed.",
                PendingAction.ReturnToMainMenu => "Confirm returning to the main menu and saving this run.",
                PendingAction.ExitGame => "Confirm exiting the game and saving this run.",
                _ => "Confirm the current pause menu action."
            };
        }

        private static string GetCurrentRunUnitName()
        {
            return GameManager.Instance != null && GameManager.Instance.CurrentGameMode == GameMode.Campaign
                ? "Stage"
                : "Game";
        }

        private static void SetButtonLabel(Button button, string labelText)
        {
            if (button == null)
            {
                return;
            }

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = labelText;
            }
        }

        private Button CreateActionButton(
            Transform parent,
            string labelText,
            Sprite icon = null,
            float width = PauseMenuPrimaryButtonWidth,
            bool secondaryStyle = false,
            bool useSelectedAsNormal = false,
            Color? iconColor = null)
        {
            GameObject buttonObject = CreateUiObject(labelText.Replace(" ", string.Empty) + "Button", parent);

            Image image = buttonObject.AddComponent<Image>();
            image.color = UIStyleTokens.Button.BackgroundDefault;

            Button button = buttonObject.AddComponent<Button>();

            if (icon != null)
            {
                GameObject contentRoot = CreateUiObject("ButtonContent", buttonObject.transform);
                RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0.5f, 0.5f);
                contentRect.anchorMax = new Vector2(0.5f, 0.5f);
                contentRect.pivot = new Vector2(0.5f, 0.5f);
                contentRect.anchoredPosition = Vector2.zero;

                HorizontalLayoutGroup contentLayout = contentRoot.AddComponent<HorizontalLayoutGroup>();
                contentLayout.spacing = ActionButtonContentSpacing;
                contentLayout.childAlignment = TextAnchor.MiddleCenter;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = false;
                contentLayout.childForceExpandHeight = false;

                ContentSizeFitter contentFitter = contentRoot.AddComponent<ContentSizeFitter>();
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                CreateIconLayoutImage(
                    contentRoot.transform,
                    "ButtonIcon",
                    icon,
                    ActionButtonIconSize,
                    iconColor ?? UIStyleTokens.Button.TextDefault);

                TextMeshProUGUI iconLabel = CreateActionButtonContentLabel(contentRoot.transform, labelText);
                iconLabel.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                TextMeshProUGUI label = CreateLabel(buttonObject.transform, labelText, 24f, FontStyles.Bold);
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                label.alignment = TextAlignmentOptions.Center;
                label.margin = Vector4.zero;
            }

            if (secondaryStyle)
            {
                UIStyleTokens.Button.ApplySecondaryMenuAction(button, width);
            }
            else
            {
                UIStyleTokens.Button.ApplyPrimaryMenuAction(button, width, useSelectedAsNormal);
            }

            return button;
        }

        private static GameObject CreateVerticalSection(Transform parent, string name, float spacing)
        {
            GameObject section = CreateUiObject(name, parent);

            VerticalLayoutGroup layoutGroup = section.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = spacing;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            ContentSizeFitter fitter = section.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement layout = section.AddComponent<LayoutElement>();
            layout.flexibleHeight = 0f;

            return section;
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize, FontStyles fontStyle)
        {
            GameObject labelObject = CreateUiObject("Label", parent);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            LayoutElement layout = labelObject.AddComponent<LayoutElement>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = UIStyleTokens.Text.Primary;
            label.raycastTarget = false;

            if (sharedFont != null)
            {
                label.font = sharedFont;
            }

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            float preferredHeight = fontSize + 18f;
            labelRect.sizeDelta = new Vector2(0f, preferredHeight);
            layout.minHeight = preferredHeight;
            layout.preferredHeight = preferredHeight;
            layout.flexibleHeight = 0f;
            return label;
        }

        private static void ConfigureDynamicWrappedLabel(TextMeshProUGUI label)
        {
            if (label == null)
            {
                return;
            }

            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;

            LayoutElement layout = label.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = 0f;
                layout.preferredHeight = -1f;
                layout.flexibleHeight = 0f;
            }

            RectTransform labelRect = label.rectTransform;
            labelRect.sizeDelta = new Vector2(0f, 0f);

            ContentSizeFitter fitter = label.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = label.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void RefreshCardLayout()
        {
            if (cardRect == null || contentRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);

            float preferredContentHeight = Mathf.Max(contentRect.rect.height, LayoutUtility.GetPreferredHeight(contentRect));
            float desiredHeight = Mathf.Max(CardMinimumHeight, preferredContentHeight + (CardPadding * 2f));
            RectTransform canvasRect = rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;
            float viewportHeight = canvasRect != null
                ? canvasRect.rect.height - CardViewportMargin
                : desiredHeight;
            float clampedHeight = Mathf.Min(desiredHeight, Mathf.Max(CardMinimumHeight, viewportHeight));
            cardRect.sizeDelta = new Vector2(CardWidth, clampedHeight);
        }

        private TextMeshProUGUI CreateActionButtonContentLabel(Transform parent, string text)
        {
            GameObject labelObject = CreateUiObject("Label", parent);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            LayoutElement layout = labelObject.AddComponent<LayoutElement>();

            label.text = text;
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = UIStyleTokens.Button.TextDefault;
            label.raycastTarget = false;

            if (sharedFont != null)
            {
                label.font = sharedFont;
            }

            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(0f, 42f);

            layout.minHeight = 42f;
            layout.preferredHeight = 42f;
            layout.flexibleHeight = 0f;
            layout.flexibleWidth = 0f;
            return label;
        }

        private static void CreateHamburgerIcon(Transform parent)
        {
            GameObject iconRoot = CreateUiObject("HamburgerIcon", parent);
            RectTransform iconRect = iconRoot.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(12f, 10f);
            iconRect.anchoredPosition = Vector2.zero;

            for (int barIndex = 0; barIndex < 3; barIndex++)
            {
                GameObject barObject = CreateUiObject($"Bar{barIndex + 1}", iconRoot.transform);
                Image bar = barObject.AddComponent<Image>();
                bar.color = UIStyleTokens.Button.TextDefault;
                bar.raycastTarget = false;

                RectTransform barRect = barObject.GetComponent<RectTransform>();
                barRect.anchorMin = new Vector2(0.5f, 0.5f);
                barRect.anchorMax = new Vector2(0.5f, 0.5f);
                barRect.pivot = new Vector2(0.5f, 0.5f);
                barRect.sizeDelta = new Vector2(12f, 2f);
                barRect.anchoredPosition = new Vector2(0f, 4f - (barIndex * 4f));
            }
        }

        private static Image CreateIconImage(
            Transform parent,
            string name,
            Sprite icon,
            float size,
            Vector2 anchoredPosition,
            bool anchorLeft = false)
        {
            GameObject iconObject = CreateUiObject(name, parent);
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.color = UIStyleTokens.Button.TextDefault;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            if (anchorLeft)
            {
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
            }
            else
            {
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
            }

            iconRect.sizeDelta = new Vector2(size, size);
            iconRect.anchoredPosition = anchoredPosition;
            return iconImage;
        }

        private static Image CreateIconLayoutImage(Transform parent, string name, Sprite icon, float size, Color color)
        {
            GameObject iconObject = CreateUiObject(name, parent);
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.color = color;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            LayoutElement layout = iconObject.AddComponent<LayoutElement>();
            layout.minWidth = size;
            layout.preferredWidth = size;
            layout.minHeight = size;
            layout.preferredHeight = size;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(size, size);
            return iconImage;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }
    }
}
