using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Unity.UI.Tooltips;

namespace FungusToast.Unity.UI
{
    public class UI_PauseMenuPanel : MonoBehaviour
    {
        private enum PendingAction
        {
            None,
            ReturnToMainMenu,
            ExitGame
        }

        private const float HudButtonWidth = 24f;
        private const float HudButtonHeight = 24f;
        private const float CardWidth = 420f;
        private const float CardHeight = 700f;
        private const float CardPadding = 24f;
        private const float ContentSpacing = 14f;

        private GameUIManager gameUI;
        private Action onOpenRequested;
        private Action onResumeRequested;
        private Action onReturnToMainMenuRequested;
        private Action onExitRequested;

        private Canvas rootCanvas;
        private TMP_FontAsset sharedFont;

        private GameObject hudButtonRoot;
        private Button hudMenuButton;

        private GameObject overlayRoot;
        private CanvasGroup overlayCanvasGroup;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI subtitleLabel;
        private GameObject primaryActionsRoot;
        private GameObject soundSettingsRoot;
        private GameObject confirmationRoot;
        private TextMeshProUGUI confirmationLabel;
        private Button soundEffectsToggleButton;
        private Button soundEffectsVolumeButton;
        private Button musicVolumeButton;

        private PendingAction pendingAction;
        private bool gameplayVisible;

        public bool IsOpen { get; private set; }
        public bool IsConfirming => pendingAction != PendingAction.None;

        public void SetDependencies(
            GameUIManager ui,
            Action openRequested,
            Action resumeRequested,
            Action returnToMainMenuRequested,
            Action exitRequested)
        {
            gameUI = ui;
            onOpenRequested = openRequested;
            onResumeRequested = resumeRequested;
            onReturnToMainMenuRequested = returnToMainMenuRequested;
            onExitRequested = exitRequested;

            EnsureBuilt();
        }

        public void SetGameplayVisibility(bool isVisible)
        {
            gameplayVisible = isVisible;
            EnsureBuilt();

            if (hudButtonRoot != null)
            {
                hudButtonRoot.SetActive(isVisible);
            }

            if (!isVisible)
            {
                Hide();
            }
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

            IsOpen = false;
        }

        public void CancelPendingAction()
        {
            pendingAction = PendingAction.None;
            ApplyPanelState();
        }

        private void EnsureBuilt()
        {
            if (overlayRoot != null && hudButtonRoot != null)
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

            if (hudButtonRoot == null)
            {
                BuildHudButton(rootCanvas.transform);
            }

            if (overlayRoot == null)
            {
                BuildOverlay(rootCanvas.transform);
            }

            if (hudButtonRoot != null)
            {
                hudButtonRoot.SetActive(gameplayVisible);
            }

            ApplyPanelState();
            Hide();
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

            Canvas anyCanvas = FindFirstObjectByType<Canvas>();
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
            rootRect.anchoredPosition = new Vector2(-8f, -6f);

            Image background = hudButtonRoot.AddComponent<Image>();
            background.color = UIStyleTokens.Button.BackgroundDefault;

            hudMenuButton = hudButtonRoot.AddComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(hudMenuButton);
            hudMenuButton.onClick.AddListener(() => onOpenRequested?.Invoke());

            TooltipTrigger tooltip = hudButtonRoot.AddComponent<TooltipTrigger>();
            tooltip.SetStaticText("Open the pause menu.");

            TextMeshProUGUI label = CreateLabel(hudButtonRoot.transform, "≡", 16f, FontStyles.Bold);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UIStyleTokens.Button.TextDefault;
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
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(CardWidth, CardHeight);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardImage = card.AddComponent<Image>();
            cardImage.color = UIStyleTokens.Surface.PanelPrimary;

            GameObject contentRoot = CreateUiObject("PauseMenuContent", card.transform);
            RectTransform contentRect = contentRoot.GetComponent<RectTransform>();
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

            subtitleLabel = CreateLabel(contentRoot.transform, "Gameplay is paused.", 22f, FontStyles.Normal);
            subtitleLabel.alignment = TextAlignmentOptions.Center;
            subtitleLabel.color = UIStyleTokens.Text.Secondary;

            primaryActionsRoot = CreateVerticalSection(contentRoot.transform, "PrimaryActions", 10f);

            Button resumeButton = CreateActionButton(primaryActionsRoot.transform, "Resume");
            resumeButton.onClick.AddListener(() => onResumeRequested?.Invoke());

            Button mainMenuButton = CreateActionButton(primaryActionsRoot.transform, "Main Menu");
            mainMenuButton.onClick.AddListener(RequestMainMenuConfirmation);

            Button exitButton = CreateActionButton(primaryActionsRoot.transform, "Exit Game");
            exitButton.onClick.AddListener(RequestExitConfirmation);

            soundSettingsRoot = CreateVerticalSection(contentRoot.transform, "SoundSettings", 10f);

            TextMeshProUGUI soundLabel = CreateLabel(soundSettingsRoot.transform, "Audio", 22f, FontStyles.Bold);
            soundLabel.alignment = TextAlignmentOptions.Center;
            soundLabel.color = UIStyleTokens.Text.Primary;

            soundEffectsToggleButton = CreateActionButton(soundSettingsRoot.transform, string.Empty);
            soundEffectsToggleButton.onClick.AddListener(OnSoundEffectsToggleClicked);

            soundEffectsVolumeButton = CreateActionButton(soundSettingsRoot.transform, string.Empty);
            soundEffectsVolumeButton.onClick.AddListener(OnSoundEffectsVolumeClicked);

            musicVolumeButton = CreateActionButton(soundSettingsRoot.transform, string.Empty);
            musicVolumeButton.onClick.AddListener(OnMusicVolumeClicked);

            confirmationRoot = CreateVerticalSection(contentRoot.transform, "Confirmation", 12f);

            confirmationLabel = CreateLabel(confirmationRoot.transform, string.Empty, 21f, FontStyles.Normal);
            confirmationLabel.alignment = TextAlignmentOptions.Center;
            confirmationLabel.color = UIStyleTokens.Text.Secondary;
            confirmationLabel.textWrappingMode = TextWrappingModes.Normal;

            GameObject confirmationButtons = CreateUiObject("ConfirmationButtons", confirmationRoot.transform);
            HorizontalLayoutGroup buttonRow = confirmationButtons.AddComponent<HorizontalLayoutGroup>();
            buttonRow.spacing = 12f;
            buttonRow.childAlignment = TextAnchor.MiddleCenter;
            buttonRow.childControlWidth = true;
            buttonRow.childControlHeight = false;
            buttonRow.childForceExpandWidth = true;
            buttonRow.childForceExpandHeight = false;

            Button confirmButton = CreateActionButton(confirmationButtons.transform, "Confirm");
            UIStyleTokens.Button.ApplyStyle(confirmButton, useSelectedAsNormal: true);
            confirmButton.onClick.AddListener(ConfirmPendingAction);

            Button cancelButton = CreateActionButton(confirmationButtons.transform, "Cancel");
            cancelButton.onClick.AddListener(CancelPendingAction);

            RefreshSoundSettingsButtons();
        }

