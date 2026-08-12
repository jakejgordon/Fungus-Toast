using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;
using FungusToast.Unity;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI.GameStart; // for UI_StartGamePanel
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;

namespace FungusToast.Unity.UI.Campaign
{
    /// <summary>
    /// First screen shown on launch: lets player choose Hotseat (single game) or Campaign.
    /// </summary>
    public class UI_ModeSelectPanelController : MonoBehaviour
    {
        private const float ExpandedContentWidth = UIStyleTokens.Startup.ContentWidth;
        private const float ExpandedButtonWidth = UIStyleTokens.Startup.CardWidth;
        private const float ExpandedDescriptionWidth = UIStyleTokens.Startup.CardWidth;
        private const float CreditsCardWidth = 800f;
        private const float CreditsTextWidth = 700f;
        private const float WideLogoWidth = 520f;
        private const float WideLogoHeight = 223f;
        private const float TitleHeight = 34f;
        private const float FooterHeight = 24f;
        private const float CompactMenuButtonIconSize = 22f;
        private const float CompactMenuButtonContentSpacing = 10f;
        private const float CompactMenuButtonHorizontalPadding = 14f;
        private const float MinimumVerticalMargin = 32f;
        private const float ResponsiveScaleSafetyFactor = 0.97f;
        private const float SettingsCardWidth = 860f;
        private const float SettingsTextWidth = 700f;
        private const int AmbientMoldSpriteIndexScanLimit = 12;
        private const float AmbientMoldBaseAlpha = 0.12f;
        private const float AmbientMoldAlphaRange = 0.06f;
        private const float AmbientMoldScalePulse = 0.06f;
        private const float AmbientMoldDriftDistance = 10f;
        private const float AmbientEncroachmentBaseAlpha = 0.025f;
        private const float AmbientEncroachmentAlphaRange = 0.025f;
        private const float AmbientEncroachmentScalePulse = 0.035f;
        private const float AmbientEncroachmentDriftDistance = 5f;
        private const float AmbientEncroachmentRevealLeadInSeconds = 1f;
        private const float AmbientEncroachmentRevealWindowSeconds = 30f;
        private const float AmbientBackdropVignetteAlpha = 0.2f;
        private const float OverlayCardAlpha = 0.84f;
        private const int MainMenuHorizontalPadding = 40;
        private const int MainMenuVerticalPadding = 32;
        private const float MainMenuElementSpacing = 16f;
        private const string AlphaHeadingText = "ALPHA BUILD";
        private const string CustomGameDescription = "Play solo against AI or share this device.";
        private const string CampaignDescription = "Play a persistent run with unlocks and escalating challenges.";
        private const string CreditsHeadingText = "Credits";
        private const string ArtworkHeadingText = "Artwork";
        private const string ArtworkCreditName = "Matthew";
        private const string ArtworkCreditCopy = "Original artwork and game graphics";
        private const string MusicHeadingText = "Music";
        private const string MusicCreditName = "Chris Howard";
        private const string MusicCreditCopy = "“Fungus Toast” — original music";
        private const string SettingsHeadingText = "Settings";
        private const string SettingsAudioHeadingText = "Audio";
        private const string SettingsHelpHeadingText = "Help & Tutorials";
        private const string SettingsAdvancedHeadingText = "Campaign Data";
        private const string SettingsTutorialSummaryText = "Re-enable tutorial popups and guidance hints you previously dismissed. This does not reset campaign progress.";
        private const string SettingsResetPromptText = "Confirm reset? This cannot be undone.";
        private const string SettingsResetSummaryText = "Erases Moldiness level and progress, permanent Moldiness rewards, pending reward choices, and preserved Adaptation carryover. It does not delete the campaign save. If defeat carryover is pending, that run resets.";

        [Header("Panels")] 
        [SerializeField] private UI_StartGamePanel startGamePanel = null; // existing start / player config panel
        [SerializeField] private GameObject campaignPanel = null; // UI_CampaignPanel root

        [Header("Buttons")] 
        [SerializeField] private Button hotseatButton = null;
        [SerializeField] private Button campaignButton = null;

        [Header("Layout")]
        [SerializeField] private RectTransform contentRoot = null;
        [SerializeField] private Image titleLogoImage = null;
        [SerializeField] private TextMeshProUGUI titleText = null;
        [SerializeField] private TextMeshProUGUI hotseatDescriptionText = null;
        [SerializeField] private TextMeshProUGUI campaignDescriptionText = null;
        [SerializeField] private Sprite wideTitleLogoSprite = null;
        [SerializeField] private Sprite settingsButtonIcon = null;
        [SerializeField] private Sprite backButtonIcon = null;

        private TextMeshProUGUI alphaSummaryText;
        private TextMeshProUGUI versionText;
        private RectTransform buildStatusBadgeRoot;
        private GameObject creditsPanel;
        private GameObject settingsPanel;
        private Button creditsButton;
        private Button settingsButton;
        private Button creditsBackButton;
        private Button settingsBackButton;
        private Slider settingsSoundEffectsSlider;
        private Slider settingsMusicSlider;
        private TextMeshProUGUI settingsSoundEffectsValueLabel;
        private TextMeshProUGUI settingsMusicValueLabel;
        private Button settingsTutorialReplayButton;
        private Button settingsResetButton;
        private Button settingsResetCancelButton;
        private Button quitButton;
        private TextMeshProUGUI settingsTutorialStatusText;
        private TextMeshProUGUI settingsResetStatusText;
        private TextMeshProUGUI settingsResetPromptLabel;
        private GameObject compatibilityNoticeModalRoot;
        private TextMeshProUGUI compatibilityNoticeTitleText;
        private TextMeshProUGUI compatibilityNoticeBodyText;
        private Button compatibilityNoticeCloseButton;
        private bool isConfirmingCampaignReset;
        private RectTransform ambientBackdropLayerRoot;
        private RectTransform ambientMoldLayerRoot;
        private readonly List<AmbientBackdropDecoration> ambientBackdropDecorations = new();
        private readonly List<AmbientMoldDecoration> ambientMoldDecorations = new();
        private float ambientSequenceStartTime = -1f;

        private sealed class AmbientBackdropDecoration
        {
            public RectTransform RectTransform;
            public Image Image;
            public Vector2 BaseSize;
            public Vector2 AnchoredPosition;
            public float BaseAlpha;
            public float AlphaPhase;
            public float AlphaSpeed;
            public float AlphaRange;
            public float ScalePhase;
            public float ScaleSpeed;
        }

        private sealed class AmbientMoldDecoration
        {
            public Image Image;
            public Vector2 AnchoredPosition;
            public Vector2 DriftDirection;
            public float BaseScale;
            public float ScalePhase;
            public float PulseSpeed;
            public float AlphaPhase;
            public float AlphaSpeed;
            public float Rotation;
            public bool FlipX;
            public bool FlipY;
            public float BaseAlpha;
            public float AlphaRange;
            public float ScalePulse;
            public float DriftDistance;
            public float RotationAmplitude;
            public bool IsEncroachment;
            public float GrowthPhase;
            public float GrowthSpeed;
            public float RevealDelay;
            public float RevealDuration;
        }

