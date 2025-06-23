using UnityEngine;
using TMPro;
using FungusToast.Core.Players;
using System.Collections;
using System.Collections.Generic;

namespace FungusToast.Unity.UI
{
    public class UI_MoldProfilePanel : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI growthChanceText;
        public TextMeshProUGUI deathChanceText;
        public TextMeshProUGUI mpIncomeText;

        private Player trackedPlayer;
        private List<Player> allPlayers;

        // Coroutine handle for safely stopping/restarting pulse
        private Coroutine pulseCoroutine;

        public void Initialize(Player player, List<Player> players)
        {
            trackedPlayer = player;
            allPlayers = players;
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (trackedPlayer == null) return;

            growthChanceText.text = $"Hyphal Outgrowth Chance: {(trackedPlayer.GetEffectiveGrowthChance() * 100f):F2}%";

            if (allPlayers != null)
            {
                float decay = trackedPlayer.GetBaseMycelialDegradationRisk(allPlayers);
                deathChanceText.text = $"Mycelial Degradation: {decay * 100f:F2}%";
            }

            mpIncomeText.text = $"Mutation Points per Turn: {trackedPlayer.GetBaseMutationPointIncome()}";
        }

        public void Refresh()
        {
            UpdateDisplay();
        }

        /// <summary>
        /// Pulses the mutation point income text for visual feedback.
        /// </summary>
        public void PulseMutationPoints()
        {
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(PulseTextRoutine(mpIncomeText));
        }

        private IEnumerator PulseTextRoutine(TextMeshProUGUI text)
        {
            Color originalColor = text.color;
            Color pulseColor = new Color(1f, 0.92f, 0.3f); // Bright yellowish
            float originalFontSize = text.fontSize;
            float targetFontSize = originalFontSize * 1.14f;
            float duration = 0.32f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.Sin(Mathf.PI * (elapsed / duration)); // Ease in/out
                text.color = Color.Lerp(originalColor, pulseColor, t);
                text.fontSize = Mathf.Lerp(originalFontSize, targetFontSize, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            text.color = originalColor;
            text.fontSize = originalFontSize;
        }
    }
}
