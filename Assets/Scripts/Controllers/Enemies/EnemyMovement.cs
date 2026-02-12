using UnityEngine;

[RequireComponent(typeof(EntityStats))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Drag the Player object here, or leave empty to auto-find by tag.")]
    public Transform target;

    [Header("Stopping")]
    [Tooltip("How far from the target the enemy will stop.")]
    public float stoppingDistance = 2.0f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayLength = 10.0f;
    public float heightOffset = 0.5f;
    public float groundAnchorOffset = 1.0f;

    private EntityStats myStats;
    private LocalAvoidance localAvoidance;
    private SpriteRenderer characterSpriteRenderer;
    private Vector3 moveDirection;

    public bool isMoving { get; private set; }

    void Start()
    {
        myStats = GetComponent<EntityStats>();
        localAvoidance = GetComponent<LocalAvoidance>();
        characterSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // auto-find player if no target was assigned in the Inspector
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
                target = player.transform;
            else
                Debug.LogWarning("EnemyMovement: No target assigned and no 'Player' object found!");
        }

        if (characterSpriteRenderer == null)
            Debug.LogError("EnemyMovement: No SpriteRenderer found in children!");
    }

    void FixedUpdate()
    {
        if (target == null || myStats == null) return;

        HandleMovement();
        ClampToGround();
    }

    private void HandleMovement()
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0; // ignore vertical difference

        float distance = toTarget.magnitude;

        // always face the target, even when standing still
        FlipSpriteTowardsTarget();

        // debug
        // Debug.Log($"[EnemyMovement] Target: {target.name} at {target.position}, Distance: {distance:F2}, Stopping: {stoppingDistance}");

        if (distance > stoppingDistance)
        {
            isMoving = true;

            // project movement onto ground normal for slopes
            Vector3 desiredDir = toTarget.normalized;
            RaycastHit hit;
            Vector3 rayOrigin = transform.position + Vector3.up * heightOffset;

            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength, groundLayer))
            {
                moveDirection = Vector3.ProjectOnPlane(desiredDir, hit.normal).normalized;

                float targetY = hit.point.y;
                if (Mathf.Abs(transform.position.y - targetY) < 0.5f)
                {
                    Vector3 newPos = transform.position;
                    newPos.y = Mathf.MoveTowards(transform.position.y, targetY, 10f * Time.fixedDeltaTime);
                    transform.position = newPos;
                }
            }
            else
            {
                moveDirection = desiredDir;
            }

            // blend in local avoidance so entities don't overlap
            if (localAvoidance != null)
            {
                moveDirection = (moveDirection + localAvoidance.AvoidanceVector).normalized;
            }

            float currentSpeed = myStats.GetStatValue(StatType.MoveSpeed);
            transform.Translate(moveDirection * currentSpeed * Time.fixedDeltaTime, Space.World);
        }
        else
        {
            isMoving = false;
        }
    }

    private void FlipSpriteTowardsTarget()
    {
        if (characterSpriteRenderer == null || target == null) return;

        if (target.position.x < transform.position.x)
            characterSpriteRenderer.flipX = true;  // face Left
        else
            characterSpriteRenderer.flipX = false;  // face Right
    }

    private void ClampToGround()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 2.0f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength + 2.0f, groundLayer))
        {
            Vector3 newPos = transform.position;
            newPos.y = hit.point.y + groundAnchorOffset;
            transform.position = newPos;
        }
    }
}
