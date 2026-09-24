using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FungusToast.Unity.UI.Onboarding
{
    /// <summary>
    /// Centered first-game coachmark that frames what the player is here to do before the
    /// round-1 Spend Points and camera coachmarks appear. Those two check <see cref="IsActive"/>
    /// so a brand-new player reads one thing at a time: this closes, then Spend Points shows
    /// right away and the camera hint follows after its own delay.
    /// </summary>
    public sealed class NewPlayerWelcomeCoachmark
    {
        // Larger than the anchored coachmarks: it sits alone in the middle of the board.
        private const float Width = 560f;
        private const float MinHeight = 150f;
        private const float TitleFontSize = 28f;
        private const float BodyFontSize = 21f;
        private const float TitleRowHeight = 42f;
        private const float BodyTopInset = CoachmarkLayoutUtility.TitleTopInset + TitleRowHeight + 6f;
        private const float BodyHorizontalPadding = 20f;
        private const float BodyBottomPadding = BodyHorizontalPadding + CoachmarkLayoutUtility.BodyBottomExtraInset;

        private readonly Func<Canvas> resolveRootCanvas;
        private readonly Func<bool> getForceFirstGameExperience;

        private CoachmarkLayoutUtility.CoachmarkCard card;
        private bool isArmed;
        private bool hasDismissedThisGame;
        private bool hasEvaluatedThisGame;

        public NewPlayerWelcomeCoachmark(Func<Canvas> resolveRootCanvas, Func<bool> getForceFirstGameExperience)
        {
            this.resolveRootCanvas = resolveRootCanvas;
            this.getForceFirstGameExperience = getForceFirstGameExperience;
        }

        /// <summary>Raised only when the player closes the coachmark with its X button.</summary>
        public event Action ClosedByPlayer;

        /// <summary>
        /// True until the coachmark is acknowledged, or until the round-1 check has run and
        /// declined it. Dependent coachmarks stay hidden through the whole window, including
        /// the game-start intro that plays before round 1 evaluates this one, and the show delay.
        /// </summary>
        public bool IsActive => !hasEvaluatedThisGame || isArmed || IsVisible;

        public bool IsVisible => card != null && card.IsShowing;

        /// <summary>True from the round-1 check passing until acknowledged: the show delay plus on-screen time.</summary>
        public bool IsPendingOrVisible => isArmed || IsVisible;

        /// <summary>
        /// Runs the catalog rule and, if it passes, reserves the round-1 coachmark slot. The
        /// caller then shows it after the game-start title card has cleared.
        /// </summary>
        public bool TryArm(int currentRound, int humanPlayerCount, bool isFastForwarding)
        {
            if (IsPendingOrVisible)
            {
                return false;
            }

            hasEvaluatedThisGame = true;
            if (!NewPlayerTooltipRules.ShouldShowWelcomeIntro(
                    getForceFirstGameExperience(),
                    currentRound,
                    humanPlayerCount,
                    hasDismissedThisGame,
                    isFastForwarding))
            {
                return false;
            }

            isArmed = true;
            return true;
        }

        public void ShowIfArmed()
        {
            if (!isArmed)
            {
                return;
            }

            isArmed = false;
            card ??= BuildCard();
            if (card == null)
            {
                return;
            }

            card.Show(NewPlayerTooltipCatalog.Get(NewPlayerTooltipId.WelcomeIntro));
            RefreshLayout();
        }

        /// <summary>
        /// Closes the coachmark without raising <see cref="ClosedByPlayer"/>. Used when the
        /// player moves on by themselves, e.g. by clicking Spend Points while it is open.
        /// </summary>
        public void Acknowledge()
        {
            bool wasShowing = IsPendingOrVisible;
            isArmed = false;
            hasDismissedThisGame = true;
            hasEvaluatedThisGame = true;

            if (wasShowing && !getForceFirstGameExperience())
            {
                NewPlayerTooltipCatalog.MarkSeen(NewPlayerTooltipId.WelcomeIntro);
            }

            HideImmediate();
        }

        public void ResetForNewGame()
        {
            isArmed = false;
            hasDismissedThisGame = false;
            hasEvaluatedThisGame = false;
            HideImmediate();
        }

        private void OnCloseClicked()
        {
            Acknowledge();
            ClosedByPlayer?.Invoke();
        }

        private void HideImmediate()
        {
            card?.HideImmediate();
        }

        private CoachmarkLayoutUtility.CoachmarkCard BuildCard()
        {
            Canvas rootCanvas = resolveRootCanvas?.Invoke();
            if (rootCanvas == null)
            {
                // Same fallback as the camera coachmark: any canvas beats not showing at all.
                Canvas anyCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                rootCanvas = anyCanvas != null ? anyCanvas.rootCanvas : null;
            }

            if (rootCanvas == null)
            {
                Debug.LogWarning("[NewPlayerWelcomeCoachmark] No canvas found; skipping the welcome coachmark.");
                return null;
            }

            var built = CoachmarkLayoutUtility.BuildCard(
                "UI_WelcomeCoachmark",
                rootCanvas.transform,
                new Vector2(Width, MinHeight),
                OnCloseClicked,
                TitleFontSize,
                BodyFontSize,
                pivot: new Vector2(0.5f, 0.5f),
                titleRowHeight: TitleRowHeight,
                contentInset: BodyHorizontalPadding);
            built.Root.anchoredPosition = Vector2.zero;
            return built;
        }

        private void RefreshLayout()
        {
            if (card == null || card.Root == null || card.Body == null)
            {
                return;
            }

            float availableBodyWidth = Mathf.Max(1f, Width - (2f * BodyHorizontalPadding));
            Vector2 bodyPreferredSize = card.Body.GetPreferredValues(card.Body.text, availableBodyWidth, 0f);
            float requiredHeight = BodyTopInset + bodyPreferredSize.y + BodyBottomPadding;

            card.Root.sizeDelta = new Vector2(Width, Mathf.Max(MinHeight, requiredHeight));

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(card.Root);
        }
    }
}
