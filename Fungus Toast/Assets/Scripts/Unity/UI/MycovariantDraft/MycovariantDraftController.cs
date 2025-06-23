using Assets.Scripts.Unity.UI.MycovariantDraft;
using FungusToast.Core.Board;
using FungusToast.Core.Config;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using FungusToast.Unity.Effects;
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
                HideDraftUI();
                uiState = DraftUIState.Complete;
                GameManager.Instance.OnMycovariantDraftComplete();
                return;
            }
            currentPlayer = draftOrder[draftIndex];
            UpdateDraftBanner();

            // Progress bar: highlight current, gray-out done, normal for next
            draftOrderRow?.SetDraftOrder(draftOrder, draftIndex);   // <--- **RIGHT HERE**

            draftChoices = MycovariantDraftManager.GetDraftChoices(
                currentPlayer, poolManager, draftChoicesCount, rng);

            PopulateChoices(draftChoices);

            if (currentPlayer.PlayerType == PlayerTypeEnum.AI)
            {
                SetDraftState(DraftUIState.AITurn);
                // Animate a "thinking" moment, then pick
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
            // Only highlight for human turn
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

            SetDraftState(DraftUIState.AnimatingPick);

            GameManager.Instance.ResolveMycovariantDraftPick(currentPlayer, picked);

            if (!picked.IsUniversal)
                poolManager.RemoveFromPool(picked);

            // Get the PlayerMycovariant for the just-picked mycovariant
            var playerMyco = currentPlayer.PlayerMycovariants
                .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

            if (playerMyco == null)
            {
                Debug.LogError($"[MycovariantDraftController] PlayerMycovariant is null for {picked.Name} (ID: {picked.Id}) and player {currentPlayer.PlayerId}. Aborting effect resolution.");
                AnimatePickFeedback(picked, () => {
                    draftIndex++;
                    BeginNextDraft();
                });
                return;
            }

            StartCoroutine(MycovariantEffectResolver.Instance.ResolveEffect(
                currentPlayer,
                picked,
                playerMyco,
                () => {
                    // After effect is done, restore panel and animate feedback
                    draftPanel.SetActive(true);
                    AnimatePickFeedback(picked, () => {
                        draftIndex++;
                        BeginNextDraft();
                    });
                }
            ));
        }


        private IEnumerator ResolveDraftEffectAndAdvance(Mycovariant picked)
        {
            // Handle Jetting Mycelium (requires cell selection)
            if (MycovariantEffectResolver.IsJettingMycelium(picked.Id))
            {
                bool effectResolved = false;
                yield return StartCoroutine(HandleJettingMycelium(currentPlayer, picked, () => effectResolved = true));
                while (!effectResolved) yield return null;
            }
            // Handle Plasmid Bounty (instant)
            else if (picked.Id == MycovariantIds.PlasmidBountyId)
            {
                HandlePlasmidBounty(currentPlayer, picked);
                yield return new WaitForSeconds(0.5f); // Small feedback delay
            }
            else
            {
                // Default: wait for pick animation only
                yield return new WaitForSeconds(0.3f);
            }

            // Animate feedback (pulse, highlight, etc.), then advance
            AnimatePickFeedback(picked, () => {
                draftIndex++;
                BeginNextDraft();
            });
        }


        private IEnumerator AnimateAIPickRoutine()
        {
            // Optional: show "AI thinking" with progress dots, brief delay
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.7f, 1.2f));

            var randomPick = draftChoices[rng.Next(draftChoices.Count)];
            OnChoicePicked(randomPick);
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

        // Animate AI or human pick: card pulse and color flash
        private void AnimatePickFeedback(Mycovariant picked, System.Action onComplete)
        {
            // Find the card object for the picked Mycovariant
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

        // Animation: scale pulse and color flash
        private System.Collections.IEnumerator PlayPickAnimation(MycovariantCard card, System.Action onComplete)
        {
            var rt = card.GetComponent<RectTransform>();
            var origScale = rt.localScale;
            var highlightColor = new Color(1f, 0.93f, 0.45f, 1f); // warm yellow

            float duration = 0.24f;
            float elapsed = 0f;

            var btnImage = card.pickButton.image;
            Color origColor = btnImage.color;

            // Highlight card visually (scale up, color pulse)
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.localScale = Vector3.Lerp(origScale, origScale * 1.13f, t);
                btnImage.color = Color.Lerp(origColor, highlightColor, t);
                yield return null;
            }

            // Hold highlight briefly
            yield return new WaitForSeconds(0.14f);

            // Restore card visuals
            rt.localScale = origScale;
            btnImage.color = origColor;

            // Optional: fade out the card, or show "chosen" badge here

            yield return new WaitForSeconds(0.1f);

            onComplete?.Invoke();
        }

        private IEnumerator HandleJettingMycelium(Player player, Mycovariant picked, Action onComplete)
        {
            var direction = DirectionFromMycovariantId(picked.Id); // Use your direction helper!

            if (player.PlayerType == PlayerTypeEnum.AI)
            {
                // AI selects a source cell automatically
                var livingCells = GameManager.Instance.Board.GetAllCellsOwnedBy(player.PlayerId)
                    .Where(c => c.IsAlive)
                    .ToList();
                FungalCell cell = livingCells.Count > 0
                    ? livingCells[UnityEngine.Random.Range(0, livingCells.Count)]
                    : null;

                if (cell != null)
                {
                    var playerMyco = player.PlayerMycovariants
                        .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                    MycovariantEffectProcessor.ResolveJettingMycelium(
                        playerMyco,
                        player,
                        GameManager.Instance.Board,
                        cell.TileId,
                        direction
                    );
                }
                yield return new WaitForSeconds(0.6f);
                onComplete?.Invoke();
            }
            else
            {
                bool done = false;
                FungalCell selectedCell = null;

                TileSelectionController.Instance.PromptSelectLivingCell(
                    player.PlayerId,
                    (cell) => {
                        selectedCell = cell;
                        done = true;
                    },
                    () => {
                        // If canceled, just continue
                        done = true;
                    }
                );

                while (!done) yield return null;

                if (selectedCell != null)
                {
                    var playerMyco = player.PlayerMycovariants
                        .FirstOrDefault(pm => pm.MycovariantId == picked.Id);

                    MycovariantEffectProcessor.ResolveJettingMycelium(
                        playerMyco,
                        player,
                        GameManager.Instance.Board,
                        selectedCell.TileId,
                        direction
                    );
                }
                onComplete?.Invoke();
            }
        }


        private CardinalDirection DirectionFromMycovariantId(int id)
        {
            if (id == MycovariantIds.JettingMyceliumNorthId) return CardinalDirection.North;
            if (id == MycovariantIds.JettingMyceliumEastId) return CardinalDirection.East;
            if (id == MycovariantIds.JettingMyceliumSouthId) return CardinalDirection.South;
            if (id == MycovariantIds.JettingMyceliumWestId) return CardinalDirection.West;
            throw new ArgumentException("Invalid Jetting Mycelium ID");
        }



        private void HandlePlasmidBounty(Player player, Mycovariant picked)
        {
            player.AddMutationPoints(15);
            //GameManager.Instance.LogEvent($"{player.PlayerName} gained 15 mutation points from Plasmid Bounty!");

            GameManager.Instance.GameUI.MoldProfilePanel?.PulseMutationPoints();
        }

    }
}
