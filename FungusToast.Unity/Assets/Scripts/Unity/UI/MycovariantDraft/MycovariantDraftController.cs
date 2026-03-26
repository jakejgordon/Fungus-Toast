using Assets.Scripts.Unity.UI.MycovariantDraft;
using FungusToast.Core.Board;
using FungusToast.Core.Campaign;
using FungusToast.Core.Config;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Effects;
using FungusToast.Unity.Grid;
using FungusToast.Unity.UI.Campaign;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
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

    public class MycovariantDraftController : MonoBehaviour
    {
        private const string DefaultDraftTitle = "Choose a Mycovariant";
        private const string DefaultDraftBlurb = "Select a unique mycovariant mutation.";
        private const string DefaultHumanTurnBannerText = "Your turn to draft a Mycovariant!";
        private const string DefaultAiTurnBannerPrefix = "AI Drafting";

        [Header("UI References")]
        [SerializeField] private GameObject draftPanel; // Main draft panel root
        [SerializeField] private CanvasGroup interactionBlocker; // semi-transparent overlay, blocks raycasts
        [SerializeField] private TextMeshProUGUI draftBannerText;
        [SerializeField] private TextMeshProUGUI draftBlurbText;
        [SerializeField] private DraftOrderRow draftOrderRow; // progress bar, highlights current/next/done
        [SerializeField] private Transform choiceContainer; // Parent for card prefabs
        [SerializeField] private MycovariantCard cardPrefab; // Assign in inspector
        [SerializeField] private GridVisualizer gridVisualizer;

        [Header("Draft Message Feed (Left Panel)")]
        [SerializeField] private GameObject draftMessagePanel;
        [SerializeField] private TextMeshProUGUI draftMessageTitleText;
        [SerializeField] private TextMeshProUGUI draftMessageBodyText;
        [SerializeField] private int draftMessageMaxLines = 14;
        [SerializeField] private float draftCompletionHoldSeconds = 2f;

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
        private readonly Queue<string> draftMessageLines = new();
        private bool isFinishingDraftPhase;
        private bool isCampaignAdaptationDraft;
        private Action<AdaptationDefinition> onAdaptationPicked;

        private bool _cameraRecenteredThisDraftPhase = false;

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

            EnsureDraftMessageUI();
            ClearDraftMessages();
            AddDraftMessage(this.draftStartMessage);
            TryAnnounceAscusPrimacyDraftPriority();

            SetDraftHeader(draftHeaderTitle, draftHeaderBlurb);

            isCampaignAdaptationDraft = false;
            onAdaptationPicked = null;
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
            _cameraRecenteredThisDraftPhase = false;

            ClearChoiceCards();
            ClearDraftMessages();
            HideDraftUI();
        }

        public void StartCampaignAdaptationDraft(
            IReadOnlyList<AdaptationDefinition> choices,
            Action<AdaptationDefinition> onPicked)
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
                AddDraftMessage("Draft complete. Spores settle while the colonies prepare for the next round...");
                BeginDraftCompletionSequence();
                return;
            }
            currentPlayer = draftOrder[draftIndex];
            UpdateDraftBanner();

            AddDraftMessage(BuildTurnAnnouncement(currentPlayer));

            if (draftOrderRow != null)
            {
                draftOrderRow.gameObject.SetActive(draftOrder.Count > 1);
                draftOrderRow.SetDraftOrder(draftOrder, draftIndex);
            }

            int? forcedMycovariantId = null;
            if (GameManager.Instance.IsTestingModeEnabled && currentPlayer.PlayerType == PlayerTypeEnum.Human)
                forcedMycovariantId = GameManager.Instance.TestingMycovariantId;
            draftChoices = MycovariantDraftManager.GetDraftChoices(
                currentPlayer, poolManager, draftChoicesCount, rng, forcedMycovariantId);

            PopulateChoices(draftChoices);

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

            GameManager.Instance.GameUI.GameLogRouter?.OnDraftPick(currentPlayer.PlayerName, picked.Name);

            AddDraftMessage(BuildPickAnnouncement(currentPlayer, picked));

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
                    AddDraftResultMessage(currentPlayer, picked, playerMyco);
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
            ReplacePickedCard(picked);
            
            draftIndex++;
            BeginNextDraftWithExistingCards();
        }

        private void BeginNextDraftWithExistingCards()
        {
            if (draftIndex >= draftOrder.Count)
            {
                var allMycovariants = MycovariantRepository.All;
                poolManager.ReturnUndraftedToPool(allMycovariants, rng);

                AddDraftMessage("Draft complete. Spores settle while the colonies prepare for the next round...");
                BeginDraftCompletionSequence();
                return;
            }
            
            currentPlayer = draftOrder[draftIndex];
            UpdateDraftBanner();

            if (draftOrderRow != null)
            {
                draftOrderRow.gameObject.SetActive(draftOrder.Count > 1);
                draftOrderRow.SetDraftOrder(draftOrder, draftIndex);
            }

            draftChoices = new List<Mycovariant>();
            foreach (Transform child in choiceContainer)
            {
                var card = child.GetComponent<MycovariantCard>();
                if (card != null && card.gameObject.activeInHierarchy)
                {
                    draftChoices.Add(card.Mycovariant);
                }
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

        private IEnumerator AnimateAIPickRoutine()
        {
            yield return new WaitForSeconds(FungusToast.Unity.UI.UIEffectConstants.AIDraftPickDelaySeconds);

            Mycovariant pick;
            if (GameManager.Instance.IsTestingModeEnabled && GameManager.Instance.TestingMycovariantId.HasValue && draftChoices.Any(m => m.Id == GameManager.Instance.TestingMycovariantId.Value))
            {
                pick = draftChoices.First(m => m.Id == GameManager.Instance.TestingMycovariantId.Value);
            }
            else
            {
                float maxScore = float.MinValue;
                List<Mycovariant> bestChoices = new List<Mycovariant>();
                foreach (var m in draftChoices)
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
            AddDraftMessage($"Adaptation acquired: {picked.Name}.");

            var callback = onAdaptationPicked;
            onAdaptationPicked = null;
            isCampaignAdaptationDraft = false;

            HideDraftUI();
            callback?.Invoke(picked);
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
        }

        private void EnsureDraftMessageUI()
        {
            if (draftMessagePanel != null && draftMessageBodyText != null)
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

            if (draftMessageBodyText == null)
            {
                var bodyGO = new GameObject("DraftMessageBody", typeof(RectTransform));
                bodyGO.transform.SetParent(draftMessagePanel.transform, false);
                draftMessageBodyText = bodyGO.AddComponent<TextMeshProUGUI>();
                draftMessageBodyText.fontSize = 20f;
                draftMessageBodyText.color = UIStyleTokens.Text.Secondary;
                draftMessageBodyText.alignment = TextAlignmentOptions.TopLeft;
                draftMessageBodyText.textWrappingMode = TextWrappingModes.Normal;
                draftMessageBodyText.overflowMode = TextOverflowModes.Overflow;
                draftMessageBodyText.text = string.Empty;

                var bodyRect = bodyGO.GetComponent<RectTransform>();
                bodyRect.anchorMin = new Vector2(0f, 0f);
                bodyRect.anchorMax = new Vector2(1f, 1f);
                bodyRect.offsetMin = new Vector2(14f, 14f);
                bodyRect.offsetMax = new Vector2(-14f, -52f);
            }
        }

        private void ClearDraftMessages()
        {
            draftMessageLines.Clear();
            RefreshDraftMessageText();
        }

        private void AddDraftMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            draftMessageLines.Enqueue($"• {message}");
            while (draftMessageLines.Count > draftMessageMaxLines)
                draftMessageLines.Dequeue();

            RefreshDraftMessageText();
        }

        private void RefreshDraftMessageText()
        {
            if (draftMessageBodyText == null)
                return;

            var sb = new StringBuilder();
            foreach (var line in draftMessageLines)
            {
                sb.AppendLine(line);
            }
            draftMessageBodyText.text = sb.ToString().TrimEnd();
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

        private void AddDraftResultMessage(Player player, Mycovariant picked, PlayerMycovariant playerMyco)
        {
            if (playerMyco == null)
            {
                AddDraftMessage($"Mycelial pulse: {picked.Name} resolved.");
                return;
            }

            string countSummary = BuildEffectCountSummary(playerMyco);
            if (!string.IsNullOrEmpty(countSummary))
            {
                AddDraftMessage($"Impact: {countSummary}.");
                return;
            }

            if (picked.Id == MycovariantIds.PlasmidBountyId)
            {
                AddDraftMessage($"Plasmids absorbed: +{MycovariantGameBalance.PlasmidBountyMutationPointAward} mutation points.");
            }
            else if (picked.Id == MycovariantIds.PlasmidBountyIIId)
            {
                AddDraftMessage($"Plasmids absorbed: +{MycovariantGameBalance.PlasmidBountyIIMutationPointAward} mutation points.");
            }
            else if (picked.Id == MycovariantIds.PlasmidBountyIIIId)
            {
                AddDraftMessage($"Plasmids absorbed: +{MycovariantGameBalance.PlasmidBountyIIIMutationPointAward} mutation points.");
            }
            else if (picked.Type == MycovariantType.Passive)
            {
                AddDraftMessage("Passive trait established for the rest of the game.");
            }
            else
            {
                AddDraftMessage($"Effect resolved: {picked.Name}.");
            }
        }

        private static string BuildTurnAnnouncement(Player player)
        {
            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                string[] aiLines =
                {
                    $"{player.PlayerName} is scanning the mycelial options...",
                    $"{player.PlayerName} is plotting a fungal gambit...",
                    $"{player.PlayerName} is weighing spore-risk and reward..."
                };
                return aiLines[UnityEngine.Random.Range(0, aiLines.Length)];
            }

            return "Your turn: choose a Mycovariant and shape the colony's fate.";
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
