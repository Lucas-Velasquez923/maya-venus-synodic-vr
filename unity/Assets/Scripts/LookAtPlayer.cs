using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    public Transform playerCamera;   // XR Camera

    void LateUpdate()
    {
        if (playerCamera == null) return;

        Vector3 direction = transform.position - playerCamera.position;

        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);

    }
}