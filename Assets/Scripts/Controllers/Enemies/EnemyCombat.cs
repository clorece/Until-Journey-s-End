using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("Dependencies")]
    public AnimationController animController;
    public AimController aimController;
    public CombatSystem combatSystem;

    public enum EnemyTier { Tier1, Tier2, Tier3 }

    [Header("Combat Type")]
    [Tooltip("Tier 1 = Minions, Tier 2 = High Value Targets, Tier 3 = Bosses (bypasses queues)")]
    public EnemyTier tier = EnemyTier.Tier1;
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
    [Tooltip("The range at which a ranged enemy will actively run away from the player.")]
    public float rangedAvoidanceRadius = 6.0f;

    [Header("Behavior Integration")]
    [Tooltip("If checked, the enemy will automatically stop moving exactly when it reaches its attack range, ensuring it naturally stops to attack.")]
    public bool syncStoppingWithAttackRange = true;
    [Tooltip("After attacking, the enemy will back off for this many seconds to let others attack.")]
    public float postAttackRecoveryDuration = 2.0f;
    [Tooltip("How far the enemy will back off during its recovery period.")]
    public float recoveryDistanceMultiplier = 1.8f;


    private EnemyMovement cachedMovement;
    private float recoveryTimer = 0f;
    private int currentComboIndex = 0;
    private bool isExecutingSkill = false;
    
    [HideInInspector]
    public bool isQueued = false;
    private bool wasDamaged = false;

    void Start()
    {
        if (animController == null) Debug.LogWarning("EnemyCombat: Missing AnimationController! Please assign it in the Inspector.");
        if (aimController == null) Debug.LogWarning("EnemyCombat: Missing AimController! Please assign it in the Inspector.");
        
        cachedMovement = GetComponent<EnemyMovement>();

        if (animController != null)
        {
            animController.OnAttackEnd += HandleAttackEnd;
        }
    }

    void Update()
    {
        if (animController == null || aimController == null) return;
        if (aimController.target == null) return;

        // Continuously aim the attack point at the target
        if (!disableAimingAttackPoint && combatSystem != null && combatSystem.attackPoint != null && !animController.IsAttacking && !animController.IsDamaged)
        {
            Vector3 direction = aimController.target.position - combatSystem.attackPoint.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                combatSystem.attackPoint.rotation = Quaternion.LookRotation(direction);
            }
        }

        // Manage recovery timer
        if (recoveryTimer > 0)
        {
            recoveryTimer -= Time.deltaTime;
        }
        
        // Check for interruptions
        bool currentDamaged = animController.IsDamaged;
        if (currentDamaged && !wasDamaged && EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.OnEnemyInterrupted(this);
            recoveryTimer = 0; // Cancel recovery if hit
        }
        wasDamaged = currentDamaged;

        // Don't try to attack or queue if already attacking, damaged, or RECOVERING
        if (animController.IsAttacking || animController.IsDamaged || recoveryTimer > 0)
        {
            // Maintain the recovery stopping distance if needed (melee only)
            if (recoveryTimer > 0 && syncStoppingWithAttackRange && cachedMovement != null && !isRanged)
            {
                cachedMovement.stoppingDistance = meleeAttackRange * recoveryDistanceMultiplier;
            }
            return;
        }

        // Calculate distance from root transform to match EnemyMovement's calculation.
        // Using aimController.GetDistanceToTarget() can cause directional bias if
        // the AimController is on a child object with a local offset.
        Vector3 diff = aimController.target.position - transform.position;
        diff.y = 0f;
        float distance = diff.magnitude;

        if (isRanged)
        {
            float actualRangedRange = rangedAttackRange > 0f ? rangedAttackRange : aimController.detectionRadius;
            
            if (syncStoppingWithAttackRange && cachedMovement != null)
                cachedMovement.stoppingDistance = rangedAvoidanceRadius; // flee threshold is now independently controlled

            if (distance <= actualRangedRange)
            {
                if (tier == EnemyTier.Tier3)
                    BeginAttack();
                else if (EnemyCombatManager.Instance != null && EnemyCombatManager.Instance.IsLayerManaged(gameObject.layer))
                    EnemyCombatManager.Instance.QueueAttack(this);
                else
                    BeginAttack(); 
            }
            else
            {
                if (EnemyCombatManager.Instance != null)
                    EnemyCombatManager.Instance.DequeueAttack(this);
            }
        }
        else
        {
            // Dynamically determine melee engage range based on available skills
            float effectiveMeleeRange = meleeAttackRange;
            if (animController != null && animController.skills != null)
            {
                for (int i = 0; i < animController.skills.Length; i++)
                {
                    if (animController.CooldownTimer != null && i < animController.CooldownTimer.Length && animController.CooldownTimer[i] <= 0f)
                    {
                        AnimationFrames skillData = animController.skills[i];
                        
                        // Check if we meet the minimum distance requirement (if one is set)
                        if (skillData.skillMinimumRange <= 0f || distance >= skillData.skillMinimumRange)
                        {
                            // Expand the queue distance to the skill's MAXIMUM execution range!
                            if (skillData.skillExecutionRange > effectiveMeleeRange)
                            {
                                effectiveMeleeRange = skillData.skillExecutionRange;
                            }
                        }
                    }
                }
            }

            if (syncStoppingWithAttackRange && cachedMovement != null)
                cachedMovement.stoppingDistance = effectiveMeleeRange * 0.95f;

            // Melee check
            if (distance <= effectiveMeleeRange)
            {
                if (tier == EnemyTier.Tier3)
                    BeginAttack();
                else if (EnemyCombatManager.Instance != null && EnemyCombatManager.Instance.IsLayerManaged(gameObject.layer))
                    EnemyCombatManager.Instance.QueueAttack(this);
                else
                    BeginAttack();
            }
            else
            {
                if (EnemyCombatManager.Instance != null)
                    EnemyCombatManager.Instance.DequeueAttack(this);
            }
        }
    }

    public void BeginAttack()
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

        isExecutingSkill = false;

        // 1. Prioritize Skills (if any exist and are off cooldown)
        if (animController.skills != null && animController.skills.Length > 0)
        {
            float distToTarget = 0f;
            if (aimController != null && aimController.target != null)
            {
                Vector3 toTarget = aimController.target.position - transform.position;
                toTarget.y = 0;
                distToTarget = toTarget.magnitude;
            }

            for (int i = 0; i < animController.skills.Length; i++)
            {
                if (animController.CooldownTimer != null && i < animController.CooldownTimer.Length && animController.CooldownTimer[i] <= 0f)
                {
                    AnimationFrames skillData = animController.skills[i];
                    bool inRange = true;

                    if (skillData.skillExecutionRange > 0f)
                    {
                        if (distToTarget > skillData.skillExecutionRange)
                            inRange = false;
                    }
                    
                    if (skillData.skillMinimumRange > 0f)
                    {
                        if (distToTarget < skillData.skillMinimumRange)
                            inRange = false;
                    }

                    if (inRange)
                    {
                        isExecutingSkill = true;
                        animController.TriggerSkill(i);
                        return;
                    }
                }
            }
        }

        // 2. Otherwise, start the standard attack combo
        currentComboIndex = 0;
        if (animController.attackClips != null && animController.attackClips.Length > 0)
        {
            animController.ExecuteAttackClip(currentComboIndex);
        }
        else
        {
            // Failsafe if no attacks exist
            FinishAttackSequence();
        }
    }

    private void HandleAttackEnd(AnimationFrames finishedClip)
    {
        if (isExecutingSkill)
        {
            isExecutingSkill = false;
            FinishAttackSequence();
        }
        else
        {
            currentComboIndex++;
            if (animController.attackClips != null && currentComboIndex < animController.attackClips.Length)
            {
                // Continue the combo!
                
                // Re-aim for the next hit of the combo to track the player if they moved
                if (!disableAimingAttackPoint && combatSystem != null && combatSystem.attackPoint != null && aimController != null && aimController.target != null)
                {
                    Vector3 direction = aimController.target.position - combatSystem.attackPoint.position;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        combatSystem.attackPoint.rotation = Quaternion.LookRotation(direction);
                    }
                }

                animController.ExecuteAttackClip(currentComboIndex);
            }
            else
            {
                // Combo finished!
                FinishAttackSequence();
            }
        }
    }

    private void FinishAttackSequence()
    {
        // Start recovery timer immediately when attack finishes
        recoveryTimer = postAttackRecoveryDuration;

        // Force a temporary back-off distance (melee only)
        if (cachedMovement != null && !isRanged)
        {
            cachedMovement.stoppingDistance = meleeAttackRange * recoveryDistanceMultiplier;
        }

        if (EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.NotifyAttackFinished(this);
        }
    }

    void OnDestroy()
    {
        if (animController != null)
        {
            animController.OnAttackEnd -= HandleAttackEnd;
        }
        if (EnemyCombatManager.Instance != null)
        {
            EnemyCombatManager.Instance.DequeueAttack(this);
            // Optionally, if they are the active attacker when destroyed, we can call OnEnemyInterrupted to free the token.
            EnemyCombatManager.Instance.OnEnemyInterrupted(this);
        }
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
