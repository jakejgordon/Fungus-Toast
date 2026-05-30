using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FungusToast.Core.Players;
using FungusToast.Unity.UI;

namespace FungusToast.Unity.UI.Hotseat
{
    public class UI_HotseatTurnPrompt : MonoBehaviour
    {
        private static UI_HotseatTurnPrompt singleton; // guard against duplicate components registering same button

        [Header("Root / Blocking Layer")] [SerializeField] private GameObject root;
        [SerializeField] private Button okButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private CanvasGroup canvasGroup;
        [Header("Player Visuals")] [SerializeField] private Image playerIconImage;
        [Header("Overlay Sorting")] [SerializeField] private bool forceTopMostCanvas = true;
        [SerializeField] private int overlaySortingOrder = 500;
        [Header("Behaviour")] [SerializeField] private bool useFade = true;
        [SerializeField] private float fadeDuration = 0.15f;

        private Canvas overlayCanvas;
        private GraphicRaycaster graphicRaycaster;

        private Action onConfirmed;
        private bool isShowing;
        private string instancePath;

        private void Awake()
        {
            instancePath = GetHierarchyPath(transform);
            if (singleton != null && singleton != this)
            {
                Debug.LogWarning($"[HotseatTurnPrompt] Duplicate instance detected on {instancePath}. Destroying this component; existing singleton lives on {singleton.instancePath}");
                Destroy(this);
                return;
            }
            singleton = this;

            if (root == null) root = gameObject;
            if (canvasGroup == null)
            {
                canvasGroup = root.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = root.AddComponent<CanvasGroup>();
            }

            ApplyStyle();
            EnsureOverlayCanvas();

            WireOkButton();
            HideImmediate(preserveCallback: true); // ensure hidden but do not clear future callback placeholder
        }

        private void ApplyStyle()
        {
            UIStyleTokens.ApplyPanelSurface(root, UIStyleTokens.Surface.OverlayDim);

            if (titleText != null)
            {
                titleText.color = UIStyleTokens.Text.Primary;
            }

            UIStyleTokens.Button.ApplyStyle(okButton, useSelectedAsNormal: true);

            if (playerIconImage != null)
            {
                playerIconImage.color = UIStyleTokens.Text.Primary;
            }

            UIStyleTokens.ApplyNonButtonTextPalette(root, headingSizeThreshold: 30f);
        }

        private static string GetHierarchyPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
            return path;
        }

        private void EnsureOverlayCanvas()
        {
            if (!forceTopMostCanvas || root == null)
            {
                return;
            }

            overlayCanvas = root.GetComponent<Canvas>();
            if (overlayCanvas == null)
            {
                overlayCanvas = root.AddComponent<Canvas>();
            }

            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = overlaySortingOrder;

            graphicRaycaster = root.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
            {
                graphicRaycaster = root.AddComponent<GraphicRaycaster>();
            }
        }

        private void WireOkButton()
        {
            if (okButton == null)
            {
                Debug.LogError("[HotseatTurnPrompt] okButton not assigned.");
                return;
            }
            okButton.onClick.RemoveListener(OnOkClicked);
            okButton.onClick.AddListener(OnOkClicked);
        }

        // Backward-compatible string-based Show (unused now, but retained to avoid null ref if something else still calls it)
        public void Show(string playerName, Action confirmedCallback)
        {
            ShowInternal(playerName, null, confirmedCallback);
        }

        public void Show(Player player, Action confirmedCallback)
        {
            if (player == null) { ShowInternal("(Unknown)", null, confirmedCallback); return; }

            Sprite iconSprite = null;
            try
            {
                var binder = GameManager.Instance?.GameUI?.PlayerUIBinder;
                iconSprite = binder != null ? binder.GetPlayerIcon(player.PlayerId) : null;
                if (iconSprite == null)
                {
                    iconSprite = GameManager.Instance?.gridVisualizer?.GetMoldIconTileForPlayer(player.PlayerId)?.sprite;
                }
            }
            catch { }

            ShowInternal(player.PlayerName, iconSprite, confirmedCallback);
        }

        private void ShowInternal(string playerName, Sprite icon, Action confirmedCallback)
        {
            Debug.Log($"[HotseatTurnPrompt] Show (instance={instancePath}) for '{playerName}' activeSelf={gameObject.activeSelf} activeInHierarchy={gameObject.activeInHierarchy}");
            onConfirmed = confirmedCallback; // replace previous
            isShowing = true;

            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (root != null && !root.activeSelf) root.SetActive(true);

            EnsureOverlayCanvas();
            if (root != null)
            {
                root.transform.SetAsLastSibling();
            }

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("[HotseatTurnPrompt] Not active in hierarchy after activation attempt; invoking callback immediately.");
                InvokeAndClearCallback();
                return;
            }

            if (titleText != null)
                titleText.text = $"{playerName}'s Turn";

            if (playerIconImage != null)
            {
                if (icon != null)
                {
                    playerIconImage.sprite = icon;
                    playerIconImage.enabled = true;
                    playerIconImage.color = Color.white;
                }
                else
                {
                    playerIconImage.enabled = false; // fallback hide
                }
            }

            PrepareCanvasGroup();
            if (useFade)
            {
                StopAllCoroutines();
                StartCoroutine(FadeCanvas(1f));
            }
            else if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        private void PrepareCanvasGroup()
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        private void OnOkClicked()
        {
            Debug.Log($"[HotseatTurnPrompt] OK clicked (instance={instancePath}) isShowing={isShowing} callbackSet={(onConfirmed!=null)}");
            var cb = onConfirmed;
            onConfirmed = null;
            isShowing = false;

            if (!gameObject.activeInHierarchy)
            {
                cb?.Invoke();
                return;
            }

            if (!useFade || canvasGroup == null)
            {
                HideImmediate(preserveCallback: true);
                cb?.Invoke();
                return;
            }
            StopAllCoroutines();
            StartCoroutine(FadeOutThen(() => { HideImmediate(preserveCallback: true); cb?.Invoke(); }));
        }

        private void InvokeAndClearCallback()
        {
            var cb = onConfirmed; onConfirmed = null; cb?.Invoke();
        }

        private System.Collections.IEnumerator FadeOutThen(Action after)
        {
            yield return FadeCanvas(0f);
            after?.Invoke();
        }

        private System.Collections.IEnumerator FadeCanvas(float target)
        {
            if (canvasGroup == null) yield break;
            float start = canvasGroup.alpha;
            float duration = Mathf.Max(0.0001f, fadeDuration);
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            canvasGroup.alpha = target;
        }

        public void HideImmediate(bool preserveCallback = false)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            if (root != null) root.SetActive(false);
            if (gameObject.activeSelf) gameObject.SetActive(false);
            isShowing = false;
            if (!preserveCallback) onConfirmed = null;
        }
    }
}
