using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerTarget;       
    public Transform orientationRef;     

    [Header("Settings")]
    public float cameraDistance = 15f;
    public float smoothSpeed = 0.125f;

    public Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f); // offset of the camera according to the player

    void LateUpdate()
    {
        if (playerTarget == null || orientationRef == null) return;

        Vector3 targetPos = playerTarget.position + pivotOffset;
        Vector3 viewDirection = orientationRef.forward;
        Vector3 desiredPosition = targetPos - (viewDirection * cameraDistance);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        transform.position = smoothedPosition;
        transform.rotation = orientationRef.rotation;
    }
}