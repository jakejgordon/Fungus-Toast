using Assets.Scripts.Unity.UI.MycovariantDraft;
using FungusToast.Core.AI;
using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Effects;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI.Campaign;
using FungusToast.Unity.UI.Onboarding;
using FungusToast.Unity.UI.Tooltips;
using FungusToast.Unity.UI.Tooltips.TooltipProviders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.MycovariantDraft
{
    public enum DraftUIState
    {
        Idle,
        HumanTurn,
        AITurn,
        AnimatingPick,
        Complete
    }

    internal sealed class ModalScrollInterceptor : MonoBehaviour, IScrollHandler
    {
        public ScrollRect TargetScrollRect { get; set; }

        public void OnScroll(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            if (TargetScrollRect != null && TargetScrollRect.isActiveAndEnabled)
            {
                TargetScrollRect.OnScroll(eventData);
            }

            eventData.Use();
        }
    }

    public class MycovariantDraftController : MonoBehaviour
    {
        private const string DefaultDraftTitle = "Choose a Mycovariant";
        private const string DefaultDraftBlurb = "Select a unique mycovariant mutation.";
        private const string DefaultHumanTurnBannerText = "Your Turn to Draft a Mycovariant!";
        private const string DefaultAiTurnBannerPrefix = "AI Drafting";
        private const string DraftHistoryOverlayTitle = "Mycovariant Draft Log";

        private const float FeedEntryFontSize = 18f;
        private const float FeedIconSize = 36f;
        private const float FeedRowSpacing = 8f;
        private const int FeedRowPadding = 8;
        private const float FeedIconSpacerWidth = 8f;
        private const float DraftHistoryOverlayWidth = 900f;
        private const float DraftHistoryOverlayHeight = 680f;
        private const float DraftHistoryEntryDetailFontSize = 18f;
        private const float CampaignAdaptationUtilityPanelHeight = 92f;
        private const float CampaignAdaptationUtilityPanelWidth = 840f;
        private const float CampaignAdaptationUtilityStatusWidth = 808f;
        private const float CampaignDraftUtilityButtonWidth = 330f;
        private const float CampaignDraftUtilityButtonHeight = 36f;
        private const float CampaignDraftUtilityPanelAlpha = 0.96f;
        private const float CampaignAdaptationSummaryPanelWidth = 840f;
        private const float CampaignAdaptationSummaryPanelMinHeight = 82f;
        private const float CampaignAdaptationSummaryIconSize = 34f;
        private const float CampaignAdaptationSummaryIconPadding = 4f;
        private const float DraftContentPreferredHeight = 780f;
        private const int DraftContentTopPadding = 20;
        private const int DraftContentBottomPadding = 12;
        private const float DraftContentVerticalSpacing = 10f;
        private const float DraftHeaderPreferredHeight = 56f;
        private const float DraftBlurbPreferredHeight = 30f;
        private const float DraftChoiceContainerPreferredHeight = 452f;
        private const float DraftCardPreferredWidth = 240f;
        private const float DraftCardPreferredHeight = 452f;
        private const int CampaignMycovariantRedrawRetryCount = 12;
        private const string CampaignAdaptationRedrawReadyLabel = "Use Spore Sifting";
        private const string CampaignAdaptationRedrawConfirmLabel = "Confirm Redraw";
        private const string CampaignAdaptationRedrawUsedLabel = "Spore Sifting Used";
        private const string CampaignAdaptationRedrawTooltipText = "You unlocked the Spore Sifting moldiness reward, allowing you to redraw all Mycovariants to present new draft options once per level.";
        private const string DraftFeedTitle = "Draft Feed";
        private const string CampaignAdaptationSummaryTitle = "Current Adaptations";

        [Header("UI References")]
        [SerializeField] private GameObject draftPanel; // Main draft panel root
        [SerializeField] private CanvasGroup interactionBlocker; // semi-transparent overlay, blocks raycasts
        [SerializeField] private TextMeshProUGUI draftBannerText;
        [SerializeField] private TextMeshProUGUI draftBlurbText;
        [SerializeField] private DraftOrderRow draftOrderRow; // progress bar, highlights current/next/done
        [SerializeField] private Transform choiceContainer; // Parent for card prefabs
        [SerializeField] private MycovariantCard cardPrefab; // Assign in inspector
        [SerializeField] private GridVisualizer gridVisualizer;
        [SerializeField] private AudioClip draftPickConfirmClip = null;
        [SerializeField, Range(0f, 1f)] private float draftPickConfirmVolume = 1f;
        [SerializeField] private AudioClip adaptationDraftPickConfirmClip = null;
        [SerializeField, Range(0f, 1f)] private float adaptationDraftPickConfirmVolume = 1f;

        [Header("Draft Message Feed (Left Panel)")]
        [SerializeField] private GameObject draftMessagePanel;
        [SerializeField] private TextMeshProUGUI draftMessageTitleText;
        [SerializeField] private float draftCompletionHoldSeconds = 3f;

        private List<Mycovariant> draftChoices;
        private Player currentPlayer;
        private List<Player> draftOrder;
        private int draftIndex;

        private MycovariantPoolManager poolManager;
        private System.Random rng;
        private int draftChoicesCount;
        private string draftHeaderTitle = DefaultDraftTitle;
        private string draftHeaderBlurb = DefaultDraftBlurb;
        private string draftStartMessage = string.Empty;
        private string humanTurnBannerText = DefaultHumanTurnBannerText;
        private string aiTurnBannerPrefix = DefaultAiTurnBannerPrefix;

        private DraftUIState uiState = DraftUIState.Idle;
        private ScrollRect draftFeedScrollRect;
        private Transform draftFeedContentTransform;
        private TextMeshProUGUI currentPlayerEntryText;
        private int draftFeedEntryCount;
        private readonly List<DraftHistoryEntry> draftHistoryEntries = new();
        private bool isFinishingDraftPhase;
        private bool isCampaignAdaptationDraft;
        private Action<AdaptationDefinition> onAdaptationPicked;
        private GameObject campaignAdaptationUtilityRoot;
        private TextMeshProUGUI campaignAdaptationUtilityStatusText;
        private Button campaignAdaptationRedrawButton;
        private TextMeshProUGUI campaignAdaptationRedrawButtonText;
        private GameObject campaignAdaptationSummaryRoot;
        private TextMeshProUGUI campaignAdaptationSummaryTitleText;
        private RectTransform campaignAdaptationSummaryIconRow;
        private readonly List<GameObject> campaignAdaptationSummaryIconObjects = new();
        private bool showCampaignAdaptationRedrawControl;
        private bool campaignAdaptationRedrawAvailable;
        private bool campaignAdaptationRedrawConfirmArmed;
        private Func<IReadOnlyList<AdaptationDefinition>> onCampaignAdaptationRedrawRequested;
        private bool showCampaignMycovariantRedrawControl;
        private bool campaignMycovariantRedrawAvailable;
        private bool campaignMycovariantRedrawConfirmArmed;
        private AudioSource soundEffectAudioSource;
        private RectTransform draftHistoryOverlayRoot;
        private CanvasGroup draftHistoryOverlayCanvasGroup;
        private ScrollRect draftHistoryOverlayScrollRect;
        private Transform draftHistoryOverlayContentTransform;
        private TextMeshProUGUI draftHistoryEmptyStateText;
        private Button draftHistoryCloseButton;
        private RectTransform mycovariantDraftCoachmarkRoot;
        private CanvasGroup mycovariantDraftCoachmarkCanvasGroup;
        private TextMeshProUGUI mycovariantDraftCoachmarkTitleTextLabel;
        private TextMeshProUGUI mycovariantDraftCoachmarkBodyTextLabel;
        private Button mycovariantDraftCoachmarkCloseButton;
        private bool hasDismissedMycovariantDraftCoachmarkThisGame;

        private bool _cameraRecenteredThisDraftPhase = false;

        private sealed class DraftHistoryEntry
        {
            public int Round { get; set; }
            public int PlayerId { get; set; }
            public string Announcement { get; set; }
            public string Detail { get; set; }
        }

        public bool IsDraftUiVisible => draftPanel != null && draftPanel.activeInHierarchy;

        // Public entry point: starts draft phase
        public void StartDraft(
            List<Player> players,
            MycovariantPoolManager poolManager,
            List<Player> draftOrder,
            System.Random rng,
            int draftChoicesCount,
            string draftTitle = DefaultDraftTitle,
            string draftBlurb = DefaultDraftBlurb,
            string draftStartMessage = null,
            string humanTurnBannerText = DefaultHumanTurnBannerText,
            string aiTurnBannerPrefix = DefaultAiTurnBannerPrefix)
        {
            this.poolManager = poolManager;
            this.rng = rng;
            this.draftOrder = draftOrder;
            draftIndex = 0;
            this.draftChoicesCount = draftChoicesCount;
            draftHeaderTitle = string.IsNullOrWhiteSpace(draftTitle) ? DefaultDraftTitle : draftTitle;
            draftHeaderBlurb = string.IsNullOrWhiteSpace(draftBlurb) ? DefaultDraftBlurb : draftBlurb;
            this.draftStartMessage = string.IsNullOrWhiteSpace(draftStartMessage)
                ? $"Draft started. {draftOrder.Count} player{(draftOrder.Count == 1 ? "" : "s")} picking in order."
                : draftStartMessage;
            this.humanTurnBannerText = string.IsNullOrWhiteSpace(humanTurnBannerText) ? DefaultHumanTurnBannerText : humanTurnBannerText;
            this.aiTurnBannerPrefix = string.IsNullOrWhiteSpace(aiTurnBannerPrefix) ? DefaultAiTurnBannerPrefix : aiTurnBannerPrefix;
            isFinishingDraftPhase = false;
            MycovariantDraftManager.MarkLastAiDrafterForCurrentDraft(players, draftOrder);

            EnsureDraftMessageUI();
            SetDraftMessagePanelTitle(DraftFeedTitle);
            ClearDraftMessages();
            AddDraftMessage(this.draftStartMessage);
            TryAnnounceAscusPrimacyDraftPriority();

            SetDraftHeader(draftHeaderTitle, draftHeaderBlurb);
            ApplyDraftLayoutSizing();

            isCampaignAdaptationDraft = false;
            onAdaptationPicked = null;
            ConfigureCampaignAdaptationRedrawControl(false, false, null);
            ConfigureCampaignMycovariantRedrawControl();
            ShowDraftUI();
            TryShowMycovariantDraftCoachmark();
            BeginNextDraft();
        }

        public void ResetForGameTransition()
        {
            StopAllCoroutines();

            draftChoices = null;
            currentPlayer = null;
            draftOrder = null;
            draftIndex = 0;
            poolManager = null;
            rng = null;
            draftChoicesCount = 0;
            uiState = DraftUIState.Idle;
            isFinishingDraftPhase = false;
            isCampaignAdaptationDraft = false;
            onAdaptationPicked = null;
            showCampaignAdaptationRedrawControl = false;
            campaignAdaptationRedrawAvailable = false;
            campaignAdaptationRedrawConfirmArmed = false;
            onCampaignAdaptationRedrawRequested = null;
            showCampaignMycovariantRedrawControl = false;
            campaignMycovariantRedrawAvailable = false;
            campaignMycovariantRedrawConfirmArmed = false;
            _cameraRecenteredThisDraftPhase = false;
            hasDismissedMycovariantDraftCoachmarkThisGame = false;

            if (draftOrder != null)
            {
                MycovariantDraftManager.ClearLastAiDrafterForCurrentDraft(draftOrder);
            }

            ClearChoiceCards();
            ClearDraftMessages();
            ClearDraftHistory();
            HideDraftUI();
        }

        public bool HasDraftHistory => draftHistoryEntries.Count > 0;

        public void ShowDraftHistoryOverlay()
        {
            if (!HasDraftHistory)
            {
                return;
            }

            EnsureDraftHistoryOverlayUI();
            RebuildDraftHistoryOverlay();

            if (draftHistoryOverlayRoot == null || draftHistoryOverlayCanvasGroup == null)
            {
                return;
            }

            draftHistoryOverlayRoot.gameObject.SetActive(true);
            draftHistoryOverlayRoot.SetAsLastSibling();
            draftHistoryOverlayCanvasGroup.alpha = 1f;
            draftHistoryOverlayCanvasGroup.blocksRaycasts = true;
            draftHistoryOverlayCanvasGroup.interactable = true;

            if (draftHistoryOverlayScrollRect != null)
            {
                draftHistoryOverlayScrollRect.normalizedPosition = new Vector2(0f, 1f);
            }
        }

        public void HideDraftHistoryOverlay()
        {
            if (draftHistoryOverlayCanvasGroup != null)
            {
                draftHistoryOverlayCanvasGroup.alpha = 0f;
                draftHistoryOverlayCanvasGroup.blocksRaycasts = false;
                draftHistoryOverlayCanvasGroup.interactable = false;
            }

            if (draftHistoryOverlayRoot != null)
            {
                draftHistoryOverlayRoot.gameObject.SetActive(false);
            }
        }

        public void StartCampaignAdaptationDraft(
            IReadOnlyList<AdaptationDefinition> choices,
            Action<AdaptationDefinition> onPicked,
            bool showRedrawControl,
            bool redrawAvailable,
            Func<IReadOnlyList<AdaptationDefinition>> onRedrawRequested)
        {
            if (choices == null || choices.Count == 0)
            {
                Debug.LogWarning("[MycovariantDraftController] Cannot start adaptation draft with no choices.");
                return;
            }

            isCampaignAdaptationDraft = true;
            onAdaptationPicked = onPicked;
            isFinishingDraftPhase = false;

            EnsureDraftMessageUI();
            SetDraftMessagePanelTitle(GetCampaignAdaptationDraftFeedTitle());
            ClearDraftMessages();
            AddDraftMessage("Victory secured. Choose an Adaptation to evolve your colony for the rest of this campaign.");

            SetDraftHeader(
                "Choose an Adaptation",
                "Select one adaptation to strengthen your colony for the rest of the campaign.");
            ApplyDraftLayoutSizing();

            ConfigureCampaignAdaptationRedrawControl(showRedrawControl, redrawAvailable, onRedrawRequested);
            showCampaignMycovariantRedrawControl = false;
            campaignMycovariantRedrawAvailable = false;
            campaignMycovariantRedrawConfirmArmed = false;
            ShowDraftUI();
            if (draftOrderRow != null)
            {
                draftOrderRow.gameObject.SetActive(false);
            }
            PopulateAdaptationChoices(choices);
            PopulateCampaignAdaptationSummary();
            SetDraftState(DraftUIState.HumanTurn);
        }

        private void BeginNextDraft()
        {
            if (draftIndex >= draftOrder.Count)
            {
                var allMycovariants = MycovariantRepository.All;
                poolManager.ReturnUndraftedToPool(allMycovariants, rng);
                MycovariantDraftManager.ClearLastAiDrafterForCurrentDraft(draftOrder);
                AddDraftMessage("Draft complete. Spores settle while the colonies prepare for the next round...");
                BeginDraftCompletionSequence();
                return;
            }

            currentPlayer = draftOrder[draftIndex];
            EnterDraftTurn(GetFreshDraftChoicesForCurrentPlayer(), repopulateChoices: true);
        }

        private void UpdateDraftBanner()
        {
            if (currentPlayer.PlayerType == PlayerTypeEnum.AI)
                draftBannerText.text = $"{aiTurnBannerPrefix}: {currentPlayer.PlayerName}";
            else
                draftBannerText.text = humanTurnBannerText;
        }

        private void SetDraftHeader(string title, string blurb)
        {
            if (draftBannerText != null)
            {
                draftBannerText.text = title;
            }

            if (draftBlurbText != null)
            {
                draftBlurbText.text = blurb;
            }
        }

        private void SetDraftState(DraftUIState state)
        {
            uiState = state;
            if (state != DraftUIState.HumanTurn)
            {
                campaignAdaptationRedrawConfirmArmed = false;
                campaignMycovariantRedrawConfirmArmed = false;
            }

            switch (state)
            {
                case DraftUIState.HumanTurn:
                    SetAllPickButtonsInteractable(true);
                    interactionBlocker.blocksRaycasts = false;
                    interactionBlocker.alpha = 0f;
                    HighlightActiveCards(true);
                    break;
                case DraftUIState.AITurn:
                    SetAllPickButtonsInteractable(false);
                    interactionBlocker.blocksRaycasts = true;
                    interactionBlocker.alpha = 0.35f;
                    HighlightActiveCards(false);
                    break;
                case DraftUIState.AnimatingPick:
                    SetAllPickButtonsInteractable(false);
                    interactionBlocker.blocksRaycasts = true;
                    interactionBlocker.alpha = 0.55f;
                    HighlightActiveCards(false);
                    break;
                case DraftUIState.Idle:
                case DraftUIState.Complete:
                default:
                    SetAllPickButtonsInteractable(false);
                    interactionBlocker.blocksRaycasts = false;
                    interactionBlocker.alpha = 0f;
                    HighlightActiveCards(false);
                    break;
            }

            RefreshCampaignAdaptationUtilityUi();
        }

        private void HighlightActiveCards(bool highlight)
        {
            foreach (Transform child in choiceContainer)
            {
                var card = child.GetComponent<MycovariantCard>();
                if (card != null)
                    card.SetActiveHighlight(highlight);
            }
        }

        private void PopulateChoices(List<Mycovariant> choices)
        {
            ClearChoiceCards();

            int currentRound = GameManager.Instance?.Board?.CurrentRound ?? 0;

            foreach (var m in choices)
            {
                CreateChoiceCard(
                    m,
                    m.Name,
                    MycovariantDescriptionFormatter.GetDraftPreviewDescription(m, currentRound),
                    MycovariantArtRepository.GetIcon(m),
                    () => OnChoicePicked(m),
                    highlight: false);
            }
        }

        private void OnChoicePicked(Mycovariant picked)
        {
            if (uiState != DraftUIState.HumanTurn && uiState != DraftUIState.AITurn)
                return;

            PlayDraftPickConfirmSound();
            GameManager.Instance.GameUI.GameLogRouter?.OnDraftPick(currentPlayer.PlayerName, picked.Name);

            GameManager.Instance.ResolveMycovariantDraftPick(currentPlayer, picked);

            if (!picked.IsUniversal)
                poolManager.RemoveFromPool(picked);

            var playerMyco = currentPlayer.PlayerMycovariants
                .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

            if (currentPlayer.PlayerType == PlayerTypeEnum.AI && playerMyco != null)
            {
                float score = MycovariantDraftManager.GetRecordedAIDraftScore(
                    currentPlayer,
                    picked,
                    GameManager.Instance.Board,
                    GetCurrentCampaignStartDifficulty());
                playerMyco.AIScoreAtDraft = score;
            }

            string pickAnnouncement = BuildPickAnnouncement(currentPlayer, picked, playerMyco);
            AddPlayerFeedEntry(currentPlayer, pickAnnouncement);

            if (playerMyco == null)
            {
                Debug.LogError($"[MycovariantDraftController] PlayerMycovariant is null for {picked?.Name} ({picked?.Id}) and player {currentPlayer.PlayerId}. Aborting effect resolution.");
                RecordDraftHistoryEntry(currentPlayer, pickAnnouncement, "Effect resolution could not be displayed.");
                AnimatePickFeedback(picked, () => {
                    ReplacePickedCardAndContinue(picked);
                });
                return;
            }

            if (!picked.AutoMarkTriggered)
            {
                draftPanel.SetActive(false);
                interactionBlocker.blocksRaycasts = false;
                interactionBlocker.alpha = 0f;
            }

            var routine = MycovariantEffectResolver.Instance.ResolveEffect(
                currentPlayer,
                picked,
                playerMyco,
                () => {
                    gridVisualizer.RenderBoard(GameManager.Instance.Board);
                    string resolvedAnnouncement = BuildPickAnnouncement(currentPlayer, picked, playerMyco);
                    ReplaceCurrentPlayerEntry(resolvedAnnouncement);
                    string resultMessage = AddDraftResultMessage(currentPlayer, picked, playerMyco);
                    RecordDraftHistoryEntry(currentPlayer, resolvedAnnouncement, resultMessage);
                    draftPanel.SetActive(true);
                    SetDraftState(DraftUIState.AnimatingPick);
                    AnimatePickFeedback(picked, () => {
                        ReplacePickedCardAndContinue(picked);
                    });
                }
            );

            if (this.gameObject.activeInHierarchy)
            {
                StartCoroutine(routine);
            }
            else if (GameManager.Instance != null)
            {
                GameManager.Instance.StartCoroutine(routine);
            }
            else
            {
                Debug.LogError("No active runner available to start ResolveEffect coroutine.");
            }
        }

        private void ReplacePickedCardAndContinue(Mycovariant picked)
        {
            draftIndex++;
            if (draftIndex >= draftOrder.Count)
            {
                BeginNextDraft();
                return;
            }

            currentPlayer = draftOrder[draftIndex];
            var nextChoices = BuildCarriedForwardDraftChoices(picked);
            if (nextChoices == null || nextChoices.Count == 0)
            {
                EnterDraftTurn(GetFreshDraftChoicesForCurrentPlayer(), repopulateChoices: true);
                return;
            }

            EnterDraftTurn(nextChoices, repopulateChoices: false);
        }

        private void EnterDraftTurn(List<Mycovariant> choices, bool repopulateChoices)
        {
            UpdateDraftBanner();

            if (draftOrderRow != null)
            {
                draftOrderRow.gameObject.SetActive(draftOrder.Count > 1);
                draftOrderRow.SetDraftOrder(draftOrder, draftIndex);
            }

            draftChoices = choices ?? new List<Mycovariant>();

            if (repopulateChoices)
            {
                PopulateChoices(draftChoices);
            }
            else
            {
                ReconcileVisibleChoices(draftChoices);
            }

            if (currentPlayer.PlayerType == PlayerTypeEnum.AI)
            {
                SetDraftState(DraftUIState.AITurn);
                StartCoroutine(AnimateAIPickRoutine());
            }
            else
            {
                SetDraftState(DraftUIState.HumanTurn);
            }
        }

        private List<Mycovariant> GetFreshDraftChoicesForCurrentPlayer()
        {
            return GetFreshDraftChoicesForCurrentPlayer(null);
        }

        private List<Mycovariant> GetFreshDraftChoicesForCurrentPlayer(IReadOnlyCollection<int> excludedChoiceIds)
        {
            int? forcedMycovariantId = GetForcedMycovariantIdForCurrentPlayer();
            if (excludedChoiceIds == null || excludedChoiceIds.Count == 0)
            {
                return MycovariantDraftManager.GetDraftChoices(
                    currentPlayer,
                    poolManager,
                    draftChoicesCount,
                    rng,
                    forcedMycovariantId);
            }

            var eligibleChoices = poolManager
                .GetEligibleMycovariantsForPlayer(currentPlayer)
                .GroupBy(mycovariant => mycovariant.Id)
                .Select(group => group.First())
                .Where(mycovariant => !excludedChoiceIds.Contains(mycovariant.Id)
                    || (forcedMycovariantId.HasValue && mycovariant.Id == forcedMycovariantId.Value))
                .OrderBy(_ => rng.Next())
                .ToList();

            if (eligibleChoices.Count < draftChoicesCount)
            {
                return MycovariantDraftManager.GetDraftChoices(
                    currentPlayer,
                    poolManager,
                    draftChoicesCount,
                    rng,
                    forcedMycovariantId);
            }

            var freshChoices = eligibleChoices.Take(draftChoicesCount).ToList();
            ForceMycovariantIntoChoicesIfNeeded(freshChoices, eligibleChoices);
            return freshChoices;
        }

        private List<Mycovariant> BuildCarriedForwardDraftChoices(Mycovariant picked)
        {
            if (draftChoices == null || draftChoices.Count == 0)
            {
                return GetFreshDraftChoicesForCurrentPlayer();
            }

            var nextChoices = new Mycovariant[draftChoices.Count];
            for (int index = 0; index < draftChoices.Count; index++)
            {
                var existingChoice = draftChoices[index];
                if (existingChoice == null || existingChoice.Id == picked.Id)
                {
                    continue;
                }

                nextChoices[index] = existingChoice;
            }

            var visibleChoiceIds = new HashSet<int>(nextChoices
                .Where(choice => choice != null)
                .Select(choice => choice.Id));

            var replacementPool = poolManager
                .GetEligibleMycovariantsForPlayer(currentPlayer)
                .GroupBy(mycovariant => mycovariant.Id)
                .Select(group => group.First())
                .Where(choice => choice.Id != picked.Id)
                .Where(choice => !visibleChoiceIds.Contains(choice.Id))
                .OrderBy(_ => rng.Next())
                .ToList();

            int replacementIndex = 0;
            for (int index = 0; index < nextChoices.Length; index++)
            {
                if (nextChoices[index] != null)
                {
                    continue;
                }

                if (replacementIndex >= replacementPool.Count)
                {
                    return GetFreshDraftChoicesForCurrentPlayer();
                }

                nextChoices[index] = replacementPool[replacementIndex++];
            }

            var carriedChoices = nextChoices.ToList();
            ForceMycovariantIntoChoicesIfNeeded(carriedChoices, replacementPool);
            return carriedChoices;
        }

        private void ForceMycovariantIntoChoicesIfNeeded(List<Mycovariant> choices, IEnumerable<Mycovariant> eligibleChoices)
        {
            int? forcedMycovariantId = GetForcedMycovariantIdForCurrentPlayer();
            if (!forcedMycovariantId.HasValue)
            {
                return;
            }

            var forcedChoice = eligibleChoices?.FirstOrDefault(mycovariant => mycovariant.Id == forcedMycovariantId.Value);
            if (forcedChoice == null || choices.Any(choice => choice != null && choice.Id == forcedChoice.Id))
            {
                return;
            }

            if (choices.Count < draftChoicesCount)
            {
                choices.Add(forcedChoice);
                return;
            }

            if (choices.Count > 0)
            {
                choices[0] = forcedChoice;
            }
        }

        private int? GetForcedMycovariantIdForCurrentPlayer()
        {
            if (GameManager.Instance.IsTestingModeEnabled && currentPlayer.PlayerType == PlayerTypeEnum.Human)
            {
                return GameManager.Instance.TestingMycovariantId;
            }

            return null;
        }

        private void ReconcileVisibleChoices(IReadOnlyList<Mycovariant> choices)
        {
            if (choices == null || choiceContainer == null || choiceContainer.childCount != choices.Count)
            {
                PopulateChoices(choices?.ToList() ?? new List<Mycovariant>());
                return;
            }

            int index = 0;
            foreach (Transform child in choiceContainer)
            {
                var card = child.GetComponent<MycovariantCard>();
                var choice = choices[index++];
                if (card == null || choice == null)
                {
                    PopulateChoices(choices.ToList());
                    return;
                }

                if (card.Mycovariant == null || card.Mycovariant.Id != choice.Id)
                {
                    card.SetMycovariant(choice, OnChoicePicked);
                }

                card.SetActiveHighlight(false);
                card.gameObject.SetActive(true);
            }
        }

        private void ReplacePickedCard(Mycovariant picked)
        {
            MycovariantCard pickedCard = null;
            foreach (Transform child in choiceContainer)
            {
                var card = child.GetComponent<MycovariantCard>();
                if (card != null && card.Mycovariant == picked)
                {
                    pickedCard = card;
                    break;
                }
            }

            if (pickedCard == null)
            {
                Debug.LogWarning("Could not find picked card to replace");
                return;
            }

            var replacement = GetReplacementMycovariant(picked);
            if (replacement != null)
            {
                pickedCard.SetMycovariant(replacement, OnChoicePicked);
                pickedCard.SetActiveHighlight(false);
                pickedCard.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"No replacement mycovariant found for {picked.Name}. This should not happen with 3+ universal mycovariants.");
                draftChoices = MycovariantDraftManager.GetDraftChoices(
                    currentPlayer, poolManager, draftChoicesCount, rng);
                PopulateChoices(draftChoices);
            }
        }

        private Mycovariant GetReplacementMycovariant(Mycovariant excludeMycovariant)
        {
            var eligible = poolManager.GetEligibleMycovariantsForPlayer(currentPlayer);
            
            var uniqueEligible = eligible.GroupBy(m => m.Id).Select(g => g.First()).ToList();
            uniqueEligible = uniqueEligible.Where(m => m.Id != excludeMycovariant.Id).ToList();
            
            var currentOfferedIds = new HashSet<int>();
            foreach (Transform child in choiceContainer)
            {
                var card = child.GetComponent<MycovariantCard>();
                if (card != null && card.gameObject.activeInHierarchy && card.Mycovariant != excludeMycovariant)
                {
                    currentOfferedIds.Add(card.Mycovariant.Id);
                }
            }
            uniqueEligible = uniqueEligible.Where(m => !currentOfferedIds.Contains(m.Id)).ToList();
            
            if (uniqueEligible.Count == 0)
                return null;

            return uniqueEligible[rng.Next(uniqueEligible.Count)];
        }

        private List<Mycovariant> GetEligibleVisibleChoicesForCurrentPlayer()
        {
            if (draftChoices == null || draftChoices.Count == 0)
            {
                return new List<Mycovariant>();
            }

            var eligibleIds = poolManager
                .GetEligibleMycovariantsForPlayer(currentPlayer)
                .Select(mycovariant => mycovariant.Id)
                .ToHashSet();

            return draftChoices
                .Where(mycovariant => mycovariant != null && eligibleIds.Contains(mycovariant.Id))
                .GroupBy(mycovariant => mycovariant.Id)
                .Select(group => group.First())
                .ToList();
        }

        private IEnumerator AnimateAIPickRoutine()
        {
            yield return new WaitForSeconds(FungusToast.Unity.UI.UIEffectConstants.AIDraftPickDelaySeconds);

            var eligibleChoices = GetEligibleVisibleChoicesForCurrentPlayer();
            if (eligibleChoices.Count == 0)
            {
                draftChoices = GetFreshDraftChoicesForCurrentPlayer();
                PopulateChoices(draftChoices);
                eligibleChoices = draftChoices;
            }

            Mycovariant pick;
            if (GameManager.Instance.IsTestingModeEnabled && GameManager.Instance.TestingMycovariantId.HasValue && eligibleChoices.Any(m => m.Id == GameManager.Instance.TestingMycovariantId.Value))
            {
                pick = eligibleChoices.First(m => m.Id == GameManager.Instance.TestingMycovariantId.Value);
            }
            else
            {
                pick = MycovariantDraftManager.SelectAIDraftPick(
                    currentPlayer,
                    eligibleChoices,
                    GameManager.Instance.Board,
                    rng,
                    GetCurrentCampaignStartDifficulty());
            }
            OnChoicePicked(pick);
        }

        private CampaignDifficulty? GetCurrentCampaignStartDifficulty()
        {
            var gameManager = GameManager.Instance;
            if (gameManager == null || gameManager.CurrentGameMode != FungusToast.Unity.Campaign.GameMode.Campaign)
            {
                return null;
            }

            return gameManager.CampaignController?.CurrentStartDifficulty;
        }

        private void SetAllPickButtonsInteractable(bool interactable)
        {
            foreach (Transform child in choiceContainer)
            {
                var card = child.GetComponent<MycovariantCard>();
                if (card != null && card.pickButton != null)
                    card.pickButton.interactable = interactable;
            }
        }

        private void PopulateAdaptationChoices(IReadOnlyList<AdaptationDefinition> choices)
        {
            ClearChoiceCards();

            for (int i = 0; i < choices.Count; i++)
            {
                var adaptation = choices[i];
                CreateChoiceCard(
                    null,
                    adaptation.Name,
                    adaptation.Description,
                    AdaptationArtRepository.GetIcon(adaptation),
                    () => OnAdaptationChoicePicked(adaptation),
                    highlight: true);
            }

            RefreshCampaignAdaptationUtilityUi();
        }

        private void ClearChoiceCards()
        {
            foreach (Transform child in choiceContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private MycovariantCard CreateChoiceCard(Mycovariant boundMycovariant, string title, string description, Sprite icon, Action onPicked, bool highlight)
        {
            var card = Instantiate(cardPrefab, choiceContainer);
            ConfigureChoiceCardLayout(card);
            card.SetChoiceContent(boundMycovariant, title, description, icon, onPicked);
            card.SetActiveHighlight(highlight);
            return card;
        }

        private void ConfigureChoiceCardLayout(MycovariantCard card)
        {
            if (card == null)
            {
                return;
            }

            var cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.sizeDelta = new Vector2(DraftCardPreferredWidth, DraftCardPreferredHeight);
            }

            var layoutElement = card.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = card.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = DraftCardPreferredWidth;
            layoutElement.minWidth = DraftCardPreferredWidth;
            layoutElement.preferredHeight = DraftCardPreferredHeight;
            layoutElement.minHeight = DraftCardPreferredHeight;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        private void ApplyDraftLayoutSizing()
        {
            var contentRoot = choiceContainer?.parent as RectTransform;
            var contentLayoutGroup = contentRoot != null ? contentRoot.GetComponent<VerticalLayoutGroup>() : null;
            if (contentRoot != null)
            {
                contentRoot.sizeDelta = new Vector2(contentRoot.sizeDelta.x, DraftContentPreferredHeight);
                var contentLayout = contentRoot.GetComponent<LayoutElement>();
                if (contentLayout != null)
                {
                    contentLayout.preferredHeight = DraftContentPreferredHeight;
                    contentLayout.minHeight = DraftContentPreferredHeight;
                }
            }

            if (contentLayoutGroup != null)
            {
                contentLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                contentLayoutGroup.spacing = DraftContentVerticalSpacing;
                contentLayoutGroup.padding.top = DraftContentTopPadding;
                contentLayoutGroup.padding.bottom = DraftContentBottomPadding;
            }

            var headerRoot = draftBannerText != null ? draftBannerText.transform.parent as RectTransform : null;
            var headerRootLayout = headerRoot != null ? headerRoot.GetComponent<LayoutElement>() : null;
            if (headerRoot != null)
            {
                headerRoot.sizeDelta = new Vector2(headerRoot.sizeDelta.x, DraftHeaderPreferredHeight);
            }

            if (headerRootLayout != null)
            {
                headerRootLayout.preferredHeight = DraftHeaderPreferredHeight;
                headerRootLayout.minHeight = DraftHeaderPreferredHeight;
            }

            var headerRect = draftBannerText != null ? draftBannerText.GetComponent<RectTransform>() : null;
            var headerLayout = draftBannerText != null ? draftBannerText.GetComponent<LayoutElement>() : null;
            if (headerRect != null)
            {
                StretchDraftTextRect(headerRect);
            }

            if (headerLayout != null)
            {
                headerLayout.preferredHeight = -1f;
                headerLayout.minHeight = -1f;
            }

            var blurbRoot = draftBlurbText != null ? draftBlurbText.transform.parent as RectTransform : null;
            var blurbRootLayout = blurbRoot != null ? blurbRoot.GetComponent<LayoutElement>() : null;
            if (blurbRoot != null)
            {
                blurbRoot.sizeDelta = new Vector2(blurbRoot.sizeDelta.x, DraftBlurbPreferredHeight);
            }

            if (blurbRootLayout != null)
            {
                blurbRootLayout.preferredHeight = DraftBlurbPreferredHeight;
                blurbRootLayout.minHeight = DraftBlurbPreferredHeight;
            }

            var blurbTextRect = draftBlurbText != null ? draftBlurbText.GetComponent<RectTransform>() : null;
            var blurbTextLayout = draftBlurbText != null ? draftBlurbText.GetComponent<LayoutElement>() : null;
            if (blurbTextRect != null)
            {
                StretchDraftTextRect(blurbTextRect);
            }

            if (blurbTextLayout != null)
            {
                blurbTextLayout.preferredHeight = -1f;
                blurbTextLayout.minHeight = -1f;
            }

            var choiceContainerRect = choiceContainer as RectTransform;
            if (choiceContainerRect != null)
            {
                choiceContainerRect.sizeDelta = new Vector2(choiceContainerRect.sizeDelta.x, DraftChoiceContainerPreferredHeight);
            }

            if (contentRoot != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
            }
        }

        private static void StretchDraftTextRect(RectTransform textRect)
        {
            if (textRect == null)
            {
                return;
            }

            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void OnAdaptationChoicePicked(AdaptationDefinition picked)
        {
            if (!isCampaignAdaptationDraft || picked == null)
            {
                return;
            }

            SetDraftState(DraftUIState.AnimatingPick);
            PlayAdaptationDraftPickConfirmSound();
            AddDraftMessage($"Adaptation acquired: {picked.Name}.");

            var callback = onAdaptationPicked;
            onAdaptationPicked = null;
            isCampaignAdaptationDraft = false;

            HideDraftUI();
            callback?.Invoke(picked);
        }

        private void EnsureSoundEffectAudioSource()
        {
            if (soundEffectAudioSource != null)
            {
                return;
            }

            var soundEffectHost = GameManager.Instance != null ? GameManager.Instance.gameObject : gameObject;

            soundEffectAudioSource = soundEffectHost.GetComponent<AudioSource>();
            if (soundEffectAudioSource == null)
            {
                soundEffectAudioSource = soundEffectHost.AddComponent<AudioSource>();
            }

            soundEffectAudioSource.playOnAwake = false;
            soundEffectAudioSource.loop = false;
            soundEffectAudioSource.spatialBlend = 0f;
        }

        private void PlayDraftPickConfirmSound()
        {
            if (draftPickConfirmClip == null)
            {
                return;
            }

            EnsureSoundEffectAudioSource();
            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(draftPickConfirmVolume);
            if (effectiveVolume <= 0f)
            {
                return;
            }

            soundEffectAudioSource.PlayOneShot(draftPickConfirmClip, effectiveVolume);
        }

        private void PlayAdaptationDraftPickConfirmSound()
        {
            if (adaptationDraftPickConfirmClip == null)
            {
                return;
            }

            EnsureSoundEffectAudioSource();
            float effectiveVolume = SoundEffectsSettings.GetEffectiveVolume(adaptationDraftPickConfirmVolume);
            if (effectiveVolume <= 0f)
            {
                return;
            }

            soundEffectAudioSource.PlayOneShot(adaptationDraftPickConfirmClip, effectiveVolume);
        }

        private void TryShowMycovariantDraftCoachmark()
        {
            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            bool isFastForwarding = GameManager.Instance != null && GameManager.Instance.IsFastForwarding;
            if (!NewPlayerTooltipRules.ShouldShowMycovariantDraftIntro(forceFirstGame, hasDismissedMycovariantDraftCoachmarkThisGame, isFastForwarding))
            {
                return;
            }

            EnsureMycovariantDraftCoachmarkUi();
            if (mycovariantDraftCoachmarkRoot == null || mycovariantDraftCoachmarkCanvasGroup == null)
            {
                return;
            }

            NewPlayerTooltipDefinition definition = NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.MycovariantDraftIntro);
            mycovariantDraftCoachmarkTitleTextLabel.text = definition.Title;
            mycovariantDraftCoachmarkBodyTextLabel.text = definition.Body;
            PositionMycovariantDraftCoachmark();
            mycovariantDraftCoachmarkRoot.gameObject.SetActive(true);
            mycovariantDraftCoachmarkRoot.SetAsLastSibling();
            mycovariantDraftCoachmarkCanvasGroup.alpha = 1f;
            mycovariantDraftCoachmarkCanvasGroup.blocksRaycasts = true;
            mycovariantDraftCoachmarkCanvasGroup.interactable = true;
        }

        private void EnsureMycovariantDraftCoachmarkUi()
        {
            if (mycovariantDraftCoachmarkRoot != null || draftPanel == null)
            {
                return;
            }

            Transform parent = draftPanel.GetComponentInParent<Canvas>()?.rootCanvas?.transform ?? transform.parent;
            if (parent == null)
            {
                return;
            }

            var rootObject = new GameObject("UI_MycovariantDraftCoachmark", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Outline));
            rootObject.transform.SetParent(parent, false);

            mycovariantDraftCoachmarkRoot = rootObject.GetComponent<RectTransform>();
            mycovariantDraftCoachmarkRoot.anchorMin = new Vector2(0.5f, 0.5f);
            mycovariantDraftCoachmarkRoot.anchorMax = new Vector2(0.5f, 0.5f);
            mycovariantDraftCoachmarkRoot.pivot = new Vector2(1f, 0.5f);
            mycovariantDraftCoachmarkRoot.sizeDelta = new Vector2(320f, 200f);

            mycovariantDraftCoachmarkCanvasGroup = rootObject.GetComponent<CanvasGroup>();
            mycovariantDraftCoachmarkCanvasGroup.alpha = 0f;
            mycovariantDraftCoachmarkCanvasGroup.blocksRaycasts = false;
            mycovariantDraftCoachmarkCanvasGroup.interactable = false;

            var background = rootObject.GetComponent<Image>();
            var backgroundColor = Color.Lerp(UIStyleTokens.Surface.PanelSecondary, UIStyleTokens.Accent.Spore, 0.14f);
            backgroundColor.a = 0.97f;
            background.color = backgroundColor;
            background.raycastTarget = true;

            var outline = rootObject.GetComponent<Outline>();
            outline.effectColor = new Color(UIStyleTokens.State.Focus.r, UIStyleTokens.State.Focus.g, UIStyleTokens.State.Focus.b, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(rootObject.transform, false);
            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(14f, -48f);
            titleRect.offsetMax = new Vector2(-52f, -12f);

            mycovariantDraftCoachmarkTitleTextLabel = titleObject.GetComponent<TextMeshProUGUI>();
            mycovariantDraftCoachmarkTitleTextLabel.text = string.Empty;
            mycovariantDraftCoachmarkTitleTextLabel.color = UIStyleTokens.Text.Primary;
            mycovariantDraftCoachmarkTitleTextLabel.fontStyle = FontStyles.Bold;
            mycovariantDraftCoachmarkTitleTextLabel.fontSize = 22f;
            mycovariantDraftCoachmarkTitleTextLabel.alignment = TextAlignmentOptions.Left;
            mycovariantDraftCoachmarkTitleTextLabel.textWrappingMode = TextWrappingModes.NoWrap;
            TMPOverflowUtility.SetSafeEllipsis(mycovariantDraftCoachmarkTitleTextLabel);
            mycovariantDraftCoachmarkTitleTextLabel.raycastTarget = false;

            var bodyObject = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyObject.transform.SetParent(rootObject.transform, false);
            var bodyRect = bodyObject.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(14f, 14f);
            bodyRect.offsetMax = new Vector2(-14f, -50f);

            mycovariantDraftCoachmarkBodyTextLabel = bodyObject.GetComponent<TextMeshProUGUI>();
            mycovariantDraftCoachmarkBodyTextLabel.color = UIStyleTokens.Text.Primary;
            mycovariantDraftCoachmarkBodyTextLabel.fontSize = 17f;
            mycovariantDraftCoachmarkBodyTextLabel.alignment = TextAlignmentOptions.TopLeft;
            mycovariantDraftCoachmarkBodyTextLabel.textWrappingMode = TextWrappingModes.Normal;
            mycovariantDraftCoachmarkBodyTextLabel.overflowMode = TextOverflowModes.Overflow;
            mycovariantDraftCoachmarkBodyTextLabel.raycastTarget = false;

            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(rootObject.transform, false);
            var closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(34f, 34f);
            closeRect.anchoredPosition = new Vector2(-8f, -8f);

            var closeImage = closeObject.GetComponent<Image>();
            closeImage.color = UIStyleTokens.Surface.PanelElevated;

            mycovariantDraftCoachmarkCloseButton = closeObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyStyle(mycovariantDraftCoachmarkCloseButton);
            mycovariantDraftCoachmarkCloseButton.onClick.RemoveAllListeners();
            mycovariantDraftCoachmarkCloseButton.onClick.AddListener(OnMycovariantDraftCoachmarkDismissed);

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
            closeLabel.fontSize = 20f;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                mycovariantDraftCoachmarkTitleTextLabel.font = TMP_Settings.defaultFontAsset;
                mycovariantDraftCoachmarkBodyTextLabel.font = TMP_Settings.defaultFontAsset;
                closeLabel.font = TMP_Settings.defaultFontAsset;
            }

            rootObject.SetActive(false);
        }

        private void PositionMycovariantDraftCoachmark()
        {
            if (mycovariantDraftCoachmarkRoot == null || draftPanel == null)
            {
                return;
            }

            RectTransform anchorRect = choiceContainer?.parent as RectTransform;
            RectTransform parentRect = mycovariantDraftCoachmarkRoot.parent as RectTransform;
            Canvas canvas = draftPanel.GetComponentInParent<Canvas>()?.rootCanvas;
            if (anchorRect == null || parentRect == null || canvas == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            Vector3[] corners = new Vector3[4];
            anchorRect.GetWorldCorners(corners);
            Vector3 topLeftWorld = corners[1];

            CoachmarkLayoutUtility.TryPlaceAtWorldPoint(
                mycovariantDraftCoachmarkRoot,
                parentRect,
                canvas,
                topLeftWorld,
                new Vector2(-24f, -210f),
                CoachmarkLayoutUtility.DefaultScreenPadding);
        }

        private void OnMycovariantDraftCoachmarkDismissed()
        {
            hasDismissedMycovariantDraftCoachmarkThisGame = true;
            bool forceFirstGame = GameManager.Instance != null && GameManager.Instance.ShouldForceFirstGameExperience;
            if (!forceFirstGame)
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.MycovariantDraftIntro);
            }

            HideMycovariantDraftCoachmarkImmediate(false);
        }

        private void HideMycovariantDraftCoachmarkImmediate(bool resetSessionDismissal)
        {
            if (resetSessionDismissal)
            {
                hasDismissedMycovariantDraftCoachmarkThisGame = false;
            }

            if (mycovariantDraftCoachmarkCanvasGroup != null)
            {
                mycovariantDraftCoachmarkCanvasGroup.alpha = 0f;
                mycovariantDraftCoachmarkCanvasGroup.blocksRaycasts = false;
                mycovariantDraftCoachmarkCanvasGroup.interactable = false;
            }

            if (mycovariantDraftCoachmarkRoot != null)
            {
                mycovariantDraftCoachmarkRoot.gameObject.SetActive(false);
            }
        }

        private void ShowDraftUI()
        {
            draftPanel.SetActive(true);
            if (draftMessagePanel != null)
                draftMessagePanel.SetActive(true);
            interactionBlocker.blocksRaycasts = true;
            interactionBlocker.alpha = 0.8f;
            uiState = DraftUIState.Idle;

            if (mycovariantDraftCoachmarkRoot != null && mycovariantDraftCoachmarkRoot.gameObject.activeSelf)
            {
                PositionMycovariantDraftCoachmark();
            }

            // Smoothly restore initial camera framing only once per draft start
            if (!_cameraRecenteredThisDraftPhase && GameManager.Instance?.cameraCenterer != null)
            {
                _cameraRecenteredThisDraftPhase = true;
                GameManager.Instance.cameraCenterer.RestoreInitialFramingSmooth(FungusToast.Unity.UI.UIEffectConstants.DraftCameraRecenteringDurationSeconds);
            }
        }

        private void BeginDraftCompletionSequence()
        {
            if (isFinishingDraftPhase)
                return;

            isFinishingDraftPhase = true;
            interactionBlocker.blocksRaycasts = true;
            interactionBlocker.alpha = 0.25f;
            SetAllPickButtonsInteractable(false);
            StartCoroutine(FinishDraftAfterDelayRoutine());
        }

        private IEnumerator FinishDraftAfterDelayRoutine()
        {
            float holdDuration = Mathf.Max(0f, draftCompletionHoldSeconds);
            if (holdDuration > 0f)
                yield return new WaitForSeconds(holdDuration);

            HideDraftUI();
            uiState = DraftUIState.Complete;
            GameManager.Instance.OnMycovariantDraftComplete();
        }

        private void HideDraftUI()
        {
            draftPanel.SetActive(false);
            if (draftMessagePanel != null)
                draftMessagePanel.SetActive(false);
            interactionBlocker.blocksRaycasts = false;
            interactionBlocker.alpha = 0f;
            HideMycovariantDraftCoachmarkImmediate(false);
            if (draftOrderRow != null)
            {
                draftOrderRow.gameObject.SetActive(true);
            }
            uiState = DraftUIState.Idle;
            if (campaignAdaptationSummaryRoot != null)
            {
                campaignAdaptationSummaryRoot.SetActive(false);
            }
            RefreshCampaignAdaptationUtilityUi();
        }

        private void ConfigureCampaignAdaptationRedrawControl(
            bool showControl,
            bool redrawAvailable,
            Func<IReadOnlyList<AdaptationDefinition>> onRedrawRequested)
        {
            showCampaignAdaptationRedrawControl = showControl;
            campaignAdaptationRedrawAvailable = showControl && redrawAvailable;
            campaignAdaptationRedrawConfirmArmed = false;
            onCampaignAdaptationRedrawRequested = onRedrawRequested;
            EnsureCampaignAdaptationUtilityUi();
            RefreshCampaignAdaptationUtilityUi();
        }

        private void ConfigureCampaignMycovariantRedrawControl()
        {
            var gameManager = GameManager.Instance;
            var campaignController = gameManager?.CampaignController;
            bool showControl = gameManager != null
                && gameManager.CurrentGameMode == FungusToast.Unity.Campaign.GameMode.Campaign
                && campaignController != null
                && campaignController.HasUnlockedCampaignMycovariantDraftRedraw;

            showCampaignMycovariantRedrawControl = showControl;
            campaignMycovariantRedrawAvailable = showControl
                && campaignController != null
                && campaignController.CanUseCampaignMycovariantDraftRedraw;
            campaignMycovariantRedrawConfirmArmed = false;
            EnsureCampaignAdaptationUtilityUi();
            RefreshCampaignAdaptationUtilityUi();
        }

        private void EnsureCampaignAdaptationUtilityUi()
        {
            if (choiceContainer == null || choiceContainer.parent == null || campaignAdaptationUtilityRoot != null)
            {
                return;
            }

            campaignAdaptationUtilityRoot = new GameObject(
                "CampaignAdaptationUtilityRoot",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(VerticalLayoutGroup));
            campaignAdaptationUtilityRoot.transform.SetParent(choiceContainer.parent, false);
            campaignAdaptationUtilityRoot.transform.SetSiblingIndex(choiceContainer.GetSiblingIndex());

            var rootRect = campaignAdaptationUtilityRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(CampaignAdaptationUtilityPanelWidth, CampaignAdaptationUtilityPanelHeight);

            var layoutElement = campaignAdaptationUtilityRoot.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = CampaignAdaptationUtilityPanelWidth;
            layoutElement.minWidth = CampaignAdaptationUtilityPanelWidth;
            layoutElement.preferredHeight = CampaignAdaptationUtilityPanelHeight;
            layoutElement.minHeight = CampaignAdaptationUtilityPanelHeight;

            var rootImage = campaignAdaptationUtilityRoot.GetComponent<Image>();
            var backgroundColor = UIStyleTokens.Surface.PanelPrimary;
            backgroundColor.a = CampaignDraftUtilityPanelAlpha;
            rootImage.color = backgroundColor;
            rootImage.raycastTarget = true;

            var layout = campaignAdaptationUtilityRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var statusObject = new GameObject("Status", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            statusObject.transform.SetParent(campaignAdaptationUtilityRoot.transform, false);
            var statusLayout = statusObject.GetComponent<LayoutElement>();
            statusLayout.preferredWidth = CampaignAdaptationUtilityStatusWidth;
            statusLayout.minWidth = CampaignAdaptationUtilityStatusWidth;
            statusLayout.preferredHeight = 28f;
            statusLayout.minHeight = 28f;
            campaignAdaptationUtilityStatusText = statusObject.GetComponent<TextMeshProUGUI>();
            campaignAdaptationUtilityStatusText.fontSize = 14f;
            campaignAdaptationUtilityStatusText.enableAutoSizing = true;
            campaignAdaptationUtilityStatusText.fontSizeMin = 12f;
            campaignAdaptationUtilityStatusText.fontSizeMax = 14f;
            campaignAdaptationUtilityStatusText.alignment = TextAlignmentOptions.Center;
            campaignAdaptationUtilityStatusText.textWrappingMode = TextWrappingModes.Normal;
            campaignAdaptationUtilityStatusText.overflowMode = TextOverflowModes.Overflow;
            campaignAdaptationUtilityStatusText.color = UIStyleTokens.Text.Secondary;

            var buttonObject = new GameObject("RedrawButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(campaignAdaptationUtilityRoot.transform, false);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(CampaignDraftUtilityButtonWidth, CampaignDraftUtilityButtonHeight);
            var buttonLayout = buttonObject.GetComponent<LayoutElement>();
            buttonLayout.preferredWidth = CampaignDraftUtilityButtonWidth;
            buttonLayout.minWidth = CampaignDraftUtilityButtonWidth;
            buttonLayout.preferredHeight = CampaignDraftUtilityButtonHeight;
            buttonLayout.minHeight = CampaignDraftUtilityButtonHeight;
            campaignAdaptationRedrawButton = buttonObject.GetComponent<Button>();
            campaignAdaptationRedrawButton.targetGraphic = buttonObject.GetComponent<Image>();
            UIStyleTokens.Button.ApplySecondaryMenuAction(
                campaignAdaptationRedrawButton,
                CampaignDraftUtilityButtonWidth,
                preferredHeight: CampaignDraftUtilityButtonHeight,
                minHeight: CampaignDraftUtilityButtonHeight);
            campaignAdaptationRedrawButton.onClick.AddListener(OnCampaignAdaptationRedrawButtonClicked);

            var redrawTooltipTrigger = buttonObject.GetComponent<TooltipTrigger>();
            if (redrawTooltipTrigger == null)
            {
                redrawTooltipTrigger = buttonObject.AddComponent<TooltipTrigger>();
            }

            redrawTooltipTrigger.SetStaticText(CampaignAdaptationRedrawTooltipText);

            var buttonLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            buttonLabelObject.transform.SetParent(buttonObject.transform, false);
            var buttonLabelRect = buttonLabelObject.GetComponent<RectTransform>();
            buttonLabelRect.anchorMin = Vector2.zero;
            buttonLabelRect.anchorMax = Vector2.one;
            buttonLabelRect.offsetMin = new Vector2(8f, 4f);
            buttonLabelRect.offsetMax = new Vector2(-8f, -4f);
            campaignAdaptationRedrawButtonText = buttonLabelObject.GetComponent<TextMeshProUGUI>();
            campaignAdaptationRedrawButtonText.alignment = TextAlignmentOptions.Center;
            campaignAdaptationRedrawButtonText.fontSize = 16f;
            campaignAdaptationRedrawButtonText.enableAutoSizing = true;
            campaignAdaptationRedrawButtonText.fontSizeMin = 13f;
            campaignAdaptationRedrawButtonText.fontSizeMax = 16f;
            campaignAdaptationRedrawButtonText.textWrappingMode = TextWrappingModes.NoWrap;
            TMPOverflowUtility.SetSafeEllipsis(campaignAdaptationRedrawButtonText);
            campaignAdaptationRedrawButtonText.color = UIStyleTokens.Text.Primary;
            campaignAdaptationRedrawButtonText.raycastTarget = false;
            UIStyleTokens.Button.SetButtonLabelColor(campaignAdaptationRedrawButton, UIStyleTokens.Text.Primary);

            campaignAdaptationUtilityRoot.SetActive(false);
        }

        private void EnsureCampaignAdaptationSummaryUi()
        {
            if (choiceContainer == null || choiceContainer.parent == null || campaignAdaptationSummaryRoot != null)
            {
                return;
            }

            campaignAdaptationSummaryRoot = new GameObject(
                "CampaignAdaptationSummaryRoot",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(VerticalLayoutGroup));
            campaignAdaptationSummaryRoot.transform.SetParent(choiceContainer.parent, false);

            var rootRect = campaignAdaptationSummaryRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(CampaignAdaptationSummaryPanelWidth, CampaignAdaptationSummaryPanelMinHeight);

            var layoutElement = campaignAdaptationSummaryRoot.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = CampaignAdaptationSummaryPanelWidth;
            layoutElement.minWidth = CampaignAdaptationSummaryPanelWidth;
            layoutElement.minHeight = CampaignAdaptationSummaryPanelMinHeight;

            var rootImage = campaignAdaptationSummaryRoot.GetComponent<Image>();
            var backgroundColor = UIStyleTokens.Surface.PanelSecondary;
            backgroundColor.a = 0.94f;
            rootImage.color = backgroundColor;
            rootImage.raycastTarget = true;

            var layout = campaignAdaptationSummaryRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(16, 16, 10, 12);
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(campaignAdaptationSummaryRoot.transform, false);
            var titleLayout = titleObject.GetComponent<LayoutElement>();
            titleLayout.preferredHeight = 20f;
            titleLayout.minHeight = 20f;
            campaignAdaptationSummaryTitleText = titleObject.GetComponent<TextMeshProUGUI>();
            campaignAdaptationSummaryTitleText.text = CampaignAdaptationSummaryTitle;
            campaignAdaptationSummaryTitleText.fontSize = 15f;
            campaignAdaptationSummaryTitleText.fontStyle = FontStyles.Bold;
            campaignAdaptationSummaryTitleText.color = UIStyleTokens.Text.Primary;
            campaignAdaptationSummaryTitleText.alignment = TextAlignmentOptions.Left;
            campaignAdaptationSummaryTitleText.textWrappingMode = TextWrappingModes.NoWrap;

            var iconRowObject = new GameObject(
                "IconRow",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(HorizontalLayoutGroup));
            iconRowObject.transform.SetParent(campaignAdaptationSummaryRoot.transform, false);
            campaignAdaptationSummaryIconRow = iconRowObject.GetComponent<RectTransform>();
            var iconRowLayout = iconRowObject.GetComponent<LayoutElement>();
            iconRowLayout.preferredHeight = CampaignAdaptationSummaryIconSize;
            iconRowLayout.minHeight = CampaignAdaptationSummaryIconSize;

            var horizontalLayout = iconRowObject.GetComponent<HorizontalLayoutGroup>();
            horizontalLayout.spacing = 6f;
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childControlWidth = false;
            horizontalLayout.childControlHeight = false;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childForceExpandHeight = false;

            campaignAdaptationSummaryRoot.SetActive(false);
        }

        private void PopulateCampaignAdaptationSummary()
        {
            EnsureCampaignAdaptationSummaryUi();
            ClearCampaignAdaptationSummaryIcons();

            if (campaignAdaptationSummaryRoot == null || campaignAdaptationSummaryIconRow == null)
            {
                return;
            }

            var currentAdaptations = ResolveCurrentCampaignAdaptations();
            bool shouldShow = isCampaignAdaptationDraft && currentAdaptations.Count > 0;
            campaignAdaptationSummaryRoot.SetActive(shouldShow);
            if (!shouldShow)
            {
                return;
            }

            campaignAdaptationSummaryRoot.transform.SetSiblingIndex(choiceContainer.GetSiblingIndex() + 1);
            if (campaignAdaptationSummaryTitleText != null)
            {
                campaignAdaptationSummaryTitleText.text = $"{CampaignAdaptationSummaryTitle} ({currentAdaptations.Count})";
            }

            for (int i = 0; i < currentAdaptations.Count; i++)
            {
                CreateCampaignAdaptationSummaryIcon(currentAdaptations[i]);
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(campaignAdaptationSummaryIconRow);
            LayoutRebuilder.ForceRebuildLayoutImmediate(campaignAdaptationSummaryRoot.GetComponent<RectTransform>());
        }

        private List<AdaptationDefinition> ResolveCurrentCampaignAdaptations()
        {
            var resolvedAdaptations = new List<AdaptationDefinition>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);

            var selectedIds = GameManager.Instance?.CampaignController?.State?.selectedAdaptationIds;
            if (selectedIds != null)
            {
                for (int i = 0; i < selectedIds.Count; i++)
                {
                    string adaptationId = selectedIds[i];
                    if (string.IsNullOrWhiteSpace(adaptationId) || !seenIds.Add(adaptationId))
                    {
                        continue;
                    }

                    if (AdaptationRepository.TryGetById(adaptationId, out var adaptation))
                    {
                        resolvedAdaptations.Add(adaptation);
                    }
                }
            }

            if (resolvedAdaptations.Count > 0)
            {
                return resolvedAdaptations;
            }

            var humanPlayer = GameManager.Instance?.Board?.Players?.FirstOrDefault(player => player.PlayerType == PlayerTypeEnum.Human);
            if (humanPlayer?.PlayerAdaptations == null)
            {
                return resolvedAdaptations;
            }

            for (int i = 0; i < humanPlayer.PlayerAdaptations.Count; i++)
            {
                var playerAdaptation = humanPlayer.PlayerAdaptations[i];
                var adaptation = playerAdaptation?.Adaptation;
                if (adaptation == null || !seenIds.Add(adaptation.Id))
                {
                    continue;
                }

                resolvedAdaptations.Add(adaptation);
            }

            return resolvedAdaptations;
        }

        private void CreateCampaignAdaptationSummaryIcon(AdaptationDefinition adaptation)
        {
            if (campaignAdaptationSummaryIconRow == null || adaptation == null)
            {
                return;
            }

            var iconObject = new GameObject(
                $"CampaignAdaptation_{adaptation.Id}",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(Image));
            iconObject.transform.SetParent(campaignAdaptationSummaryIconRow, false);
            campaignAdaptationSummaryIconObjects.Add(iconObject);

            var rect = iconObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(CampaignAdaptationSummaryIconSize, CampaignAdaptationSummaryIconSize);

            var layout = iconObject.GetComponent<LayoutElement>();
            layout.preferredWidth = CampaignAdaptationSummaryIconSize;
            layout.preferredHeight = CampaignAdaptationSummaryIconSize;
            layout.minWidth = CampaignAdaptationSummaryIconSize;
            layout.minHeight = CampaignAdaptationSummaryIconSize;

            var background = iconObject.GetComponent<Image>();
            var backgroundColor = UIStyleTokens.Surface.PanelPrimary;
            backgroundColor.a = 0.9f;
            background.color = backgroundColor;
            background.raycastTarget = true;

            var artObject = new GameObject("Art", typeof(RectTransform), typeof(Image));
            artObject.transform.SetParent(iconObject.transform, false);
            var artRect = artObject.GetComponent<RectTransform>();
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = new Vector2(CampaignAdaptationSummaryIconPadding, CampaignAdaptationSummaryIconPadding);
            artRect.offsetMax = new Vector2(-CampaignAdaptationSummaryIconPadding, -CampaignAdaptationSummaryIconPadding);

            var artImage = artObject.GetComponent<Image>();
            artImage.sprite = AdaptationArtRepository.GetIcon(adaptation);
            artImage.preserveAspect = true;
            artImage.enabled = artImage.sprite != null;
            artImage.raycastTarget = false;

            var provider = iconObject.AddComponent<AdaptationTooltipProvider>();
            provider.Initialize(adaptation);

            var trigger = iconObject.GetComponent<TooltipTrigger>() ?? iconObject.AddComponent<TooltipTrigger>();
            trigger.SetDynamicProvider(provider);
            trigger.SetAutoPlacementOffsetX(20f);
        }

        private void ClearCampaignAdaptationSummaryIcons()
        {
            for (int i = 0; i < campaignAdaptationSummaryIconObjects.Count; i++)
            {
                if (campaignAdaptationSummaryIconObjects[i] != null)
                {
                    Destroy(campaignAdaptationSummaryIconObjects[i]);
                }
            }

            campaignAdaptationSummaryIconObjects.Clear();
        }

        private void RefreshCampaignAdaptationUtilityUi()
        {
            bool showingAdaptationRedraw = isCampaignAdaptationDraft && showCampaignAdaptationRedrawControl;
            bool showingMycovariantRedraw = !isCampaignAdaptationDraft
                && showCampaignMycovariantRedrawControl
                && currentPlayer?.PlayerType == PlayerTypeEnum.Human;
            bool shouldShow = showingAdaptationRedraw || showingMycovariantRedraw;

            if (!shouldShow)
            {
                campaignAdaptationRedrawConfirmArmed = false;
                campaignMycovariantRedrawConfirmArmed = false;
            }

            if (campaignAdaptationUtilityRoot != null)
            {
                if (choiceContainer != null)
                {
                    int choiceIndex = choiceContainer.GetSiblingIndex();
                    int utilityIndex = campaignAdaptationUtilityRoot.transform.GetSiblingIndex();
                    int targetIndex = utilityIndex < choiceIndex ? Mathf.Max(0, choiceIndex - 1) : choiceIndex;
                    campaignAdaptationUtilityRoot.transform.SetSiblingIndex(targetIndex);
                }

                campaignAdaptationUtilityRoot.SetActive(shouldShow);
            }

            if (!shouldShow || campaignAdaptationUtilityStatusText == null || campaignAdaptationRedrawButton == null || campaignAdaptationRedrawButtonText == null)
            {
                return;
            }

            bool redrawAvailable = showingAdaptationRedraw
                ? campaignAdaptationRedrawAvailable
                : campaignMycovariantRedrawAvailable;
            bool confirmArmed = showingAdaptationRedraw
                ? campaignAdaptationRedrawConfirmArmed
                : campaignMycovariantRedrawConfirmArmed;

            bool canInteract = redrawAvailable && uiState == DraftUIState.HumanTurn;
            if (!canInteract)
            {
                if (showingAdaptationRedraw)
                {
                    campaignAdaptationRedrawConfirmArmed = false;
                    confirmArmed = false;
                }
                else
                {
                    campaignMycovariantRedrawConfirmArmed = false;
                    confirmArmed = false;
                }
            }

            string buttonLabel;
            string statusText;
            Color statusColor;
            if (!redrawAvailable)
            {
                buttonLabel = CampaignAdaptationRedrawUsedLabel;
                statusText = "Spore Sifting has already been used for this campaign level.";
                statusColor = UIStyleTokens.Text.Muted;
            }
            else if (confirmArmed)
            {
                buttonLabel = CampaignAdaptationRedrawConfirmLabel;
                statusText = "Click again to redraw all 3 Mycovariants. This spends Spore Sifting for this level.";
                statusColor = UIStyleTokens.State.Warning;
            }
            else
            {
                buttonLabel = CampaignAdaptationRedrawReadyLabel;
                statusText = "Spore Sifting ready: redraw the entire 3-card offer once before you pick.";
                statusColor = UIStyleTokens.Text.Secondary;
            }

            campaignAdaptationRedrawButton.interactable = canInteract;
            campaignAdaptationRedrawButtonText.text = buttonLabel;
            campaignAdaptationUtilityStatusText.text = statusText;
            campaignAdaptationUtilityStatusText.color = statusColor;
        }

        private void OnCampaignAdaptationRedrawButtonClicked()
        {
            if (!isCampaignAdaptationDraft)
            {
                OnCampaignMycovariantRedrawButtonClicked();
                return;
            }

            if (!isCampaignAdaptationDraft || !showCampaignAdaptationRedrawControl || !campaignAdaptationRedrawAvailable || uiState != DraftUIState.HumanTurn)
            {
                return;
            }

            if (!campaignAdaptationRedrawConfirmArmed)
            {
                campaignAdaptationRedrawConfirmArmed = true;
                RefreshCampaignAdaptationUtilityUi();
                return;
            }

            var redrawnChoices = onCampaignAdaptationRedrawRequested?.Invoke();
            campaignAdaptationRedrawConfirmArmed = false;
            if (redrawnChoices == null || redrawnChoices.Count == 0)
            {
                AddDraftMessage("Spore Sifting failed to redraw the current offer.");
                RefreshCampaignAdaptationUtilityUi();
                return;
            }

            campaignAdaptationRedrawAvailable = false;
            PopulateAdaptationChoices(redrawnChoices);
            AddDraftMessage("Spore Sifting scatters the old offer. A fresh 3-card Mycovariant draft blooms.");
            SetDraftState(DraftUIState.HumanTurn);
        }

        private void OnCampaignMycovariantRedrawButtonClicked()
        {
            if (isCampaignAdaptationDraft
                || !showCampaignMycovariantRedrawControl
                || !campaignMycovariantRedrawAvailable
                || uiState != DraftUIState.HumanTurn
                || currentPlayer?.PlayerType != PlayerTypeEnum.Human)
            {
                return;
            }

            if (!campaignMycovariantRedrawConfirmArmed)
            {
                campaignMycovariantRedrawConfirmArmed = true;
                RefreshCampaignAdaptationUtilityUi();
                return;
            }

            var redrawnChoices = TryBuildCampaignMycovariantRedrawChoices();
            campaignMycovariantRedrawConfirmArmed = false;
            if (redrawnChoices == null || redrawnChoices.Count == 0)
            {
                AddDraftMessage("Spore Sifting failed to redraw the current offer.");
                RefreshCampaignAdaptationUtilityUi();
                return;
            }

            var campaignController = GameManager.Instance?.CampaignController;
            if (campaignController == null || !campaignController.TryConsumeCampaignMycovariantDraftRedraw())
            {
                AddDraftMessage("Spore Sifting could not be spent for this draft.");
                RefreshCampaignAdaptationUtilityUi();
                return;
            }

            campaignMycovariantRedrawAvailable = false;
            AddDraftMessage("Spore Sifting scatters the old offer. A fresh 3-card Mycovariant draft blooms.");
            EnterDraftTurn(redrawnChoices, repopulateChoices: true);
        }

        private List<Mycovariant> TryBuildCampaignMycovariantRedrawChoices()
        {
            var currentOfferIds = draftChoices?
                .Where(choice => choice != null)
                .Select(choice => choice.Id)
                .ToHashSet()
                ?? new HashSet<int>();

            if (currentOfferIds.Count == 0)
            {
                return new List<Mycovariant>();
            }

            for (int attempt = 0; attempt < CampaignMycovariantRedrawRetryCount; attempt++)
            {
                var redrawnChoices = GetFreshDraftChoicesForCurrentPlayer();
                if (redrawnChoices.Count == 0)
                {
                    return new List<Mycovariant>();
                }

                if (!ChoiceSetMatches(redrawnChoices, currentOfferIds))
                {
                    return redrawnChoices;
                }
            }

            return new List<Mycovariant>();
        }

        private static bool ChoiceSetMatches(IReadOnlyCollection<Mycovariant> choices, ISet<int> offeredIds)
        {
            if (choices == null || offeredIds == null || choices.Count != offeredIds.Count)
            {
                return false;
            }

            foreach (var choice in choices)
            {
                if (choice == null || !offeredIds.Contains(choice.Id))
                {
                    return false;
                }
            }

            return true;
        }

        private void SetDraftMessagePanelTitle(string title)
        {
            if (draftMessageTitleText == null)
            {
                return;
            }

            draftMessageTitleText.text = string.IsNullOrWhiteSpace(title) ? DraftFeedTitle : title;
        }

        private string GetCampaignAdaptationDraftFeedTitle()
        {
            int nextLevelDisplay = 1;
            var campaignController = GameManager.Instance?.CampaignController;

            if (campaignController?.State?.levelIndex >= 0)
            {
                nextLevelDisplay = campaignController.State.levelIndex + 2;
            }
            else if (campaignController != null && campaignController.TryGetPendingVictorySnapshot(out var pendingSnapshot) && pendingSnapshot != null)
            {
                nextLevelDisplay = pendingSnapshot.clearedLevelDisplay + 1;
            }

            return $"Level {nextLevelDisplay} Victory!";
        }

        private void EnsureDraftMessageUI()
        {
            if (draftMessagePanel != null && draftFeedScrollRect != null)
                return;

            Transform parent = draftPanel != null ? draftPanel.transform.parent : transform;

            if (draftMessagePanel == null)
            {
                draftMessagePanel = new GameObject("DraftMessagePanel", typeof(RectTransform), typeof(Image));
                draftMessagePanel.transform.SetParent(parent, false);

                var panelRect = draftMessagePanel.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0f, 0f);
                panelRect.anchorMax = new Vector2(0f, 1f);
                panelRect.offsetMin = new Vector2(20f, 70f);
                panelRect.offsetMax = new Vector2(360f, -70f);

                var panelImage = draftMessagePanel.GetComponent<Image>();
                panelImage.color = UIStyleTokens.Surface.PanelPrimary;

                draftMessagePanel.SetActive(false);
                draftMessagePanel.transform.SetAsLastSibling();
            }

            if (draftMessageTitleText == null)
            {
                var titleGO = new GameObject("DraftMessageTitle", typeof(RectTransform));
                titleGO.transform.SetParent(draftMessagePanel.transform, false);
                draftMessageTitleText = titleGO.AddComponent<TextMeshProUGUI>();
                draftMessageTitleText.text = DraftFeedTitle;
                draftMessageTitleText.fontSize = 28f;
                draftMessageTitleText.fontStyle = FontStyles.Bold;
                draftMessageTitleText.color = UIStyleTokens.Text.Primary;
                draftMessageTitleText.alignment = TextAlignmentOptions.TopLeft;

                var titleRect = titleGO.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0f, 1f);
                titleRect.anchorMax = new Vector2(1f, 1f);
                titleRect.offsetMin = new Vector2(14f, -44f);
                titleRect.offsetMax = new Vector2(-14f, -10f);
            }

            if (draftFeedScrollRect == null)
            {
                var scrollGO = new GameObject("DraftFeedScrollView", typeof(RectTransform));
                scrollGO.transform.SetParent(draftMessagePanel.transform, false);

                draftFeedScrollRect = scrollGO.AddComponent<ScrollRect>();
                draftFeedScrollRect.horizontal = false;
                draftFeedScrollRect.vertical = true;
                draftFeedScrollRect.scrollSensitivity = 30f;
                draftFeedScrollRect.movementType = ScrollRect.MovementType.Clamped;

                var scrollRectTransform = scrollGO.GetComponent<RectTransform>();
                scrollRectTransform.anchorMin = new Vector2(0f, 0f);
                scrollRectTransform.anchorMax = new Vector2(1f, 1f);
                scrollRectTransform.offsetMin = new Vector2(0f, 0f);
                scrollRectTransform.offsetMax = new Vector2(0f, -52f);

                var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
                viewportGO.transform.SetParent(scrollGO.transform, false);

                var vpImage = viewportGO.GetComponent<Image>();
                vpImage.color = new Color(0f, 0f, 0f, 0.01f);

                var vpMask = viewportGO.GetComponent<Mask>();
                vpMask.showMaskGraphic = false;

                var vpRect = viewportGO.GetComponent<RectTransform>();
                vpRect.anchorMin = Vector2.zero;
                vpRect.anchorMax = Vector2.one;
                vpRect.offsetMin = Vector2.zero;
                vpRect.offsetMax = Vector2.zero;
                vpRect.pivot = new Vector2(0f, 1f);

                draftFeedScrollRect.viewport = vpRect;

                var contentGO = new GameObject("Content", typeof(RectTransform));
                contentGO.transform.SetParent(viewportGO.transform, false);

                var contentRect = contentGO.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0f, 1f);
                contentRect.offsetMin = new Vector2(0f, 0f);
                contentRect.offsetMax = new Vector2(0f, 0f);

                var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childForceExpandHeight = false;
                contentLayout.spacing = 0f;
                contentLayout.padding = new RectOffset(0, 0, 0, 0);

                var contentSizer = contentGO.AddComponent<ContentSizeFitter>();
                contentSizer.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentSizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                draftFeedContentTransform = contentGO.transform;
                draftFeedScrollRect.content = contentRect;
            }

            EnsureCampaignAdaptationUtilityUi();
        }

        private void ClearDraftMessages()
        {
            if (draftFeedContentTransform != null)
            {
                foreach (Transform child in draftFeedContentTransform)
                    Destroy(child.gameObject);
            }
            draftFeedEntryCount = 0;
            currentPlayerEntryText = null;
        }

        private void ClearDraftHistory()
        {
            draftHistoryEntries.Clear();
            HideDraftHistoryOverlay();
            RebuildDraftHistoryOverlay();
        }

        private void AddDraftMessage(string message)
        {
            AddGenericFeedEntry(message);
        }

        private void AddGenericFeedEntry(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || draftFeedContentTransform == null)
                return;

            currentPlayerEntryText = null;
            CreateFeedRow(null, message);
            StartCoroutine(ScrollToBottomNextFrame());
        }

        private void AddPlayerFeedEntry(Player player, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || draftFeedContentTransform == null)
                return;

            var sprite = gridVisualizer != null
                ? gridVisualizer.GetMoldIconTileForPlayer(player.PlayerId)?.sprite
                : null;

            var row = CreateFeedRow(sprite, message);
            currentPlayerEntryText = row.GetComponentInChildren<TextMeshProUGUI>();
            StartCoroutine(ScrollToBottomNextFrame());
        }

        private void AppendToCurrentPlayerEntry(string appendText)
        {
            if (string.IsNullOrWhiteSpace(appendText))
                return;

            if (currentPlayerEntryText != null)
            {
                currentPlayerEntryText.text += "\n" + appendText;
                StartCoroutine(ScrollToBottomNextFrame());
            }
            else
            {
                AddGenericFeedEntry(appendText);
            }
        }

        private void ReplaceCurrentPlayerEntry(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || currentPlayerEntryText == null)
            {
                return;
            }

            currentPlayerEntryText.text = message;
            StartCoroutine(ScrollToBottomNextFrame());
        }

        private GameObject CreateFeedRow(Sprite icon, string message)
        {
            bool isEven = draftFeedEntryCount % 2 == 0;
            Color bgColor = isEven ? UIStyleTokens.Surface.PanelSecondary : UIStyleTokens.Surface.PanelPrimary;
            draftFeedEntryCount++;

            var rowGO = new GameObject($"DraftFeedRow{draftFeedEntryCount}", typeof(RectTransform), typeof(Image));
            rowGO.transform.SetParent(draftFeedContentTransform, false);

            var rowImage = rowGO.GetComponent<Image>();
            rowImage.color = bgColor;

            var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = FeedRowSpacing;
            rowLayout.padding = new RectOffset(FeedRowPadding, FeedRowPadding, FeedRowPadding, FeedRowPadding);

            var rowSizer = rowGO.AddComponent<ContentSizeFitter>();
            rowSizer.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rowSizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (icon != null)
            {
                var iconGO = new GameObject("PlayerIcon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(rowGO.transform, false);

                var iconImage = iconGO.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;

                var iconLayout = iconGO.AddComponent<LayoutElement>();
                iconLayout.minWidth = FeedIconSize;
                iconLayout.preferredWidth = FeedIconSize;
                iconLayout.minHeight = FeedIconSize;
                iconLayout.preferredHeight = FeedIconSize;
                iconLayout.flexibleWidth = 0f;
                iconLayout.flexibleHeight = 0f;
            }
            else
            {
                var spacerGO = new GameObject("IconSpacer", typeof(RectTransform));
                spacerGO.transform.SetParent(rowGO.transform, false);

                var spacerLayout = spacerGO.AddComponent<LayoutElement>();
                spacerLayout.minWidth = FeedIconSpacerWidth;
                spacerLayout.preferredWidth = FeedIconSpacerWidth;
                spacerLayout.flexibleWidth = 0f;
            }

            var textGO = new GameObject("EntryText", typeof(RectTransform));
            textGO.transform.SetParent(rowGO.transform, false);

            var textComp = textGO.AddComponent<TextMeshProUGUI>();
            textComp.text = message;
            textComp.fontSize = FeedEntryFontSize;
            textComp.color = UIStyleTokens.Text.Secondary;
            textComp.alignment = TextAlignmentOptions.TopLeft;
            textComp.textWrappingMode = TextWrappingModes.Normal;
            textComp.overflowMode = TextOverflowModes.Overflow;

            var textLayout = textGO.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;

            return rowGO;
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null;
            if (draftFeedScrollRect != null)
                draftFeedScrollRect.normalizedPosition = new Vector2(0f, 0f);
        }

        private void TryAnnounceAscusPrimacyDraftPriority()
        {
            if (draftOrder == null || draftOrder.Count == 0)
            {
                return;
            }

            var firstPlayer = draftOrder[0];
            if (firstPlayer == null
                || firstPlayer.PlayerType != PlayerTypeEnum.Human
                || !firstPlayer.HasAdaptation(AdaptationIds.AscusPrimacy))
            {
                return;
            }

            AddDraftMessage("Ascus Primacy allows you to draft first!");
            GameManager.Instance?.GameUI?.GameLogRouter?.RecordAscusPrimacyDraftPriority(firstPlayer.PlayerId);
        }

        private string AddDraftResultMessage(Player player, Mycovariant picked, PlayerMycovariant playerMyco)
        {
            string resultMessage = BuildDraftResultMessage(player, picked, playerMyco);
            if (!string.IsNullOrWhiteSpace(resultMessage))
            {
                AppendToCurrentPlayerEntry(resultMessage);
            }

            return resultMessage;
        }

        private static string BuildDraftResultMessage(Player player, Mycovariant picked, PlayerMycovariant playerMyco)
        {
            if (playerMyco == null)
            {
                return $"Mycelial pulse: {picked.Name} resolved.";
            }

            string countSummary = BuildEffectCountSummary(playerMyco);
            if (!string.IsNullOrEmpty(countSummary))
            {
                return $"Impact: {countSummary}.";
            }

            if (picked.Id == MycovariantIds.PlasmidBountyId)
            {
                return $"Plasmids absorbed: +{MycovariantGameBalance.PlasmidBountyMutationPointAward} mutation points.";
            }

            if (picked.Id == MycovariantIds.PlasmidBountyIIId)
            {
                return $"Plasmids absorbed: +{MycovariantGameBalance.PlasmidBountyIIMutationPointAward} mutation points.";
            }

            if (picked.Id == MycovariantIds.PlasmidBountyIIIId)
            {
                return $"Plasmids absorbed: +{MycovariantGameBalance.PlasmidBountyIIIMutationPointAward} mutation points.";
            }

            if (picked.Type == MycovariantType.Passive)
            {
                return "Passive trait established for the rest of the game.";
            }

            return $"Effect resolved: {picked.Name}.";
        }

        private void RecordDraftHistoryEntry(Player player, string announcement, string detail)
        {
            if (player == null || string.IsNullOrWhiteSpace(announcement))
            {
                return;
            }

            int round = GameManager.Instance?.Board?.CurrentRound ?? -1;
            draftHistoryEntries.Add(new DraftHistoryEntry
            {
                Round = round,
                PlayerId = player.PlayerId,
                Announcement = announcement,
                Detail = detail ?? string.Empty
            });

            if (draftHistoryOverlayRoot != null && draftHistoryOverlayRoot.gameObject.activeSelf)
            {
                RebuildDraftHistoryOverlay();
            }
        }

        private void EnsureDraftHistoryOverlayUI()
        {
            if (draftHistoryOverlayRoot != null)
            {
                return;
            }

            Transform parent = draftPanel != null
                ? draftPanel.GetComponentInParent<Canvas>()?.transform
                : transform.parent;

            if (parent == null)
            {
                return;
            }

            var overlayRootObject = new GameObject("UI_MycovariantDraftHistoryOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            overlayRootObject.transform.SetParent(parent, false);

            draftHistoryOverlayRoot = overlayRootObject.GetComponent<RectTransform>();
            draftHistoryOverlayRoot.anchorMin = Vector2.zero;
            draftHistoryOverlayRoot.anchorMax = Vector2.one;
            draftHistoryOverlayRoot.offsetMin = Vector2.zero;
            draftHistoryOverlayRoot.offsetMax = Vector2.zero;

            draftHistoryOverlayCanvasGroup = overlayRootObject.GetComponent<CanvasGroup>();
            draftHistoryOverlayCanvasGroup.alpha = 0f;
            draftHistoryOverlayCanvasGroup.blocksRaycasts = false;
            draftHistoryOverlayCanvasGroup.interactable = false;

            var overlayBackground = overlayRootObject.GetComponent<Image>();
            overlayBackground.color = UIStyleTokens.Surface.OverlayDim;
            overlayBackground.raycastTarget = true;

            overlayRootObject.AddComponent<ModalScrollInterceptor>();

            var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
            panelObject.transform.SetParent(overlayRootObject.transform, false);

            var panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(DraftHistoryOverlayWidth, DraftHistoryOverlayHeight);
            panelRect.anchoredPosition = Vector2.zero;

            var panelImage = panelObject.GetComponent<Image>();
            panelImage.color = UIStyleTokens.Surface.PanelPrimary;

            var panelOutline = panelObject.GetComponent<Outline>();
            panelOutline.effectColor = new Color(UIStyleTokens.State.Focus.r, UIStyleTokens.State.Focus.g, UIStyleTokens.State.Focus.b, 0.8f);
            panelOutline.effectDistance = new Vector2(1f, -1f);

            var panelScrollInterceptor = panelObject.AddComponent<ModalScrollInterceptor>();

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(panelObject.transform, false);

            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(22f, -58f);
            titleRect.offsetMax = new Vector2(-90f, -16f);

            var titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.text = DraftHistoryOverlayTitle;
            titleText.color = UIStyleTokens.Text.Primary;
            titleText.fontSize = 30f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Left;
            titleText.raycastTarget = false;

            var closeObject = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeObject.transform.SetParent(panelObject.transform, false);

            var closeRect = closeObject.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 1f);
            closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.sizeDelta = new Vector2(40f, 40f);
            closeRect.anchoredPosition = new Vector2(-18f, -18f);

            draftHistoryCloseButton = closeObject.GetComponent<Button>();
            UIStyleTokens.Button.ApplyPanelSecondaryStyle(draftHistoryCloseButton);
            draftHistoryCloseButton.onClick.RemoveAllListeners();
            draftHistoryCloseButton.onClick.AddListener(HideDraftHistoryOverlay);

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
            closeLabel.fontSize = 22f;
            closeLabel.fontStyle = FontStyles.Bold;
            closeLabel.alignment = TextAlignmentOptions.Center;
            closeLabel.raycastTarget = false;

            UIStyleTokens.Button.SetButtonLabelColor(draftHistoryCloseButton, UIStyleTokens.Text.Primary);

            var scrollObject = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObject.transform.SetParent(panelObject.transform, false);

            var scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(18f, 18f);
            scrollRectTransform.offsetMax = new Vector2(-18f, -74f);

            draftHistoryOverlayScrollRect = scrollObject.GetComponent<ScrollRect>();
            draftHistoryOverlayScrollRect.horizontal = false;
            draftHistoryOverlayScrollRect.vertical = true;
            draftHistoryOverlayScrollRect.scrollSensitivity = 30f;
            draftHistoryOverlayScrollRect.movementType = ScrollRect.MovementType.Clamped;
            panelScrollInterceptor.TargetScrollRect = draftHistoryOverlayScrollRect;

            var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, false);

            var viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);

            var viewportMask = viewportObject.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            draftHistoryOverlayScrollRect.viewport = viewportRect;

            var contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewportObject.transform, false);

            var contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var contentLayout = contentObject.GetComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 6f;
            contentLayout.padding = new RectOffset(0, 0, 0, 0);

            var contentFitter = contentObject.GetComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            draftHistoryOverlayContentTransform = contentObject.transform;
            draftHistoryOverlayScrollRect.content = contentRect;

            var emptyStateObject = new GameObject("EmptyState", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyStateObject.transform.SetParent(panelObject.transform, false);

            var emptyStateRect = emptyStateObject.GetComponent<RectTransform>();
            emptyStateRect.anchorMin = new Vector2(0f, 0f);
            emptyStateRect.anchorMax = new Vector2(1f, 1f);
            emptyStateRect.offsetMin = new Vector2(48f, 48f);
            emptyStateRect.offsetMax = new Vector2(-48f, -96f);

            draftHistoryEmptyStateText = emptyStateObject.GetComponent<TextMeshProUGUI>();
            draftHistoryEmptyStateText.text = "No draft history recorded yet.";
            draftHistoryEmptyStateText.color = UIStyleTokens.Text.Secondary;
            draftHistoryEmptyStateText.fontSize = 24f;
            draftHistoryEmptyStateText.alignment = TextAlignmentOptions.Center;
            draftHistoryEmptyStateText.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                titleText.font = TMP_Settings.defaultFontAsset;
                closeLabel.font = TMP_Settings.defaultFontAsset;
                draftHistoryEmptyStateText.font = TMP_Settings.defaultFontAsset;
            }

            overlayRootObject.SetActive(false);
        }

        private void RebuildDraftHistoryOverlay()
        {
            if (draftHistoryOverlayContentTransform == null || draftHistoryEmptyStateText == null)
            {
                return;
            }

            foreach (Transform child in draftHistoryOverlayContentTransform)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < draftHistoryEntries.Count; i++)
            {
                CreateDraftHistoryRow(draftHistoryEntries[i], i);
            }

            bool hasEntries = draftHistoryEntries.Count > 0;
            draftHistoryEmptyStateText.gameObject.SetActive(!hasEntries);
            if (draftHistoryOverlayScrollRect != null)
            {
                draftHistoryOverlayScrollRect.gameObject.SetActive(hasEntries);
            }
        }

        private void CreateDraftHistoryRow(DraftHistoryEntry entry, int entryIndex)
        {
            if (draftHistoryOverlayContentTransform == null)
            {
                return;
            }

            Color bgColor = entryIndex % 2 == 0 ? UIStyleTokens.Surface.PanelSecondary : UIStyleTokens.Surface.PanelPrimary;

            var rowObject = new GameObject($"DraftHistoryRow{entryIndex + 1}", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            rowObject.transform.SetParent(draftHistoryOverlayContentTransform, false);

            var rowImage = rowObject.GetComponent<Image>();
            rowImage.color = bgColor;

            var rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.spacing = FeedRowSpacing;
            rowLayout.padding = new RectOffset(FeedRowPadding + 4, FeedRowPadding + 4, FeedRowPadding + 4, FeedRowPadding + 4);

            var rowFitter = rowObject.GetComponent<ContentSizeFitter>();
            rowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var iconSprite = gridVisualizer != null
                ? gridVisualizer.GetMoldIconTileForPlayer(entry.PlayerId)?.sprite
                : null;

            if (iconSprite != null)
            {
                var iconObject = new GameObject("PlayerIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconObject.transform.SetParent(rowObject.transform, false);

                var iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = iconSprite;
                iconImage.preserveAspect = true;

                var iconLayout = iconObject.GetComponent<LayoutElement>();
                iconLayout.preferredWidth = FeedIconSize;
                iconLayout.preferredHeight = FeedIconSize;
                iconLayout.minWidth = FeedIconSize;
                iconLayout.minHeight = FeedIconSize;
            }
            else
            {
                var spacerObject = new GameObject("IconSpacer", typeof(RectTransform), typeof(LayoutElement));
                spacerObject.transform.SetParent(rowObject.transform, false);

                var spacerLayout = spacerObject.GetComponent<LayoutElement>();
                spacerLayout.preferredWidth = FeedIconSize;
                spacerLayout.minWidth = FeedIconSize;
            }

            var textColumnObject = new GameObject("TextColumn", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
            textColumnObject.transform.SetParent(rowObject.transform, false);

            var textColumnLayout = textColumnObject.GetComponent<VerticalLayoutGroup>();
            textColumnLayout.childControlWidth = true;
            textColumnLayout.childControlHeight = true;
            textColumnLayout.childForceExpandWidth = true;
            textColumnLayout.childForceExpandHeight = false;
            textColumnLayout.spacing = 2f;
            textColumnLayout.padding = new RectOffset(0, 0, 0, 0);

            var textColumnFitter = textColumnObject.GetComponent<ContentSizeFitter>();
            textColumnFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            textColumnFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textColumnElement = textColumnObject.GetComponent<LayoutElement>();
            textColumnElement.flexibleWidth = 1f;

            var titleObject = new GameObject("Announcement", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(textColumnObject.transform, false);

            var titleText = titleObject.GetComponent<TextMeshProUGUI>();
            string roundPrefix = entry.Round > 0 ? $"Round {entry.Round}: " : string.Empty;
            titleText.text = roundPrefix + entry.Announcement;
            titleText.fontSize = FeedEntryFontSize;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = UIStyleTokens.Text.Primary;
            titleText.alignment = TextAlignmentOptions.TopLeft;
            titleText.textWrappingMode = TextWrappingModes.Normal;
            titleText.overflowMode = TextOverflowModes.Overflow;
            titleText.raycastTarget = false;

            var detailObject = new GameObject("Detail", typeof(RectTransform), typeof(TextMeshProUGUI));
            detailObject.transform.SetParent(textColumnObject.transform, false);

            var detailText = detailObject.GetComponent<TextMeshProUGUI>();
            detailText.text = string.IsNullOrWhiteSpace(entry.Detail) ? "Resolution complete." : entry.Detail;
            detailText.fontSize = DraftHistoryEntryDetailFontSize;
            detailText.color = UIStyleTokens.Text.Secondary;
            detailText.alignment = TextAlignmentOptions.TopLeft;
            detailText.textWrappingMode = TextWrappingModes.Normal;
            detailText.overflowMode = TextOverflowModes.Overflow;
            detailText.raycastTarget = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                titleText.font = TMP_Settings.defaultFontAsset;
                detailText.font = TMP_Settings.defaultFontAsset;
            }
        }

        private static string BuildPickAnnouncement(Player player, Mycovariant picked, PlayerMycovariant playerMyco)
        {
            string categoryFlavor = GetPickFlavor(player, picked, playerMyco);
            return $"{player.PlayerName} drafted {picked.Name} — {categoryFlavor}.";
        }

        private static string GetPickFlavor(Player player, Mycovariant picked, PlayerMycovariant playerMyco)
        {
            if (player != null && picked != null && picked.IsBait)
            {
                int effectCount = GetPrimaryEffectCount(playerMyco, picked.Id);
                return picked.Id switch
                {
                    MycovariantIds.AscusBaitId when player.PlayerType == PlayerTypeEnum.Human => "evolution economy just accelerated",
                    MycovariantIds.AscusBaitId => effectCount > 0
                        ? $"{effectCount} cells just self-culled"
                        : "the colony just culled itself",
                    MycovariantIds.SporophoreDecoyId when player.PlayerType == PlayerTypeEnum.Human => "evolution economy just accelerated",
                    MycovariantIds.SporophoreDecoyId => effectCount > 0
                        ? $"{effectCount} resistant cells just lost their shell"
                        : "resistant defenses just collapsed",
                    MycovariantIds.SporalSnareId when player.PlayerType == PlayerTypeEnum.Human => "evolution economy just accelerated",
                    MycovariantIds.SporalSnareId => "human growth just punched through their lane",
                    MycovariantIds.PerisporeCrownId when player.PlayerType == PlayerTypeEnum.Human => "evolution economy just accelerated",
                    MycovariantIds.PerisporeCrownId => "a toxin crown just bloomed around their spore",
                    _ => picked.Category switch
                    {
                        MycovariantCategory.Growth => "growth lines are expanding",
                        MycovariantCategory.Fungicide => "toxin pressure is rising",
                        MycovariantCategory.Resistance => "defenses are hardening",
                        MycovariantCategory.Reclamation => "the dead are being recycled",
                        MycovariantCategory.Economy => "evolution economy just accelerated",
                        MycovariantCategory.Defense => "defensive chemistry is online",
                        _ => "the colony strategy just shifted"
                    }
                };
            }

            return picked.Category switch
            {
                MycovariantCategory.Growth => "growth lines are expanding",
                MycovariantCategory.Fungicide => "toxin pressure is rising",
                MycovariantCategory.Resistance => "defenses are hardening",
                MycovariantCategory.Reclamation => "the dead are being recycled",
                MycovariantCategory.Economy => "evolution economy just accelerated",
                MycovariantCategory.Defense => "defensive chemistry is online",
                _ => "the colony strategy just shifted"
            };
        }

        private static string BuildEffectCountSummary(PlayerMycovariant playerMyco)
        {
            if (playerMyco.EffectCounts == null || playerMyco.EffectCounts.Count == 0)
                return string.Empty;

            var parts = playerMyco.EffectCounts
                .Where(kvp => kvp.Value > 0)
                .OrderByDescending(kvp => kvp.Value)
                .Take(3)
                .Select(kvp => $"{kvp.Value} {FormatEffectType(kvp.Key)}")
                .ToList();

            return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
        }

        private static string FormatEffectType(MycovariantEffectType effectType)
        {
            return effectType switch
            {
                MycovariantEffectType.Colonized => "colonized",
                MycovariantEffectType.Infested => "infested",
                MycovariantEffectType.Reclaimed => "reclaimed",
                MycovariantEffectType.Poisoned => "poisoned",
                MycovariantEffectType.Toxified => "toxified",
                MycovariantEffectType.MpBonus => "mutation points gained",
                MycovariantEffectType.AscusBaitSelfCullKills => "cells self-culled",
                MycovariantEffectType.SporophoreDecoyResistanceLosses => "resistant cells stripped",
                MycovariantEffectType.CytolyticBurstKills => "kills",
                MycovariantEffectType.CytolyticBurstToxins => "toxins spawned",
                MycovariantEffectType.BallistosporeDischarge => "ballistospores dropped",
                MycovariantEffectType.ResistantCellPlaced => "resistant cells placed",
                MycovariantEffectType.Bastioned => "cells bastioned",
                MycovariantEffectType.SeptalSealResistances => "cells sealed",
                MycovariantEffectType.ResistantTransfers => "resistance transfers",
                MycovariantEffectType.Neutralized => "toxins neutralized",
                MycovariantEffectType.Relocations => "toxin relocations",
                MycovariantEffectType.SecondReclamationAttempts => "second reclaim attempts",
                MycovariantEffectType.ExistingExtensions => "toxin cycles extended",
                _ => effectType.ToString()
            };
        }

        private static int GetPrimaryEffectCount(PlayerMycovariant playerMyco, int mycovariantId)
        {
            if (playerMyco?.EffectCounts == null)
            {
                return 0;
            }

            return mycovariantId switch
            {
                MycovariantIds.AscusBaitId => playerMyco.EffectCounts.TryGetValue(MycovariantEffectType.AscusBaitSelfCullKills, out int selfCullKills)
                    ? selfCullKills
                    : 0,
                MycovariantIds.SporophoreDecoyId => playerMyco.EffectCounts.TryGetValue(MycovariantEffectType.SporophoreDecoyResistanceLosses, out int resistanceLosses)
                    ? resistanceLosses
                    : 0,
                _ => 0
            };
        }

        private void AnimatePickFeedback(Mycovariant picked, System.Action onComplete)
        {
            MycovariantCard pickedCard = null;
            foreach (Transform child in choiceContainer)
            {
                var card = child.GetComponent<MycovariantCard>();
                if (card != null && card.Mycovariant == picked)
                {
                    pickedCard = card;
                    break;
                }
            }

            if (pickedCard != null)
            {
                StartCoroutine(PlayPickAnimation(pickedCard, onComplete));
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private System.Collections.IEnumerator PlayPickAnimation(MycovariantCard card, System.Action onComplete)
        {
            var rt = card.GetComponent<RectTransform>();
            var origScale = rt.localScale;
            var highlightColor = new Color(1f, 0.93f, 0.45f, 1f);

            float duration = 0.24f;
            float elapsed = 0f;

            var btnImage = card.pickButton.image;
            Color origColor = btnImage.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.localScale = Vector3.Lerp(origScale, origScale * 1.13f, t);
                btnImage.color = Color.Lerp(origColor, highlightColor, t);
                yield return null;
            }

            yield return new WaitForSeconds(0.14f);

            rt.localScale = origScale;
            btnImage.color = origColor;

            yield return new WaitForSeconds(0.1f);

            onComplete?.Invoke();
        }
    }
}
