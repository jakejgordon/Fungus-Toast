using UnityEngine;

namespace FungusToast.Unity.Cameras
{
    public class CameraControls : MonoBehaviour
    {
        public float zoomSpeed = 10f;
        public float moveSpeed = 10f;
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
            }

            // Pan with arrow keys or WASD
            Vector3 move = new Vector3(
                Input.GetAxis("Horizontal"),
                Input.GetAxis("Vertical"),
                0
            );
            Camera.main.transform.position += move * moveSpeed * Time.deltaTime;

            // Optional: Right-click drag pan
            if (Input.GetMouseButton(1)) // Right mouse button
            {
                float dragSpeed = moveSpeed * 0.5f;
                float dx = -Input.GetAxis("Mouse X") * dragSpeed;
                float dy = -Input.GetAxis("Mouse Y") * dragSpeed;
                Camera.main.transform.Translate(new Vector3(dx, dy, 0));
            }
        }
    }
}

