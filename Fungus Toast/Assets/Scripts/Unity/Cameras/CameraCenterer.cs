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

        void Start()
        {
            CenterCameraInstant();
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

            moveCoroutine = StartCoroutine(SmoothMove());
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

        private IEnumerator SmoothMove()
        {
            Vector3 startPosition = Camera.main.transform.position;
            float startSize = Camera.main.orthographicSize;

            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);

                // Smooth interpolation (ease in/out)
                t = t * t * (3f - 2f * t);

                Camera.main.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                Camera.main.orthographicSize = Mathf.Lerp(startSize, targetOrthographicSize, t);

                yield return null;
            }

            // Ensure final exact position and size
            Camera.main.transform.position = targetPosition;
            Camera.main.orthographicSize = targetOrthographicSize;
        }
    }

}
