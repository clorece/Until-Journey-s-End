using UnityEngine;
using System.Collections.Generic;

public class EnemyCombatManager : MonoBehaviour
{
    public static EnemyCombatManager Instance { get; private set; }

    [Header("Queue Settings")]
    [Tooltip("Global cooldown before another enemy can attack after one has finished.")]
    public float globalCooldown = 1.0f;
    [Tooltip("Only enemies matching this layer mask will be queued. Enemies on other layers attack instantly.")]
    public LayerMask managedEnemyLayers;

    
    [Header("Debug")]
    public bool showDebugLogs = false;

    private List<EnemyCombat> waitingQueue = new List<EnemyCombat>();
    private EnemyCombat activeAttacker;
    private float cooldownTimer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Optionally make it persistent depending on your game architecture
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (cooldownTimer > 0f)
        {
            float prevCooldown = cooldownTimer;
            cooldownTimer -= Time.deltaTime;
            
            if (cooldownTimer <= 0f && showDebugLogs)
            {
                Debug.Log("[EnemyCombatManager] Global cooldown finished. Ready for next attacker.");
            }
        }
        else if (activeAttacker == null && waitingQueue.Count > 0)
        {
            // Prune null references before executing
            waitingQueue.RemoveAll(e => e == null || !e.gameObject.activeInHierarchy);

            if (waitingQueue.Count > 0)
            {
                activeAttacker = waitingQueue[0];
                waitingQueue.RemoveAt(0);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[EnemyCombatManager] Token Granted to: {activeAttacker.gameObject.name}. Remaining in queue: {waitingQueue.Count}");
                }
                
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
        if (!waitingQueue.Contains(enemy) && activeAttacker != enemy)
        {
            waitingQueue.Add(enemy);
            enemy.isQueued = true;
            
            if (showDebugLogs)
                Debug.Log($"[EnemyCombatManager] Queued: {enemy.gameObject.name}. Total in queue: {waitingQueue.Count}");
        }
    }

    public void DequeueAttack(EnemyCombat enemy)
    {
        if (waitingQueue.Contains(enemy))
        {
            waitingQueue.Remove(enemy);
            enemy.isQueued = false;
            
            if (showDebugLogs)
                Debug.Log($"[EnemyCombatManager] Dequeued: {enemy.gameObject.name}. Remaining: {waitingQueue.Count}");
        }
    }

    public void NotifyAttackFinished(EnemyCombat enemy)
    {
        if (activeAttacker == enemy)
        {
            if (showDebugLogs)
                Debug.Log($"[EnemyCombatManager] Attack Finished constraint for: {enemy.gameObject.name}. Triggering Global Cooldown: {globalCooldown}s");

            activeAttacker.isQueued = false;
            activeAttacker = null;
            cooldownTimer = globalCooldown;
        }
    }

    public void OnEnemyInterrupted(EnemyCombat enemy)
    {
        if (activeAttacker == enemy)
        {
            if (showDebugLogs)
                Debug.Log($"[EnemyCombatManager] Active Attacker {enemy.gameObject.name} INTERRUPTED! Penalizing Global Cooldown.");

            // Eject active attacker and penalize queue
            activeAttacker.isQueued = false;
            activeAttacker = null;
            cooldownTimer = globalCooldown * 0.5f; // Penalize with half global cooldown instead of full to keep combat flowing 
        }
        else if (waitingQueue.Contains(enemy))
        {
            if (showDebugLogs)
                Debug.Log($"[EnemyCombatManager] Waiting Enemy {enemy.gameObject.name} was hit! Pushing to BACK of the queue.");

            // Requeue waiting enemy to the bottom of the list
            waitingQueue.Remove(enemy);
            waitingQueue.Add(enemy);
        }
    }
}
