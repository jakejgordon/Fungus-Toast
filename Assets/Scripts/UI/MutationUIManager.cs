using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Mutations;
using FungusToast.Core.Players;
using UnityEngine.Tilemaps;
using FungusToast.Grid;

namespace FungusToast.Game
{
    public class MutationUIManager : MonoBehaviour
    {
        [Header("General UI References")]
        [SerializeField] private MutationManager mutationManager;
        [SerializeField] private GameObject mutationTreePanel;
        [SerializeField] private Button spendPointsButton;
        [SerializeField] private TextMeshProUGUI spendPointsButtonText;
        [SerializeField] private Outline buttonOutline;

        [Header("Mold Icon Display")]
        [SerializeField] private Image playerMoldIcon;
        [SerializeField] private GridVisualizer gridVisualizer;


        [Header("Mutation Tree References")]
        [SerializeField] private GameObject mutationNodePrefab;
        [SerializeField] private Transform mutationNodeParent;

        [Header("Mutation Tree Layout Settings")]
        [SerializeField] private Vector2 mutationButtonSize = new Vector2(120, 120);

        [SerializeField] private TextMeshProUGUI dockButtonText;
        [SerializeField] private TextMeshProUGUI mutationDescriptionText;
        [SerializeField] private GameObject mutationDescriptionBackground;

        [Header("Tree Sliding Settings")]
        public float slideDuration = 0.5f;
        public Vector2 hiddenPosition = new Vector2(-1920, 0);
        public Vector2 visiblePosition = new Vector2(0, 0);

        [Header("Pulse Settings")]
        public float pulseStrength = 0.05f;
        public float pulseSpeed = 2f;

        private RectTransform mutationTreeRect;
        private Vector3 originalButtonScale;
        private bool isTreeOpen = false;
        private bool isSliding = false;

        private Player humanPlayer;
        private bool humanTurnEnded = false;
        private Coroutine tooltipFadeCoroutine;

        private void Start()
        {
            if (mutationTreePanel != null)
                mutationTreeRect = mutationTreePanel.GetComponent<RectTransform>();
            else
                Debug.LogError("mutationTreePanel is NULL at Start()!");


            GridLayoutGroup grid = mutationNodeParent.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.cellSize = mutationButtonSize;
                float spacing = mutationButtonSize.x * 0.2f;
                grid.spacing = new Vector2(spacing, spacing);
            }
            else
            {
                Debug.LogWarning("GridLayoutGroup not found on mutationNodeParent!");
            }

            RefreshSpendPointsButtonUI();
            originalButtonScale = spendPointsButton.transform.localScale;
            spendPointsButton.onClick.AddListener(OnSpendPointsClicked);
            SetSpendPointsButtonVisible(false);
        }

        private void Update()
        {
            if (humanPlayer != null && humanPlayer.MutationPoints > 0)
                AnimatePulse();
            else
                ResetPulse();
        }

        public void Initialize(Player player)
        {
            if (gridVisualizer == null)
            {
                Debug.LogError("❌ GridVisualizer is not assigned to MutationUIManager!");
                return;
            }

            if (player == null)
            {
                Debug.LogError("❌ Player passed to Initialize() is null!");
                return;
            }

            humanPlayer = player;
            humanTurnEnded = false;
            RefreshSpendPointsButtonUI();

            Tile tile = gridVisualizer.GetTileForPlayer(player.PlayerId);
            if (tile != null && tile.sprite != null)
            {
                playerMoldIcon.sprite = tile.sprite;
                playerMoldIcon.enabled = true;
            }
            else
            {
                Debug.LogWarning("Player tile or tile sprite is null.");
                playerMoldIcon.enabled = false;
            }
        }



        public void OnSpendPointsClicked()
        {
            if (isSliding) return;

            if (!isTreeOpen)
                StartCoroutine(SlideInTree());
            else
                StartCoroutine(SlideOutTree());
        }

        public void SetSpendPointsButtonVisible(bool visible)
        {
            if (spendPointsButton != null)
                spendPointsButton.gameObject.SetActive(visible);
        }

        public void PopulateRootMutations()
        {
            ClearMutationNodes();

            foreach (var rootMutation in mutationManager.RootMutations)
            {
                CreateRootMutationButton(rootMutation.Value);
            }
        }

        public bool TryUpgradeMutation(Mutation mutation)
        {
            Debug.Log($"TMutationUIManager.TryUpgradeMutation: Player {humanPlayer.PlayerId} has {humanPlayer.MutationPoints} points before upgrade.");

            if (mutationManager.TryUpgradeMutation(mutation, humanPlayer))
            {
                RefreshSpendPointsButtonUI();
                TryEndHumanTurn();
                return true;
            }
            return false;
        }


        public void TogglePanelDock()
        {
            if (isTreeOpen)
                StartCoroutine(SlideOutTree());
            else
                StartCoroutine(SlideInTree());
        }

