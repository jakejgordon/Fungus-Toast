using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Game;
using FungusToast.Core;

namespace FungusToast.UI
{
    public class MutationUIManager : MonoBehaviour
    {
        [Header("References")]
        public MutationManager mutationManager;
        public GameObject mutationTreePanel;
        public Button spendPointsButton;
        public TextMeshProUGUI spendPointsButtonText;
        [Header("Mutation Node Prefab")]
        public GameObject mutationNodePrefab;
        public Transform mutationNodeParent;

        [Header("Animation")]
        public float slideDuration = 0.5f;
        private bool isTreeOpen = false;

        private RectTransform mutationTreeRect;

        private void Start()
        {
            mutationTreeRect = mutationTreePanel.GetComponent<RectTransform>();
            CloseTreeInstant();
            UpdateSpendPointsButton();
        }

        private void Update()
        {
            UpdateSpendPointsButton();
        }

        private void UpdateSpendPointsButton()
        {
            if (mutationManager.CurrentMutationPoints > 0)
            {
                spendPointsButton.interactable = true;
                spendPointsButtonText.text = $"Spend {mutationManager.CurrentMutationPoints} Points!";
                // Optionally pulse/glow the button here (later)
            }
            else
            {
                spendPointsButton.interactable = false;
                spendPointsButtonText.text = "No Points Available";
            }
        }

        public void OnSpendPointsClicked()
        {
            if (!isTreeOpen)
                OpenTree();
            else
                CloseTree();
        }

        private void OpenTree()
        {
            mutationTreePanel.SetActive(true);
            isTreeOpen = true;
            // (Optional) Animate sliding open here later
        }

        private void CloseTree()
        {
            mutationTreePanel.SetActive(false);
            isTreeOpen = false;
            // (Optional) Animate sliding closed here later
        }

        private void CloseTreeInstant()
        {
            mutationTreePanel.SetActive(false);
            isTreeOpen = false;
        }

        private void SpawnMutationNodes()
        {
            foreach (var mutation in mutationManager.Mutations)
            {
                GameObject nodeGO = Instantiate(mutationNodePrefab, mutationNodeParent);
                MutationNodeUI nodeUI = nodeGO.GetComponent<MutationNodeUI>();
                nodeUI.Initialize(mutation, this);
            }
        }

        public bool TryUpgradeMutation(Mutation mutation)
        {
            if (mutationManager.TryUpgradeMutation(mutation))
            {
                UpdateSpendPointsButton();
                return true;
            }
            return false;
        }
    }
}
