using UnityEngine;
using FungusToast.Core;

public class CameraCenterer : MonoBehaviour
{
    [Tooltip("Reference to the GameManager.")]
    public GameManager gameManager;

    void Start()
    {
        CenterCamera();
    }

    public void CenterCamera()
    {
        if (Camera.main == null || gameManager?.Board == null)
            return;

        int width = gameManager.Board.Width;
        int height = gameManager.Board.Height;

        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);
        Camera.main.orthographicSize = Mathf.Max(width, height) / 2f + 2f; // extra padding
    }
}
