#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[ExecuteInEditMode]
public class CameraCenterer : MonoBehaviour
{
    [Tooltip("Grid size to center and zoom the camera around.")]
    public Vector2Int gridSize = new Vector2Int(100, 100);

    void Start()
    {
        // Ensure the camera is correctly positioned and zoomed at startup (in Play Mode)
        CenterCamera();
    }

#if UNITY_EDITOR
    void Update()
    {
        // While not playing, keep the camera centered in the Scene/Game view
        if (!EditorApplication.isPlaying)
        {
            CenterCamera();
        }
    }
#endif

    void CenterCamera()
    {
        if (Camera.main != null)
        {
            // Position the camera in the center of the grid (x, y) and set depth to -10 (standard for 2D view)
            Camera.main.transform.position = new Vector3(gridSize.x / 2f, gridSize.y / 2f, -10f);

            // Set the orthographic size so the camera zooms out enough to show the whole grid
            Camera.main.orthographicSize = Mathf.Max(gridSize.x, gridSize.y) / 2f;
        }
    }
}
