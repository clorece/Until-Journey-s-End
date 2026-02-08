using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform playerTarget;       
    public Transform orientationRef;     

    [Header("Settings")]
    public float cameraDistance = 15f;
    public float smoothTime = 0.2f;

    [Header("Side Scroller Settings")]
    public Vector2 deadZone = new Vector2(1f, 1f); // X and Y dead zone size
    public float lookAheadX = -5f; // Offset to viewing more of the right side

    public Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f); // offset of the camera according to the player

    // you might see that there is a seperate child object for shadows
    // since we offset the sprite and the camera to prevent major clipping
    // when going up angled terrain, we set an invisible object to cast the shadow
    // and then we offset that to try and connect it as close as we can to the player sprite.
    // this is not very accurate but it makes a convincing shadow and partly solves clipping

    // TODO: force render (might be z clipping?)  sprite over the ground layer, so we wont have
    // to offset the everything to avoid clipping

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (playerTarget == null || orientationRef == null) return;

        // 1. Calculate the ideal position in 3D space based on orientation and distance
        // This ensures the camera is at the correct height and angle relative to the player
        Vector3 targetPos = playerTarget.position + pivotOffset;
        Vector3 viewDirection = orientationRef.forward;
        Vector3 idealPosition = targetPos - (viewDirection * cameraDistance);

        // 2. Apply Side-Scroller Deadzone Logic (Primary Axis: X)
        // We want the camera to only move when the player pushes the edge of the screen
        // ideally preserving the Y/Z relative structure but delaying X movement.
        
        Vector3 currentPos = transform.position;
        float targetX = currentPos.x;
        
        // Calculate the "ideal" X we WANT to be at (with lookahead)
        float idealXWithLookAhead = idealPosition.x + lookAheadX;

        // Check if idealX is outside the deadzone centered on currentPos.x
        // The deadzone is defined relative to the camera center
        // If (Ideal - Current) > Deadzone -> Move Current towards Ideal until (Ideal - Current) == Deadzone
        
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

        // 3. For Y/Z (Height/Depth), usually in a side scroller we want to follow smoothly 
        // OR lock to a specific plane height.
        // The user mentioned "lock on a height to the plane", suggesting stable Y.
        // However, simply following 'idealPosition.y' might be "too low" if we ignored the offset before.
        // Now 'idealPosition' includes (viewDirection * cameraDistance), so it should be correct height (e.g. 15 units back/up).
        
        // Let's use the ideal Y and Z for now, but apply the deadzone X.
        // If the user wants a "fixed height" that ignores jumping, we might need a separate logic, 
        // e.g., targetPos.y = lockedY; 
        // For now, let's assume they meant "don't drop to player feet", which we fixed by using idealPosition for Y.
        
        Vector3 finalDesiredPosition = new Vector3(targetX, idealPosition.y, idealPosition.z);

        // Smoothly interpolate to the target position
        transform.position = Vector3.SmoothDamp(transform.position, finalDesiredPosition, ref currentVelocity, smoothTime);
        
        // 4. Lock Rotation 
        // "lock the rotation of the camera to always face towards the angle of the player"
        // This usually means LookAt the player, OR match player's rotation.
        // Given "face towards... player", LookAt is likely desired if the camera position is independent.
        // But if 'orientationRef' is the camera boom, matching it (as before) is safer.
        // Let's stick to orientationRef rotation but ensure it's correct. 
        // If the user wants the camera to rotate to LOOK AT the player dynamically:
        // transform.LookAt(playerTarget.position + pivotOffset);
        // But typically existing logic 'transform.rotation = orientationRef.rotation;' works if orientationRef is set up right.
        // Let's try LookAt as requested "face towards".
        
        // Using LookAt helps if the camera lags behind (Deadzone) but needs to keep player in view center? NO, side scroller looks forward.
        // If the user means "Face the direction of movement", that's player rotation.
        // "Face towards the angle of the player" -> potentially `transform.rotation = playerTarget.rotation`? 
        // Or "Face at the player" -> LookAt. 
        // For a side scroller, usually rotation is fixed.
        // The user said "lock the rotation ... to always face towards the angle of the player". 
        // I will trust the original 'orientationRef' logic but ensure users see we are setting it.
        // If they want LookAt, I'll add a check.
        
        // Restoring original behavior which used orientationRef seems safest for "angle", 
        // unless they specifically want LookAt behavior because of the deadzone lag.
        // With deadzone, the player is NOT in center. If we LookAt, the camera rotates. This creates a "panning" effect.
        // This might be what they want ("turn this to be a bit more of a side scroller..."). 
        // Let's assume standard side-scroller: Fixed Rotation (looking straight on), Position Moves.
        // But the previous code had `orientationRef`.
        
        transform.rotation = orientationRef.rotation;
    }

    void OnDrawGizmosSelected()
    {
        if (playerTarget != null)
        {
            Gizmos.color = Color.yellow;
            // Draw the "Dead Zone" box relative to the camera
            // The "Limit" lines
            Vector3 center = transform.position;
            // Effective center of player zone is Center - LookAhead
            // (Since we want Player to be at Center + LookAhead)
            
            // X-Axis limits
            Gizmos.DrawLine(center + Vector3.right * (deadZone.x - lookAheadX), center + Vector3.right * (deadZone.x - lookAheadX) + Vector3.up * 10);
            Gizmos.DrawLine(center - Vector3.right * (deadZone.x + lookAheadX), center - Vector3.right * (deadZone.x + lookAheadX) + Vector3.up * 10);
        }
    }
}
