using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Persistent singleton that orchestrates the entire run lifecycle.
/// Manages teleportation between InstanceZones, combat flow, barriers, and rewards.
/// </summary>
public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    public enum RunState { Idle, Combat, PostCombat, Transitioning }

    [Header("Zone References")]
    [Tooltip("The hub zone where the player starts and returns on death.")]
    public InstanceZone hubZone;

    [Tooltip("All available instance zones (combat, shop, respite, challenge). Exclude the hub.")]
    public InstanceZone[] instanceZones;

    [Header("Player Reference")]
    [Tooltip("The player root transform.")]
    public Transform player;

    [Header("Components")]
    [Tooltip("The EnemySpawner component — attach to the same GameObject or a child.")]
    public EnemySpawner enemySpawner;

    [Tooltip("The PortalSpawner component — attach to the same GameObject or a child.")]
    public PortalSpawner portalSpawner;

    [Header("Debug")]
    public bool showDebugLogs = true;

    [Tooltip("Skip combat and show all zones in their completed state. Use keys 1-9 to teleport to each zone in the instanceZones array.")]
    public bool debugSkipCombat = false;

    // Run State
    private RunState state = RunState.Idle;
    private InstanceZone currentZone;
    private int currentFloor = 0;
    private bool isInRun = false;
    private GameObject activeBarriers;

    public RunState CurrentState => state;
    public InstanceZone CurrentZone => currentZone;
    public int CurrentFloor => currentFloor;
    public bool IsInRun => isInRun;

    /// <summary>
    /// Fired when the run state changes. Useful for UI updates.
    /// </summary>
    public event System.Action<RunState> OnStateChanged;

    /// <summary>
    /// Fired when the player changes zone. Useful for UI updates.
    /// </summary>
    public event System.Action<InstanceZone> OnZoneChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-assign components if they aren't already set in the inspector
        if (enemySpawner == null) enemySpawner = GetComponent<EnemySpawner>();
        if (portalSpawner == null) portalSpawner = GetComponent<PortalSpawner>();
    }

    void Start()
    {
        // Start the player in the hub
        if (hubZone != null && player != null)
        {
            TeleportToZone(hubZone);
            SetState(RunState.Idle);
            SpawnHubPortal();
        }
    }

    private void SpawnHubPortal()
    {
        if (portalSpawner != null && hubZone != null)
        {
            // Spawn one combat portal at the center portal position of the hub
            Vector3 spawnPos = hubZone.GetPortalPositions()[1]; // The middle portal slot
            portalSpawner.SpawnSinglePortal(spawnPos, InstanceZone.InstanceType.Combat);
        }
    }


    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Starts a new run. Teleports the player to a random combat zone.
    /// Call this from a Hub NPC, UI button, or trigger.
    /// </summary>
    public void StartRun()
    {
        if (isInRun)
        {
            Debug.LogWarning("[RunManager] Already in a run!");
            return;
        }

        isInRun = true;
        currentFloor = 1;

        // Pick a random combat zone
        InstanceZone combatZone = GetRandomZoneOfType(InstanceZone.InstanceType.Combat);
        if (combatZone == null)
        {
            Debug.LogError("[RunManager] No combat zones configured!");
            isInRun = false;
            return;
        }

        if (showDebugLogs)
            Debug.Log($"[RunManager] Run started! Teleporting to {combatZone.gameObject.name}");

        StartInstance(combatZone);
    }

    /// <summary>
    /// Called when the player selects a portal. Begins the next instance.
    /// </summary>
    public void OnPortalSelected(InstanceZone.InstanceType type)
    {
        if (showDebugLogs)
            Debug.Log($"[RunManager] Portal selected: {type}");

        // If we are in the hub, we are now starting a run
        if (!isInRun)
        {
            isInRun = true;
            currentFloor = 1;
        }
        else
        {
            currentFloor++;
        }

        // Clean up current instance
        CleanupCurrentInstance();

        // Pick a random zone matching the portal type
        InstanceZone nextZone = GetRandomZoneOfType(type, currentZone);

        // Fallback: if no zones of that type exist, try any combat zone
        if (nextZone == null)
        {
            Debug.LogWarning($"[RunManager] No zones of type {type} found. Falling back to Combat.");
            nextZone = GetRandomZoneOfType(InstanceZone.InstanceType.Combat, currentZone);
        }

        if (nextZone == null)
        {
            Debug.LogError("[RunManager] No available zones! Returning to hub.");
            EndRun();
            return;
        }

        StartInstance(nextZone);
    }

    /// <summary>
    /// Called when the player dies. Resets the run and returns to hub.
    /// </summary>
    public void OnPlayerDeath()
    {
        if (showDebugLogs)
            Debug.Log("[RunManager] Player died! Showing death screen.");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowDeathScreen(() => {
                EndRun();
            });
        }
        else
        {
            EndRun();
        }
    }

    /// <summary>
    /// Ends the current run and returns the player to the hub.
    /// </summary>
    public void EndRun()
    {
        isInRun = false;
        currentFloor = 0;
        
        System.Action endRunAction = () => {
            CleanupCurrentInstance();
            
            if (player != null)
            {
                EntityStats pStats = player.GetComponent<EntityStats>();
                if (pStats != null)
                {
                    pStats.FullReset();
                }
            }
            
            TeleportToZone(hubZone);
            SetState(RunState.Idle);
            SpawnHubPortal();
        };

        if (UIManager.Instance != null)
        {
            UIManager.Instance.FadeAndCall(endRunAction);
        }
        else
        {
            endRunAction.Invoke();
        }
    }

    // ─────────────────────────────────────────────
    //  Instance Management
    // ─────────────────────────────────────────────

    private void StartInstance(InstanceZone zone)
    {
        SetState(RunState.Transitioning);
        
        System.Action startAction = () => {
            TeleportToZone(zone);

            switch (zone.zoneType)
            {
                case InstanceZone.InstanceType.Combat:
                case InstanceZone.InstanceType.Challenge:
                    StartCombatInstance(zone);
                    break;
                case InstanceZone.InstanceType.Shop:
                case InstanceZone.InstanceType.Respite:
                    // Non-combat zones spawn rewards immediately
                    StartNonCombatInstance(zone);
                    break;
            }
        };

        if (UIManager.Instance != null)
        {
            UIManager.Instance.FadeAndCall(startAction);
        }
        else
        {
            startAction.Invoke();
        }
    }

    private void StartCombatInstance(InstanceZone zone)
    {
        // Debug: skip combat entirely and go straight to rewards
        if (debugSkipCombat)
        {
            SpawnBarriers(zone);
            SpawnRewards(zone);
            SetState(RunState.PostCombat);
            Debug.Log($"[RunManager] DEBUG: Skipped combat in {zone.gameObject.name}. Rewards spawned.");
            return;
        }

        // Raise barriers
        SpawnBarriers(zone);

        // Start enemy spawning
        if (enemySpawner != null)
        {
            enemySpawner.OnAllEnemiesCleared += OnInstanceCleared;
            enemySpawner.StartSpawning(zone, player);
        }

        SetState(RunState.Combat);

        if (showDebugLogs)
            Debug.Log($"[RunManager] Combat started in {zone.gameObject.name}. Floor: {currentFloor}");
    }

    private void StartNonCombatInstance(InstanceZone zone)
    {
        // Spawn barriers so the player stays within the zone
        SpawnBarriers(zone);

        // Spawn rewards/portals immediately for non-combat zones
        SpawnRewards(zone);

        SetState(RunState.PostCombat);

        if (showDebugLogs)
            Debug.Log($"[RunManager] Non-combat instance ({zone.zoneType}) started in {zone.gameObject.name}.");
    }

    private void OnInstanceCleared()
    {
        if (showDebugLogs)
            Debug.Log("[RunManager] Instance cleared! Spawning rewards.");

        // Unsubscribe
        if (enemySpawner != null)
            enemySpawner.OnAllEnemiesCleared -= OnInstanceCleared;

        // Spawn chest + portals
        SpawnRewards(currentZone);
        SetState(RunState.PostCombat);
    }

    private void SpawnRewards(InstanceZone zone)
    {
        if (portalSpawner != null)
        {
            portalSpawner.SpawnRewards(zone);
        }
    }

    private void CleanupCurrentInstance()
    {
        // Destroy barriers
        DespawnBarriers();

        // Stop spawner and clean up enemies
        if (enemySpawner != null)
        {
            enemySpawner.OnAllEnemiesCleared -= OnInstanceCleared;
            enemySpawner.StopAndCleanup();
        }

        // Clean up portals and chests
        if (portalSpawner != null)
        {
            portalSpawner.CleanupSpawned();
        }
    }

    // ─────────────────────────────────────────────
    //  Teleportation
    // ─────────────────────────────────────────────

    private void TeleportToZone(InstanceZone zone)
    {
        if (zone == null || player == null) return;

        currentZone = zone;
        player.position = zone.GetPlayerSpawnPosition();

        if (Camera.main != null)
        {
            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.SnapToTarget();
            }
        }

        OnZoneChanged?.Invoke(zone);

        if (showDebugLogs)
            Debug.Log($"[RunManager] Teleported to {zone.gameObject.name} at {player.position}");
    }

    // ─────────────────────────────────────────────
    //  Barriers
    // ─────────────────────────────────────────────

    private void SpawnBarriers(InstanceZone zone)
    {
        DespawnBarriers(); // Clean up any existing barriers first
        activeBarriers = InstanceBarrier.CreateBarriersForZone(zone);

        if (showDebugLogs)
            Debug.Log($"[RunManager] Barriers raised for {zone.gameObject.name}");
    }

    private void DespawnBarriers()
    {
        if (activeBarriers != null)
        {
            Destroy(activeBarriers);
            activeBarriers = null;
        }
    }

    // ─────────────────────────────────────────────
    //  Zone Selection
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns a random InstanceZone matching the given type, optionally excluding one zone.
    /// </summary>
    private InstanceZone GetRandomZoneOfType(InstanceZone.InstanceType type, InstanceZone exclude = null)
    {
        List<InstanceZone> candidates = new List<InstanceZone>();

        foreach (InstanceZone zone in instanceZones)
        {
            if (zone == null) continue;
            if (zone.zoneType == type && zone != exclude)
                candidates.Add(zone);
        }

        // If no other zones available, allow re-entering the same zone
        if (candidates.Count == 0 && exclude != null && exclude.zoneType == type)
        {
            candidates.Add(exclude);
            if (showDebugLogs)
                Debug.Log($"[RunManager] Only one {type} zone available — re-entering {exclude.gameObject.name}");
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    // ─────────────────────────────────────────────
    //  State Management
    // ─────────────────────────────────────────────

    private void SetState(RunState newState)
    {
        state = newState;
        OnStateChanged?.Invoke(state);

        if (showDebugLogs)
            Debug.Log($"[RunManager] State: {state}");
    }
}
