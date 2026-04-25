using FungusToast.Unity.Input;
using UnityEngine;

namespace FungusToast.Unity.Cameras
{
    public class CameraControls : MonoBehaviour
    {
        public float zoomSpeed = 12.5f;
        public float moveSpeed = 7.5f;
        public float minZoom = 5f;
        public float maxZoom = 100f;

        [Header("Zoom Scaling")]
        [Tooltip("Smallest board dimension that uses the minimum zoom sensitivity scale.")]
        [SerializeField] private float minBoardDimensionForZoomScaling = 10f;
        [Tooltip("Board dimension at which zoom reaches full sensitivity.")]
        [SerializeField] private float maxBoardDimensionForZoomScaling = 160f;
        [Tooltip("Multiplier applied to zoom sensitivity on the smallest supported boards.")]
        [SerializeField] private float minZoomSensitivityScale = 0.25f;

        [Header("Input Stability")]
        [Tooltip("Caps the timestep used for pan input so animation hitches do not fling the camera across the board.")]
        [SerializeField] private float maxPanInputDeltaTime = 1f / 45f;
        [Tooltip("Caps how far camera panning can move in a single frame at minimum zoom. The cap scales up with camera zoom so normal movement stays responsive while hitch spikes remain bounded.")]
        [SerializeField] private float maxPanDistancePerFrame = 0.75f;

        [Header("Camera Bounds")]
        [Tooltip("Maximum distance camera can move from board center (in world units). For 100x100 boards, try 75-100.")]
        public float maxDistanceFromCenter = 75f;
        [Tooltip("Reference to GameManager to get board dimensions")]
        public GameManager gameManager;
        [Tooltip("Reference to CameraCenterer to get board center")]
        public CameraCenterer cameraCenterer;
        [Tooltip("Auto-calculate bounds based on board size (recommended)")]
        public bool autoCalculateBounds = true;
        [Tooltip("Extra padding beyond board edges when auto-calculating (in world units)")]
        public float autoBoundsPadding = 25f;

        [Header("Small Board Safeguards")]
        [Tooltip("Maximum zoom-out multiplier relative to the initial board framing. Keeps tiny boards from shrinking to a speck.")]
        [SerializeField] private float maxZoomOutRelativeToInitialFraming = 1.12f;
        [Tooltip("For boards smaller than the viewport, keep at least this fraction of the board visible on each axis while panning.")]
        [SerializeField] [Range(0.5f, 1f)] private float minVisibleSmallBoardFraction = 0.85f;

        void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPauseMenuOpen)
            {
                return;
            }

            if (GameManager.Instance?.GameUI?.EndGamePanel != null
                && GameManager.Instance.GameUI.EndGamePanel.BlocksGameplayCameraInput)
            {
                return;
            }

            // Zoom with scroll wheel
            float scroll = UnityInputAdapter.GetMouseScrollDelta();
            if (Camera.main != null)
            {
                Camera mainCamera = Camera.main;
                float panDeltaTime = GetPanDeltaTime();
                float maxPanDistance = GetMaxPanDistancePerFrame(mainCamera);
                float size = mainCamera.orthographicSize;

                // --- Zoom to mouse cursor logic ---
                if (Mathf.Abs(scroll) > 0.0001f)
                {
                    Camera cam = mainCamera;
                    // 1. Get world position under mouse before zoom
                    Vector2 pointerScreen = UnityInputAdapter.GetPointerScreenPosition();
                    Vector3 mouseScreenPos = new Vector3(pointerScreen.x, pointerScreen.y, 0f);
                    Vector3 worldBefore = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, cam.nearClipPlane));

                    // 2. Apply zoom
                    size -= scroll * GetZoomSpeedForCurrentBoard();
                    size = Mathf.Clamp(size, GetDynamicMinZoom(), GetDynamicMaxZoom());
                    cam.orthographicSize = size;

