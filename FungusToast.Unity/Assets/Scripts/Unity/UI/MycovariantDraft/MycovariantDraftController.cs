using Assets.Scripts.Unity.UI.MycovariantDraft;
using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Effects;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI.Campaign;
using FungusToast.Unity.UI.Tooltips;
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
        private const string DefaultHumanTurnBannerText = "Your turn to draft a Mycovariant!";
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
        private const string CampaignAdaptationRedrawReadyLabel = "Use Spore Sifting";
        private const string CampaignAdaptationRedrawConfirmLabel = "Confirm Redraw";
        private const string CampaignAdaptationRedrawUsedLabel = "Spore Sifting Used";
        private const string CampaignAdaptationRedrawTooltipText = "You unlocked the Spore Sifting moldiness reward, allowing you to redraw all Mycovariants to present new draft options once per level.";

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
        private bool showCampaignAdaptationRedrawControl;
        private bool campaignAdaptationRedrawAvailable;
        private bool campaignAdaptationRedrawConfirmArmed;
        private Func<IReadOnlyList<AdaptationDefinition>> onCampaignAdaptationRedrawRequested;
        private AudioSource soundEffectAudioSource;
        private RectTransform draftHistoryOverlayRoot;
        private CanvasGroup draftHistoryOverlayCanvasGroup;
        private ScrollRect draftHistoryOverlayScrollRect;
        private Transform draftHistoryOverlayContentTransform;
        private TextMeshProUGUI draftHistoryEmptyStateText;
        private Button draftHistoryCloseButton;

        private bool _cameraRecenteredThisDraftPhase = false;

        private sealed class DraftHistoryEntry
        {
            public int Round { get; set; }
            public int PlayerId { get; set; }
            public string Announcement { get; set; }
            public string Detail { get; set; }
        }

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
            ClearDraftMessages();
            AddDraftMessage(this.draftStartMessage);
            TryAnnounceAscusPrimacyDraftPriority();

            SetDraftHeader(draftHeaderTitle, draftHeaderBlurb);

            isCampaignAdaptationDraft = false;
            onAdaptationPicked = null;
            ConfigureCampaignAdaptationRedrawControl(false, false, null);
            ShowDraftUI();
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
            _cameraRecenteredThisDraftPhase = false;

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
            ClearDraftMessages();
            AddDraftMessage("Victory secured. Choose an Adaptation to evolve your colony for the rest of this campaign.");

            SetDraftHeader(
                "Choose an Adaptation",
                "Select one adaptation to strengthen your colony for the rest of the campaign.");

            ConfigureCampaignAdaptationRedrawControl(showRedrawControl, redrawAvailable, onRedrawRequested);
            ShowDraftUI();
            if (draftOrderRow != null)
            {
                draftOrderRow.gameObject.SetActive(false);
            }
            PopulateAdaptationChoices(choices);
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

            foreach (var m in choices)
            {
                CreateChoiceCard(
                    m,
                    m.Name,
                    m.Description,
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

            string pickAnnouncement = BuildPickAnnouncement(currentPlayer, picked);
            AddPlayerFeedEntry(currentPlayer, pickAnnouncement);

            GameManager.Instance.ResolveMycovariantDraftPick(currentPlayer, picked);

            if (!picked.IsUniversal)
                poolManager.RemoveFromPool(picked);

            var playerMyco = currentPlayer.PlayerMycovariants
                .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

            if (currentPlayer.PlayerType == PlayerTypeEnum.AI && playerMyco != null)
            {
                float score = picked.AIScore != null ? picked.AIScore(currentPlayer, GameManager.Instance.Board) : MycovariantAIGameBalance.DefaultAIScore;
                playerMyco.AIScoreAtDraft = score;
            }

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
                    string resultMessage = AddDraftResultMessage(currentPlayer, picked, playerMyco);
                    RecordDraftHistoryEntry(currentPlayer, pickAnnouncement, resultMessage);
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
            return MycovariantDraftManager.GetDraftChoices(
                currentPlayer,
                poolManager,
                draftChoicesCount,
                rng,
                GetForcedMycovariantIdForCurrentPlayer());
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
                float maxScore = float.MinValue;
                List<Mycovariant> bestChoices = new List<Mycovariant>();
                foreach (var m in eligibleChoices)
                {
                    float score = m.AIScore != null ? m.AIScore(currentPlayer, GameManager.Instance.Board) : MycovariantAIGameBalance.DefaultAIScore;
                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestChoices.Clear();
                        bestChoices.Add(m);
                    }
                    else if (score == maxScore)
                    {
                        bestChoices.Add(m);
                    }
                }
                pick = bestChoices[UnityEngine.Random.Range(0, bestChoices.Count)];
            }
            OnChoicePicked(pick);
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
            card.SetChoiceContent(boundMycovariant, title, description, icon, onPicked);
            card.SetActiveHighlight(highlight);
            return card;
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

        private void ShowDraftUI()
        {
            draftPanel.SetActive(true);
            if (draftMessagePanel != null)
                draftMessagePanel.SetActive(true);
            interactionBlocker.blocksRaycasts = true;
            interactionBlocker.alpha = 0.8f;
            uiState = DraftUIState.Idle;

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
            if (draftOrderRow != null)
            {
                draftOrderRow.gameObject.SetActive(true);
            }
            uiState = DraftUIState.Idle;
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
            var backgroundColor = UIStyleTokens.Surface.PanelSecondary;
            backgroundColor.a = 0.92f;
            rootImage.color = backgroundColor;
            rootImage.raycastTarget = true;

            var layout = campaignAdaptationUtilityRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var statusObject = new GameObject("Status", typeof(RectTransform), typeof(LayoutElement), typeof(TextMeshProUGUI));
            statusObject.transform.SetParent(campaignAdaptationUtilityRoot.transform, false);
            var statusLayout = statusObject.GetComponent<LayoutElement>();
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
            buttonRect.sizeDelta = new Vector2(260f, 36f);
            var buttonLayout = buttonObject.GetComponent<LayoutElement>();
            buttonLayout.preferredWidth = 260f;
            buttonLayout.minWidth = 260f;
            buttonLayout.preferredHeight = 36f;
            buttonLayout.minHeight = 36f;
            campaignAdaptationRedrawButton = buttonObject.GetComponent<Button>();
            campaignAdaptationRedrawButton.targetGraphic = buttonObject.GetComponent<Image>();
            UIStyleTokens.Button.ApplyStyle(campaignAdaptationRedrawButton, useSelectedAsNormal: true);
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
            campaignAdaptationRedrawButtonText.overflowMode = TextOverflowModes.Ellipsis;
            campaignAdaptationRedrawButtonText.color = UIStyleTokens.Button.TextDefault;
            campaignAdaptationRedrawButtonText.raycastTarget = false;
            UIStyleTokens.Button.SetButtonLabelColor(campaignAdaptationRedrawButton, UIStyleTokens.Button.TextDefault);

            campaignAdaptationUtilityRoot.SetActive(false);
        }

        private void RefreshCampaignAdaptationUtilityUi()
        {
            bool shouldShow = isCampaignAdaptationDraft && showCampaignAdaptationRedrawControl;

            if (!shouldShow)
            {
                campaignAdaptationRedrawConfirmArmed = false;
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

            bool canInteract = campaignAdaptationRedrawAvailable && uiState == DraftUIState.HumanTurn;
            if (!canInteract)
            {
                campaignAdaptationRedrawConfirmArmed = false;
            }

            string buttonLabel;
            string statusText;
            Color statusColor;
            if (!campaignAdaptationRedrawAvailable)
            {
                buttonLabel = CampaignAdaptationRedrawUsedLabel;
                statusText = "Spore Sifting has already been used for this campaign level.";
                statusColor = UIStyleTokens.Text.Muted;
            }
            else if (campaignAdaptationRedrawConfirmArmed)
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
                draftMessageTitleText.text = "Draft Feed";
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
                ? gridVisualizer.GetTileForPlayer(player.PlayerId)?.sprite
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
            string resultMessage = BuildDraftResultMessage(picked, playerMyco);
            if (!string.IsNullOrWhiteSpace(resultMessage))
            {
                AppendToCurrentPlayerEntry(resultMessage);
            }

            return resultMessage;
        }

        private static string BuildDraftResultMessage(Mycovariant picked, PlayerMycovariant playerMyco)
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
                ? gridVisualizer.GetTileForPlayer(entry.PlayerId)?.sprite
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

        private static string BuildPickAnnouncement(Player player, Mycovariant picked)
        {
            string categoryFlavor = picked.Category switch
            {
                MycovariantCategory.Growth => "growth lines are expanding",
                MycovariantCategory.Fungicide => "toxin pressure is rising",
                MycovariantCategory.Resistance => "defenses are hardening",
                MycovariantCategory.Reclamation => "the dead are being recycled",
                MycovariantCategory.Economy => "evolution economy just accelerated",
                MycovariantCategory.Defense => "defensive chemistry is online",
                _ => "the colony strategy just shifted"
            };

            return $"{player.PlayerName} drafted {picked.Name} — {categoryFlavor}.";
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
