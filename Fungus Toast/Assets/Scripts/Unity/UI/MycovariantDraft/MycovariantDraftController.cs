using Assets.Scripts.Unity.UI.MycovariantDraft;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Effects;
using FungusToast.Unity.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [Header("UI References")]
        [SerializeField] private GameObject draftPanel; // Main draft panel root
        [SerializeField] private CanvasGroup interactionBlocker; // semi-transparent overlay, blocks raycasts
        [SerializeField] private TextMeshProUGUI draftBannerText;
        [SerializeField] private DraftOrderRow draftOrderRow; // progress bar, highlights current/next/done
        [SerializeField] private Transform choiceContainer; // Parent for card prefabs
        [SerializeField] private MycovariantCard cardPrefab; // Assign in inspector
        [SerializeField] private GridVisualizer gridVisualizer;

        private List<Mycovariant> draftChoices;
        private Player currentPlayer;
        private List<Player> draftOrder;
        private int draftIndex;

        private MycovariantPoolManager poolManager;
        private System.Random rng;
        private int draftChoicesCount;

        private DraftUIState uiState = DraftUIState.Idle;

        // Public entry point: starts draft phase
        public void StartDraft(
            List<Player> players,
            MycovariantPoolManager poolManager,
            List<Player> draftOrder,
            System.Random rng,
            int draftChoicesCount)
        {
            this.poolManager = poolManager;
            this.rng = rng;
            this.draftOrder = draftOrder;
            draftIndex = 0;
            this.draftChoicesCount = draftChoicesCount;
            ShowDraftUI();
            BeginNextDraft();
        }

        private void BeginNextDraft()
        {
            if (draftIndex >= draftOrder.Count)
            {
                var allMycovariants = MycovariantRepository.All;
                poolManager.ReturnUndraftedToPool(allMycovariants, rng);
                
                HideDraftUI();
                uiState = DraftUIState.Complete;
                GameManager.Instance.OnMycovariantDraftComplete();
                return;
            }
            currentPlayer = draftOrder[draftIndex];
            UpdateDraftBanner();

            draftOrderRow?.SetDraftOrder(draftOrder, draftIndex);

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
                draftBannerText.text = $"AI Drafting: {currentPlayer.PlayerName}";
            else
                draftBannerText.text = $"Your turn to draft a Mycovariant!";
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
            foreach (Transform child in choiceContainer)
                Destroy(child.gameObject);

            foreach (var m in choices)
            {
                var card = Instantiate(cardPrefab, choiceContainer);
                card.SetMycovariant(m, OnChoicePicked);
                card.SetActiveHighlight(false);
            }
        }

        private void OnChoicePicked(Mycovariant picked)
        {
            if (uiState != DraftUIState.HumanTurn && uiState != DraftUIState.AITurn)
                return;

            GameManager.Instance.GameUI.GameLogRouter?.OnDraftPick(currentPlayer.PlayerName, picked.Name);

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
                
                HideDraftUI();
                uiState = DraftUIState.Complete;
                GameManager.Instance.OnMycovariantDraftComplete();
                return;
            }
            
            currentPlayer = draftOrder[draftIndex];
            UpdateDraftBanner();

            draftOrderRow?.SetDraftOrder(draftOrder, draftIndex);

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

        private void ShowDraftUI()
        {
            draftPanel.SetActive(true);
            interactionBlocker.blocksRaycasts = true;
            interactionBlocker.alpha = 0.8f;
            uiState = DraftUIState.Idle;
        }

        private void HideDraftUI()
        {
            draftPanel.SetActive(false);
            interactionBlocker.blocksRaycasts = false;
            interactionBlocker.alpha = 0f;
            uiState = DraftUIState.Idle;
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