        private void ApplyPanelState()
        {
            if (primaryActionsRoot == null || confirmationRoot == null || confirmationLabel == null || subtitleLabel == null)
            {
                return;
            }

            bool showConfirmation = pendingAction != PendingAction.None;
            primaryActionsRoot.SetActive(!showConfirmation);
            confirmationRoot.SetActive(showConfirmation);

            if (!showConfirmation)
            {
                subtitleLabel.text = "Gameplay is paused.";
                return;
            }

            switch (pendingAction)
            {
                case PendingAction.ReturnToMainMenu:
                    subtitleLabel.text = "Leave the current run?";
                    confirmationLabel.text = "Return to the main menu and abandon the current game?";
                    break;
                case PendingAction.ExitGame:
                    subtitleLabel.text = "Close Fungus Toast?";
                    confirmationLabel.text = "Exit the game now? Any unsaved progress in this run will be lost.";
                    break;
            }
        }

        private void RequestMainMenuConfirmation()
        {
            pendingAction = PendingAction.ReturnToMainMenu;
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
            RefreshSoundSettingsButtons();
        }

        private void RefreshSoundSettingsButtons()
        {
            SetButtonLabel(soundEffectsToggleButton, $"Sound Effects: {(SoundEffectsSettings.Enabled ? "On" : "Off")}");
            SetButtonLabel(soundEffectsVolumeButton, $"SFX Volume: {Mathf.RoundToInt(SoundEffectsSettings.Volume * 100f)}%");
            SetButtonLabel(musicVolumeButton, $"Music Volume: {Mathf.RoundToInt(MusicSettings.Volume * 100f)}%");
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

        private Button CreateActionButton(Transform parent, string labelText)
        {
            GameObject buttonObject = CreateUiObject(labelText.Replace(" ", string.Empty) + "Button", parent);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0f, 56f);

            LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 56f;
            layout.minHeight = 48f;
            layout.flexibleWidth = 1f;

            Image image = buttonObject.AddComponent<Image>();
            image.color = UIStyleTokens.Button.BackgroundDefault;

            Button button = buttonObject.AddComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(button);

            TextMeshProUGUI label = CreateLabel(buttonObject.transform, labelText, 24f, FontStyles.Bold);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UIStyleTokens.Button.TextDefault;
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

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;
            return go;
        }
    }
}