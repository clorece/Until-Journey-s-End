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

    // you might see that there is a seperate child object for shadows
    // since we offset the sprite and the camera to prevent major clipping
    // when going up angled terrain, we set an invisible object to cast the shadow
    // and then we offset that to try and connect it as close as we can to the player sprite.
    // this is not very accurate but it makes a convincing shadow and partly solves clipping

    // TODO: force render (might be z clipping?)  sprite over the ground layer, so we wont have
    // to offset the everything to avoid clipping

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