using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerTarget;       
    public Transform orientationRef;     

    [Header("Settings")]
    public float cameraDistance = 15.0f;
    public float smoothTime = 0.2f;

    [Header("Side Scroller Settings")]
    public Vector2 deadZone = new Vector2(0.5f, 10.0f); // X and Y dead zone size
    public float lookAheadX = 7.5f; // offset to viewing more of the right side

    public Vector3 pivotOffset = new Vector3(0.0f, 1.5f, 0.0f);


    // TODO: force render (might be z clipping?)  sprite over the ground layer, so we wont have
    // to offset the everything to avoid clipping

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (playerTarget == null || orientationRef == null) return;
        
        Vector3 targetPos = playerTarget.position + pivotOffset;
        Vector3 viewDirection = orientationRef.forward;
        Vector3 idealPosition = targetPos - (viewDirection * cameraDistance);
        
        Vector3 currentPos = transform.position;
        float targetX = currentPos.x;

        float idealXWithLookAhead = idealPosition.x + lookAheadX;
        
        float deltaX = idealXWithLookAhead - currentPos.x;

        if (deltaX > deadZone.x)
        {
            targetX = idealXWithLookAhead - deadZone.x;
        }
        else if (deltaX < -deadZone.x)
        {
             targetX = idealXWithLookAhead + deadZone.x;
        }
        else
        {
             targetX = currentPos.x;
        }
        
        Vector3 finalDesiredPosition = new Vector3(targetX, idealPosition.y, idealPosition.z);

        transform.position = Vector3.SmoothDamp(transform.position, finalDesiredPosition, ref currentVelocity, smoothTime); // smooth interpolation to the target position
        transform.rotation = orientationRef.rotation; // lock rotation of camera to the orientation of the player
    }

    void OnDrawGizmosSelected()
    {
        if (playerTarget != null)
        {
            Gizmos.color = Color.yellow;
            // Draw the "Dead Zone" box relative to the camera
            // The "Limit" lines
            Vector3 center = transform.position;

            Gizmos.DrawLine(center + Vector3.right * (deadZone.x - lookAheadX), center + Vector3.right * (deadZone.x - lookAheadX) + Vector3.up * 10);
            Gizmos.DrawLine(center - Vector3.right * (deadZone.x + lookAheadX), center - Vector3.right * (deadZone.x + lookAheadX) + Vector3.up * 10);
        }
    }
}
