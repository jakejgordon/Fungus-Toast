using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace FungusToast.Unity.Cameras
{
    public class CameraCenterer : MonoBehaviour
    {
        [Header("References")] public GameManager gameManager;
        [Tooltip("Fallback sidebar width (world units) if RectTransforms not assigned.")] public float sidebarWidthInUnits =5f;
        public float padding =2f; public float moveDuration =1.0f;
        [Header("Optional UI Sidebars")] [SerializeField] private RectTransform leftSidebarRect; [SerializeField] private RectTransform rightSidebarRect;
        [Header("Diagnostics")] public bool logFramingDebug = false;

        private CanvasScaler _canvasScaler;
        private Vector3 targetPosition;
        private float targetOrthographicSize;
        private Coroutine moveCoroutine;

        private bool _initialFramingCaptured;
        private Vector3 _initialPosition;
        private float _initialOrthographicSize;

        private void Awake()
        {
            if (leftSidebarRect) _canvasScaler = leftSidebarRect.GetComponentInParent<CanvasScaler>();
            else if (rightSidebarRect) _canvasScaler = rightSidebarRect.GetComponentInParent<CanvasScaler>();
        }
        private void Start()
        {
            if (gameManager?.Board != null) { CenterCameraInstant(); CaptureInitialFraming(); }
        }
        /// <summary>
        /// Capture current camera position & size as the reference initial framing.
        /// Call after first board render & UI layout.
        /// </summary>
        public void CaptureInitialFraming() { if (Camera.main == null) return; _initialFramingCaptured = true; _initialPosition = Camera.main.transform.position; _initialOrthographicSize = Camera.main.orthographicSize; }
        public void CenterCameraInstant() { CalculateTarget(); ApplyCameraInstantly(); }
        public void CenterCameraSmooth() { CalculateTarget(); if (moveCoroutine != null) StopCoroutine(moveCoroutine); moveCoroutine = StartCoroutine(SmoothMove(moveDuration)); }
        public void RestoreInitialFramingSmooth(float duration) { if (!_initialFramingCaptured || Camera.main == null) return; if (moveCoroutine != null) StopCoroutine(moveCoroutine); moveCoroutine = StartCoroutine(SmoothMoveTo(_initialPosition, _initialOrthographicSize, duration)); }

        private float GetCanvasScaleFactor()
        {
            if (_canvasScaler == null || _canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) return 1f;
            float wScale = Screen.width / _canvasScaler.referenceResolution.x; float hScale = Screen.height / _canvasScaler.referenceResolution.y; return Mathf.Lerp(wScale, hScale, _canvasScaler.matchWidthOrHeight);
        }

        private void CalculateTarget()
        {
            if (Camera.main == null || gameManager?.Board == null) return;
            int boardW = gameManager.Board.Width; int boardH = gameManager.Board.Height;
            float aspect = (float)Screen.width / Screen.height;

            // Fit board only (ignore sidebars for size). Choose larger dimension requirement.
            float sizeByHeight = (boardH /2f) + padding;
            float sizeByWidth = (boardW / (2f * aspect)) + padding;
            targetOrthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);

            // Visible world horizontal span
            float visibleWorldWidth =2f * targetOrthographicSize * aspect;
            float unitsPerPixel = visibleWorldWidth / Screen.width;

            // Sidebar pixel widths (with canvas scaling)
            float scaleFactor = GetCanvasScaleFactor();
            float leftPixels = leftSidebarRect ? leftSidebarRect.rect.width * scaleFactor : (sidebarWidthInUnits / unitsPerPixel);
            float rightPixels = rightSidebarRect ? rightSidebarRect.rect.width * scaleFactor : (sidebarWidthInUnits / unitsPerPixel);

            // Delta from screen center to region center: (leftPixels - rightPixels)/2
            float deltaPixels = (leftPixels - rightPixels) *0.5f;
            float deltaWorld = deltaPixels * unitsPerPixel;

            // Camera center world X so that board center aligns with region midpoint
            float boardCenterWorld = boardW /2f;
            float cameraCenterWorldX = boardCenterWorld - deltaWorld;

            targetPosition = new Vector3(cameraCenterWorldX, boardH /2f, -10f);

            if (logFramingDebug)
            {
                Debug.Log($"[CameraCenterer] board {boardW}x{boardH} sizeH={sizeByHeight:F2} sizeW={sizeByWidth:F2} finalSize={targetOrthographicSize:F2} visWidth={visibleWorldWidth:F2} unitsPx={unitsPerPixel:F4} leftPx={leftPixels:F1} rightPx={rightPixels:F1} deltaPx={deltaPixels:F1} camX={cameraCenterWorldX:F2}");
            }
        }

        private void ApplyCameraInstantly() { if (Camera.main == null) return; Camera.main.transform.position = targetPosition; Camera.main.orthographicSize = targetOrthographicSize; }
        private IEnumerator SmoothMove(float duration) { Vector3 startPos = Camera.main.transform.position; float startSize = Camera.main.orthographicSize; float elapsed =0f; while (elapsed < duration) { elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / duration); t = t * t * (3f -2f * t); Camera.main.transform.position = Vector3.Lerp(startPos, targetPosition, t); Camera.main.orthographicSize = Mathf.Lerp(startSize, targetOrthographicSize, t); yield return null; } ApplyCameraInstantly(); }
        private IEnumerator SmoothMoveTo(Vector3 endPos, float endSize, float duration) { Vector3 startPos = Camera.main.transform.position; float startSize = Camera.main.orthographicSize; float elapsed =0f; while (elapsed < duration) { elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / duration); t = t * t * (3f -2f * t); Camera.main.transform.position = Vector3.Lerp(startPos, endPos, t); Camera.main.orthographicSize = Mathf.Lerp(startSize, endSize, t); yield return null; } Camera.main.transform.position = endPos; Camera.main.orthographicSize = endSize; }
    }
}
