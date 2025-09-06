using UnityEngine;
using System.Collections;

namespace FungusToast.Unity.Cameras
{
    public class CameraCenterer : MonoBehaviour
    {
        public GameManager gameManager;
        public float sidebarWidthInUnits = 5f;
        public float padding = 2f;
        public float moveDuration = 1.0f; // How long the transition takes

        private Vector3 targetPosition;
        private float targetOrthographicSize;

        private Coroutine moveCoroutine;

        // Cache of the initial framing so it can be restored (e.g., at draft start)
        private bool _initialFramingCaptured = false;
        private Vector3 _initialPosition;
        private float _initialOrthographicSize;

        void Start()
        {
            CenterCameraInstant();
            CaptureInitialFraming();
        }

        /// <summary>
        /// Capture current camera position & size as the reference initial framing.
        /// Call after first board render & UI layout.
        /// </summary>
        public void CaptureInitialFraming()
        {
            if (Camera.main == null) return;
            _initialFramingCaptured = true;
            _initialPosition = Camera.main.transform.position;
            _initialOrthographicSize = Camera.main.orthographicSize;
        }

        public void CenterCameraInstant()
        {
            CalculateTarget();
            ApplyCameraInstantly();
        }

        public void CenterCameraSmooth()
        {
            CalculateTarget();

            if (moveCoroutine != null)
                StopCoroutine(moveCoroutine);

            moveCoroutine = StartCoroutine(SmoothMove(moveDuration));
        }

        public void RestoreInitialFramingSmooth(float duration)
        {
            if (!_initialFramingCaptured || Camera.main == null) return;
            if (moveCoroutine != null)
                StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(SmoothMoveTo(_initialPosition, _initialOrthographicSize, duration));
        }

        private void CalculateTarget()
        {
            if (Camera.main == null || gameManager?.Board == null)
                return;

            int width = gameManager.Board.Width;
            int height = gameManager.Board.Height;

            float screenAspect = (float)Screen.width / Screen.height;
            float gridAspect = (float)width / height;

            float adjustedCenterX = (width / 2f) + (sidebarWidthInUnits / 2f);
            float centerY = height / 2f;

            targetPosition = new Vector3(adjustedCenterX, centerY, -10f);

            if (screenAspect > gridAspect)
            {
                targetOrthographicSize = (height / 2f) + padding;
            }
            else
            {
                targetOrthographicSize = (width / (2f * screenAspect)) + padding;
            }
        }

        private void ApplyCameraInstantly()
        {
            Camera.main.transform.position = targetPosition;
            Camera.main.orthographicSize = targetOrthographicSize;
        }

        private IEnumerator SmoothMove(float duration)
        {
            Vector3 startPosition = Camera.main.transform.position;
            float startSize = Camera.main.orthographicSize;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Smooth interpolation (ease in/out)
                t = t * t * (3f - 2f * t);

                Camera.main.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                Camera.main.orthographicSize = Mathf.Lerp(startSize, targetOrthographicSize, t);

                yield return null;
            }

            Camera.main.transform.position = targetPosition;
            Camera.main.orthographicSize = targetOrthographicSize;
        }

        private IEnumerator SmoothMoveTo(Vector3 endPos, float endSize, float duration)
        {
            Vector3 startPosition = Camera.main.transform.position;
            float startSize = Camera.main.orthographicSize;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                Camera.main.transform.position = Vector3.Lerp(startPosition, endPos, t);
                Camera.main.orthographicSize = Mathf.Lerp(startSize, endSize, t);
                yield return null;
            }
            Camera.main.transform.position = endPos;
            Camera.main.orthographicSize = endSize;
        }
    }

}
