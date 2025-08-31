using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Core.Board;
using FungusToast.Core.Players;
using System.Linq;

namespace FungusToast.Unity.UI
{
    public class UI_EndGamePanel : MonoBehaviour
    {
        /* ─────────── Inspector ─────────── */
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform resultsContainer;
        [SerializeField] private UI_GameEndPlayerResultsRow playerResultRowPrefab;
        [SerializeField] private Button closeButton;

        /* ─────────── Unity ─────────── */
        private void Awake()
        {
            if (playerResultRowPrefab == null)
                Debug.LogError("UI_EndGamePanel: PlayerResultRowPrefab reference is missing!");

            // Safeguard: only add listener if closeButton is assigned
            if (closeButton != null)
                closeButton.onClick.AddListener(OnClose);
            else
                Debug.LogWarning("UI_EndGamePanel: CloseButton reference is missing!");

            HideInstant();
        }

        /* ─────────── Public API ─────────── */
        public void ShowResults(List<Player> ranked, GameBoard board)
        {
            /* clear previous rows */
            foreach (Transform child in resultsContainer)
                Destroy(child.gameObject);

            /* build rows */
            int rank = 1;
            foreach (var p in ranked)
            {
                var row = Instantiate(playerResultRowPrefab, resultsContainer);
                int living = board.GetAllCellsOwnedBy(p.PlayerId).Count(c => c.IsAlive);
                int dead = board.GetAllCellsOwnedBy(p.PlayerId).Count(c => !c.IsAlive);
                Sprite icon = GameManager.Instance.GameUI.PlayerUIBinder.GetIcon(p);

                row.Populate(rank, icon, p.PlayerName, living, dead);
                rank++;
            }

            //Debug.Log($"IsPrefabAsset={UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)}");

            /* activate first */
            gameObject.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            //Debug.Log($"[EndGamePanel] after SetActive: activeSelf={gameObject.activeSelf}, inHierarchy={gameObject.activeInHierarchy}");

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("UI_EndGamePanel is still inactive – coroutine skipped.");
                return;
            }

            /* fade-in */
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(1f, 0.25f));
        }

        /* ─────────── Buttons / Helpers ─────────── */
        private void OnClose()
        {
            // Just hide the panel so the player can continue to view and zoom the board
            HideInstant();

            // Re-enable the right sidebar so players can see summaries after closing results
            if (GameManager.Instance != null && GameManager.Instance.GameUI != null && GameManager.Instance.GameUI.RightSidebar != null)
            {
                GameManager.Instance.GameUI.RightSidebar.gameObject.SetActive(true);
            }
        }

        private void HideInstant()
        {
            StopAllCoroutines();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
        {
            float start = canvasGroup.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t / duration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }
    }
}
