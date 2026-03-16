using System;
using System.Collections.Generic;
using FungusToast.Core.Campaign;
using FungusToast.Unity.Campaign;
using FungusToast.Unity.UI.Testing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.Campaign
{
    /// <summary>
    /// Campaign selection panel with a deterministic layout stack:
    /// Testing card at top, action buttons beneath.
    /// </summary>
    public class UI_CampaignPanelController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button newButton;
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button backButton;

        [Header("Panels")]
        [SerializeField] private GameObject modeSelectPanel;

        [Header("Legacy Testing References")]
        [SerializeField] private GameObject testingOptionsSectionRoot;
        [SerializeField] private Toggle testingModeToggleTemplate;
        [SerializeField] private GameObject testingModePanelTemplate;

        private RectTransform contentRoot;
        private RectTransform mainStackRoot;
        private GameObject actionStack;
        private DevelopmentTestingCardController testingCardController;

        private void Awake()
        {
            contentRoot = transform.Find("UI_CampaignContent") as RectTransform;
            if (contentRoot == null)
            {
                Debug.LogWarning("UI_CampaignPanelController: UI_CampaignContent not found.");
                return;
            }

            BuildLayoutScaffold();
            BuildTestingCard();
            BuildActionStack();
            ApplyStyle();

            if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
            if (newButton != null) newButton.onClick.AddListener(OnNewClicked);
            if (deleteButton != null) deleteButton.onClick.AddListener(OnDeleteClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnEnable()
        {
            RefreshButtonStates();
            testingCardController?.RefreshDropdownOptions();
            testingCardController?.RefreshVisualState();
            ForceLayoutNow();
        }

        private void BuildLayoutScaffold()
        {
            var existing = contentRoot.Find("UI_CampaignMainStack") as RectTransform;
            if (existing != null)
            {
                mainStackRoot = existing;
            }
            else
            {
                var root = new GameObject("UI_CampaignMainStack", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
                root.transform.SetParent(contentRoot, false);
                mainStackRoot = root.GetComponent<RectTransform>();
            }

            mainStackRoot.anchorMin = new Vector2(0.5f, 1f);
            mainStackRoot.anchorMax = new Vector2(0.5f, 1f);
            mainStackRoot.pivot = new Vector2(0.5f, 1f);
            mainStackRoot.anchoredPosition = new Vector2(0f, -98f);
            mainStackRoot.sizeDelta = new Vector2(500f, 0f);

            var rootLayout = mainStackRoot.GetComponent<VerticalLayoutGroup>();
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;
            rootLayout.spacing = 14f;
            rootLayout.padding = new RectOffset(0, 0, 0, 0);

            var rootFitter = mainStackRoot.GetComponent<ContentSizeFitter>();
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var rootElement = mainStackRoot.GetComponent<LayoutElement>();
            rootElement.minWidth = 460f;
            rootElement.preferredWidth = 500f;

            if (testingOptionsSectionRoot != null)
            {
                testingOptionsSectionRoot.SetActive(false);
            }

            HideLegacyTestingBlocks();

            if (testingModeToggleTemplate != null)
            {
                testingModeToggleTemplate.gameObject.SetActive(false);
            }

            if (testingModePanelTemplate != null)
            {
                testingModePanelTemplate.SetActive(false);
            }
        }

        private void HideLegacyTestingBlocks()
        {
            if (contentRoot == null)
            {
                return;
            }

            var all = contentRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null)
                {
                    continue;
                }

                if (mainStackRoot != null && t.IsChildOf(mainStackRoot))
                {
                    continue;
                }

                var name = t.name;
                bool legacyTestingName =
                    name.IndexOf("TestingOptionsSection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("CampaignTestingOptionsSection", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("TestingModePanel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("TestingModeToggle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("FastForwardRounds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("SkipToEnd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("ForcedGameResult", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!legacyTestingName)
                {
                    continue;
                }

                // Keep modern runtime controls enabled.
                if (name.IndexOf("UI_Campaign", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                t.gameObject.SetActive(false);
            }
        }

        private void BuildTestingCard()
        {
            testingCardController = new DevelopmentTestingCardController(new DevelopmentTestingCardOptions
            {
                Parent = mainStackRoot,
                ButtonTemplate = backButton != null ? backButton : resumeButton,
                DropdownTemplate = FindDropdownTemplate(),
                SupportsForcedAdaptation = true,
                CardName = "UI_CampaignTestingCard",
                ControlPrefix = "UI_CampaignTesting",
                LogPrefix = "UI_CampaignPanelController",
                LayoutInvalidated = ForceLayoutNow
            });
            testingCardController.Build();
        }

        private void BuildActionStack()
        {
            var existing = mainStackRoot.Find("UI_CampaignActionStack");
            if (existing != null)
            {
                actionStack = existing.gameObject;
            }
            else
            {
                actionStack = new GameObject("UI_CampaignActionStack", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
                actionStack.transform.SetParent(mainStackRoot, false);
            }

            var actionLayout = actionStack.GetComponent<VerticalLayoutGroup>();
            actionLayout.childAlignment = TextAnchor.UpperCenter;
            actionLayout.childControlWidth = true;
            actionLayout.childControlHeight = true;
            actionLayout.childForceExpandWidth = false;
            actionLayout.childForceExpandHeight = false;
            actionLayout.spacing = 14f;
            actionLayout.padding = new RectOffset(0, 0, 0, 0);

            var actionFitter = actionStack.GetComponent<ContentSizeFitter>();
            actionFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            actionFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var actionElement = actionStack.GetComponent<LayoutElement>();
            actionElement.minWidth = 460f;
            actionElement.preferredWidth = 500f;

            ReparentActionButton(resumeButton, 0);
            ReparentActionButton(newButton, 1);
            ReparentActionButton(deleteButton, 2);
            ReparentActionButton(backButton, 3);

            if (deleteButton != null)
            {
                deleteButton.gameObject.SetActive(false);
            }
        }

        private void ReparentActionButton(Button button, int index)
        {
            if (button == null || actionStack == null)
            {
                return;
            }

            button.transform.SetParent(actionStack.transform, false);
            button.transform.SetSiblingIndex(index);
            EnsureButtonLayout(button, button == backButton ? 330f : 500f);
        }

        private static void EnsureButtonLayout(Button button, float width)
        {
            if (button == null)
            {
                return;
            }

            var element = button.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = button.gameObject.AddComponent<LayoutElement>();
            }

            element.minHeight = 52f;
            element.preferredHeight = 56f;
            element.minWidth = width;
            element.preferredWidth = width;
            element.flexibleWidth = 0f;
        }

        private TMP_Dropdown FindDropdownTemplate()
        {
            if (testingOptionsSectionRoot != null)
            {
                var fromLegacy = testingOptionsSectionRoot.GetComponentInChildren<TMP_Dropdown>(true);
                if (fromLegacy != null)
                {
                    return fromLegacy;
                }
            }

            return FindFirstObjectByType<TMP_Dropdown>(FindObjectsInactive.Include);
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(gameObject, UIStyleTokens.Surface.Canvas);
            UIStyleTokens.ApplyNonButtonTextPalette(gameObject);

            UIStyleTokens.Button.ApplyStyle(resumeButton);
            UIStyleTokens.Button.ApplyStyle(newButton);
            UIStyleTokens.Button.ApplyStyle(deleteButton);
            UIStyleTokens.Button.ApplyStyle(backButton);

            UIStyleTokens.Button.SetButtonLabelColor(resumeButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(newButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(deleteButton, UIStyleTokens.Button.TextDefault);
            UIStyleTokens.Button.SetButtonLabelColor(backButton, UIStyleTokens.Button.TextDefault);
        }

        private void RefreshButtonStates()
        {
            bool hasSave = GameManager.Instance != null && GameManager.Instance.HasCampaignSave();

            if (resumeButton != null)
            {
                resumeButton.gameObject.SetActive(hasSave);
                resumeButton.interactable = hasSave;
                UIStyleTokens.Button.SetButtonLabelColor(resumeButton, hasSave ? UIStyleTokens.Button.TextDefault : UIStyleTokens.Button.TextDisabled);
            }

            if (deleteButton != null)
            {
                // Start New Campaign already replaces existing progress.
                deleteButton.gameObject.SetActive(false);
                deleteButton.interactable = false;
            }
        }

        private void ApplyTestingModeToGameManager()
        {
            testingCardController?.ApplyToGameManager(GameManager.Instance);
        }

        private void OnResumeClicked()
        {
            ApplyTestingModeToGameManager();
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.StartCampaignResume();
            bool resumed = GameManager.Instance.CurrentGameMode == GameMode.Campaign
                           && (GameManager.Instance.Board != null || GameManager.Instance.IsCampaignAwaitingAdaptationSelection());

            if (resumed)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("UI_CampaignPanelController: Resume failed; keeping panel open.");
            }
        }

        private void OnNewClicked()
        {
            ApplyTestingModeToGameManager();
            if (GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.StartCampaignNew();
            if (GameManager.Instance.Board != null && GameManager.Instance.CurrentGameMode == GameMode.Campaign)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("UI_CampaignPanelController: New campaign failed; keeping panel open.");
            }
        }

        private void OnDeleteClicked()
        {
            CampaignSaveService.Delete();
            RefreshButtonStates();
            ForceLayoutNow();
        }

        private void OnBackClicked()
        {
            gameObject.SetActive(false);
            if (modeSelectPanel != null)
            {
                modeSelectPanel.SetActive(true);
            }
        }

        private void ForceLayoutNow()
        {
            Canvas.ForceUpdateCanvases();
            if (mainStackRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(mainStackRoot);
            }
            if (contentRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            }
        }

    }
}
