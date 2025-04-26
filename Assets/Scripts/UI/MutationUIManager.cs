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
        public float pulseSpeed = 2f;       // Speed of pulsation
        public float pulseStrength = 0.05f;  // How much it grows/shrinks
        private Outline buttonOutline;

        private Vector3 originalButtonScale;

        [Header("Animation")]
        public float slideDuration = 0.5f;
        private bool isTreeOpen = false;

        private RectTransform mutationTreeRect;

        private void Start()
        {
            mutationTreeRect = mutationTreePanel.GetComponent<RectTransform>();
            buttonOutline = spendPointsButton.GetComponent<Outline>();
            originalButtonScale = spendPointsButton.transform.localScale;

            CloseTreeInstant();
            UpdateSpendPointsButton();
        }

        private void Update()
        {
            UpdateSpendPointsButton();

            if (mutationManager.CurrentMutationPoints > 0)
            {
                AnimatePulse();
            }
            else
            {
                ResetPulse();
            }
        }

        private void AnimatePulse()
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed);

            // Button Scale Pulsing
            float scale = 1f + pulse * pulseStrength;
            spendPointsButton.transform.localScale = originalButtonScale * scale;

            // Outline Glow Pulsing (only if outline exists)
            if (buttonOutline != null)
            {
                // Safely read the original base color (static, doesn't change)
                Color baseColor = new Color(1f, 1f, 0.7f, 1f); // Soft yellow glow (adjust if you want)

                // Smooth alpha pulse
                float normalizedPulse = (pulse + 1f) / 2f; // Normalize -1..1 to 0..1
                float alpha = Mathf.Lerp(0.5f, 1f, normalizedPulse); // Alpha goes from 0.5 to 1.0

                buttonOutline.effectColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }
        }



        private void ResetPulse()
        {
            spendPointsButton.transform.localScale = originalButtonScale;
        }

        private void UpdateSpendPointsButton()
        {
            if (mutationManager.CurrentMutationPoints > 0)
            {
                spendPointsButton.interactable = true;
                spendPointsButtonText.text = $"Spend {mutationManager.CurrentMutationPoints} Points!";
                buttonOutline.enabled = true; // Turn on glow
            }
            else
            {
                spendPointsButton.interactable = false;
                spendPointsButtonText.text = "No Points Available";
                buttonOutline.enabled = false; // Turn off glow
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
