using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Game;
using FungusToast.Core;
using System.Collections;

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

        // panel sliding stuff
        [Header("Mutation Tree Slide Settings")]
        public float slideDuration = 0.5f;
        public Vector2 hiddenPosition = new Vector2(-1920, 0); // Offscreen left
        public Vector2 visiblePosition = new Vector2(0, 0);    // Centered onscreen
        private RectTransform mutationTreeRect;
        private bool isTreeOpen = false;

        private Vector3 originalButtonScale;

        private void Start()
        {
            mutationTreePanel.SetActive(false); // Hide Mutation Tree Panel at start
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
                StartCoroutine(SlideInTree());
            else
                StartCoroutine(SlideOutTree());
        }

        private IEnumerator SlideInTree()
        {
            mutationTreePanel.SetActive(true);
            isTreeOpen = true;

            float elapsedTime = 0f;
            Vector2 startingPos = mutationTreeRect.anchoredPosition;

            while (elapsedTime < slideDuration)
            {
                mutationTreeRect.anchoredPosition = Vector2.Lerp(startingPos, visiblePosition, elapsedTime / slideDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            mutationTreeRect.anchoredPosition = visiblePosition;

            // Populate the mutation nodes AFTER sliding in
            PopulateMutationTree();
        }


        private IEnumerator SlideOutTree()
        {
            isTreeOpen = false;

            float elapsedTime = 0f;
            Vector2 startingPos = mutationTreeRect.anchoredPosition;

            while (elapsedTime < slideDuration)
            {
                mutationTreeRect.anchoredPosition = Vector2.Lerp(startingPos, hiddenPosition, elapsedTime / slideDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            mutationTreeRect.anchoredPosition = hiddenPosition;
            mutationTreePanel.SetActive(false); // Hide panel after slide out
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

        private void PopulateMutationTree()
        {
            // Clear any old nodes first
            foreach (Transform child in mutationNodeParent)
            {
                Destroy(child.gameObject);
            }

            // Spawn all root-level mutations
            foreach (var mutation in mutationManager.RootMutations)
            {
                CreateMutationNode(mutation);
            }
        }

        private void CreateMutationNode(Mutation mutation)
        {
            GameObject nodeGO = Instantiate(mutationNodePrefab, mutationNodeParent);
            MutationNodeUI nodeUI = nodeGO.GetComponent<MutationNodeUI>();

            nodeUI.Initialize(mutation, this);
        }

    }
}
