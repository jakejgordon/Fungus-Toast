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
        private const float ExpandedContentWidth = 760f;
        private const float ExpandedButtonWidth = 620f;
        private const float ExpandedDescriptionWidth = 620f;
        private const float CreditsCardWidth = 860f;
        private const float CreditsTextWidth = 700f;
        private const float WideLogoWidth = 520f;
        private const float WideLogoHeight = 223f;
        private const float TitleHeight = 34f;
        private const float SummaryHeight = 68f;
        private const float FooterHeight = 24f;
        private const float CompactMenuButtonIconSize = 22f;
        private const float CompactMenuButtonContentSpacing = 10f;
        private const float CompactMenuButtonHorizontalPadding = 14f;
        private const float MinimumVerticalMargin = 32f;
        private const float ResponsiveScaleSafetyFactor = 0.97f;
        private const float SettingsCardWidth = 860f;
        private const float SettingsTextWidth = 700f;
        private const int AmbientMoldSpriteIndexScanLimit = 12;
        private const float AmbientMoldBaseAlpha = 0.13f;
        private const float AmbientMoldAlphaRange = 0.055f;
        private const float AmbientMoldScalePulse = 0.072f;
        private const float AmbientMoldDriftDistance = 12f;
        private const float AmbientEncroachmentBaseAlpha = 0.024f;
        private const float AmbientEncroachmentAlphaRange = 0.038f;
        private const float AmbientEncroachmentScalePulse = 0.045f;
        private const float AmbientEncroachmentDriftDistance = 6f;
        private const float AmbientEncroachmentRevealLeadInSeconds = 1f;
        private const float AmbientEncroachmentRevealWindowSeconds = 10f;
        private const float AmbientBackdropVignetteAlpha = 0.018f;
        private const int MainMenuHorizontalPadding = 40;
        private const int MainMenuVerticalPadding = 32;
        private const float MainMenuElementSpacing = 16f;
        private const string AlphaHeadingText = "Alpha Test Build";
        private const string AlphaSummaryCopy = "Alpha build for testing. Hotseat and campaign are both available; progression and balance are still in flux.";
        private const string CreditsHeadingText = "Special Credits";
        private const string ArtworkHeadingText = "Artwork";
        private const string ArtworkCreditCopy = "Special thanks to my teenage son Matthew for doing many of the graphics.";
        private const string MusicHeadingText = "Music";
        private const string MusicCreditCopy = "Thanks to Chris Howard for the music track of Fungus Toast. It sounds great!";
        private const string SettingsHeadingText = "Settings";
        private const string SettingsAudioHeadingText = "Audio";
        private const string SettingsHelpHeadingText = "Help & Tutorials";
        private const string SettingsAdvancedHeadingText = "Advanced Campaign";
        private const string SettingsTutorialSummaryText = "Re-enable tutorial popups and guidance hints you previously dismissed. This does not reset campaign progress.";
        private const string SettingsResetPromptText = "Are you sure you want to reset all of your campaign rewards?";

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
        private GameObject creditsPanel;
        private GameObject settingsPanel;
        private Button creditsButton;
        private Button settingsButton;
        private Button creditsBackButton;
        private Button settingsBackButton;
        private Button settingsSoundEffectsButton;
        private Button settingsMusicButton;
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
            UIStyleTokens.ApplyPanelSurface(gameObject, Color.Lerp(UIStyleTokens.Surface.Canvas, UIStyleTokens.Surface.PanelPrimary, 0.18f));
            UIStyleTokens.ApplyNonButtonTextPalette(gameObject);

            if (titleText != null)
            {
                titleText.color = UIStyleTokens.Text.Primary;
            }

            if (alphaSummaryText != null)
            {
                alphaSummaryText.color = UIStyleTokens.Text.Secondary;
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
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsSoundEffectsButton);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsMusicButton);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsTutorialReplayButton);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsResetButton);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsResetCancelButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            UIStyleTokens.Button.ApplySecondaryMenuAction(quitButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
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

        private void OnSettingsSoundEffectsClicked()
        {
            SoundEffectsSettings.CycleVolumeForward();
            RefreshSettingsAudioLabels();
        }

        private void OnSettingsMusicClicked()
        {
            MusicSettings.CycleVolumeForward();
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
                titleText.fontSizeMin = 22f;
                titleText.fontSizeMax = 32f;
                titleText.fontSize = 28f;
                titleText.alignment = TextAlignmentOptions.Center;
                ResizeRectTransform(titleText.rectTransform, ExpandedDescriptionWidth, TitleHeight);
            }
        }

        private void EnsureReleaseUi()
        {
            if (contentRoot == null)
            {
                return;
            }

            if (alphaSummaryText == null)
            {
                alphaSummaryText = CreateLabel(
                    "UI_ModeSelectAlphaSummary",
                    AlphaSummaryCopy,
                    22f,
                    SummaryHeight,
                    UIStyleTokens.Text.Secondary);
                alphaSummaryText.enableAutoSizing = true;
                alphaSummaryText.fontSizeMin = 18f;
                alphaSummaryText.fontSizeMax = 22f;
                alphaSummaryText.transform.SetSiblingIndex(Mathf.Min(2, contentRoot.childCount - 1));
            }
            else
            {
                alphaSummaryText.text = AlphaSummaryCopy;
            }

            if (creditsButton == null)
            {
                creditsButton = CreateButton("UI_ModeSelectCreditsButton", "Special Credits");
                creditsButton.onClick.AddListener(OnCreditsClicked);
            }

            if (settingsButton == null)
            {
                settingsButton = CreateButton("UI_ModeSelectSettingsButton", "Settings", settingsButtonIcon);
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

            if (versionText != null)
            {
                versionText.transform.SetAsLastSibling();
            }

            EnsureCreditsPanel();
            EnsureSettingsPanel();
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
            CreateAmbientEncroachmentDecoration("UpperInnerLeft", new Vector2(0.5f, 0.5f), new Vector2(-382f, 152f), new Vector2(144f, 144f), 10f, new Vector2(1f, -0.08f));
            CreateAmbientEncroachmentDecoration("LowerInnerLeft", new Vector2(0.5f, 0.5f), new Vector2(-338f, -170f), new Vector2(156f, 156f), -6f, new Vector2(1f, 0.12f));
            CreateAmbientEncroachmentDecoration("UpperInnerRight", new Vector2(0.5f, 0.5f), new Vector2(382f, 134f), new Vector2(148f, 148f), -12f, new Vector2(-1f, -0.06f));
            CreateAmbientEncroachmentDecoration("LowerInnerRight", new Vector2(0.5f, 0.5f), new Vector2(344f, -196f), new Vector2(164f, 164f), 8f, new Vector2(-1f, 0.16f));
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
                GrowthSpeed = 0f,
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
                    decoration.PulseSpeed = UnityEngine.Random.Range(0.12f, 0.18f);
                    decoration.AlphaSpeed = UnityEngine.Random.Range(0.07f, 0.11f);
                    decoration.GrowthSpeed = UnityEngine.Random.Range(0.03f, 0.05f);
                    decoration.RevealDelay = UnityEngine.Random.Range(
                        AmbientEncroachmentRevealLeadInSeconds,
                        AmbientEncroachmentRevealLeadInSeconds + (AmbientEncroachmentRevealWindowSeconds * 0.55f));
                    decoration.RevealDuration = UnityEngine.Random.Range(2.8f, 4.4f);
                }
                else
                {
                    decoration.BaseScale = UnityEngine.Random.Range(0.9f, 1.08f);
                    decoration.PulseSpeed = UnityEngine.Random.Range(0.18f, 0.28f);
                    decoration.AlphaSpeed = UnityEngine.Random.Range(0.12f, 0.18f);
                    decoration.GrowthSpeed = 0f;
                    decoration.RevealDelay = 0f;
                    decoration.RevealDuration = 0f;
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
                    AddAmbientMoldSprite(sprites, variantTiles.clusteredTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.clusteredAlternateTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.denseTile?.sprite);
                    AddAmbientMoldSprite(sprites, variantTiles.denseAlternateTile?.sprite);
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
            if (!decoration.IsEncroachment)
            {
                return 1f;
            }

            float revealDuration = Mathf.Max(0.01f, decoration.RevealDuration);
            float progress = Mathf.Clamp01((elapsed - decoration.RevealDelay) / revealDuration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            return Mathf.Lerp(0.04f, 1f, easedProgress);
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
            EnsureTooltip(settingsSoundEffectsButton, "Cycle the sound effects volume to the next preset.");
            EnsureTooltip(settingsMusicButton, "Cycle the music volume to the next preset.");
            EnsureTooltip(settingsTutorialReplayButton, "Re-enable tutorial popups and onboarding hints you dismissed earlier.");
            EnsureTooltip(settingsResetButton, GetSettingsResetTooltipText);
            EnsureTooltip(settingsResetCancelButton, "Cancel the campaign reset prompt.");
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

        private static string GetHotseatTooltipText()
        {
            return "Start a local hotseat game on this machine. You will choose player count, human seats, and setup options next.";
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
            cardBackground.color = UIStyleTokens.Surface.PanelPrimary;

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

            TextMeshProUGUI artworkCopy = CreateCreditsLabel(
                cardObject.transform,
                "UI_ModeSelectArtworkCopy",
                ArtworkCreditCopy,
                22f,
                72f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);
            artworkCopy.enableAutoSizing = true;
            artworkCopy.fontSizeMin = 18f;
            artworkCopy.fontSizeMax = 22f;

            TextMeshProUGUI musicHeading = CreateCreditsLabel(
                cardObject.transform,
                "UI_ModeSelectMusicHeading",
                MusicHeadingText,
                24f,
                34f,
                UIStyleTokens.Accent.Spore,
                FontStyles.Bold);
            musicHeading.fontStyle = FontStyles.Bold;

            TextMeshProUGUI musicCopy = CreateCreditsLabel(
                cardObject.transform,
                "UI_ModeSelectMusicCopy",
                MusicCreditCopy,
                22f,
                72f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);
            musicCopy.enableAutoSizing = true;
            musicCopy.fontSizeMin = 18f;
            musicCopy.fontSizeMax = 22f;

            creditsBackButton = CreateCreditsButton(cardObject.transform, "UI_ModeSelectCreditsBackButton", "Back to Menu", backButtonIcon);
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
            panelLayout.padding = new RectOffset(72, 72, 72, 72);
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
            cardBackground.color = UIStyleTokens.Surface.PanelPrimary;

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

            TextMeshProUGUI titleLabel = CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsTitle",
                SettingsHeadingText,
                34f,
                56f,
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
                34f,
                UIStyleTokens.Accent.Spore,
                FontStyles.Bold);

            settingsSoundEffectsButton = CreateSettingsButton(cardObject.transform, "UI_ModeSelectSettingsSfxButton", string.Empty);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsSoundEffectsButton);
            settingsSoundEffectsButton.onClick.AddListener(OnSettingsSoundEffectsClicked);

            settingsMusicButton = CreateSettingsButton(cardObject.transform, "UI_ModeSelectSettingsMusicButton", string.Empty);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsMusicButton);
            settingsMusicButton.onClick.AddListener(OnSettingsMusicClicked);

            TextMeshProUGUI helpHeading = CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsHelpHeading",
                SettingsHelpHeadingText,
                24f,
                34f,
                UIStyleTokens.Accent.Spore,
                FontStyles.Bold);

            CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsHelpSummary",
                SettingsTutorialSummaryText,
                20f,
                78f,
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
                36f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);

            TextMeshProUGUI advancedHeading = CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsAdvancedHeading",
                SettingsAdvancedHeadingText,
                24f,
                34f,
                UIStyleTokens.Accent.Moss,
                FontStyles.Bold);

            CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsAdvancedSummary",
                "Resetting campaign rewards clears your persistent moldiness progress, unlocked moldiness rewards, and pending moldiness reward choices.",
                20f,
                78f,
                UIStyleTokens.Text.Secondary,
                FontStyles.Normal);

            settingsResetPromptLabel = CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsResetPrompt",
                SettingsResetPromptText,
                20f,
                64f,
                UIStyleTokens.State.Warning,
                FontStyles.Bold);

            settingsResetButton = CreateSettingsButton(cardObject.transform, "UI_ModeSelectSettingsResetButton", string.Empty);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsResetButton);
            settingsResetButton.onClick.AddListener(OnSettingsResetClicked);

            settingsResetCancelButton = CreateSettingsButton(cardObject.transform, "UI_ModeSelectSettingsResetCancelButton", "Cancel");
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsResetCancelButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            settingsResetCancelButton.onClick.AddListener(OnSettingsResetCancelClicked);

            settingsResetStatusText = CreateSettingsLabel(
                cardObject.transform,
                "UI_ModeSelectSettingsResetStatus",
                string.Empty,
                18f,
                52f,
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

        private Button CreateCreditsButton(Transform parent, string objectName, string labelText, Sprite icon = null)
        {
            Button button = CreateButtonCore(parent, objectName, labelText, 22f, FontStyles.Normal, icon);
            button.onClick.AddListener(OnCreditsBackClicked);
            UIStyleTokens.Button.ApplySecondaryMenuAction(button, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
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
            SetButtonLabel(settingsSoundEffectsButton, $"SFX Volume: {Mathf.RoundToInt(SoundEffectsSettings.Volume * 100f)}%");
            SetButtonLabel(settingsMusicButton, $"Music Volume: {Mathf.RoundToInt(MusicSettings.Volume * 100f)}%");
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