        private void Awake()
        {
            ResolveSceneReferences();
            ConfigureLayout();
            EnsureReleaseUi();
            EnsureAmbientBackdropLayer();
            EnsureAmbientMoldLayer();
            ApplyStyle();

            if (hotseatButton != null) hotseatButton.onClick.AddListener(OnHotseatClicked);
            if (campaignButton != null) campaignButton.onClick.AddListener(OnCampaignClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

            ApplyTooltips();
        }

        private void ResolveSceneReferences()
        {
            if (contentRoot == null)
            {
                contentRoot = FindChildComponent<RectTransform>("UI_ModeSelectContent");
            }

            if (titleLogoImage == null)
            {
                titleLogoImage = FindChildComponent<Image>("UI_ModeSelectContent/UI_ModeSelectTitleLogo");
            }

            if (titleText == null)
            {
                titleText = FindChildComponent<TextMeshProUGUI>("UI_ModeSelectContent/UI_ModeSelectTitle");
            }

            if (hotseatButton == null)
            {
                hotseatButton = FindChildComponent<Button>("UI_ModeSelectContent/UI_ModeSelectHotseatButton");
            }

            if (campaignButton == null)
            {
                campaignButton = FindChildComponent<Button>("UI_ModeSelectContent/UI_ModeSelectCampaignButton");
            }

            if (hotseatDescriptionText == null)
            {
                hotseatDescriptionText = FindChildComponent<TextMeshProUGUI>("UI_ModeSelectContent/UI_ModeSelectHotseatDescriptionText");
            }

            if (campaignDescriptionText == null)
            {
                campaignDescriptionText = FindChildComponent<TextMeshProUGUI>("UI_ModeSelectContent/UI_ModeSelectCampaignDescriptionText");
            }
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, Color.Lerp(UIStyleTokens.Surface.Canvas, UIStyleTokens.Accent.Hyphae, 0.09f));
            UIStyleTokens.ApplyNonButtonTextPalette(gameObject);

            if (contentRoot != null)
            {
                Image contentCard = contentRoot.GetComponent<Image>();
                if (contentCard == null)
                {
                    contentCard = contentRoot.gameObject.AddComponent<Image>();
                }

                UIStyleTokens.Startup.ApplyCard(contentCard, alpha: OverlayCardAlpha);
                contentCard.raycastTarget = false;
            }

            if (titleText != null)
            {
                titleText.color = UIStyleTokens.Accent.Spore;
            }

            if (alphaSummaryText != null)
            {
                alphaSummaryText.gameObject.SetActive(false);
            }

            if (versionText != null)
            {
                versionText.color = UIStyleTokens.Text.Muted;
            }

            UIStyleTokens.Button.ApplyNeutralMenuAction(hotseatButton, ExpandedButtonWidth, preferredHeight: 90f, minHeight: 72f);
            UIStyleTokens.Button.ApplyNeutralMenuAction(campaignButton, ExpandedButtonWidth, preferredHeight: 90f, minHeight: 72f);
            UIStyleTokens.Button.ApplySecondaryMenuAction(creditsButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            UIStyleTokens.Button.ApplySecondaryMenuAction(creditsBackButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsBackButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsTutorialReplayButton);
            UIStyleTokens.Button.ApplyDangerMenuAction(settingsResetButton);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsResetCancelButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            UIStyleTokens.Button.ApplySecondaryMenuAction(quitButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);

            ApplyModeSelectCompactButtonStyle(creditsButton);
            ApplyModeSelectCompactButtonStyle(settingsButton);
            ApplyModeSelectCompactButtonStyle(creditsBackButton);
            ApplyModeSelectCompactButtonStyle(settingsBackButton);
            ApplyModeSelectCompactButtonStyle(quitButton);
        }

        private static void ApplyModeSelectCompactButtonStyle(Button button)
        {
            UIStyleTokens.Button.ApplyStartupUtilityAction(button);
        }

        private void OnEnable()
        {
            UpdateVersionLabel();
            ShowMainMenuContent();
            RefreshCampaignButtonState();
            RefreshSettingsState();

            // Ensure subordinate panels start hidden so only mode select is visible.
            if (startGamePanel != null) startGamePanel.gameObject.SetActive(false);
            if (campaignPanel != null) campaignPanel.SetActive(false);

            ambientSequenceStartTime = Time.unscaledTime;
            RefreshAmbientMoldDecorations();
            RefreshResponsiveLayout();
            TryShowPendingCompatibilityNotice();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshResponsiveLayout();
        }

        private void Update()
        {
            AnimateAmbientMoldDecorations();
        }

        private void OnHotseatClicked()
        {
            ShowBackdropOnlyForSubpanel(startGamePanel != null ? startGamePanel.transform : null);
            if (startGamePanel != null)
            {
                startGamePanel.gameObject.SetActive(true);
            }
        }

        private void OnCampaignClicked()
        {
            GameManager manager = FindAnyObjectByType<GameManager>();
            if (manager != null && manager.HasPendingCampaignMoldinessUnlockOnSavedRun())
            {
                manager.ShowPendingCampaignMoldinessRewardFromMainMenu();
                gameObject.SetActive(false);
                return;
            }

            ShowBackdropOnlyForSubpanel(campaignPanel != null ? campaignPanel.transform : null);
            if (campaignPanel != null)
            {
                campaignPanel.SetActive(true);
            }
        }

        private void OnQuitClicked()
        {
            GameManager manager = FindAnyObjectByType<GameManager>();
            if (manager != null)
            {
                manager.QuitGame();
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnCreditsClicked()
        {
            ShowCreditsContent();
        }

        private void OnSettingsClicked()
        {
            ShowSettingsContent();
        }

        private void OnCreditsBackClicked()
        {
            ShowMainMenuContent();
        }

        private void OnSettingsBackClicked()
        {
            ShowMainMenuContent();
        }

        private void OnSettingsSoundEffectsChanged(float value)
        {
            SoundEffectsSettings.SetVolume(value);
            RefreshSettingsAudioLabels();
        }

        private void OnSettingsMusicChanged(float value)
        {
            MusicSettings.SetVolume(value);
            GameManager.Instance?.RefreshMusicVolume();
            RefreshSettingsAudioLabels();
        }

        private void OnSettingsTutorialReplayClicked()
        {
            GameManager manager = FindAnyObjectByType<GameManager>();
            if (manager != null)
            {
                manager.ResetDismissedTutorialTips();
            }

            if (settingsTutorialStatusText != null)
            {
                settingsTutorialStatusText.text = "Tutorial tips re-enabled.";
                settingsTutorialStatusText.color = UIStyleTokens.State.Success;
            }
        }

        private void OnSettingsResetClicked()
        {
            if (!isConfirmingCampaignReset)
            {
                isConfirmingCampaignReset = true;
                if (settingsResetStatusText != null)
                {
                    settingsResetStatusText.text = string.Empty;
                }

                RefreshSettingsResetControls();
                return;
            }

            GameManager manager = FindAnyObjectByType<GameManager>();
            bool resetApplied = manager != null && manager.ResetCampaignMoldinessProgression();
            isConfirmingCampaignReset = false;

            if (settingsResetStatusText != null)
            {
                settingsResetStatusText.text = resetApplied
                    ? "Campaign rewards and moldiness progress have been reset."
                    : "No campaign save was found to reset.";
                settingsResetStatusText.color = resetApplied ? UIStyleTokens.State.Success : UIStyleTokens.Text.Secondary;
            }

            RefreshCampaignButtonState();
            RefreshSettingsState();
        }

        private void OnSettingsResetCancelClicked()
        {
            isConfirmingCampaignReset = false;
            RefreshSettingsResetControls();
        }

        private void ConfigureLayout()
        {
            if (contentRoot != null)
            {
                contentRoot.sizeDelta = new Vector2(ExpandedContentWidth, contentRoot.sizeDelta.y);

                VerticalLayoutGroup contentLayout = contentRoot.GetComponent<VerticalLayoutGroup>();
                if (contentLayout != null)
                {
                    contentLayout.padding = new RectOffset(
                        MainMenuHorizontalPadding,
                        MainMenuHorizontalPadding,
                        MainMenuVerticalPadding,
                        MainMenuVerticalPadding);
                    contentLayout.spacing = MainMenuElementSpacing;
                }
            }

            ResizeRectTransform(hotseatButton != null ? hotseatButton.GetComponent<RectTransform>() : null, ExpandedButtonWidth, 90f);
            ResizeRectTransform(campaignButton != null ? campaignButton.GetComponent<RectTransform>() : null, ExpandedButtonWidth, 90f);
            ResizeRectTransform(hotseatDescriptionText != null ? hotseatDescriptionText.rectTransform : null, ExpandedDescriptionWidth, 50f);
            ResizeRectTransform(campaignDescriptionText != null ? campaignDescriptionText.rectTransform : null, ExpandedDescriptionWidth, 50f);

            if (titleLogoImage != null)
            {
                if (wideTitleLogoSprite != null)
                {
                    titleLogoImage.sprite = wideTitleLogoSprite;
                }

                titleLogoImage.preserveAspect = true;
                ResizeRectTransform(titleLogoImage.rectTransform, WideLogoWidth, WideLogoHeight);
            }

            if (titleText != null)
            {
                titleText.text = AlphaHeadingText;
                titleText.enableAutoSizing = true;
                titleText.fontSizeMin = 14f;
                titleText.fontSizeMax = 16f;
                titleText.fontSize = 16f;
                titleText.fontStyle = FontStyles.Bold;
                titleText.color = UIStyleTokens.Accent.Spore;
                titleText.characterSpacing = 2f;
                titleText.alignment = TextAlignmentOptions.Center;
                ResizeRectTransform(titleText.rectTransform, 180f, TitleHeight);
                EnsureBuildStatusBadge();
            }

            SetButtonLabel(hotseatButton, "Custom Game");
            if (hotseatDescriptionText != null)
            {
                hotseatDescriptionText.text = CustomGameDescription;
                UIStyleTokens.Startup.ApplySupportingCopy(hotseatDescriptionText);
            }

            if (campaignDescriptionText != null)
            {
                campaignDescriptionText.text = CampaignDescription;
                UIStyleTokens.Startup.ApplySupportingCopy(campaignDescriptionText);
            }
        }

        private void EnsureReleaseUi()
        {
            if (contentRoot == null)
            {
                return;
            }

            if (alphaSummaryText != null)
            {
                alphaSummaryText.gameObject.SetActive(false);
            }

            if (creditsButton == null)
            {
                creditsButton = CreateButton("UI_ModeSelectCreditsButton", "Credits");
                creditsButton.onClick.AddListener(OnCreditsClicked);
            }

            if (settingsButton == null)
            {
                settingsButton = CreateButton("UI_ModeSelectSettingsButton", "Settings");
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (quitButton == null && ShouldShowQuitButton())
            {
                quitButton = CreateButton("UI_ModeSelectQuitButton", "Quit to Desktop");
                quitButton.transform.SetAsLastSibling();
            }

            if (quitButton != null)
            {
                quitButton.gameObject.SetActive(ShouldShowQuitButton());
            }

            if (versionText == null)
            {
                versionText = CreateLabel(
                    "UI_ModeSelectVersionText",
                    BuildVersionLabel(),
                    18f,
                    FooterHeight,
                    UIStyleTokens.Text.Muted);
                versionText.enableAutoSizing = false;
                versionText.transform.SetAsLastSibling();
            }

            UpdateVersionLabel();

            if (settingsButton != null)
            {
                settingsButton.transform.SetAsLastSibling();
            }

            if (creditsButton != null)
            {
                creditsButton.transform.SetAsLastSibling();
            }

            if (quitButton != null)
            {
                quitButton.transform.SetAsLastSibling();
            }

            if (buildStatusBadgeRoot != null)
            {
                buildStatusBadgeRoot.SetAsLastSibling();
            }
            else if (titleText != null)
            {
                titleText.transform.SetAsLastSibling();
            }

            if (versionText != null)
            {
                versionText.transform.SetAsLastSibling();
            }

            EnsureCreditsPanel();
            EnsureSettingsPanel();
        }

        private void EnsureBuildStatusBadge()
        {
            if (contentRoot == null || titleText == null)
            {
                return;
            }

            if (buildStatusBadgeRoot == null)
            {
                GameObject badgeObject = new GameObject(
                    "UI_ModeSelectBuildStatusBadge",
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(Image),
                    typeof(Outline));
                int titleSiblingIndex = titleText.transform.GetSiblingIndex();
                badgeObject.transform.SetParent(contentRoot, false);
                badgeObject.transform.SetSiblingIndex(titleSiblingIndex);
                badgeObject.layer = gameObject.layer;
                buildStatusBadgeRoot = badgeObject.GetComponent<RectTransform>();

                LayoutElement layout = badgeObject.GetComponent<LayoutElement>();
                layout.minWidth = 180f;
                layout.preferredWidth = 180f;
                layout.minHeight = TitleHeight;
                layout.preferredHeight = TitleHeight;
                layout.flexibleWidth = 0f;
                layout.flexibleHeight = 0f;

                Image background = badgeObject.GetComponent<Image>();
                background.color = UIStyleTokens.WithAlpha(UIStyleTokens.Surface.PanelSecondary, 0.94f);
                background.raycastTarget = false;

                Outline outline = badgeObject.GetComponent<Outline>();
                outline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.Accent.Lichen, UIStyleTokens.Alpha.AccentOutline);
                outline.effectDistance = new Vector2(1f, -1f);

                titleText.transform.SetParent(buildStatusBadgeRoot, false);
            }

            buildStatusBadgeRoot.sizeDelta = new Vector2(180f, TitleHeight);
            RectTransform titleRect = titleText.rectTransform;
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(10f, 0f);
            titleRect.offsetMax = new Vector2(-10f, 0f);
        }

        private void UpdateVersionLabel()
        {
            if (versionText != null)
            {
                versionText.text = BuildVersionLabel();
            }
        }

        private void TryShowPendingCompatibilityNotice()
        {
            if (!BoardLayoutCompatibilityService.TryConsumePendingRestartNotice(out string title, out string body))
            {
                return;
            }

            EnsureCompatibilityNoticeModal();
            if (compatibilityNoticeModalRoot == null || compatibilityNoticeTitleText == null || compatibilityNoticeBodyText == null)
            {
                return;
            }

            compatibilityNoticeTitleText.text = title;
            compatibilityNoticeBodyText.text = body;
            compatibilityNoticeModalRoot.SetActive(true);
            compatibilityNoticeModalRoot.transform.SetAsLastSibling();
            RefreshResponsiveLayout();
        }

        private void HideCompatibilityNotice()
        {
            if (compatibilityNoticeModalRoot != null)
            {
                compatibilityNoticeModalRoot.SetActive(false);
            }
        }

        private void EnsureCompatibilityNoticeModal()
        {
            if (compatibilityNoticeModalRoot != null)
            {
                return;
            }

            GameObject modalRoot = new GameObject("UI_ModeSelectCompatibilityNotice", typeof(RectTransform), typeof(Image));
            modalRoot.transform.SetParent(transform, false);
            modalRoot.layer = gameObject.layer;
            compatibilityNoticeModalRoot = modalRoot;

            RectTransform modalRootRect = modalRoot.GetComponent<RectTransform>();
            modalRootRect.anchorMin = Vector2.zero;
            modalRootRect.anchorMax = Vector2.one;
            modalRootRect.offsetMin = Vector2.zero;
            modalRootRect.offsetMax = Vector2.zero;

            Image modalRootImage = modalRoot.GetComponent<Image>();
            modalRootImage.color = UIStyleTokens.Surface.OverlayDim;
            modalRootImage.raycastTarget = true;

            GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement), typeof(Outline));
            panelObject.transform.SetParent(modalRoot.transform, false);
            panelObject.layer = gameObject.layer;

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(760f, 0f);

            Image panelImage = panelObject.GetComponent<Image>();
            panelImage.color = UIStyleTokens.Surface.PanelPrimary;
            panelImage.raycastTarget = true;

            Outline panelOutline = panelObject.GetComponent<Outline>();
            panelOutline.effectColor = new Color(UIStyleTokens.Accent.Spore.r, UIStyleTokens.Accent.Spore.g, UIStyleTokens.Accent.Spore.b, UIStyleTokens.Alpha.FocusOutline);
            panelOutline.effectDistance = new Vector2(1f, -1f);

            VerticalLayoutGroup panelLayout = panelObject.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(32, 32, 28, 28);
            panelLayout.spacing = 18f;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = false;
            panelLayout.childForceExpandHeight = false;

            ContentSizeFitter panelFitter = panelObject.GetComponent<ContentSizeFitter>();
            panelFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            panelFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            LayoutElement panelLayoutElement = panelObject.GetComponent<LayoutElement>();
            panelLayoutElement.preferredWidth = 760f;
            panelLayoutElement.minWidth = 760f;
            panelLayoutElement.flexibleWidth = 0f;

            compatibilityNoticeTitleText = CreateCompatibilityNoticeLabel(
                panelObject.transform,
                "Title",
                30f,
                FontStyles.Bold,
                UIStyleTokens.Text.Primary,
                TextAlignmentOptions.Center,
                680f);

            compatibilityNoticeBodyText = CreateCompatibilityNoticeLabel(
                panelObject.transform,
                "Body",
                22f,
                FontStyles.Normal,
                UIStyleTokens.Text.Secondary,
                TextAlignmentOptions.Left,
                680f);

            compatibilityNoticeCloseButton = CreateButtonCore(panelObject.transform, "CloseButton", "Close", 24f, FontStyles.Bold);
            compatibilityNoticeCloseButton.onClick.AddListener(HideCompatibilityNotice);
            UIStyleTokens.Button.ApplySecondaryMenuAction(compatibilityNoticeCloseButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);

            compatibilityNoticeModalRoot.SetActive(false);
        }

        private void EnsureAmbientMoldLayer()
        {
            if (ambientMoldLayerRoot != null)
            {
                return;
            }

            GameObject layerObject = new GameObject("UI_ModeSelectAmbientMoldLayer", typeof(RectTransform));
            layerObject.transform.SetParent(transform, false);
            layerObject.layer = gameObject.layer;

            ambientMoldLayerRoot = layerObject.GetComponent<RectTransform>();
            ambientMoldLayerRoot.anchorMin = Vector2.zero;
            ambientMoldLayerRoot.anchorMax = Vector2.one;
            ambientMoldLayerRoot.offsetMin = Vector2.zero;
            ambientMoldLayerRoot.offsetMax = Vector2.zero;
            ambientMoldLayerRoot.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));

