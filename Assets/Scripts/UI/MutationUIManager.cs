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

    [SerializeField] private TextMeshProUGUI dockButtonText;

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
        if (mutationManager == null)
            mutationManager = FindFirstObjectByType<MutationManager>();

        mutationTreeRect = mutationTreePanel.GetComponent<RectTransform>();
        buttonOutline = spendPointsButton.GetComponent<Outline>();

        CloseTreeInstant();
        mutationTreePanel.SetActive(false); // hide at start

        UpdateSpendPointsButton();
        originalButtonScale = spendPointsButton.transform.localScale;

        // Wire up button click
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
        if (isSliding)
            return; // Prevent clicking while sliding

        if (!isTreeOpen)
            StartCoroutine(SlideInTree());
        else
            StartCoroutine(SlideOutTree());
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

        ClearMutationNodes();
        PopulateMutationTree();

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

        // 🆕 Update chevron
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

}