                    // 3. Get world position under mouse after zoom
                    Vector3 worldAfter = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, cam.nearClipPlane));

                    // 4. Offset camera position so the world point under the cursor stays fixed
                    Vector3 delta = worldBefore - worldAfter;
                    Vector3 newPosition = cam.transform.position + new Vector3(delta.x, delta.y, 0);

                    // 5. Apply bounds checking after zoom-to-cursor movement
                    cam.transform.position = ClampCameraPosition(newPosition);
                }
                else
                {
                    // If no zoom, just clamp orthographic size
                    size = Mathf.Clamp(size, GetDynamicMinZoom(), GetDynamicMaxZoom());
                    mainCamera.orthographicSize = size;
                }

                // --- Panning with WASD/Arrow Keys ---
                Vector2 moveInput = UnityInputAdapter.GetKeyboardMoveVector();
                Vector3 move = new Vector3(moveInput.x, moveInput.y, 0f);
                if (move != Vector3.zero)
                {
                    // Scale movement by camera size for consistent feel
                    float scaledSpeed = moveSpeed * mainCamera.orthographicSize;
                    Vector3 frameDelta = Vector3.ClampMagnitude(move * scaledSpeed * panDeltaTime, maxPanDistance);
                    Vector3 newPosition = mainCamera.transform.position + frameDelta;
                    mainCamera.transform.position = ClampCameraPosition(newPosition);
                }

                // --- Right-click drag pan ---
                if (UnityInputAdapter.IsSecondaryPointerPressed())
                {
                    // Pointer delta is already frame-relative, so convert pixels directly into world-space movement.
                    float unitsPerPixel = (2f * mainCamera.orthographicSize) / Mathf.Max(1f, mainCamera.pixelHeight);
                    Vector2 pointerDelta = UnityInputAdapter.GetPointerDelta();
                    Vector3 frameDelta = Vector3.ClampMagnitude(
                        new Vector3(-pointerDelta.x * unitsPerPixel, -pointerDelta.y * unitsPerPixel, 0f),
                        maxPanDistance);
                    Vector3 newPosition = mainCamera.transform.position + frameDelta;
                    mainCamera.transform.position = ClampCameraPosition(newPosition);
                }
            }
        }

        private float GetPanDeltaTime()
        {
            return Mathf.Min(Time.unscaledDeltaTime, maxPanInputDeltaTime);
        }

        private float GetZoomSpeedForCurrentBoard()
        {
            if (gameManager?.Board == null)
            {
                return zoomSpeed;
            }

            float boardDimension = Mathf.Max(gameManager.Board.Width, gameManager.Board.Height);
            float fullSensitivityDimension = Mathf.Max(minBoardDimensionForZoomScaling, maxBoardDimensionForZoomScaling);
            float t = fullSensitivityDimension <= minBoardDimensionForZoomScaling
                ? 1f
                : Mathf.InverseLerp(minBoardDimensionForZoomScaling, fullSensitivityDimension, boardDimension);

            float sensitivityScale = Mathf.Lerp(minZoomSensitivityScale, 1f, t);
            return zoomSpeed * sensitivityScale;
        }

        private float GetMaxPanDistancePerFrame(Camera camera)
        {
            if (camera == null)
            {
                return maxPanDistancePerFrame;
            }

            float zoomScale = camera.orthographicSize / Mathf.Max(0.01f, GetDynamicMinZoom());
            return maxPanDistancePerFrame * Mathf.Max(1f, zoomScale);
        }

        private float GetDynamicMinZoom()
        {
            return minZoom;
        }

        private float GetDynamicMaxZoom()
        {
            float dynamicMaxZoom = maxZoom;

            if (cameraCenterer != null && cameraCenterer.HasInitialFraming)
            {
                float framedZoomCap = cameraCenterer.InitialOrthographicSize * Mathf.Max(1f, maxZoomOutRelativeToInitialFraming);
                dynamicMaxZoom = Mathf.Min(dynamicMaxZoom, framedZoomCap);
            }

            return Mathf.Max(GetDynamicMinZoom(), dynamicMaxZoom);
        }

        private void GetBoardExtents(out float minX, out float maxX, out float minY, out float maxY)
        {
            int boardWidth = gameManager.Board.Width;
            int boardHeight = gameManager.Board.Height;
            int visualPaddingTiles = Mathf.Max(0, gameManager.gridVisualizer?.CurrentBoardVisualPaddingTiles ?? 0);

            minX = -visualPaddingTiles;
            minY = -visualPaddingTiles;
            maxX = boardWidth + visualPaddingTiles;
            maxY = boardHeight + visualPaddingTiles;
        }

        /// <summary>
        /// Clamp camera movement against the actual board footprint rather than a loose radius.
        /// Small boards keep most of the toast visible; large boards still allow edge exploration.
        /// </summary>
        private Vector3 ClampCameraPosition(Vector3 desiredPosition)
        {
            Camera camera = Camera.main;
            if (camera == null || gameManager?.Board == null)
            {
                return desiredPosition;
            }

            GetBoardExtents(out float boardMinX, out float boardMaxX, out float boardMinY, out float boardMaxY);

            float viewHalfHeight = camera.orthographicSize;
            float viewHalfWidth = camera.orthographicSize * camera.aspect;
            float boardWidth = boardMaxX - boardMinX;
            float boardHeight = boardMaxY - boardMinY;

            float clampedX = ClampAxis(
                desiredPosition.x,
                boardMinX,
                boardMaxX,
                boardWidth,
                viewHalfWidth,
                allowFullBoardOffscreen: false);
            float clampedY = ClampAxis(
                desiredPosition.y,
                boardMinY,
                boardMaxY,
                boardHeight,
                viewHalfHeight,
                allowFullBoardOffscreen: false);

            return new Vector3(clampedX, clampedY, desiredPosition.z);
        }

        private float ClampAxis(
            float desiredCenter,
            float boardMin,
            float boardMax,
            float boardSize,
            float viewHalfSpan,
            bool allowFullBoardOffscreen)
        {
            float viewSpan = viewHalfSpan * 2f;

            if (boardSize <= viewSpan)
            {
                float visibleRequirement = boardSize * minVisibleSmallBoardFraction;
                float minCenter = boardMin + visibleRequirement - viewHalfSpan;
                float maxCenter = boardMax - visibleRequirement + viewHalfSpan;
                return Mathf.Clamp(desiredCenter, minCenter, maxCenter);
            }

            if (!autoCalculateBounds)
            {
                float boardCenter = (boardMin + boardMax) * 0.5f;
                return Mathf.Clamp(desiredCenter, boardCenter - maxDistanceFromCenter, boardCenter + maxDistanceFromCenter);
            }

            float edgePadding = allowFullBoardOffscreen ? autoBoundsPadding : Mathf.Min(autoBoundsPadding, boardSize * 0.25f);
            float minLargeBoardCenter = boardMin + viewHalfSpan - edgePadding;
            float maxLargeBoardCenter = boardMax - viewHalfSpan + edgePadding;
            return Mathf.Clamp(desiredCenter, minLargeBoardCenter, maxLargeBoardCenter);
        }
    }
}