            CreateAmbientMoldDecoration("TopLeft", new Vector2(0f, 1f), new Vector2(42f, -46f), new Vector2(230f, 230f), 20f, new Vector2(0.9f, -0.5f));
            CreateAmbientMoldDecoration("UpperLeft", new Vector2(0f, 1f), new Vector2(86f, -222f), new Vector2(150f, 150f), -14f, new Vector2(1f, -0.2f));
            CreateAmbientMoldDecoration("MidLeft", new Vector2(0f, 0.5f), new Vector2(38f, 58f), new Vector2(186f, 186f), 6f, new Vector2(1f, 0.08f));
            CreateAmbientMoldDecoration("BottomLeft", new Vector2(0f, 0f), new Vector2(54f, 48f), new Vector2(196f, 196f), -18f, new Vector2(0.84f, 0.48f));
            CreateAmbientMoldDecoration("TopRight", new Vector2(1f, 1f), new Vector2(-44f, -44f), new Vector2(224f, 224f), -18f, new Vector2(-0.92f, -0.46f));
            CreateAmbientMoldDecoration("UpperRight", new Vector2(1f, 1f), new Vector2(-94f, -230f), new Vector2(152f, 152f), 12f, new Vector2(-1f, -0.16f));
            CreateAmbientMoldDecoration("MidRight", new Vector2(1f, 0.5f), new Vector2(-36f, -18f), new Vector2(184f, 184f), -8f, new Vector2(-1f, 0.06f));
            CreateAmbientMoldDecoration("BottomRight", new Vector2(1f, 0f), new Vector2(-56f, 54f), new Vector2(214f, 214f), 16f, new Vector2(-0.88f, 0.5f));
            CreateAmbientMoldDecoration("TopCenterLeft", new Vector2(0.5f, 1f), new Vector2(-356f, -54f), new Vector2(176f, 176f), 16f, new Vector2(0.54f, -0.84f));
            CreateAmbientMoldDecoration("TopCenterMidLeft", new Vector2(0.5f, 1f), new Vector2(-174f, -36f), new Vector2(138f, 138f), -10f, new Vector2(0.28f, -0.96f));
            CreateAmbientMoldDecoration("TopCenterMidRight", new Vector2(0.5f, 1f), new Vector2(172f, -40f), new Vector2(144f, 144f), 12f, new Vector2(-0.32f, -0.94f));
            CreateAmbientMoldDecoration("TopCenterRight", new Vector2(0.5f, 1f), new Vector2(354f, -58f), new Vector2(184f, 184f), -14f, new Vector2(-0.56f, -0.82f));
            CreateAmbientMoldDecoration("BottomCenterLeft", new Vector2(0.5f, 0f), new Vector2(-362f, 66f), new Vector2(182f, 182f), -18f, new Vector2(0.52f, 0.86f));
            CreateAmbientMoldDecoration("BottomCenterMidLeft", new Vector2(0.5f, 0f), new Vector2(-186f, 52f), new Vector2(142f, 142f), 8f, new Vector2(0.26f, 0.96f));
            CreateAmbientMoldDecoration("BottomCenterMidRight", new Vector2(0.5f, 0f), new Vector2(196f, 54f), new Vector2(148f, 148f), -12f, new Vector2(-0.24f, 0.96f));
            CreateAmbientMoldDecoration("BottomCenterRight", new Vector2(0.5f, 0f), new Vector2(372f, 70f), new Vector2(188f, 188f), 14f, new Vector2(-0.48f, 0.88f));
            CreateAmbientEncroachmentDecoration("UpperInnerLeft", new Vector2(0.5f, 0.5f), new Vector2(-382f, 152f), new Vector2(144f, 144f), 10f, new Vector2(1f, -0.08f));
            CreateAmbientEncroachmentDecoration("MidUpperInnerLeft", new Vector2(0.5f, 0.5f), new Vector2(-258f, 212f), new Vector2(128f, 128f), 14f, new Vector2(0.86f, -0.26f));
            CreateAmbientEncroachmentDecoration("LowerInnerLeft", new Vector2(0.5f, 0.5f), new Vector2(-338f, -170f), new Vector2(156f, 156f), -6f, new Vector2(1f, 0.12f));
            CreateAmbientEncroachmentDecoration("MidLowerInnerLeft", new Vector2(0.5f, 0.5f), new Vector2(-248f, -246f), new Vector2(132f, 132f), -16f, new Vector2(0.92f, 0.18f));
            CreateAmbientEncroachmentDecoration("UpperInnerRight", new Vector2(0.5f, 0.5f), new Vector2(382f, 134f), new Vector2(148f, 148f), -12f, new Vector2(-1f, -0.06f));
            CreateAmbientEncroachmentDecoration("MidUpperInnerRight", new Vector2(0.5f, 0.5f), new Vector2(262f, 206f), new Vector2(132f, 132f), -14f, new Vector2(-0.82f, -0.24f));
            CreateAmbientEncroachmentDecoration("LowerInnerRight", new Vector2(0.5f, 0.5f), new Vector2(344f, -196f), new Vector2(164f, 164f), 8f, new Vector2(-1f, 0.16f));
            CreateAmbientEncroachmentDecoration("MidLowerInnerRight", new Vector2(0.5f, 0.5f), new Vector2(252f, -248f), new Vector2(138f, 138f), 18f, new Vector2(-0.88f, 0.22f));
            CreateAmbientEncroachmentDecoration("TopApproachLeft", new Vector2(0.5f, 0.5f), new Vector2(-126f, 292f), new Vector2(126f, 126f), 6f, new Vector2(0.24f, -1f));
            CreateAmbientEncroachmentDecoration("TopApproachCenter", new Vector2(0.5f, 0.5f), new Vector2(0f, 316f), new Vector2(134f, 134f), -4f, new Vector2(0f, -1f));
            CreateAmbientEncroachmentDecoration("TopApproachRight", new Vector2(0.5f, 0.5f), new Vector2(132f, 286f), new Vector2(128f, 128f), -8f, new Vector2(-0.22f, -1f));
            CreateAmbientEncroachmentDecoration("BottomApproachLeft", new Vector2(0.5f, 0.5f), new Vector2(-118f, -304f), new Vector2(130f, 130f), -6f, new Vector2(0.18f, 1f));
            CreateAmbientEncroachmentDecoration("BottomApproachCenter", new Vector2(0.5f, 0.5f), new Vector2(0f, -324f), new Vector2(138f, 138f), 4f, new Vector2(0f, 1f));
            CreateAmbientEncroachmentDecoration("BottomApproachRight", new Vector2(0.5f, 0.5f), new Vector2(122f, -296f), new Vector2(132f, 132f), 8f, new Vector2(-0.16f, 1f));
            CreateAmbientEncroachmentDecoration("MidLeftUpperPocket", new Vector2(0.5f, 0.5f), new Vector2(-458f, 66f), new Vector2(122f, 122f), -10f, new Vector2(0.98f, -0.04f));
            CreateAmbientEncroachmentDecoration("MidLeftLowerPocket", new Vector2(0.5f, 0.5f), new Vector2(-432f, -72f), new Vector2(126f, 126f), 12f, new Vector2(0.96f, 0.08f));
            CreateAmbientEncroachmentDecoration("MidRightUpperPocket", new Vector2(0.5f, 0.5f), new Vector2(456f, 52f), new Vector2(124f, 124f), 8f, new Vector2(-0.98f, -0.04f));
            CreateAmbientEncroachmentDecoration("MidRightLowerPocket", new Vector2(0.5f, 0.5f), new Vector2(438f, -86f), new Vector2(128f, 128f), -12f, new Vector2(-0.96f, 0.08f));
            CreateAmbientEncroachmentDecoration("FarLeftUpperLane", new Vector2(0.5f, 0.5f), new Vector2(-628f, 182f), new Vector2(128f, 128f), -14f, new Vector2(0.98f, -0.08f));
            CreateAmbientEncroachmentDecoration("FarLeftCenterLane", new Vector2(0.5f, 0.5f), new Vector2(-602f, 8f), new Vector2(136f, 136f), 10f, new Vector2(1f, 0.02f));
            CreateAmbientEncroachmentDecoration("FarLeftLowerLane", new Vector2(0.5f, 0.5f), new Vector2(-618f, -188f), new Vector2(132f, 132f), -8f, new Vector2(0.98f, 0.1f));
            CreateAmbientEncroachmentDecoration("LeftInnerUpperLane", new Vector2(0.5f, 0.5f), new Vector2(-518f, 256f), new Vector2(118f, 118f), 12f, new Vector2(0.9f, -0.18f));
            CreateAmbientEncroachmentDecoration("LeftInnerLowerLane", new Vector2(0.5f, 0.5f), new Vector2(-506f, -254f), new Vector2(122f, 122f), -10f, new Vector2(0.88f, 0.16f));
            CreateAmbientEncroachmentDecoration("FarRightUpperLane", new Vector2(0.5f, 0.5f), new Vector2(626f, 176f), new Vector2(130f, 130f), 14f, new Vector2(-0.98f, -0.08f));
            CreateAmbientEncroachmentDecoration("FarRightCenterLane", new Vector2(0.5f, 0.5f), new Vector2(606f, 4f), new Vector2(138f, 138f), -10f, new Vector2(-1f, 0.02f));
            CreateAmbientEncroachmentDecoration("FarRightLowerLane", new Vector2(0.5f, 0.5f), new Vector2(620f, -194f), new Vector2(134f, 134f), 8f, new Vector2(-0.98f, 0.1f));
            CreateAmbientEncroachmentDecoration("RightInnerUpperLane", new Vector2(0.5f, 0.5f), new Vector2(522f, 248f), new Vector2(120f, 120f), -12f, new Vector2(-0.9f, -0.18f));
            CreateAmbientEncroachmentDecoration("RightInnerLowerLane", new Vector2(0.5f, 0.5f), new Vector2(512f, -262f), new Vector2(124f, 124f), 10f, new Vector2(-0.88f, 0.16f));
            CreateAmbientEncroachmentDecoration("CenterLeftUpperGap", new Vector2(0.5f, 0.5f), new Vector2(-212f, 92f), new Vector2(112f, 112f), 10f, new Vector2(0.62f, -0.18f));
            CreateAmbientEncroachmentDecoration("CenterLeftLowerGap", new Vector2(0.5f, 0.5f), new Vector2(-198f, -84f), new Vector2(118f, 118f), -8f, new Vector2(0.58f, 0.14f));
            CreateAmbientEncroachmentDecoration("CenterRightUpperGap", new Vector2(0.5f, 0.5f), new Vector2(218f, 84f), new Vector2(114f, 114f), -10f, new Vector2(-0.62f, -0.16f));
            CreateAmbientEncroachmentDecoration("CenterRightLowerGap", new Vector2(0.5f, 0.5f), new Vector2(202f, -92f), new Vector2(120f, 120f), 8f, new Vector2(-0.58f, 0.18f));
            CreateAmbientEncroachmentDecoration("LowerCenterLeftGap", new Vector2(0.5f, 0.5f), new Vector2(-82f, -226f), new Vector2(116f, 116f), -6f, new Vector2(0.26f, 0.42f));
            CreateAmbientEncroachmentDecoration("LowerCenterRightGap", new Vector2(0.5f, 0.5f), new Vector2(88f, -232f), new Vector2(118f, 118f), 6f, new Vector2(-0.24f, 0.4f));
            CreateAmbientEncroachmentDecoration("UpperCenterLeftGap", new Vector2(0.5f, 0.5f), new Vector2(-94f, 204f), new Vector2(108f, 108f), 8f, new Vector2(0.22f, -0.48f));
            CreateAmbientEncroachmentDecoration("UpperCenterRightGap", new Vector2(0.5f, 0.5f), new Vector2(96f, 198f), new Vector2(110f, 110f), -8f, new Vector2(-0.22f, -0.46f));
        }

