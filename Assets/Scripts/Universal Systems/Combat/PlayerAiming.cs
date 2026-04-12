using UnityEngine;

public class AimController : MonoBehaviour
{
    public enum AimMode { Player, Enemy }
    
    [Header("Aiming Mode")]
    public AimMode aimMode = AimMode.Player;

    [Header("Enemy Aiming Settings")]
    [Tooltip("The transform to aim at. Auto-finds the Player if left empty.")]
    public Transform target;
    [Tooltip("Maximum distance at which the enemy will track the target. 0 = always track.")]
    public float detectionRadius = 0f;
    [Tooltip("How quickly the enemy rotates to face its target (degrees / sec). 0 = instant.")]
    public float rotationSpeed = 0f;
    [Tooltip("Show debug line towards target")]
    public bool showDebugLine = true;

    private Camera mainCam;
    private Plane mathGround; 

    void Start()
    {
        if (aimMode == AimMode.Player)
        {
            mainCam = Camera.main;

            if (mainCam == null) 
            {
                Debug.LogError("CRITICAL ERROR: 'Camera.main' is NULL! Go tag your camera as 'MainCamera' in the Inspector.");
            }
            else
            {
                Debug.Log("AimController: Camera found successfully.");
            }
        }
        else if (aimMode == AimMode.Enemy)
        {
            if (target == null)
            {
                GameObject player = GameObject.Find("Player");
                if (player != null)
                    target = player.transform;
                else
                    Debug.LogWarning("AimController: No target assigned and no 'Player' object found!");
            }
        }
    }

    void Update()
    {
        if (aimMode == AimMode.Player)
        {
            RotateSelfPlayer();
        }
        else if (aimMode == AimMode.Enemy)
        {
            RotateSelfEnemy();
        }
    }

    void RotateSelfPlayer()
    {
        if (mainCam == null) return;

        mathGround = new Plane(Vector3.up, transform.position);
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        float enter = 0.0f;
        if (mathGround.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            hitPoint.y = transform.position.y;

            transform.LookAt(hitPoint);

            Debug.DrawLine(transform.position, hitPoint, Color.green);
        }
        else
        {
            Debug.LogWarning("AimController: Math Plane Raycast failed (Mouse might be off screen).");
        }
    }

    void RotateSelfEnemy()
    {
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // ignore vertical difference – rotate on Y axis only

        // outside detection range (when radius is non-zero) – do nothing
        if (detectionRadius > 0f && direction.sqrMagnitude > detectionRadius * detectionRadius)
            return;

        if (direction.sqrMagnitude < 0.001f) return; // too close to compute meaningful direction

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        if (rotationSpeed <= 0f)
        {
            // instant snap
            transform.rotation = targetRotation;
        }
        else
        {
            // smooth rotation
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        if (showDebugLine)
        {
            Debug.DrawLine(transform.position, target.position, Color.red);
        }
    }
    
    // ─────────────────────────────────────────────
    //  Public helpers for other scripts
    // ─────────────────────────────────────────────

    /// <summary>Returns the flat direction towards the current target (Y zeroed).</summary>
    public Vector3 GetDirectionToTarget()
    {
        if (aimMode == AimMode.Player) return transform.forward; // For player, usually mouse direction

        if (target == null) return transform.forward;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        return dir.normalized;
    }

    /// <summary>Returns the flat distance to the current target.</summary>
    public float GetDistanceToTarget()
    {
        if (aimMode == AimMode.Player) return 0f; // Alternatively track distance to mouse

        if (target == null) return Mathf.Infinity;

        Vector3 diff = target.position - transform.position;
        diff.y = 0f;
        return diff.magnitude;
    }

    /// <summary>True when the enemy is currently facing within <paramref name="angleTolerance"/> degrees of the target.</summary>
    public bool IsFacingTarget(float angleTolerance = 5f)
    {
        if (aimMode == AimMode.Player) return true; // Player assumes always facing mouse

        if (target == null) return false;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return true;

        return Vector3.Angle(transform.forward, dir) <= angleTolerance;
    }

    // ─────────────────────────────────────────────
    //  Gizmos
    // ─────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (aimMode == AimMode.Enemy && detectionRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        // forward direction ray
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 3f);
    }
}