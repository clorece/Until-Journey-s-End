using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("Dependencies")]
    public AnimationController animController;
    public AimController aimController;
    public CombatSystem combatSystem;

    [Header("Combat Type")]
    [Tooltip("If true, the enemy is ranged. If false, the enemy is melee.")]
    public bool isRanged = false;
    [Tooltip("If checked, EnemyCombat will completely ignore trying to aim the attack point. Use this to test if EnemyCombat is causing your sprite spin.")]
    public bool disableAimingAttackPoint = false;

    [Header("Melee Settings")]
    [Tooltip("How close the player must be to trigger a melee attack.")]
    public float meleeAttackRange = 1.5f;

    [Header("Ranged Settings")]
    [Tooltip("The range at which this enemy can fire projectiles. Leave at 0 to just use the AimController's detectionRadius.")]
    public float rangedAttackRange = 0f;

    [Header("Behavior Integration")]
    [Tooltip("If checked, the enemy will automatically stop moving exactly when it reaches its attack range, ensuring it naturally stops to attack.")]
    public bool syncStoppingWithAttackRange = true;

    private Transform attackProxy;
    private EnemyMovement cachedMovement;

    void Start()
    {
        if (animController == null) Debug.LogWarning("EnemyCombat: Missing AnimationController! Please assign it in the Inspector.");
        if (aimController == null) Debug.LogWarning("EnemyCombat: Missing AimController! Please assign it in the Inspector.");
        
        cachedMovement = GetComponent<EnemyMovement>();

        // If the user slaps the sprite or base object into the CombatSystem attack point, we don't want to rotate it!
        // Instead, we create an invisible proxy to handle the aiming safely.
        if (combatSystem != null && combatSystem.attackPoint != null)
        {
            GameObject proxyObj = new GameObject("EnemyAttackProxy");
            proxyObj.transform.SetParent(transform);
            proxyObj.transform.position = combatSystem.attackPoint.position;
            
            attackProxy = proxyObj.transform;
            combatSystem.attackPoint = attackProxy;
        }
    }

    void Update()
    {
        if (animController == null || aimController == null) return;
        if (aimController.target == null) return;
        
        // Don't try to attack if already attacking
        if (animController.IsAttacking) return;

        float distance = aimController.GetDistanceToTarget();

        if (isRanged)
        {
            float actualRangedRange = rangedAttackRange > 0f ? rangedAttackRange : aimController.detectionRadius;
            
            if (syncStoppingWithAttackRange && cachedMovement != null)
                cachedMovement.stoppingDistance = actualRangedRange * 0.95f; // stop just barely inside the range

            if (distance <= actualRangedRange)
            {
                TryAttack();
            }
        }
        else
        {
            if (syncStoppingWithAttackRange && cachedMovement != null)
                cachedMovement.stoppingDistance = meleeAttackRange * 0.95f;

            // Melee check
            if (distance <= meleeAttackRange)
            {
                TryAttack();
            }
        }
    }

    private void TryAttack()
    {
        // Ensure the attack point is aimed perfectly at the player
        if (!disableAimingAttackPoint && combatSystem != null && combatSystem.attackPoint != null && aimController != null && aimController.target != null)
        {
            Vector3 direction = aimController.target.position - combatSystem.attackPoint.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                combatSystem.attackPoint.rotation = Quaternion.LookRotation(direction);
            }
        }

        // Execute the first attack clip defined in AnimationController
        animController.ExecuteAttackClip(0);
    }

    void OnDrawGizmosSelected()
    {
        // Draw the attack range in the scene view
        if (!isRanged)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f); // Red for melee
            Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
        }
        else
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f); // Orange for ranged
            if (rangedAttackRange > 0f)
            {
                Gizmos.DrawWireSphere(transform.position, rangedAttackRange);
            }
            // If rangedAttackRange is 0, we rely on AimController's gizmo for the detectionRadius
        }

        // Draw the aim direction of the proxy so the user can see exactly where the attacks will fire!
        if (combatSystem != null && combatSystem.attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(combatSystem.attackPoint.position, combatSystem.attackPoint.forward * 4f);
        }
    }
}
