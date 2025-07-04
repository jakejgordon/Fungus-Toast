using UnityEngine;

namespace FungusToast.Unity.Cameras
{
    public class CameraControls : MonoBehaviour
    {
        public float zoomSpeed = 25f;
        public float moveSpeed = 15f;
        public float minZoom = 5f;
        public float maxZoom = 100f;

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
                    cam.transform.position += new Vector3(delta.x, delta.y, 0);
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
                // Scale movement by camera size for consistent feel
                float scaledSpeed = moveSpeed * Camera.main.orthographicSize;
                Camera.main.transform.position += move * scaledSpeed * Time.deltaTime;

                // --- Right-click drag pan ---
                if (Input.GetMouseButton(1)) // Right mouse button
                {
                    // Right-drag panning also scaled by camera size
                    float dragSpeed = moveSpeed * Camera.main.orthographicSize;
                    float dx = -Input.GetAxis("Mouse X") * dragSpeed * Time.deltaTime;
                    float dy = -Input.GetAxis("Mouse Y") * dragSpeed * Time.deltaTime;
                    Camera.main.transform.Translate(new Vector3(dx, dy, 0));
                }
            }
        }

    }
}

