using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine.EventSystems;
using System.Text;

namespace FungusToast.Unity.UI.MutationTree
{
    public class MutationNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI mutationNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField] private GameObject upgradeCostGroup;
        [SerializeField] private TextMeshProUGUI upgradeCostText;

        [Header("Surge UI")]
        [SerializeField] private GameObject surgeActiveOverlay;    // Should be top left, with icon+text child
        [SerializeField] private Image surgeActiveIcon;            // The hourglass icon
        [SerializeField] private TextMeshProUGUI surgeActiveText;  // The countdown number

        [Header("Highlight")]
        [SerializeField] private Outline highlightOutline;

        private Mutation mutation;
        private UI_MutationManager uiManager;
        private Player player;

        public int MutationId => mutation.Id;

        public void Initialize(Mutation mutation, Player player, UI_MutationManager uiManager)
        {
            this.mutation = mutation;
            this.player = player;
            this.uiManager = uiManager;

            mutationNameText.text = mutation.Name;
            UpdateDisplay();

            // Ensure highlight is off by default
            if (highlightOutline != null)
                highlightOutline.enabled = false;

            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        private void OnUpgradeClicked()
        {
            if (!player.CanUpgrade(mutation))
                return;

            upgradeButton.interactable = false;

            bool success = uiManager.TryUpgradeMutation(mutation);

            if (success)
            {
                UpdateDisplay();
            }
            else
            {
                upgradeButton.interactable = true;
            }
        }

        public void UpdateDisplay()
        {
            int currentLevel = player.GetMutationLevel(mutation.Id);
            levelText.text = $"Level {currentLevel}/{mutation.MaxLevel}";

            // SURGE LOGIC
            bool isSurge = mutation.IsSurge;
            bool isSurgeActive = isSurge && player.IsSurgeActive(mutation.Id);
            int surgeTurns = isSurgeActive ? player.GetSurgeTurnsRemaining(mutation.Id) : 0;

            // PREREQS
            bool isLocked = false;
            foreach (var prereq in mutation.Prerequisites)
            {
                if (player.GetMutationLevel(prereq.MutationId) < prereq.RequiredLevel)
                {
                    isLocked = true;
                    break;
                }
            }
            bool isMaxed = currentLevel >= mutation.MaxLevel;

            // COST CALC
            int upgradeCost = isSurge
                ? mutation.GetSurgeActivationCost(currentLevel)
                : mutation.PointsPerUpgrade;

            bool canAfford = player.MutationPoints >= upgradeCost;

            // INTERACTABLE LOGIC
            bool interactable = !isLocked && canAfford && !isMaxed;
            if (isSurge && isSurgeActive)
                interactable = false;

            upgradeButton.interactable = interactable;

            // LOCK/SURGE UI
            lockOverlay.SetActive(isLocked && !isSurgeActive);

            if (canvasGroup != null)
                canvasGroup.alpha = (isLocked || isSurgeActive) ? 0.5f : 1f;

            // Surge overlay (shows when surge is active)
            if (surgeActiveOverlay != null)
            {
                surgeActiveOverlay.SetActive(isSurgeActive);
                if (isSurgeActive)
                {
                    if (surgeActiveIcon != null)
                        surgeActiveIcon.enabled = true;
                    if (surgeActiveText != null)
                        surgeActiveText.text = surgeTurns.ToString();
                }
            }

            // Show cost (top right)
            if (upgradeCostGroup != null && upgradeCostText != null)
            {
                if (upgradeCost > 1)
                {
                    upgradeCostGroup.SetActive(true);
                    upgradeCostText.text = $"x{upgradeCost}";
                }
                else
                {
                    upgradeCostGroup.SetActive(false);
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var tooltipText = BuildTooltip();
            Vector2 screenPosition = Input.mousePosition;
            uiManager.ShowMutationDescription(tooltipText, screenPosition);
            uiManager.HighlightUnmetPrerequisites(mutation, player);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            uiManager.ClearMutationDescription();
            uiManager.ClearAllHighlights();
        }

        private string BuildTooltip()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"<b>{mutation.Name}</b>");
            sb.AppendLine($"<i>(Tier {mutation.TierNumber} • {mutation.Category})</i>");
            sb.AppendLine();

            int currentLevel = player.GetMutationLevel(mutation.Id);

            // Show surge state if relevant
            if (mutation.IsSurge && player.IsSurgeActive(mutation.Id))
            {
                int turns = player.GetSurgeTurnsRemaining(mutation.Id);
                sb.AppendLine($"<color=#90f>Currently Active ({turns} turn{(turns == 1 ? "" : "s")} left)</color>");
                sb.AppendLine();
            }

            int cost = mutation.IsSurge
                ? mutation.GetSurgeActivationCost(currentLevel)
                : mutation.PointsPerUpgrade;

            sb.AppendLine($"<b>Cost:</b> {cost} mutation point{(cost == 1 ? "" : "s")}");
            sb.AppendLine();

            if (mutation.Prerequisites.Count > 0)
            {
                sb.AppendLine("<i>Requires:</i>");
                foreach (var prereq in mutation.Prerequisites)
                {
                    int ownedLevel = player.GetMutationLevel(prereq.MutationId);
                    var prereqMutation = uiManager.GetMutationById(prereq.MutationId);
                    string prereqText = $"- {prereqMutation?.Name ?? "Unknown"} (Level {ownedLevel}/{prereq.RequiredLevel})";
                    if (ownedLevel < prereq.RequiredLevel)
                    {
                        sb.AppendLine($"<color=#CFFF04>{prereqText}</color>"); // Yellow-green for unmet
                    }
                    else
                    {
                        sb.AppendLine(prereqText);
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine(mutation.Description);

            if (!string.IsNullOrEmpty(mutation.FlavorText))
            {
                sb.AppendLine();
                sb.AppendLine($"<i>{mutation.FlavorText}</i>");
            }

            return sb.ToString();
        }

        public void DisableUpgrade()
        {
            if (upgradeButton != null)
                upgradeButton.interactable = false;
        }

        public void SetHighlight(bool on)
        {
            if (highlightOutline != null)
                highlightOutline.enabled = on;
        }
    }
}
