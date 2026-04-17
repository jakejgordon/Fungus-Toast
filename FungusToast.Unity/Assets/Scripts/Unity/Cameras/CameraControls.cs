using FungusToast.Unity.Input;
using UnityEngine;

namespace FungusToast.Unity.Cameras
{
    public class CameraControls : MonoBehaviour
    {
        public float zoomSpeed = 25f;
        public float moveSpeed = 15f;
        public float minZoom = 5f;
        public float maxZoom = 100f;

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
                    size -= scroll * zoomSpeed;
                    size = Mathf.Clamp(size, minZoom, maxZoom);
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
                    size = Mathf.Clamp(size, minZoom, maxZoom);
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

        private float GetMaxPanDistancePerFrame(Camera camera)
        {
            if (camera == null)
            {
                return maxPanDistancePerFrame;
            }

            float zoomScale = camera.orthographicSize / Mathf.Max(0.01f, minZoom);
            return maxPanDistancePerFrame * Mathf.Max(1f, zoomScale);
        }

        /// <summary>
        /// Simple bounds that prevent the camera from moving too far from the board center.
        /// This ensures players can always find their way back to the game area while allowing
        /// generous panning freedom around the board.
        /// </summary>
        private Vector3 ClampCameraPosition(Vector3 desiredPosition)
        {
            if (Camera.main == null || gameManager?.Board == null)
                return desiredPosition;

            // Get the board center (matching CameraCenterer calculation)
            int boardWidth = gameManager.Board.Width;
            int boardHeight = gameManager.Board.Height;
            float sidebarOffsetInUnits = cameraCenterer != null ? cameraCenterer.sidebarWidthInUnits : 5f;
            
            float boardCenterX = (boardWidth / 2f) + (sidebarOffsetInUnits / 2f);
            float boardCenterY = boardHeight / 2f;
            Vector3 boardCenter = new Vector3(boardCenterX, boardCenterY, desiredPosition.z);

            // Calculate effective max distance
            float effectiveMaxDistance;
            if (autoCalculateBounds)
            {
                // Auto-calculate based on board size: distance to furthest corner + padding
                float maxBoardRadius = Mathf.Max(boardWidth, boardHeight) / 2f;
                effectiveMaxDistance = maxBoardRadius + autoBoundsPadding;
            }
            else
            {
                // Use manual setting
                effectiveMaxDistance = maxDistanceFromCenter;
            }

            // Calculate distance from board center
            float distanceFromCenter = Vector2.Distance(
                new Vector2(desiredPosition.x, desiredPosition.y),
                new Vector2(boardCenter.x, boardCenter.y)
            );

            // If within allowed distance, no clamping needed
            if (distanceFromCenter <= effectiveMaxDistance)
                return desiredPosition;

            // Clamp to max distance from center
            Vector2 direction = (new Vector2(desiredPosition.x, desiredPosition.y) - new Vector2(boardCenter.x, boardCenter.y)).normalized;
            Vector2 clampedPosition = new Vector2(boardCenter.x, boardCenter.y) + direction * effectiveMaxDistance;

            return new Vector3(clampedPosition.x, clampedPosition.y, desiredPosition.z);
        }
    }
}

