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
                size -= scroll * zoomSpeed;
                size = Mathf.Clamp(size, minZoom, maxZoom);
                Camera.main.orthographicSize = size;

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

