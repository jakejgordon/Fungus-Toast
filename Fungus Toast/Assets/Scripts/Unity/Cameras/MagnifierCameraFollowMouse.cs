using UnityEngine;

public class MagnifierCameraFollowMouse : MonoBehaviour
{
    public Camera mainCamera; // Assign your main camera in the Inspector

    void Update()
    {
        Vector3 mouseScreen = Input.mousePosition;
        // Use the main camera's Z for correct world depth
        float z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, z));
        // Set the camera's position to center on the mouse, keep the correct Z
        transform.position = new Vector3(mouseWorld.x, mouseWorld.y, transform.position.z);
    }
} 