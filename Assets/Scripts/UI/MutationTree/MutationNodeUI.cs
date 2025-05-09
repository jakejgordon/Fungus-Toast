using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine.EventSystems;
using System.Text;

namespace FungusToast.UI.MutationTree
{
    public class MutationNodeUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI mutationNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private CanvasGroup canvasGroup;

        private Mutation mutation;
        private UI_MutationManager uiManager;
        private Player player;

        public void Initialize(Mutation mutation, Player player, UI_MutationManager uiManager)
        {
            this.mutation = mutation;
            this.player = player;
            this.uiManager = uiManager;

            mutationNameText.text = mutation.Name;
            UpdateDisplay();

            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        private void OnUpgradeClicked()
        {
            if (!player.CanUpgrade(mutation))
                return;

            // Disable button immediately to prevent spam clicks
            upgradeButton.interactable = false;

            // Let the central manager handle the logic and side effects
            bool success = uiManager.TryUpgradeMutation(mutation);

            if (success)
            {
                UpdateDisplay();
            }
            else
            {
                // If it failed (e.g., race condition or bad state), re-enable the button
                upgradeButton.interactable = true;
            }
        }

        public void UpdateDisplay()
        {
            int currentLevel = player.GetMutationLevel(mutation.Id);
            levelText.text = $"Level {currentLevel}/{mutation.MaxLevel}";

            bool isLocked = false;
            foreach (var prereq in mutation.Prerequisites)
            {
                if (player.GetMutationLevel(prereq.MutationId) < prereq.RequiredLevel)
                {
                    isLocked = true;
                    break;
                }
            }

            bool canAfford = player.MutationPoints >= mutation.PointsPerUpgrade;
            bool isMaxed = currentLevel >= mutation.MaxLevel;

            upgradeButton.interactable = !isLocked && canAfford && !isMaxed;
            lockOverlay.SetActive(isLocked);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = isLocked ? 0.5f : 1f;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            var tooltipText = BuildTooltip();
            uiManager.ShowMutationDescription(tooltipText, transform as RectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            uiManager.ClearMutationDescription();
        }

        private string BuildTooltip()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<b>{mutation.Name}</b>");
            sb.AppendLine();

            if (mutation.Prerequisites.Count > 0)
            {
                sb.AppendLine("<i>Requires:</i>");
                foreach (var prereq in mutation.Prerequisites)
                {
                    int ownedLevel = player.GetMutationLevel(prereq.MutationId);
                    var prereqMutation = uiManager.GetMutationById(prereq.MutationId);
                    sb.AppendLine($"- {prereqMutation?.Name ?? "Unknown"} (Level {ownedLevel}/{prereq.RequiredLevel})");
                }
                sb.AppendLine();
            }

            sb.AppendLine(mutation.Description);
            return sb.ToString();
        }
    }
}