        private void EnsureAmbientBackdropLayer()
        {
            if (ambientBackdropLayerRoot != null)
            {
                return;
            }

            GameObject layerObject = new GameObject("UI_ModeSelectAmbientBackdropLayer", typeof(RectTransform));
            layerObject.transform.SetParent(transform, false);
            layerObject.layer = gameObject.layer;

            ambientBackdropLayerRoot = layerObject.GetComponent<RectTransform>();
            ambientBackdropLayerRoot.anchorMin = Vector2.zero;
            ambientBackdropLayerRoot.anchorMax = Vector2.one;
            ambientBackdropLayerRoot.offsetMin = Vector2.zero;
            ambientBackdropLayerRoot.offsetMax = Vector2.zero;
            ambientBackdropLayerRoot.SetSiblingIndex(0);

            Color vignetteColor = new Color(
                UIStyleTokens.Surface.PanelPrimary.r,
                UIStyleTokens.Surface.PanelPrimary.g,
                UIStyleTokens.Surface.PanelPrimary.b,
                AmbientBackdropVignetteAlpha);
            CreateBackdropBand("TopVignette", new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(0f, 156f), vignetteColor, stretchHorizontally: true);
            CreateBackdropBand("BottomVignette", new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(0f, 168f), vignetteColor, stretchHorizontally: true);
            CreateBackdropBand("LeftVignette", new Vector2(0f, 0.5f), new Vector2(92f, 0f), new Vector2(184f, 0f), vignetteColor, stretchVertically: true);
            CreateBackdropBand("RightVignette", new Vector2(1f, 0.5f), new Vector2(-92f, 0f), new Vector2(184f, 0f), vignetteColor, stretchVertically: true);
        }

