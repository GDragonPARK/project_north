using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    public Transform player;
    public float height = 100f;

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 newPos = player.position;
        newPos.y = height;
        transform.position = newPos;

        // Keep a fixed top-down rotation
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
