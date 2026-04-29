using UnityEngine;
using System.Collections.Generic;

public class EnemyCombatManager : MonoBehaviour
{
    public static EnemyCombatManager Instance { get; private set; }

    [Header("Queue Settings")]
    [Tooltip("Global cooldown before another melee enemy can attack after one has finished.")]
    public float meleeGlobalCooldown = 1.0f;
    [Tooltip("Global cooldown before another ranged enemy can attack after one has finished.")]
    public float rangedGlobalCooldown = 0.5f;
    [Tooltip("Only enemies matching this layer mask will be queued. Enemies on other layers attack instantly.")]
    public LayerMask managedEnemyLayers;

    
    [Header("Debug")]
    public bool showDebugLogs = false;

    // Separate queues for melee and ranged
    private List<EnemyCombat> meleeQueue = new List<EnemyCombat>();
    private List<EnemyCombat> rangedQueue = new List<EnemyCombat>();
    private EnemyCombat activeMeleeAttacker;
    private EnemyCombat activeRangedAttacker;
    private float meleeCooldownTimer;
    private float rangedCooldownTimer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Process melee queue
        ProcessQueue(ref meleeCooldownTimer, ref activeMeleeAttacker, meleeQueue, meleeGlobalCooldown, "Melee");

        // Process ranged queue (independently)
        ProcessQueue(ref rangedCooldownTimer, ref activeRangedAttacker, rangedQueue, rangedGlobalCooldown, "Ranged");
    }

    private void ProcessQueue(ref float cooldownTimer, ref EnemyCombat activeAttacker, List<EnemyCombat> queue, float cooldown, string label)
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;

            if (cooldownTimer <= 0f && showDebugLogs)
                Debug.Log($"[EnemyCombatManager] {label} cooldown finished. Ready for next attacker.");
        }
        else if (activeAttacker == null && queue.Count > 0)
        {
            // Prune null references before executing
            queue.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);

            if (queue.Count > 0)
            {
                activeAttacker = queue[0];
                queue.RemoveAt(0);

                if (showDebugLogs)
                    Debug.Log($"[EnemyCombatManager] {label} Token Granted to: {activeAttacker.gameObject.name}. Remaining in queue: {queue.Count}");

                activeAttacker.BeginAttack();
            }
        }
    }

    public bool IsLayerManaged(int layer)
    {
        return (managedEnemyLayers.value & (1 << layer)) != 0;
    }

    public void QueueAttack(EnemyCombat enemy)
    {
        List<EnemyCombat> queue = enemy.isRanged ? rangedQueue : meleeQueue;
        EnemyCombat active = enemy.isRanged ? activeRangedAttacker : activeMeleeAttacker;

        if (!queue.Contains(enemy) && active != enemy)
        {
            queue.Add(enemy);
            enemy.isQueued = true;

            if (showDebugLogs)
            {
                string label = enemy.isRanged ? "Ranged" : "Melee";
                Debug.Log($"[EnemyCombatManager] {label} Queued: {enemy.gameObject.name}. Total in queue: {queue.Count}");
            }
        }
    }

    public void DequeueAttack(EnemyCombat enemy)
    {
        List<EnemyCombat> queue = enemy.isRanged ? rangedQueue : meleeQueue;

        if (queue.Contains(enemy))
        {
            queue.Remove(enemy);
            enemy.isQueued = false;

            if (showDebugLogs)
            {
                string label = enemy.isRanged ? "Ranged" : "Melee";
                Debug.Log($"[EnemyCombatManager] {label} Dequeued: {enemy.gameObject.name}. Remaining: {queue.Count}");
            }
        }
    }

    public void NotifyAttackFinished(EnemyCombat enemy)
    {
        if (enemy.isRanged && activeRangedAttacker == enemy)
        {
            if (showDebugLogs)
                Debug.Log($"[EnemyCombatManager] Ranged Attack Finished: {enemy.gameObject.name}. Cooldown: {rangedGlobalCooldown}s");

            activeRangedAttacker.isQueued = false;
            activeRangedAttacker = null;
            rangedCooldownTimer = rangedGlobalCooldown;
        }
        else if (!enemy.isRanged && activeMeleeAttacker == enemy)
        {
            if (showDebugLogs)
                Debug.Log($"[EnemyCombatManager] Melee Attack Finished: {enemy.gameObject.name}. Cooldown: {meleeGlobalCooldown}s");

            activeMeleeAttacker.isQueued = false;
            activeMeleeAttacker = null;
            meleeCooldownTimer = meleeGlobalCooldown;
        }
    }

    public void OnEnemyInterrupted(EnemyCombat enemy)
    {
        if (enemy.isRanged)
        {
            if (activeRangedAttacker == enemy)
            {
                if (showDebugLogs)
                    Debug.Log($"[EnemyCombatManager] Ranged Attacker {enemy.gameObject.name} INTERRUPTED!");

                activeRangedAttacker.isQueued = false;
                activeRangedAttacker = null;
                rangedCooldownTimer = rangedGlobalCooldown * 0.5f;
            }
            else if (rangedQueue.Contains(enemy))
            {
                if (showDebugLogs)
                    Debug.Log($"[EnemyCombatManager] Waiting Ranged {enemy.gameObject.name} hit! Pushed to back.");

                rangedQueue.Remove(enemy);
                rangedQueue.Add(enemy);
            }
        }
        else
        {
            if (activeMeleeAttacker == enemy)
            {
                if (showDebugLogs)
                    Debug.Log($"[EnemyCombatManager] Melee Attacker {enemy.gameObject.name} INTERRUPTED!");

                activeMeleeAttacker.isQueued = false;
                activeMeleeAttacker = null;
                meleeCooldownTimer = meleeGlobalCooldown * 0.5f;
            }
            else if (meleeQueue.Contains(enemy))
            {
                if (showDebugLogs)
                    Debug.Log($"[EnemyCombatManager] Waiting Melee {enemy.gameObject.name} hit! Pushed to back.");

                meleeQueue.Remove(enemy);
                meleeQueue.Add(enemy);
            }
        }
    }
}