        private void CreateBackdropBand(
            string objectName,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color,
            bool stretchHorizontally = false,
            bool stretchVertically = false)
        {
            if (ambientBackdropLayerRoot == null)
            {
                return;
            }

            GameObject bandObject = new GameObject($"UI_ModeSelectAmbientBackdrop{objectName}", typeof(RectTransform), typeof(Image));
            bandObject.transform.SetParent(ambientBackdropLayerRoot, false);
            bandObject.layer = gameObject.layer;

            RectTransform rectTransform = bandObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = stretchHorizontally
                ? new Vector2(0f, anchor.y)
                : stretchVertically
                    ? new Vector2(anchor.x, 0f)
                    : anchor;
            rectTransform.anchorMax = stretchHorizontally
                ? new Vector2(1f, anchor.y)
                : stretchVertically
                    ? new Vector2(anchor.x, 1f)
                    : anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            Image image = bandObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private void CreateAmbientMoldDecoration(
            string objectName,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            float rotation,
            Vector2 driftDirection)
        {
            if (ambientMoldLayerRoot == null)
            {
                return;
            }

            GameObject imageObject = new GameObject($"UI_ModeSelectAmbientMold{objectName}", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(ambientMoldLayerRoot, false);
            imageObject.layer = gameObject.layer;

            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            Image image = imageObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, AmbientMoldBaseAlpha);

            Vector2 normalizedDrift = driftDirection.sqrMagnitude > 0.001f
                ? driftDirection.normalized
                : Vector2.right;

            ambientMoldDecorations.Add(new AmbientMoldDecoration
            {
                Image = image,
                AnchoredPosition = anchoredPosition,
                DriftDirection = normalizedDrift,
                BaseScale = UnityEngine.Random.Range(0.92f, 1.08f),
                ScalePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                PulseSpeed = UnityEngine.Random.Range(0.18f, 0.28f),
                AlphaPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                AlphaSpeed = UnityEngine.Random.Range(0.12f, 0.2f),
                Rotation = rotation,
                BaseAlpha = AmbientMoldBaseAlpha,
                AlphaRange = AmbientMoldAlphaRange,
                ScalePulse = AmbientMoldScalePulse,
                DriftDistance = AmbientMoldDriftDistance,
                RotationAmplitude = 3f,
                GrowthPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                GrowthSpeed = UnityEngine.Random.Range(0.06f, 0.11f),
                RevealDelay = 0f,
                RevealDuration = 0f
            });
        }

        private void CreateAmbientEncroachmentDecoration(
            string objectName,
            Vector2 anchor,
            Vector2 anchoredPosition,
            Vector2 size,
            float rotation,
            Vector2 driftDirection)
        {
            CreateAmbientMoldDecoration(objectName, anchor, anchoredPosition, size, rotation, driftDirection);
            if (ambientMoldDecorations.Count == 0)
            {
                return;
            }

            AmbientMoldDecoration decoration = ambientMoldDecorations[ambientMoldDecorations.Count - 1];
            decoration.BaseAlpha = AmbientEncroachmentBaseAlpha;
            decoration.AlphaRange = AmbientEncroachmentAlphaRange;
            decoration.ScalePulse = AmbientEncroachmentScalePulse;
            decoration.DriftDistance = AmbientEncroachmentDriftDistance;
            decoration.RotationAmplitude = 1.2f;
            decoration.IsEncroachment = true;
            decoration.GrowthPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            decoration.GrowthSpeed = UnityEngine.Random.Range(0.03f, 0.05f);
            decoration.PulseSpeed = UnityEngine.Random.Range(0.12f, 0.18f);
            decoration.AlphaSpeed = UnityEngine.Random.Range(0.07f, 0.11f);
            decoration.BaseScale = UnityEngine.Random.Range(0.82f, 0.94f);
            decoration.RevealDelay = UnityEngine.Random.Range(
                AmbientEncroachmentRevealLeadInSeconds,
                AmbientEncroachmentRevealLeadInSeconds + (AmbientEncroachmentRevealWindowSeconds * 0.55f));
            decoration.RevealDuration = UnityEngine.Random.Range(2.8f, 4.4f);
        }

        private void RefreshAmbientMoldDecorations()
        {
            if (ambientMoldDecorations.Count == 0)
            {
                return;
            }

            List<Sprite> candidateSprites = CollectAmbientMoldSprites();
            if (candidateSprites.Count == 0)
            {
                return;
            }

            for (int i = 0; i < ambientMoldDecorations.Count; i++)
            {
                AmbientMoldDecoration decoration = ambientMoldDecorations[i];
                if (decoration.Image == null)
                {
                    continue;
                }

                decoration.Image.sprite = candidateSprites[UnityEngine.Random.Range(0, candidateSprites.Count)];
                decoration.Image.enabled = true;
                decoration.FlipX = UnityEngine.Random.value > 0.5f;
                decoration.FlipY = UnityEngine.Random.value > 0.65f;
                if (decoration.IsEncroachment)
                {
                    decoration.BaseScale = UnityEngine.Random.Range(0.82f, 0.94f);
                    decoration.PulseSpeed = UnityEngine.Random.Range(0.34f, 0.54f);
                    decoration.AlphaSpeed = UnityEngine.Random.Range(0.18f, 0.28f);
                    decoration.GrowthSpeed = UnityEngine.Random.Range(0.08f, 0.13f);
                    decoration.RevealDelay = UnityEngine.Random.Range(
                        AmbientEncroachmentRevealLeadInSeconds,
                        AmbientEncroachmentRevealLeadInSeconds + (AmbientEncroachmentRevealWindowSeconds * 0.82f));
                    decoration.RevealDuration = UnityEngine.Random.Range(2.3f, 3.75f);
                }
                else
                {
                    decoration.BaseScale = UnityEngine.Random.Range(0.9f, 1.08f);
                    decoration.PulseSpeed = UnityEngine.Random.Range(0.28f, 0.46f);
                    decoration.AlphaSpeed = UnityEngine.Random.Range(0.16f, 0.26f);
                    decoration.GrowthSpeed = UnityEngine.Random.Range(0.06f, 0.11f);
                    decoration.RevealDelay = UnityEngine.Random.Range(0f, AmbientEncroachmentRevealWindowSeconds * 0.6f);
                    decoration.RevealDuration = UnityEngine.Random.Range(1.8f, 3.1f);
                }

                decoration.ScalePhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                decoration.AlphaPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                decoration.GrowthPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private List<Sprite> CollectAmbientMoldSprites()
        {
            var sprites = new List<Sprite>();
            GridVisualizer gridVisualizer = FindAnyObjectByType<GridVisualizer>();
            if (gridVisualizer == null)
            {
                return sprites;
            }

            var eligibleMoldIndices = new List<int>();
            for (int moldIndex = 0; moldIndex < AmbientMoldSpriteIndexScanLimit; moldIndex++)
            {
                Tile moldTile = gridVisualizer.GetMoldIconTileForMoldIndex(moldIndex);
                if (moldTile?.sprite != null)
                {
                    eligibleMoldIndices.Add(moldIndex);
                }
            }

            if (eligibleMoldIndices.Count == 0)
            {
                return sprites;
            }

            int selectedMoldIndex = eligibleMoldIndices[UnityEngine.Random.Range(0, eligibleMoldIndices.Count)];
            AddAmbientMoldSprite(sprites, gridVisualizer.GetMoldIconTileForMoldIndex(selectedMoldIndex)?.sprite);
            if (gridVisualizer.playerMoldTiles != null
                && selectedMoldIndex >= 0
                && selectedMoldIndex < gridVisualizer.playerMoldTiles.Length)
            {
                AddAmbientMoldSprite(sprites, gridVisualizer.playerMoldTiles[selectedMoldIndex]?.sprite);
            }

            if (gridVisualizer.playerMoldAliveVariantTiles != null
                && selectedMoldIndex >= 0
                && selectedMoldIndex < gridVisualizer.playerMoldAliveVariantTiles.Length)
            {
                GridVisualizer.MoldAliveVisualTiles variantTiles = gridVisualizer.playerMoldAliveVariantTiles[selectedMoldIndex];
                if (variantTiles != null)
                {
                    AddAmbientMoldSprite(sprites, variantTiles.isolatedTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.isolatedAlternateTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.isolatedSecondAlternateTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.clusteredTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.clusteredAlternateTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.clusteredSecondAlternateTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.clusteredThirdAlternateTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.denseTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.denseAlternateTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.denseSecondAlternateTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.denseThirdAlternateTile?.sprite);
                }
            }

            return sprites;
        }

        private static void AddAmbientMoldSprite(List<Sprite> sprites, Sprite sprite)
        {
            if (sprite != null && !sprites.Contains(sprite))
            {
                sprites.Add(sprite);
            }
        }

        private void AnimateAmbientMoldDecorations()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            float time = Time.unscaledTime;
            float elapsed = ambientSequenceStartTime < 0f ? 0f : Mathf.Max(0f, time - ambientSequenceStartTime);

            if (ambientMoldDecorations.Count == 0)
            {
                return;
            }

            for (int i = 0; i < ambientMoldDecorations.Count; i++)
            {
                AmbientMoldDecoration decoration = ambientMoldDecorations[i];
                if (decoration.Image == null)
                {
                    continue;
                }

                RectTransform rectTransform = decoration.Image.rectTransform;
                float alphaWave = 0.5f + (0.5f * Mathf.Sin(decoration.AlphaPhase + (time * decoration.AlphaSpeed)));
                float pulseWave = Mathf.Sin(decoration.ScalePhase + (time * decoration.PulseSpeed));
                float driftWave = Mathf.Sin(decoration.ScalePhase + (time * decoration.PulseSpeed * 0.75f));
                float growthWave = decoration.GrowthSpeed > 0f
                    ? 0.5f + (0.5f * Mathf.Sin(decoration.GrowthPhase + (time * decoration.GrowthSpeed)))
                    : 1f;
                float revealMultiplier = GetAmbientDecorationRevealMultiplier(decoration, elapsed);

                rectTransform.anchoredPosition = decoration.AnchoredPosition + (decoration.DriftDirection * (driftWave * decoration.DriftDistance * growthWave * Mathf.Lerp(0.7f, 1f, revealMultiplier)));
                float scale = decoration.BaseScale + (pulseWave * decoration.ScalePulse) + (decoration.IsEncroachment ? growthWave * 0.035f : 0f);
                rectTransform.localScale = new Vector3(
                    decoration.FlipX ? -scale : scale,
                    decoration.FlipY ? -scale : scale,
                    1f);
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, decoration.Rotation + (pulseWave * decoration.RotationAmplitude));

                Color color = decoration.Image.color;
                color.a = (decoration.BaseAlpha + (alphaWave * decoration.AlphaRange * growthWave)) * revealMultiplier;
                decoration.Image.color = color;
            }
        }

        private static float GetAmbientDecorationRevealMultiplier(AmbientMoldDecoration decoration, float elapsed)
        {
            if (decoration.RevealDuration <= 0f)
            {
                return 1f;
            }

            float revealDuration = Mathf.Max(0.01f, decoration.RevealDuration);
            float progress = Mathf.Clamp01((elapsed - decoration.RevealDelay) / revealDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            float hiddenFloor = decoration.IsEncroachment ? 0.02f : 0f;
            return Mathf.Lerp(hiddenFloor, 1f, easedProgress);
        }

        public void ShowMainMenuAfterSubpanel()
        {
            transform.SetAsLastSibling();
            ShowMainMenuContent();
            RefreshCampaignButtonState();
            RefreshSettingsState();
            RefreshResponsiveLayout();
        }

        public void HideForGameplay()
        {
            gameObject.SetActive(false);
        }

        private void ShowBackdropOnlyForSubpanel(Transform activeSubpanel)
        {
            HideCompatibilityNotice();

            if (contentRoot != null)
            {
                contentRoot.gameObject.SetActive(false);
            }

            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            transform.SetAsFirstSibling();
            if (activeSubpanel != null)
            {
                activeSubpanel.SetAsLastSibling();
            }
        }

        private void RefreshCampaignButtonState()
        {
            if (campaignButton == null)
            {
                return;
            }

            GameManager manager = FindAnyObjectByType<GameManager>();
            bool hasPendingReward = manager != null && manager.HasPendingCampaignMoldinessUnlockOnSavedRun();
            SetButtonLabel(campaignButton, hasPendingReward ? "Campaign (Pending Reward)" : "Campaign");
        }

        private void ApplyTooltips()
        {
            EnsureTooltip(hotseatButton, GetHotseatTooltipText);
            EnsureTooltip(campaignButton, GetCampaignTooltipText);
            EnsureTooltip(creditsButton, "Open the credits panel.");
            EnsureTooltip(settingsButton, "Open audio, tutorial, and campaign reset settings.");
            EnsureTooltip(quitButton, "Close Fungus Toast and return to desktop.");
            EnsureTooltip(creditsBackButton, "Return to the main menu.");
            EnsureTooltip(settingsBackButton, "Return to the main menu.");
            EnsureTooltip(settingsTutorialReplayButton, "Re-enable tutorial popups and onboarding hints you dismissed earlier.");
            EnsureTooltip(settingsResetButton, GetSettingsResetTooltipText);
            EnsureTooltip(settingsResetCancelButton, "Cancel the campaign reset prompt.");
        }

        private static void EnsureTooltip(Button button, string text)
        {
            EnsureTooltip(button, () => text);
        }

        private static void EnsureTooltip(Slider slider, string text)
        {
            if (slider == null || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var provider = slider.GetComponent<MoldButtonTooltipProvider>()
                ?? slider.gameObject.AddComponent<MoldButtonTooltipProvider>();
            provider.Initialize(() => text);
            var trigger = slider.GetComponent<TooltipTrigger>()
                ?? slider.gameObject.AddComponent<TooltipTrigger>();
            trigger.SetDynamicProvider(provider);
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

        private static string GetHotseatTooltipText()
        {
            return "Start a custom game against AI players or share this device with other human players.";
        }

        private string GetCampaignTooltipText()
        {
            GameManager manager = FindAnyObjectByType<GameManager>();
            bool hasPendingReward = manager != null && manager.HasPendingCampaignMoldinessUnlockOnSavedRun();

            return hasPendingReward
                ? "Open the campaign flow and claim the pending moldiness reward from your saved run before normal campaign choices."
                : "Open the campaign menu to resume an existing run or start a new one.";
        }

        private string GetSettingsResetTooltipText()
        {
            GameManager manager = FindAnyObjectByType<GameManager>();
            bool hasCampaignSave = manager != null && manager.HasCampaignSave();

            if (!hasCampaignSave)
            {
                return "Campaign reset is unavailable until a campaign save exists.";
            }

            return isConfirmingCampaignReset
                ? "Confirm wiping campaign rewards, moldiness progression, and pending moldiness reward choices."
                : "Begin resetting campaign rewards, moldiness progression, and pending moldiness reward choices.";
        }

        private static void ResizeRectTransform(RectTransform rectTransform, float width, float height)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.sizeDelta = new Vector2(width, height);
        }

        private static void SetButtonLabel(Button button, string text)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = text;
            }
        }

        private TextMeshProUGUI CreateLabel(string objectName, string textValue, float fontSize, float preferredHeight, Color color)
        {
            GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(contentRoot, false);
            labelObject.layer = gameObject.layer;

            RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(ExpandedDescriptionWidth, preferredHeight);

            LayoutElement layoutElement = labelObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = ExpandedDescriptionWidth;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = textValue;
            label.font = ResolveSharedFont();
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.color = color;

            return label;
        }

        private void EnsureCreditsPanel()
        {
            if (creditsPanel != null)
            {
                return;
            }

            creditsPanel = new GameObject("UI_ModeSelectCreditsPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            creditsPanel.transform.SetParent(transform, false);
            creditsPanel.layer = gameObject.layer;
            creditsPanel.SetActive(false);

            RectTransform panelRect = creditsPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup panelLayout = creditsPanel.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(72, 72, 72, 72);
            panelLayout.spacing = 24f;
            panelLayout.childAlignment = TextAnchor.MiddleCenter;
            panelLayout.childControlWidth = false;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandWidth = false;
            panelLayout.childForceExpandHeight = false;

            GameObject cardObject = new GameObject(
                "UI_ModeSelectCreditsCard",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            cardObject.transform.SetParent(creditsPanel.transform, false);
            cardObject.layer = gameObject.layer;

            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(CreditsCardWidth, 0f);

            LayoutElement cardLayoutElement = cardObject.GetComponent<LayoutElement>();
            cardLayoutElement.preferredWidth = CreditsCardWidth;
            cardLayoutElement.flexibleWidth = 0f;
            cardLayoutElement.flexibleHeight = 0f;

            Image cardBackground = cardObject.GetComponent<Image>();
            ApplyOverlayCardStyle(cardBackground);

            VerticalLayoutGroup cardLayout = cardObject.GetComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(44, 44, 40, 40);
            cardLayout.spacing = 14f;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = false;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;

            ContentSizeFitter cardFitter = cardObject.GetComponent<ContentSizeFitter>();
            cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateCreditsLogo(cardObject.transform);

            TextMeshProUGUI titleLabel = CreateCreditsLabel(
                cardObject.transform,
                "UI_ModeSelectCreditsTitle",
                CreditsHeadingText,
                34f,
                56f,
                UIStyleTokens.Text.Primary,
                FontStyles.Bold);
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMin = 26f;
            titleLabel.fontSizeMax = 34f;

            TextMeshProUGUI artworkHeading = CreateCreditsLabel(
                cardObject.transform,
                "UI_ModeSelectArtworkHeading",
                ArtworkHeadingText,
                24f,
                34f,
                UIStyleTokens.Accent.Spore,
                FontStyles.Bold);
            artworkHeading.fontStyle = FontStyles.Bold;
            artworkHeading.alignment = TextAlignmentOptions.Left;

            TextMeshProUGUI artworkName = CreateCreditsLabel(
                cardObject.transform,
                "UI_ModeSelectArtworkName",
                ArtworkCreditName,
                22f,
                30f,
                UIStyleTokens.Text.Primary,
                FontStyles.Bold);
            artworkName.alignment = TextAlignmentOptions.Left;

            TextMeshProUGUI artworkCopy = CreateCreditsLabel(
                cardObject.transform,
                "UI_ModeSelectArtworkCopy",
                ArtworkCreditCopy,
                22f,
                34f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);
            artworkCopy.enableAutoSizing = true;
            artworkCopy.fontSizeMin = 18f;
            artworkCopy.fontSizeMax = 22f;
            artworkCopy.alignment = TextAlignmentOptions.Left;

            TextMeshProUGUI musicHeading = CreateCreditsLabel(
                cardObject.transform,
                "UI_ModeSelectMusicHeading",
                MusicHeadingText,
                24f,
                34f,
                UIStyleTokens.Accent.Spore,
                FontStyles.Bold);
            musicHeading.fontStyle = FontStyles.Bold;
            musicHeading.alignment = TextAlignmentOptions.Left;

            TextMeshProUGUI musicName = CreateCreditsLabel(
                cardObject.transform,
                "UI_ModeSelectMusicName",
                MusicCreditName,
                22f,
                30f,
                UIStyleTokens.Text.Primary,
                FontStyles.Bold);
            musicName.alignment = TextAlignmentOptions.Left;

            TextMeshProUGUI musicCopy = CreateCreditsLabel(
                cardObject.transform,
                "UI_ModeSelectMusicCopy",
                MusicCreditCopy,
                22f,
                34f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);
            musicCopy.enableAutoSizing = true;
            musicCopy.fontSizeMin = 18f;
            musicCopy.fontSizeMax = 22f;
            musicCopy.alignment = TextAlignmentOptions.Left;

            creditsBackButton = CreateCreditsButton(cardObject.transform, "UI_ModeSelectCreditsBackButton", "Back to Menu", backButtonIcon);
        }

        private void CreateCreditsLogo(Transform parent)
        {
            if (parent == null || wideTitleLogoSprite == null)
            {
                return;
            }

            GameObject logoObject = new GameObject(
                "UI_ModeSelectCreditsLogo",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(Image));
            logoObject.transform.SetParent(parent, false);
            logoObject.layer = gameObject.layer;

            Image logo = logoObject.GetComponent<Image>();
            logo.sprite = wideTitleLogoSprite;
            logo.preserveAspect = true;
            logo.raycastTarget = false;

            LayoutElement layout = logoObject.GetComponent<LayoutElement>();
            layout.minWidth = 220f;
            layout.preferredWidth = 220f;
            layout.minHeight = 94f;
            layout.preferredHeight = 94f;
        }

        private void EnsureSettingsPanel()
        {
            if (settingsPanel != null)
            {
                return;
            }

            settingsPanel = new GameObject("UI_ModeSelectSettingsPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            settingsPanel.transform.SetParent(transform, false);
            settingsPanel.layer = gameObject.layer;
            settingsPanel.SetActive(false);

            RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup panelLayout = settingsPanel.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(48, 48, 48, 48);
            panelLayout.spacing = 24f;
            panelLayout.childAlignment = TextAnchor.MiddleCenter;
            panelLayout.childControlWidth = false;
            panelLayout.childControlHeight = false;
            panelLayout.childForceExpandWidth = false;
            panelLayout.childForceExpandHeight = false;

            GameObject cardObject = new GameObject(
                "UI_ModeSelectSettingsCard",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            cardObject.transform.SetParent(settingsPanel.transform, false);
            cardObject.layer = gameObject.layer;

            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(SettingsCardWidth, 0f);

            LayoutElement cardLayoutElement = cardObject.GetComponent<LayoutElement>();
            cardLayoutElement.preferredWidth = SettingsCardWidth;
            cardLayoutElement.flexibleWidth = 0f;
            cardLayoutElement.flexibleHeight = 0f;

            Image cardBackground = cardObject.GetComponent<Image>();
            ApplyOverlayCardStyle(cardBackground);

            VerticalLayoutGroup cardLayout = cardObject.GetComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(44, 44, 28, 28);
            cardLayout.spacing = 10f;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            cardLayout.childControlWidth = true;
            cardLayout.childControlHeight = false;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;

            ContentSizeFitter cardFitter = cardObject.GetComponent<ContentSizeFitter>();
            cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI titleLabel = CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsTitle",
                SettingsHeadingText,
                34f,
                44f,
                UIStyleTokens.Text.Primary,
                FontStyles.Bold);
            titleLabel.enableAutoSizing = true;
            titleLabel.fontSizeMin = 26f;
            titleLabel.fontSizeMax = 34f;

            TextMeshProUGUI audioHeading = CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsAudioHeading",
                SettingsAudioHeadingText,
                24f,
                28f,
                UIStyleTokens.Accent.Spore,
                FontStyles.Bold);

            settingsSoundEffectsSlider = CreateSettingsSliderRow(
                cardObject.transform,
                "UI_ModeSelectSettingsSfx",
                "Sound Effects",
                SoundEffectsSettings.Volume,
                OnSettingsSoundEffectsChanged,
                out settingsSoundEffectsValueLabel);
            settingsMusicSlider = CreateSettingsSliderRow(
                cardObject.transform,
                "UI_ModeSelectSettingsMusic",
                "Music",
                MusicSettings.Volume,
                OnSettingsMusicChanged,
                out settingsMusicValueLabel);

            TextMeshProUGUI helpHeading = CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsHelpHeading",
                SettingsHelpHeadingText,
                24f,
                28f,
                UIStyleTokens.Accent.Spore,
                FontStyles.Bold);

            CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsHelpSummary",
                SettingsTutorialSummaryText,
                18f,
                50f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);

            settingsTutorialReplayButton = CreateSettingsButton(cardObject.transform, "UI_ModeSelectSettingsTutorialReplayButton", "Replay Tutorial Tips");
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsTutorialReplayButton);
            settingsTutorialReplayButton.onClick.AddListener(OnSettingsTutorialReplayClicked);

            settingsTutorialStatusText = CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsTutorialStatus",
                string.Empty,
                18f,
                28f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);

