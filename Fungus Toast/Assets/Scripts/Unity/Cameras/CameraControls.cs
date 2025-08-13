using UnityEngine;

namespace FungusToast.Unity.Cameras
{
    public class CameraControls : MonoBehaviour
    {
        public float zoomSpeed = 25f;
        public float moveSpeed = 15f;
        public float minZoom = 5f;
        public float maxZoom = 100f;

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
            // Zoom with scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Camera.main != null)
            {
                float size = Camera.main.orthographicSize;

                // --- Zoom to mouse cursor logic ---
                if (Mathf.Abs(scroll) > 0.0001f)
                {
                    Camera cam = Camera.main;
                    // 1. Get world position under mouse before zoom
                    Vector3 mouseScreenPos = Input.mousePosition;
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
                    Camera.main.orthographicSize = size;
                }

                // --- Panning with WASD/Arrow Keys ---
                Vector3 move = new Vector3(
                    Input.GetAxis("Horizontal"),
                    Input.GetAxis("Vertical"),
                    0
                );
                if (move != Vector3.zero)
                {
                    // Scale movement by camera size for consistent feel
                    float scaledSpeed = moveSpeed * Camera.main.orthographicSize;
                    Vector3 newPosition = Camera.main.transform.position + move * scaledSpeed * Time.deltaTime;
                    Camera.main.transform.position = ClampCameraPosition(newPosition);
                }

                // --- Right-click drag pan ---
                if (Input.GetMouseButton(1)) // Right mouse button
                {
                    // Right-drag panning also scaled by camera size
                    float dragSpeed = moveSpeed * Camera.main.orthographicSize;
                    float dx = -Input.GetAxis("Mouse X") * dragSpeed * Time.deltaTime;
                    float dy = -Input.GetAxis("Mouse Y") * dragSpeed * Time.deltaTime;
                    Vector3 newPosition = Camera.main.transform.position + new Vector3(dx, dy, 0);
                    Camera.main.transform.position = ClampCameraPosition(newPosition);
                }
            }
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

