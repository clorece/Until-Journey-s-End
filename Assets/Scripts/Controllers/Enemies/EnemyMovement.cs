using UnityEngine;
using System.Collections;
using Navigation.ORCA;

[RequireComponent(typeof(EntityStats))]
[RequireComponent(typeof(ORCAAgent))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Drag the Player object here, or leave empty to auto-find by tag.")]
    public Transform target;

    public enum EnemyType { Melee, Ranged }

    [Header("Behavior")]
    [Tooltip("Melee will move towards the player. Ranged will move away from the player.")]
    public EnemyType enemyType = EnemyType.Melee;

    [Header("Stopping & Detection")]
    [Tooltip("How far from the target the enemy will stop.")]
    public float stoppingDistance = 2.0f;
    [Tooltip("How close the target must be to start moving. 0 = always move.")]
    public float detectionRadius = 10.0f;

    [Header("Zone Boundary")]
    [Tooltip("If assigned, the enemy will be clamped within this zone's boundaries.")]
    public InstanceZone boundaryZone;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float rayLength = 10.0f;
    public float heightOffset = 0.5f;
    public float groundAnchorOffset = 1.0f;

    private EntityStats myStats;
    private ORCAAgent orcaAgent;
    private SpriteRenderer characterSpriteRenderer;
    private Vector3 moveDirection;

    public bool isMoving { get; private set; }
    public bool isJumping { get; private set; }
    public float jumpProgress { get; private set; }

    void Start()
    {
        myStats = GetComponent<EntityStats>();
        orcaAgent = GetComponent<ORCAAgent>();
        characterSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // auto-find player if no target was assigned in the Inspector
        if (target == null)
        {
            if (RunManager.Instance != null && RunManager.Instance.player != null)
                target = RunManager.Instance.player;
            else
                Debug.LogWarning("EnemyMovement: No target assigned and RunManager player not found!");
        }

        if (characterSpriteRenderer == null)
            Debug.LogError("EnemyMovement: No SpriteRenderer found in children!");

        if (orcaAgent != null)
        {
            orcaAgent.maxSpeedOverride = myStats.GetStatValue(StatType.MoveSpeed);
        }
    }

    void FixedUpdate()
    {
        if (target == null || myStats == null) return;
        if (myStats.IsDead) 
        {
            ClampToGround();
            return;
        }
        if (myStats.IsKnockedBack) return;

        if (!isJumping)
        {
            HandleMovement();
            ClampToGround();
        }

        // Clamp position to zone boundary if one is assigned
        if (boundaryZone != null)
        {
            transform.position = boundaryZone.ClampToBounds(transform.position);
        }
    }

    private void HandleMovement()
    {
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0; // ignore vertical difference

        float distance = toTarget.magnitude;

        // check detection radius
        if (detectionRadius > 0f && distance > detectionRadius)
        {
            isMoving = false;
            if (orcaAgent != null) orcaAgent.preferredVelocity = Vector2.zero;
            return;
        }

        // always face the target, even when standing still
        FlipSpriteTowardsTarget();

        float currentSpeed = myStats.GetStatValue(StatType.MoveSpeed);
        if (orcaAgent != null) orcaAgent.maxSpeedOverride = currentSpeed;

        // Use a small buffer to prevent jitter
        float buffer = 0.1f;

        if (enemyType == EnemyType.Melee)
        {
            if (distance > stoppingDistance + buffer)
            {
                isMoving = true;
                Move(toTarget.normalized, currentSpeed);
            }
            else if (distance < stoppingDistance - buffer && stoppingDistance > 0.5f)
            {
                // We are too close! Move away to make room.
                isMoving = true;
                Move(-toTarget.normalized, currentSpeed * 0.75f); // Move back slightly slower
            }
            else
            {
                isMoving = false;
                if (orcaAgent != null) orcaAgent.preferredVelocity = Vector2.zero;
            }
        }
        else if (enemyType == EnemyType.Ranged)
        {
            if (distance < stoppingDistance - buffer)
            {
                // Player breached avoidance range! Move away to maintain distance.
                isMoving = true;
                Move(-toTarget.normalized, currentSpeed);
            }
            else if (distance > stoppingDistance + buffer)
            {
                // Inside detection range, but outside avoidance range! Approach the player.
                isMoving = true;
                Move(toTarget.normalized, currentSpeed);
            }
            else
            {
                // Sweet spot (right at the edge of avoidance range), hold position and fire
                isMoving = false;
                if (orcaAgent != null) orcaAgent.preferredVelocity = Vector2.zero;
            }
        }
    }

    private void Move(Vector3 desiredDir, float speed)
    {
        // Set preferred velocity for ORCA
        if (orcaAgent != null)
        {
            orcaAgent.preferredVelocity = new Vector2(desiredDir.x, desiredDir.z) * speed;
            
            // Use ORCA's calculated velocity
            Vector2 orcaVel = orcaAgent.currentVelocity;
            
            if (orcaVel.sqrMagnitude < 0.001f && speed > 0.001f)
            {
                moveDirection = desiredDir * speed;
            }
            else
            {
                moveDirection = new Vector3(orcaVel.x, 0, orcaVel.y);
            }
        }
        else
        {
            moveDirection = desiredDir * speed;
        }

        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * heightOffset;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayLength, groundLayer))
        {
            Vector3 projectedDir = Vector3.ProjectOnPlane(moveDirection, hit.normal);
            moveDirection = projectedDir.normalized * moveDirection.magnitude;

            float targetY = hit.point.y;
            if (Mathf.Abs(transform.position.y - targetY) < 0.5f)
            {
                Vector3 newPos = transform.position;
                newPos.y = Mathf.MoveTowards(transform.position.y, targetY, 10f * Time.fixedDeltaTime);
                transform.position = newPos;
            }
        }

        transform.Translate(moveDirection * Time.fixedDeltaTime, Space.World);
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

    void OnDrawGizmosSelected()
    {
        if (detectionRadius > 0f)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }

    public void ApplyJump(Vector3 targetPos, float speed, float arcHeightFactor)
    {
        if (isJumping) return;
        StartCoroutine(JumpRoutine(targetPos, speed, arcHeightFactor));
    }

    private IEnumerator JumpRoutine(Vector3 targetPos, float speed, float arcHeightFactor)
    {
        isJumping = true;
        isMoving = false;
        jumpProgress = 0f;
        
        if (orcaAgent != null) 
            orcaAgent.preferredVelocity = Vector2.zero;

        Vector3 startPos = transform.position;
        float dist = Vector3.Distance(startPos, targetPos);
        float flightTime = dist / Mathf.Max(speed, 0.1f);
        float arcPeakHeight = dist * arcHeightFactor;
        
        float elapsedTime = 0f;

        while (elapsedTime < flightTime)
        {
            elapsedTime += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / flightTime);
            jumpProgress = t;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);
            currentPos.y += arcPeakHeight * 4f * t * (1f - t);
            
            transform.position = currentPos;
            
            // Still face the target while in the air if needed, or face the destination
            if (targetPos.x < transform.position.x)
                characterSpriteRenderer.flipX = true;  // face Left
            else
                characterSpriteRenderer.flipX = false; // face Right

            yield return new WaitForFixedUpdate();
        }

        // Snap precisely to target at the end
        transform.position = targetPos;
        ClampToGround(); // Ensure we are planted firmly upon landing

        jumpProgress = 1f;
        isJumping = false;
    }
}