        public void ShowMutationDescription(string description, RectTransform sourceRect)
        {
            if (mutationDescriptionBackground != null)
            {
                CanvasGroup cg = mutationDescriptionBackground.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    if (tooltipFadeCoroutine != null)
                        StopCoroutine(tooltipFadeCoroutine);

                    tooltipFadeCoroutine = StartCoroutine(FadeTooltip(cg, 1f, 0.2f));
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
            }

            if (mutationDescriptionText != null)
            {
                mutationDescriptionText.gameObject.SetActive(true);
                mutationDescriptionText.text = description;
            }

            if (sourceRect != null && mutationDescriptionBackground != null)
            {
                RectTransform descRect = mutationDescriptionBackground.GetComponent<RectTransform>();

                Vector2 anchoredPosition = sourceRect.anchoredPosition;
                float verticalOffset = sourceRect.rect.height + 20f;

                descRect.pivot = new Vector2(0f, 1f);
                anchoredPosition.y -= verticalOffset;

                descRect.anchoredPosition = anchoredPosition;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(mutationDescriptionBackground.GetComponent<RectTransform>());
        }

        public void ClearMutationDescription()
        {
            if (mutationDescriptionBackground != null)
            {
                CanvasGroup cg = mutationDescriptionBackground.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    if (tooltipFadeCoroutine != null)
                        StopCoroutine(tooltipFadeCoroutine);

                    tooltipFadeCoroutine = StartCoroutine(FadeTooltip(cg, 0f, 0.2f));
                    cg.interactable = false;
                    cg.blocksRaycasts = false;
                }
            }

            if (mutationDescriptionText != null)
                mutationDescriptionText.text = "";
        }

        private IEnumerator SlideInTree()
        {
            isSliding = true;

            mutationTreePanel.SetActive(true);
            isTreeOpen = true;

            Vector2 startingPos = mutationTreeRect.anchoredPosition;
            float elapsedTime = 0f;

            while (elapsedTime < slideDuration)
            {
                mutationTreeRect.anchoredPosition = Vector2.Lerp(startingPos, visiblePosition, elapsedTime / slideDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            mutationTreeRect.anchoredPosition = visiblePosition;

            if (dockButtonText != null)
                dockButtonText.text = "<";

            isSliding = false;
        }

        private IEnumerator SlideOutTree()
        {
            isSliding = true;

            isTreeOpen = false;

            Vector2 startingPos = mutationTreeRect.anchoredPosition;
            float elapsedTime = 0f;

            while (elapsedTime < slideDuration)
            {
                mutationTreeRect.anchoredPosition = Vector2.Lerp(startingPos, hiddenPosition, elapsedTime / slideDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            mutationTreeRect.anchoredPosition = hiddenPosition;
            mutationTreePanel.SetActive(false);

            if (dockButtonText != null)
                dockButtonText.text = ">";

            isSliding = false;
        }

        private void RefreshSpendPointsButtonUI()
        {
            Debug.Log($"RefreshSpendPointsButtonUI() called");

            if (spendPointsButton == null || buttonOutline == null || humanPlayer == null)
                return;

            int points = humanPlayer.MutationPoints;
            Debug.Log($"RefreshSpendPointsButtonUI() made it to points assignment with {points} points.");
            if (points > 0)
            {
                spendPointsButton.interactable = true;
                spendPointsButtonText.text = $"Spend {points} Points!";
                Debug.Log($"[UI] Updated button: {points} points");
                buttonOutline.enabled = true;
            }
            else
            {
                spendPointsButton.interactable = false;
                spendPointsButtonText.text = "No Points Available";
                buttonOutline.enabled = false;
            }
        }

        private void AnimatePulse()
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed);
            float scale = 1f + pulse * pulseStrength;
            spendPointsButton.transform.localScale = originalButtonScale * scale;

            if (buttonOutline != null)
            {
                Color baseColor = new Color(1f, 1f, 0.7f, 1f);
                float normalizedPulse = (pulse + 1f) / 2f;
                float alpha = Mathf.Lerp(0.5f, 1f, normalizedPulse);
                buttonOutline.effectColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }
        }

        private void ResetPulse()
        {
            if (spendPointsButton != null)
                spendPointsButton.transform.localScale = originalButtonScale;
        }

        private void ClearMutationNodes()
        {
            foreach (Transform child in mutationNodeParent)
                Destroy(child.gameObject);
        }

        private void CreateRootMutationButton(Mutation mutation)
        {
            GameObject buttonGO = Instantiate(mutationNodePrefab, mutationNodeParent);
            MutationNodeUI nodeUI = buttonGO.GetComponent<MutationNodeUI>();
            nodeUI.Initialize(mutation, this);

            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.sizeDelta = mutationButtonSize;
        }

        private IEnumerator FadeTooltip(CanvasGroup cg, float targetAlpha, float duration)
        {
            float startAlpha = cg.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            cg.alpha = targetAlpha;
        }

        private void TryEndHumanTurn()
        {
            if (!humanTurnEnded && humanPlayer != null && humanPlayer.MutationPoints <= 0)
            {
                humanTurnEnded = true;
                Debug.Log("Human has spent all mutation points. Closing panel and triggering AI...");
                StartCoroutine(ClosePanelThenTriggerAI());
            }
        }

        private IEnumerator ClosePanelThenTriggerAI()
        {
            if (isTreeOpen)
                yield return StartCoroutine(SlideOutTree());

            GameManager.Instance.SpendAllMutationPointsForAIPlayers();
        }
    }
}
