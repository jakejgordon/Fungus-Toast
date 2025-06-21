using UnityEngine;
using FungusToast.Core.Mycovariants;
using FungusToast.Core.Players;
using System.Collections.Generic;
using TMPro;
using Assets.Scripts.Unity.UI.MycovariantDraft;
using System;
using FungusToast.Core.Config;

namespace FungusToast.Unity.UI.MycovariantDraft
{
    public class MycovariantDraftController : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject draftPanel; // Main draft panel root
        public TextMeshProUGUI draftBannerText;
        public DraftOrderRow draftOrderRow;
        public Transform choiceContainer; // Parent for choice cards
        public MycovariantCard cardPrefab; // Assign in inspector

        private List<Mycovariant> draftChoices;
        private Player currentPlayer;
        private List<Player> draftOrder;
        private int draftIndex;

        private MycovariantPoolManager poolManager;
        private System.Random rng;
        private int draftChoicesCount;

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
                GameManager.Instance.OnMycovariantDraftComplete();
                return;
            }
            currentPlayer = draftOrder[draftIndex];
            draftBannerText.text = $"Drafting: {currentPlayer.PlayerName}";
            draftOrderRow.SetDraftOrder(draftOrder, draftIndex);
            // Get N choices for this player from backend logic
            draftChoices = MycovariantDraftManager.GetDraftChoices(
                currentPlayer, poolManager, draftChoicesCount, rng);

            PopulateChoices(draftChoices);
            if (currentPlayer.PlayerType == PlayerTypeEnum.AI)
            {
                Invoke(nameof(AutoPickForAI), 1.0f);
            }
        }

        private void PopulateChoices(List<Mycovariant> choices)
        {
            foreach (Transform child in choiceContainer) Destroy(child.gameObject);
            foreach (var m in choices)
            {
                var card = Instantiate(cardPrefab, choiceContainer);
                card.SetMycovariant(m, OnChoicePicked);
            }
        }

        private void OnChoicePicked(Mycovariant picked)
        {
            // Backend: assign to player, resolve effects
            GameManager.Instance.ResolveMycovariantDraftPick(currentPlayer, picked);
            // UI: Visual feedback (animation), then next drafter
            AnimatePickFeedback(picked, () => {
                draftIndex++;
                BeginNextDraft();
            });
        }

        private void AutoPickForAI()
        {
            var randomPick = draftChoices[rng.Next(draftChoices.Count)];

            OnChoicePicked(randomPick);
        }

        private void ShowDraftUI() => draftPanel.SetActive(true);
        private void HideDraftUI() => draftPanel.SetActive(false);
        private void AnimatePickFeedback(Mycovariant picked, System.Action onComplete)
        {
            // Optional: scale up, glow, play sound, etc.
            onComplete?.Invoke();
        }
    }

}