            TextMeshProUGUI advancedHeading = CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsAdvancedHeading",
                SettingsAdvancedHeadingText,
                24f,
                28f,
                UIStyleTokens.Accent.Moss,
                FontStyles.Bold);

            Transform dangerZone = CreateSettingsDangerZone(cardObject.transform);
            CreateSettingsLabel(
                dangerZone,
                "UI_ModeSelectSettingsDangerHeading",
                "Danger Zone",
                22f,
                28f,
                UIStyleTokens.State.Danger,
                FontStyles.Bold);

            CreateSettingsLabel(
                dangerZone,
                "UI_ModeSelectSettingsAdvancedSummary",
                SettingsResetSummaryText,
                17f,
                62f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);

            settingsResetPromptLabel = CreateSettingsLabel(
                dangerZone,
                "UI_ModeSelectSettingsResetPrompt",
                SettingsResetPromptText,
                18f,
                44f,
                UIStyleTokens.State.Warning,
                FontStyles.Bold);

            settingsResetButton = CreateSettingsButton(dangerZone, "UI_ModeSelectSettingsResetButton", string.Empty);
            UIStyleTokens.Button.ApplyDangerMenuAction(settingsResetButton);
            settingsResetButton.onClick.AddListener(OnSettingsResetClicked);

            settingsResetCancelButton = CreateSettingsButton(dangerZone, "UI_ModeSelectSettingsResetCancelButton", "Cancel");
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsResetCancelButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            settingsResetCancelButton.onClick.AddListener(OnSettingsResetCancelClicked);

            settingsResetStatusText = CreateSettingsLabel(
                dangerZone,
                "UI_ModeSelectSettingsResetStatus",
                string.Empty,
                17f,
                42f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);

            settingsBackButton = CreateSettingsButton(cardObject.transform, "UI_ModeSelectSettingsBackButton", "Back to Menu", backButtonIcon);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsBackButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            settingsBackButton.onClick.AddListener(OnSettingsBackClicked);

            _ = audioHeading;
            _ = helpHeading;
            _ = advancedHeading;
            RefreshSettingsState();
        }

        private TextMeshProUGUI CreateCreditsLabel(
            Transform parent,
            string objectName,
            string textValue,
            float fontSize,
            float preferredHeight,
            Color color,
            FontStyles fontStyle)
        {
            GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            labelObject.layer = gameObject.layer;

            RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(CreditsTextWidth, preferredHeight);

            LayoutElement layoutElement = labelObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = CreditsTextWidth;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = textValue;
            label.font = ResolveSharedFont();
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.color = color;

            return label;
        }

        private TextMeshProUGUI CreateSettingsLabel(
            Transform parent,
            string objectName,
            string textValue,
            float fontSize,
            float preferredHeight,
            Color color,
            FontStyles fontStyle)
        {
            GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            labelObject.layer = gameObject.layer;

            RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(SettingsTextWidth, preferredHeight);

            LayoutElement layoutElement = labelObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = SettingsTextWidth;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = textValue;
            label.font = ResolveSharedFont();
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
            label.color = color;

            return label;
        }

        private Slider CreateSettingsSliderRow(
            Transform parent,
            string objectName,
            string labelText,
            float initialValue,
            UnityEngine.Events.UnityAction<float> onValueChanged,
            out TextMeshProUGUI valueLabel)
        {
            GameObject rowObject = new GameObject(
                $"{objectName}Row",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(Image),
                typeof(HorizontalLayoutGroup));
            rowObject.transform.SetParent(parent, false);
            rowObject.layer = gameObject.layer;

            Image rowBackground = rowObject.GetComponent<Image>();
            rowBackground.color = UIStyleTokens.WithAlpha(UIStyleTokens.Surface.PanelSecondary, 0.82f);
            rowBackground.raycastTarget = false;

            LayoutElement rowElement = rowObject.GetComponent<LayoutElement>();
            rowElement.minWidth = SettingsTextWidth;
            rowElement.preferredWidth = SettingsTextWidth;
            rowElement.minHeight = 52f;
            rowElement.preferredHeight = 52f;

            HorizontalLayoutGroup rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(16, 16, 8, 8);
            rowLayout.spacing = 14f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            TextMeshProUGUI nameLabel = CreateInlineSettingsLabel(rowObject.transform, $"{objectName}Label", labelText, 170f, TextAlignmentOptions.Left);
            UIStyleTokens.Startup.ApplySupportingCopy(nameLabel);
            nameLabel.fontStyle = FontStyles.Bold;

            GameObject sliderObject = new GameObject(
                $"{objectName}Slider",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(Slider));
            sliderObject.transform.SetParent(rowObject.transform, false);
            sliderObject.layer = gameObject.layer;
            LayoutElement sliderElement = sliderObject.GetComponent<LayoutElement>();
            sliderElement.minWidth = 380f;
            sliderElement.preferredWidth = 380f;
            sliderElement.minHeight = 28f;
            sliderElement.preferredHeight = 28f;

            GameObject trackObject = new GameObject("Track", typeof(RectTransform), typeof(Image));
            RectTransform trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.SetParent(sliderObject.transform, false);
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.sizeDelta = new Vector2(0f, 10f);
            Image trackImage = trackObject.GetComponent<Image>();
            trackImage.color = UIStyleTokens.Surface.PanelPrimary;
            trackImage.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.SetParent(sliderObject.transform, false);
            fillRect.anchorMin = new Vector2(0f, 0.5f);
            fillRect.anchorMax = new Vector2(1f, 0.5f);
            fillRect.pivot = new Vector2(0.5f, 0.5f);
            fillRect.sizeDelta = new Vector2(-4f, 6f);
            Image fillImage = fillObject.GetComponent<Image>();
            fillImage.color = UIStyleTokens.Accent.Lichen;
            fillImage.raycastTarget = false;

            GameObject handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.SetParent(sliderObject.transform, false);
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(18f, 26f);
            Image handleImage = handleObject.GetComponent<Image>();
            handleImage.color = UIStyleTokens.Accent.Spore;

            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(Mathf.Clamp01(initialValue));
            slider.onValueChanged.AddListener(onValueChanged);

            valueLabel = CreateInlineSettingsLabel(rowObject.transform, $"{objectName}Value", string.Empty, 92f, TextAlignmentOptions.Right);
            valueLabel.fontStyle = FontStyles.Bold;
            EnsureTooltip(slider, $"Adjust {labelText.ToLowerInvariant()} volume.");
            return slider;
        }

        private TextMeshProUGUI CreateInlineSettingsLabel(
            Transform parent,
            string objectName,
            string textValue,
            float width,
            TextAlignmentOptions alignment)
        {
            GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            labelObject.layer = gameObject.layer;
            LayoutElement layout = labelObject.GetComponent<LayoutElement>();
            layout.minWidth = width;
            layout.preferredWidth = width;
            layout.minHeight = 32f;
            layout.preferredHeight = 32f;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = ResolveSharedFont();
            label.fontSize = 18f;
            label.text = textValue;
            label.alignment = alignment;
            label.color = UIStyleTokens.Text.Primary;
            label.raycastTarget = false;
            return label;
        }

        private Transform CreateSettingsDangerZone(Transform parent)
        {
            GameObject dangerObject = new GameObject(
                "UI_ModeSelectSettingsDangerZone",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(Image),
                typeof(Outline),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            dangerObject.transform.SetParent(parent, false);
            dangerObject.layer = gameObject.layer;

            LayoutElement element = dangerObject.GetComponent<LayoutElement>();
            element.minWidth = SettingsTextWidth + 36f;
            element.preferredWidth = SettingsTextWidth + 36f;
            element.preferredHeight = -1f;

            Image background = dangerObject.GetComponent<Image>();
            background.color = UIStyleTokens.WithAlpha(UIStyleTokens.State.Danger, 0.1f);
            background.raycastTarget = false;

            Outline outline = dangerObject.GetComponent<Outline>();
            outline.effectColor = UIStyleTokens.WithAlpha(UIStyleTokens.State.Danger, 0.58f);
            outline.effectDistance = new Vector2(1f, -1f);

            VerticalLayoutGroup layout = dangerObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 14, 14);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = dangerObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return dangerObject.transform;
        }

        private Button CreateCreditsButton(Transform parent, string objectName, string labelText, Sprite icon = null)
        {
            Button button = CreateButtonCore(parent, objectName, labelText, 22f, FontStyles.Normal, icon);
            button.onClick.AddListener(OnCreditsBackClicked);
            UIStyleTokens.Button.ApplySecondaryMenuAction(button, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            ApplyModeSelectCompactButtonStyle(button);
            return button;
        }

        private TextMeshProUGUI CreateCompatibilityNoticeLabel(
            Transform parent,
            string objectName,
            float fontSize,
            FontStyles fontStyle,
            Color color,
            TextAlignmentOptions alignment,
            float preferredWidth)
        {
            GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            labelObject.layer = gameObject.layer;

            LayoutElement layoutElement = labelObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.minWidth = preferredWidth;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(preferredWidth, 0f);

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = ResolveSharedFont();
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;

            return label;
        }

        private Button CreateSettingsButton(Transform parent, string objectName, string labelText, Sprite icon = null)
        {
            return CreateButtonCore(parent, objectName, labelText, 24f, FontStyles.Bold, icon);
        }

        private static void ApplyOverlayCardStyle(Image cardBackground)
        {
            if (cardBackground == null)
            {
                return;
            }

            Color color = Color.Lerp(UIStyleTokens.Surface.PanelPrimary, UIStyleTokens.Surface.PanelSecondary, 0.18f);
            cardBackground.color = UIStyleTokens.WithAlpha(color, OverlayCardAlpha);
        }

        private Button CreateButtonCore(Transform parent, string objectName, string labelText, float fontSize, FontStyles fontStyle, Sprite icon = null)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.layer = gameObject.layer;

            Image background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelElevated;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;

            TextMeshProUGUI label;
            Image iconImage = null;
            if (icon != null)
            {
                GameObject contentObject = new GameObject("ButtonContent", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                contentObject.transform.SetParent(buttonObject.transform, false);
                contentObject.layer = gameObject.layer;

                RectTransform contentRect = contentObject.GetComponent<RectTransform>();
                contentRect.anchorMin = Vector2.zero;
                contentRect.anchorMax = Vector2.one;
                contentRect.pivot = new Vector2(0.5f, 0.5f);
                contentRect.offsetMin = new Vector2(CompactMenuButtonHorizontalPadding, 0f);
                contentRect.offsetMax = new Vector2(-CompactMenuButtonHorizontalPadding, 0f);
                contentRect.anchoredPosition = Vector2.zero;

                HorizontalLayoutGroup contentLayout = contentObject.GetComponent<HorizontalLayoutGroup>();
                contentLayout.spacing = CompactMenuButtonContentSpacing;
                contentLayout.padding = new RectOffset(0, 0, 0, 0);
                contentLayout.childAlignment = TextAnchor.MiddleCenter;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = false;
                contentLayout.childForceExpandHeight = false;

                GameObject iconObject = new GameObject("ButtonIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconObject.transform.SetParent(contentObject.transform, false);
                iconObject.layer = gameObject.layer;

                iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
                iconLayout.minWidth = CompactMenuButtonIconSize;
                iconLayout.preferredWidth = CompactMenuButtonIconSize;
                iconLayout.minHeight = CompactMenuButtonIconSize;
                iconLayout.preferredHeight = CompactMenuButtonIconSize;
                iconLayout.flexibleWidth = 0f;
                iconLayout.flexibleHeight = 0f;

                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(CompactMenuButtonIconSize, CompactMenuButtonIconSize);

                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                labelObject.transform.SetParent(contentObject.transform, false);
                labelObject.layer = gameObject.layer;

                label = labelObject.GetComponent<TextMeshProUGUI>();
                LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
                labelLayout.minHeight = 28f;
                labelLayout.preferredHeight = 28f;
                labelLayout.flexibleWidth = 1f;
                labelLayout.flexibleHeight = 0f;

                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.sizeDelta = new Vector2(0f, 28f);
            }
            else
            {
                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(buttonObject.transform, false);
                labelObject.layer = gameObject.layer;

                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                label = labelObject.GetComponent<TextMeshProUGUI>();
            }

            label.text = labelText;
            label.font = ResolveSharedFont();
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            TMPOverflowUtility.SetSafeEllipsis(label);
            label.margin = Vector4.zero;
            label.raycastTarget = false;

            if (iconImage != null)
            {
                iconImage.color = label.color;
            }

            return button;
        }

        private void ShowMainMenuContent()
        {
            if (contentRoot != null)
            {
                contentRoot.gameObject.SetActive(true);
            }

            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            RefreshResponsiveLayout();
        }

        private void ShowCreditsContent()
        {
            if (contentRoot != null)
            {
                contentRoot.gameObject.SetActive(false);
            }

            if (creditsPanel != null)
            {
                creditsPanel.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            RefreshResponsiveLayout();
        }

        private void ShowSettingsContent()
        {
            if (contentRoot != null)
            {
                contentRoot.gameObject.SetActive(false);
            }

            if (creditsPanel != null)
            {
                creditsPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }

            RefreshSettingsState();

            RefreshResponsiveLayout();
        }

        private void RefreshSettingsState()
        {
            RefreshSettingsAudioLabels();
            RefreshSettingsTutorialControls();
            RefreshSettingsResetControls();
        }

        private void RefreshSettingsAudioLabels()
        {
            float sfxVolume = SoundEffectsSettings.Volume;
            float musicVolume = MusicSettings.Volume;
            settingsSoundEffectsSlider?.SetValueWithoutNotify(sfxVolume);
            settingsMusicSlider?.SetValueWithoutNotify(musicVolume);

            if (settingsSoundEffectsValueLabel != null)
            {
                bool muted = !SoundEffectsSettings.Enabled || sfxVolume <= 0.001f;
                settingsSoundEffectsValueLabel.text = $"{Mathf.RoundToInt(sfxVolume * 100f)}% {(muted ? "🔇" : "🔊")}";
            }

            if (settingsMusicValueLabel != null)
            {
                bool muted = musicVolume <= 0.001f;
                settingsMusicValueLabel.text = $"{Mathf.RoundToInt(musicVolume * 100f)}% {(muted ? "🔇" : "🔊")}";
            }
        }

        private void RefreshSettingsTutorialControls()
        {
            SetButtonLabel(settingsTutorialReplayButton, "Replay Tutorial Tips");
        }

        private void RefreshSettingsResetControls()
        {
            GameManager manager = FindAnyObjectByType<GameManager>();
            bool hasCampaignSave = manager != null && manager.HasCampaignSave();

            if (settingsResetPromptLabel != null)
            {
                settingsResetPromptLabel.gameObject.SetActive(isConfirmingCampaignReset);
            }

            if (settingsResetCancelButton != null)
            {
                settingsResetCancelButton.gameObject.SetActive(isConfirmingCampaignReset);
            }

            if (settingsResetButton != null)
            {
                settingsResetButton.interactable = hasCampaignSave;
                SetButtonLabel(
                    settingsResetButton,
                    isConfirmingCampaignReset
                        ? "Yes, Reset Campaign Rewards"
                        : "Reset Campaign Rewards");
            }

            if (!hasCampaignSave)
            {
                isConfirmingCampaignReset = false;
                if (settingsResetPromptLabel != null)
                {
                    settingsResetPromptLabel.gameObject.SetActive(false);
                }

                if (settingsResetCancelButton != null)
                {
                    settingsResetCancelButton.gameObject.SetActive(false);
                }

                if (settingsResetStatusText != null && string.IsNullOrWhiteSpace(settingsResetStatusText.text))
                {
                    settingsResetStatusText.text = "No campaign save found. Start or resume a campaign before using this reset option.";
                    settingsResetStatusText.color = UIStyleTokens.Text.Secondary;
                }
            }
            else if (settingsResetStatusText != null && settingsResetStatusText.text == "No campaign save found. Start or resume a campaign before using this reset option.")
            {
                settingsResetStatusText.text = string.Empty;
            }
        }

        private void RefreshResponsiveLayout()
        {
            if (contentRoot == null)
            {
                return;
            }

            RectTransform parentRect = contentRoot.parent as RectTransform;
            if (parentRect == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

            float preferredHeight = LayoutUtility.GetPreferredHeight(contentRoot);
            float availableHeight = Mathf.Max(0f, parentRect.rect.height - (MinimumVerticalMargin * 2f));
            float scale = preferredHeight > 0f && availableHeight > 0f
                ? Mathf.Min(1f, availableHeight / preferredHeight)
                : 1f;

            scale *= ResponsiveScaleSafetyFactor;

            contentRoot.localScale = new Vector3(scale, scale, 1f);
            RefreshOverlayPanelScale(settingsPanel);
            RefreshOverlayPanelScale(creditsPanel);
        }

        private static void RefreshOverlayPanelScale(GameObject panel)
        {
            if (panel == null || !panel.activeInHierarchy || panel.transform.childCount == 0)
            {
                return;
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            RectTransform cardRect = panel.transform.GetChild(0) as RectTransform;
            if (panelRect == null || cardRect == null)
            {
                return;
            }

            cardRect.localScale = Vector3.one;
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
            float preferredHeight = LayoutUtility.GetPreferredHeight(cardRect);
            float availableHeight = Mathf.Max(0f, panelRect.rect.height - (MinimumVerticalMargin * 2f));
            float scale = preferredHeight > 0f && availableHeight > 0f
                ? Mathf.Min(1f, availableHeight / preferredHeight)
                : 1f;
            scale *= ResponsiveScaleSafetyFactor;
            cardRect.localScale = new Vector3(scale, scale, 1f);
        }

        private Button CreateButton(string objectName, string labelText, Sprite icon = null)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(contentRoot, false);
            buttonObject.layer = gameObject.layer;

            Image background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelElevated;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;

            TextMeshProUGUI label;
            Image iconImage = null;
            if (icon != null)
            {
                GameObject contentObject = new GameObject("ButtonContent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
                contentObject.transform.SetParent(buttonObject.transform, false);
                contentObject.layer = gameObject.layer;

                RectTransform contentRect = contentObject.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0.5f, 0.5f);
                contentRect.anchorMax = new Vector2(0.5f, 0.5f);
                contentRect.pivot = new Vector2(0.5f, 0.5f);
                contentRect.anchoredPosition = Vector2.zero;

                HorizontalLayoutGroup contentLayout = contentObject.GetComponent<HorizontalLayoutGroup>();
                contentLayout.spacing = CompactMenuButtonContentSpacing;
                contentLayout.padding = new RectOffset(0, 0, 0, 0);
                contentLayout.childAlignment = TextAnchor.MiddleCenter;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = false;
                contentLayout.childForceExpandHeight = false;

                ContentSizeFitter contentFitter = contentObject.GetComponent<ContentSizeFitter>();
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                GameObject iconObject = new GameObject("ButtonIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconObject.transform.SetParent(contentObject.transform, false);
                iconObject.layer = gameObject.layer;

                iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

                LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
                iconLayout.minWidth = CompactMenuButtonIconSize;
                iconLayout.preferredWidth = CompactMenuButtonIconSize;
                iconLayout.minHeight = CompactMenuButtonIconSize;
                iconLayout.preferredHeight = CompactMenuButtonIconSize;
                iconLayout.flexibleWidth = 0f;
                iconLayout.flexibleHeight = 0f;

                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(CompactMenuButtonIconSize, CompactMenuButtonIconSize);

                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
                labelObject.transform.SetParent(contentObject.transform, false);
                labelObject.layer = gameObject.layer;

                label = labelObject.GetComponent<TextMeshProUGUI>();
                LayoutElement labelLayout = labelObject.GetComponent<LayoutElement>();
                labelLayout.minHeight = 28f;
                labelLayout.preferredHeight = 28f;
                labelLayout.flexibleWidth = 0f;
                labelLayout.flexibleHeight = 0f;

                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.sizeDelta = new Vector2(0f, 28f);
            }
            else
            {
                GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(buttonObject.transform, false);
                labelObject.layer = gameObject.layer;

                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                label = labelObject.GetComponent<TextMeshProUGUI>();
            }

            label.text = labelText;
            label.font = ResolveSharedFont();
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            UIStyleTokens.Button.ApplySecondaryMenuAction(button, UIStyleTokens.Button.DesktopCompactMenuActionWidth);

            if (iconImage != null)
            {
                iconImage.color = label.color;
            }

            return button;
        }

        private TMP_FontAsset ResolveSharedFont()
        {
            if (titleText != null && titleText.font != null)
            {
                return titleText.font;
            }

            TextMeshProUGUI sample = GetComponentInChildren<TextMeshProUGUI>(true);
            if (sample != null && sample.font != null)
            {
                return sample.font;
            }

            return TMP_Settings.defaultFontAsset;
        }

        private T FindChildComponent<T>(string relativePath) where T : Component
        {
            Transform child = transform.Find(relativePath);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static bool ShouldShowQuitButton()
        {
            return Application.platform != RuntimePlatform.WebGLPlayer;
        }

        private static string BuildVersionLabel()
        {
            string version = ResolveVersion();
            return string.IsNullOrWhiteSpace(version)
                ? "Version not set"
                : $"Version {version}";
        }

        private static string ResolveVersion()
        {
            const string versionFileName = "version.txt";
            const int maxAncestorSearchDepth = 6;

            DirectoryInfo directory = new DirectoryInfo(Application.dataPath);
            int depth = 0;

            while (directory != null && depth <= maxAncestorSearchDepth)
            {
                string candidatePath = Path.Combine(directory.FullName, versionFileName);
                if (File.Exists(candidatePath))
                {
                    string rawContents = File.ReadAllText(candidatePath).Trim();
                    if (!string.IsNullOrWhiteSpace(rawContents))
                    {
                        return rawContents;
                    }
                }

                directory = directory.Parent;
                depth++;
            }

            return string.IsNullOrWhiteSpace(Application.version)
                ? string.Empty
                : Application.version.Trim();
        }
    }
}
