using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core;
using FungusToast.Game;

public class MutationUIManager : MonoBehaviour
{
    [Header("General UI References")]
    [SerializeField] private MutationManager mutationManager;
    [SerializeField] private GameObject mutationTreePanel;
    [SerializeField] private Button spendPointsButton;
    [SerializeField] private TextMeshProUGUI spendPointsButtonText;
    [SerializeField] private Outline buttonOutline;

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
    private bool isTreeOpen = false;
    private Vector3 originalButtonScale;

    private void Start()
    {
        Debug.Log("MutationUIManager Start() running");

        if (mutationTreePanel != null)
        {
            mutationTreeRect = mutationTreePanel.GetComponent<RectTransform>();
        }
        else
        {
            Debug.LogError("mutationTreePanel is NULL at Start()!");
        }

        //  Dynamically adjust Grid Layout Group for Mutation Tree buttons
        GridLayoutGroup grid = mutationNodeParent.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.cellSize = mutationButtonSize;

            // Optional: Set Spacing to be about 15%-20% of button size
            float spacing = mutationButtonSize.x * 0.2f; // 20% of button width
            grid.spacing = new Vector2(spacing, spacing);
        }
        else
        {
            Debug.LogWarning("GridLayoutGroup not found on mutationNodeParent!");
        }

        UpdateSpendPointsButton();
        originalButtonScale = spendPointsButton.transform.localScale;
        spendPointsButton.onClick.AddListener(OnSpendPointsClicked);
        SetSpendPointsButtonVisible(false);
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

    private bool isSliding = false;

    public void OnSpendPointsClicked()
    {
        Debug.Log("Spend Points Button clicked!");

        if (isSliding)
        {
            Debug.Log("Blocked click because already sliding");
            return;
        }

        if (!isTreeOpen)
        {
            Debug.Log("Starting SlideInTree coroutine");
            StartCoroutine(SlideInTree());
        }
        else
        {
            Debug.Log("Starting SlideOutTree coroutine");
            StartCoroutine(SlideOutTree());
        }
    }

    private IEnumerator SlideInTree()
    {
        isSliding = true;

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

        Debug.Log("AFTER SlideInTree:");
        Debug.Log($"Background Active: {mutationDescriptionBackground.activeSelf}");
        Debug.Log($"Text Active: {mutationDescriptionText.gameObject.activeSelf}");
        Debug.Log($"Text anchorMin: {mutationDescriptionText.rectTransform.anchorMin}");
        Debug.Log($"Text anchorMax: {mutationDescriptionText.rectTransform.anchorMax}");

        ClearMutationNodes();
        PopulateRootMutations();

        if (dockButtonText != null)
            dockButtonText.text = "<";

        isSliding = false;
    }


    private IEnumerator SlideOutTree()
    {
        isSliding = true;

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
        mutationTreePanel.SetActive(false);

        // Update chevron
        if (dockButtonText != null)
            dockButtonText.text = ">";

        isSliding = false;
    }


    private void CloseTreeInstant()
    {
        mutationTreePanel.SetActive(false);
        mutationTreeRect.anchoredPosition = hiddenPosition;
        isTreeOpen = false;
    }

    private void UpdateSpendPointsButton()
    {
        if (spendPointsButton == null || buttonOutline == null)
            return;

        if (mutationManager.CurrentMutationPoints > 0)
        {
            spendPointsButton.interactable = true;
            spendPointsButtonText.text = $"Spend {mutationManager.CurrentMutationPoints} Points!";
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

        // Button Scale
        float scale = 1f + pulse * pulseStrength;
        spendPointsButton.transform.localScale = originalButtonScale * scale;

        // Outline Glow Pulse
        if (buttonOutline != null)
        {
            Color baseColor = new Color(1f, 1f, 0.7f, 1f); // soft yellow
            float normalizedPulse = (pulse + 1f) / 2f;
            float alpha = Mathf.Lerp(0.5f, 1f, normalizedPulse);
            buttonOutline.effectColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        }
    }

    private void ResetPulse()
    {
        spendPointsButton.transform.localScale = originalButtonScale;
    }

    private void PopulateMutationTree()
    {
        // Clear existing nodes
        foreach (Transform child in mutationNodeParent)
        {
            Destroy(child.gameObject);
        }

        // Spawn new nodes for each root mutation
        foreach (var mutation in mutationManager.RootMutations)
        {
            CreateMutationNode(mutation);
        }
    }

    private void CreateMutationNode(Mutation mutation)
    {
        if (mutationNodePrefab == null)
        {
            Debug.LogError("MutationNodePrefab is NULL when trying to create MutationNode!");
            return;
        }

        GameObject nodeGO = Instantiate(mutationNodePrefab, mutationNodeParent);
        MutationNodeUI nodeUI = nodeGO.GetComponent<MutationNodeUI>();

        nodeUI.Initialize(mutation, this);
    }

    public bool TryUpgradeMutation(Mutation mutation)
    {
        if (mutationManager.TryUpgradeMutation(mutation))
        {
            UpdateSpendPointsButton(); // Refresh UI if upgrade succeeds
            return true;
        }
        return false;
    }

    public void SetSpendPointsButtonVisible(bool visible)
    {
        spendPointsButton.gameObject.SetActive(visible);
    }

    private void ClearMutationNodes()
    {
        foreach (Transform child in mutationNodeParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void TogglePanelDock()
    {
        if (isTreeOpen)
        {
            StartCoroutine(SlideOutTree());
        }
        else
        {
            StartCoroutine(SlideInTree());
        }
    }

    public void PopulateRootMutations()
    {
        foreach (var rootMutation in mutationManager.RootMutations)
        {
            CreateRootMutationButton(rootMutation);
        }
    }

    private void CreateRootMutationButton(Mutation mutation)
    {
        GameObject buttonGO = Instantiate(mutationNodePrefab, mutationNodeParent);
        MutationNodeUI nodeUI = buttonGO.GetComponent<MutationNodeUI>();
        nodeUI.Initialize(mutation, this);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.sizeDelta = mutationButtonSize; // Use configured size
    }



    private Coroutine tooltipFadeCoroutine;

    public void ShowMutationDescription(string description, RectTransform sourceRect)
    {
        if (mutationDescriptionBackground != null)
        {
            CanvasGroup cg = mutationDescriptionBackground.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                // If another fade is running, stop it
                if (tooltipFadeCoroutine != null)
                    StopCoroutine(tooltipFadeCoroutine);

                tooltipFadeCoroutine = StartCoroutine(FadeTooltip(cg, 1f, 0.2f)); // Fade IN
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        if (mutationDescriptionText != null)
        {
            mutationDescriptionText.gameObject.SetActive(true); // Stays active, now just visible/invisible
            mutationDescriptionText.text = description;
        }

        if (sourceRect != null && mutationDescriptionBackground != null)
        {
            RectTransform descRect = mutationDescriptionBackground.GetComponent<RectTransform>();

            Vector2 anchoredPosition = sourceRect.anchoredPosition;
            float buttonHeight = sourceRect.rect.height;
            float verticalOffset = buttonHeight + 20f; // Button height + breathing room

            // Set pivot so that left side of tooltip aligns with left side of button
            descRect.pivot = new Vector2(0f, 1f); // (0,1) = Top-Left corner of the tooltip

            // Set anchoredPosition so that tooltip's top-left corner matches button's bottom-left
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

                tooltipFadeCoroutine = StartCoroutine(FadeTooltip(cg, 0f, 0.2f)); // Fade OUT
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        if (mutationDescriptionText != null)
        {
            mutationDescriptionText.text = "";
        }
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




}
