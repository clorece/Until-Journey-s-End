using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TierSettings
{
    public float meleeGlobalCooldown = 1.0f;
    public float rangedGlobalCooldown = 0.5f;
}

public class TierQueue
{
    public List<EnemyCombat> meleeQueue = new List<EnemyCombat>();
    public List<EnemyCombat> rangedQueue = new List<EnemyCombat>();
    public EnemyCombat activeMeleeAttacker;
    public EnemyCombat activeRangedAttacker;
    public float meleeCooldownTimer;
    public float rangedCooldownTimer;
}

public class EnemyCombatManager : MonoBehaviour
{
    public static EnemyCombatManager Instance { get; private set; }

    [Header("Tier 1 Settings (Minions)")]
    public TierSettings tier1Settings = new TierSettings { meleeGlobalCooldown = 1.0f, rangedGlobalCooldown = 0.5f };

    [Header("Tier 2 Settings (High Value Targets)")]
    public TierSettings tier2Settings = new TierSettings { meleeGlobalCooldown = 0.8f, rangedGlobalCooldown = 0.4f };

    [Header("Global Settings")]
    [Tooltip("Only enemies matching this layer mask will be queued. Enemies on other layers attack instantly.")]
    public LayerMask managedEnemyLayers;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private Dictionary<EnemyCombat.EnemyTier, TierQueue> tierQueues;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeQueues();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeQueues()
    {
        tierQueues = new Dictionary<EnemyCombat.EnemyTier, TierQueue>
        {
            { EnemyCombat.EnemyTier.Tier1, new TierQueue() },
            { EnemyCombat.EnemyTier.Tier2, new TierQueue() }
            // Bosses (Tier3) bypass queues completely
        };
    }

    void Update()
    {
        ProcessTier(EnemyCombat.EnemyTier.Tier1, tier1Settings);
        ProcessTier(EnemyCombat.EnemyTier.Tier2, tier2Settings);
    }

    private void ProcessTier(EnemyCombat.EnemyTier tier, TierSettings settings)
    {
        if (!tierQueues.TryGetValue(tier, out TierQueue tq)) return;

        ProcessQueue(ref tq.meleeCooldownTimer, ref tq.activeMeleeAttacker, tq.meleeQueue, settings.meleeGlobalCooldown, $"{tier} Melee");
        ProcessQueue(ref tq.rangedCooldownTimer, ref tq.activeRangedAttacker, tq.rangedQueue, settings.rangedGlobalCooldown, $"{tier} Ranged");
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
        if (enemy.tier == EnemyCombat.EnemyTier.Tier3) return; // Bosses don't queue
        if (!tierQueues.TryGetValue(enemy.tier, out TierQueue tq)) return;

        List<EnemyCombat> queue = enemy.isRanged ? tq.rangedQueue : tq.meleeQueue;
        EnemyCombat active = enemy.isRanged ? tq.activeRangedAttacker : tq.activeMeleeAttacker;

        if (!queue.Contains(enemy) && active != enemy)
        {
            queue.Add(enemy);
            enemy.isQueued = true;

            if (showDebugLogs)
            {
                string label = enemy.isRanged ? "Ranged" : "Melee";
                Debug.Log($"[EnemyCombatManager] {enemy.tier} {label} Queued: {enemy.gameObject.name}. Total in queue: {queue.Count}");
            }
        }
    }

    public void DequeueAttack(EnemyCombat enemy)
    {
        if (enemy.tier == EnemyCombat.EnemyTier.Tier3) return;
        if (!tierQueues.TryGetValue(enemy.tier, out TierQueue tq)) return;

        List<EnemyCombat> queue = enemy.isRanged ? tq.rangedQueue : tq.meleeQueue;

        if (queue.Contains(enemy))
        {
            queue.Remove(enemy);
            enemy.isQueued = false;

            if (showDebugLogs)
            {
                string label = enemy.isRanged ? "Ranged" : "Melee";
                Debug.Log($"[EnemyCombatManager] {enemy.tier} {label} Dequeued: {enemy.gameObject.name}. Remaining: {queue.Count}");
            }
        }
    }

    public void NotifyAttackFinished(EnemyCombat enemy)
    {
        if (enemy.tier == EnemyCombat.EnemyTier.Tier3) return;
        if (!tierQueues.TryGetValue(enemy.tier, out TierQueue tq)) return;
        TierSettings settings = enemy.tier == EnemyCombat.EnemyTier.Tier1 ? tier1Settings : tier2Settings;

        if (enemy.isRanged && tq.activeRangedAttacker == enemy)
        {
            if (showDebugLogs)
                Debug.Log($"[EnemyCombatManager] {enemy.tier} Ranged Attack Finished: {enemy.gameObject.name}. Cooldown: {settings.rangedGlobalCooldown}s");

            tq.activeRangedAttacker.isQueued = false;
            tq.activeRangedAttacker = null;
            tq.rangedCooldownTimer = settings.rangedGlobalCooldown;
        }
        else if (!enemy.isRanged && tq.activeMeleeAttacker == enemy)
        {
            if (showDebugLogs)
                Debug.Log($"[EnemyCombatManager] {enemy.tier} Melee Attack Finished: {enemy.gameObject.name}. Cooldown: {settings.meleeGlobalCooldown}s");

            tq.activeMeleeAttacker.isQueued = false;
            tq.activeMeleeAttacker = null;
            tq.meleeCooldownTimer = settings.meleeGlobalCooldown;
        }
    }

    public void OnEnemyInterrupted(EnemyCombat enemy)
    {
        if (enemy.tier == EnemyCombat.EnemyTier.Tier3) return;
        if (!tierQueues.TryGetValue(enemy.tier, out TierQueue tq)) return;
        TierSettings settings = enemy.tier == EnemyCombat.EnemyTier.Tier1 ? tier1Settings : tier2Settings;

        if (enemy.isRanged)
        {
            if (tq.activeRangedAttacker == enemy)
            {
                if (showDebugLogs)
                    Debug.Log($"[EnemyCombatManager] {enemy.tier} Ranged Attacker {enemy.gameObject.name} INTERRUPTED!");

                tq.activeRangedAttacker.isQueued = false;
                tq.activeRangedAttacker = null;
                tq.rangedCooldownTimer = settings.rangedGlobalCooldown * 0.5f;
            }
            else if (tq.rangedQueue.Contains(enemy))
            {
                if (showDebugLogs)
                    Debug.Log($"[EnemyCombatManager] Waiting {enemy.tier} Ranged {enemy.gameObject.name} hit! Pushed to back.");

                tq.rangedQueue.Remove(enemy);
                tq.rangedQueue.Add(enemy);
            }
        }
        else
        {
            if (tq.activeMeleeAttacker == enemy)
            {
                if (showDebugLogs)
                    Debug.Log($"[EnemyCombatManager] {enemy.tier} Melee Attacker {enemy.gameObject.name} INTERRUPTED!");

                tq.activeMeleeAttacker.isQueued = false;
                tq.activeMeleeAttacker = null;
                tq.meleeCooldownTimer = settings.meleeGlobalCooldown * 0.5f;
            }
            else if (tq.meleeQueue.Contains(enemy))
            {
                if (showDebugLogs)
                    Debug.Log($"[EnemyCombatManager] Waiting {enemy.tier} Melee {enemy.gameObject.name} hit! Pushed to back.");

                tq.meleeQueue.Remove(enemy);
                tq.meleeQueue.Add(enemy);
            }
        }
    }
}
