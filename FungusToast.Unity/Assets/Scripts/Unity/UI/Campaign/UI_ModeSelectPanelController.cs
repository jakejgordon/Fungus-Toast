using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using FungusToast.Unity;
using FungusToast.Unity.UI.GameStart; // for UI_StartGamePanel

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
        private const float MinimumVerticalMargin = 32f;
        private const float ResponsiveScaleSafetyFactor = 0.97f;
        private const float SettingsCardWidth = 860f;
        private const float SettingsTextWidth = 700f;
        private const int MainMenuHorizontalPadding = 40;
        private const int MainMenuVerticalPadding = 32;
        private const float MainMenuElementSpacing = 16f;
        private const string AlphaHeadingText = "Alpha test build";
        private const string AlphaSummaryCopy = "Alpha build for testing. Hotseat and campaign are both available; progression and balance are still in flux.";
        private const string CreditsHeadingText = "Special Credits";
        private const string ArtworkHeadingText = "Artwork";
        private const string ArtworkCreditCopy = "Special thanks to my teenage son Matthew for doing many of the graphics.";
        private const string MusicHeadingText = "Music";
        private const string MusicCreditCopy = "Thanks to Chris Howard for the music track of Fungus Toast. It sounds great!";
        private const string SettingsHeadingText = "Settings";
        private const string SettingsAudioHeadingText = "Audio";
        private const string SettingsAdvancedHeadingText = "Advanced Campaign";
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
        private Button settingsResetButton;
        private Button settingsResetCancelButton;
        private Button quitButton;
        private TextMeshProUGUI settingsResetStatusText;
        private TextMeshProUGUI settingsResetPromptLabel;
        private bool isConfirmingCampaignReset;

        private void Awake()
        {
            ResolveSceneReferences();
            ConfigureLayout();
            EnsureReleaseUi();
            ApplyStyle();

            if (hotseatButton != null) hotseatButton.onClick.AddListener(OnHotseatClicked);
            if (campaignButton != null) campaignButton.onClick.AddListener(OnCampaignClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
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
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.Canvas);
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

            UIStyleTokens.Button.ApplyPrimaryMenuAction(hotseatButton, ExpandedButtonWidth, preferredHeight: 90f, minHeight: 72f);
            UIStyleTokens.Button.ApplyPrimaryMenuAction(campaignButton, ExpandedButtonWidth, preferredHeight: 90f, minHeight: 72f);
            UIStyleTokens.Button.ApplySecondaryMenuAction(creditsButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            UIStyleTokens.Button.ApplySecondaryMenuAction(creditsBackButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsBackButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsSoundEffectsButton);
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsMusicButton);
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

            RefreshResponsiveLayout();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshResponsiveLayout();
        }

        private void OnHotseatClicked()
        {
            // Show the existing start game panel (player count selection etc.)
            if (startGamePanel != null)
                startGamePanel.gameObject.SetActive(true);
            // Hide self
            gameObject.SetActive(false);
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

            if (campaignPanel != null)
                campaignPanel.SetActive(true);
            gameObject.SetActive(false);
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
            RefreshSettingsAudioLabels();
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

            if (creditsButton != null)
            {
                creditsButton.transform.SetAsLastSibling();
            }

            if (settingsButton != null)
            {
                settingsButton.transform.SetAsLastSibling();
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

            creditsBackButton = CreateCreditsButton(cardObject.transform, "UI_ModeSelectCreditsBackButton", "Back to Menu");
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

            settingsBackButton = CreateSettingsButton(cardObject.transform, "UI_ModeSelectSettingsBackButton", "Back to Menu");
            UIStyleTokens.Button.ApplySecondaryMenuAction(settingsBackButton, UIStyleTokens.Button.DesktopCompactMenuActionWidth);
            settingsBackButton.onClick.AddListener(OnSettingsBackClicked);

            _ = audioHeading;
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

        private Button CreateCreditsButton(Transform parent, string objectName, string labelText)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.layer = gameObject.layer;

            Image background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelElevated;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(OnCreditsBackClicked);

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            labelObject.layer = gameObject.layer;

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.font = ResolveSharedFont();
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            UIStyleTokens.Button.ApplySecondaryMenuAction(button, UIStyleTokens.Button.DesktopCompactMenuActionWidth);

            return button;
        }

        private Button CreateSettingsButton(Transform parent, string objectName, string labelText)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.layer = gameObject.layer;

            Image background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelElevated;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            labelObject.layer = gameObject.layer;

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.font = ResolveSharedFont();
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

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
            RefreshSettingsResetControls();
        }

        private void RefreshSettingsAudioLabels()
        {
            SetButtonLabel(settingsSoundEffectsButton, $"SFX Volume: {Mathf.RoundToInt(SoundEffectsSettings.Volume * 100f)}%");
            SetButtonLabel(settingsMusicButton, $"Music Volume: {Mathf.RoundToInt(MusicSettings.Volume * 100f)}%");
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

        private Button CreateButton(string objectName, string labelText)
        {
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(contentRoot, false);
            buttonObject.layer = gameObject.layer;

            Image background = buttonObject.GetComponent<Image>();
            background.color = UIStyleTokens.Surface.PanelElevated;

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);
            labelObject.layer = gameObject.layer;

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.font = ResolveSharedFont();
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;

            UIStyleTokens.Button.ApplySecondaryMenuAction(button, UIStyleTokens.Button.DesktopCompactMenuActionWidth);

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
