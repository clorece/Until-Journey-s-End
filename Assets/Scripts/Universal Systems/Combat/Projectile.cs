using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Universal projectile script. Attach to any projectile prefab (arrow, fireball, etc).
/// Initialized at spawn by CombatSystem.SpawnProjectile().
/// Uses manual interpolation for predictable arc trajectories.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Lifetime")]
    [Tooltip("Seconds before the projectile self-destructs (safety net).")]
    public float lifetime = 5f;

    [Header("Arc Settings")]
    [Tooltip("How high the arc peaks relative to the distance traveled.")]
    [Range(0f, 1f)] public float arcHeightFactor = 0.15f;

    // --- Set at runtime by CombatSystem.SpawnProjectile() ---
    [HideInInspector] public float damage;
    [HideInInspector] public float knockbackForce;
    [HideInInspector] public bool penetrates;
    [HideInInspector] public LayerMask targetLayers;
    [HideInInspector] public GameObject owner;

    private Rigidbody rb;
    private SpriteRenderer spriteRenderer;
    private HashSet<GameObject> alreadyHit = new HashSet<GameObject>();
    private float spawnTime;

    // Arc interpolation state
    private Vector3 startPos;
    private Vector3 targetPos;
    private float flightTime;
    private float elapsedTime;
    private float arcPeakHeight;
    private bool launched = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Disable physics — we control movement manually for predictable arcs
        rb.useGravity = false;
        rb.isKinematic = true;

        // Find the sprite renderer (on this object or a child)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        spawnTime = Time.time;

        // Ensure the projectile and its visuals are on a sharp layer to avoid TAA blur.
        // If the prefab is on a sharp layer (Entities/Players), propagate that to children.
        // Otherwise, default to the Entities layer for sharpness.
        int sharpLayer = gameObject.layer;
        if (((1 << sharpLayer) & PostProcess.ExclusionLayers) == 0)
        {
            sharpLayer = LayerMask.NameToLayer("Entities");
        }
        
        if (sharpLayer != -1)
        {
            PostProcess.SetLayerRecursively(gameObject, sharpLayer);
        }
    }

    void Update()
    {
        if (launched)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / flightTime;

            if (t >= 1f)
            {
                // Arrived at target — destroy if not penetrating
                transform.position = targetPos;
                if (!penetrates)
                {
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                // Lerp position along the straight line
                Vector3 pos = Vector3.Lerp(startPos, targetPos, t);

                // Add parabolic arc height: peaks at t=0.5, zero at t=0 and t=1
                pos.y += arcPeakHeight * 4f * t * (1f - t);

                // Orient the sprite to face the travel direction
                UpdateSpriteOrientation(t);

                transform.position = pos;
            }
        }

        // Self-destruct after lifetime expires
        if (Time.time - spawnTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Launches the projectile toward a target position with an arcing trajectory.
    /// </summary>
    public void Launch(Vector3 targetPosition, float speed)
    {
        startPos = transform.position;
        targetPos = targetPosition;

        float dist = Vector3.Distance(startPos, targetPos);
        flightTime = dist / Mathf.Max(speed, 0.1f);
        arcPeakHeight = dist * arcHeightFactor;
        elapsedTime = 0f;
        launched = true;
    }

    /// <summary>
    /// Orients the arrow sprite to face its travel direction while staying visible to the camera.
    /// The sprite's default "right" direction (tip) is rotated to align with the projected velocity.
    /// </summary>
    private void UpdateSpriteOrientation(float t)
    {
        if (spriteRenderer == null) return;

        // Calculate the tangent (velocity direction) at the current point on the arc
        // Derivative of lerp is constant: (targetPos - startPos) / flightTime
        // Derivative of arc height: arcPeakHeight * 4 * (1 - 2t) / flightTime
        Vector3 flatVelocity = (targetPos - startPos) / flightTime;
        float arcDerivative = arcPeakHeight * 4f * (1f - 2f * t) / flightTime;
        Vector3 velocity3D = flatVelocity;
        velocity3D.y += arcDerivative;

        // Project the velocity onto the camera's view plane to get a 2D direction
        Camera cam = Camera.main;
        if (cam == null) return;

        // Get screen-space direction of the velocity
        Vector3 screenPos = cam.WorldToScreenPoint(transform.position);
        Vector3 screenPosAhead = cam.WorldToScreenPoint(transform.position + velocity3D.normalized);
        Vector2 screenDir = ((Vector2)(screenPosAhead - screenPos)).normalized;

        // Calculate the angle: 0° = right (sprite default), rotates to match travel direction
        float angle = Mathf.Atan2(screenDir.y, screenDir.x) * Mathf.Rad2Deg;

        // Apply rotation on the sprite's local Z axis to point the arrow tip along the path
        // Keep the sprite facing the camera by matching the camera's rotation, then add the Z tilt
        Quaternion cameraFacing = cam.transform.rotation;
        spriteRenderer.transform.rotation = cameraFacing * Quaternion.Euler(0f, 0f, angle);

        // Flip sprite if traveling left so the arrow doesn't appear backwards
        spriteRenderer.flipY = (screenDir.x < 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        // Skip the owner
        if (other.gameObject == owner) return;
        if (other.transform.IsChildOf(owner.transform)) return;

        // Skip if not on a target layer
        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

        // Skip if we already hit this target (penetration mode)
        GameObject rootTarget = other.transform.root.gameObject;
        if (alreadyHit.Contains(rootTarget)) return;
        alreadyHit.Add(rootTarget);

        // Apply damage
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
            damageable.TakeDamage(damage, knockbackDir * knockbackForce);
        }

        // Destroy on hit unless penetrating
        if (!penetrates)
        {
            Destroy(gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        if (launched)
        {
            // Draw the arc path
            Gizmos.color = Color.yellow;
            Vector3 prev = startPos;
            for (int i = 1; i <= 20; i++)
            {
                float t = i / 20f;
                Vector3 p = Vector3.Lerp(startPos, targetPos, t);
                p.y += arcPeakHeight * 4f * t * (1f - t);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
