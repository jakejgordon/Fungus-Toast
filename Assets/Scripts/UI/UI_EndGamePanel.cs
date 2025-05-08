using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FungusToast.Core;
using FungusToast.Core.Players;
using FungusToast.Game;
using System.Linq;

namespace FungusToast.UI
{
    public class UI_EndGamePanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Transform resultsContainer;
        [SerializeField] private UI_GameEndPlayerResultsRow playerResultRowPrefab;
        [SerializeField] private Button closeButton;

        /* ---------- Unity ---------- */
        private void Awake()
        {
            if (playerResultRowPrefab == null)
                Debug.LogError("UI_EndGamePanel: PlayerResultRowPrefab reference is missing!");

            closeButton.onClick.AddListener(OnClose);
            HideInstant();
        }

        /* ---------- Public API ---------- */
        public void ShowResults(List<Player> ranked, GameBoard board)
        {
            /*── clear previous rows ───────────────────────────────────*/
            foreach (Transform child in resultsContainer)
                Destroy(child.gameObject);

            /*── build rows ────────────────────────────────────────────*/
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

            /*── ACTIVATE first ────────────────────────────────────────*/
            gameObject.SetActive(true);                 // must be active BEFORE coroutine
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            /*── confirm state AFTER activation ───────────────────────*/
            Debug.Log($"[EndGamePanel] after SetActive: activeSelf={gameObject.activeSelf}, inHierarchy={gameObject.activeInHierarchy}");

            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning("UI_EndGamePanel is still inactive – coroutine skipped.");
                return;
            }

            /*── fade in ───────────────────────────────────────────────*/
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(1f, 0.25f));
        }



        /* ---------- Private helpers ---------- */
        private void OnClose()
        {
            HideInstant();   // optional: keep fade-out for polish
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void HideInstant()
        {
            StopAllCoroutines();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            float startAlpha = canvasGroup.alpha;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t / duration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }
    }
}
