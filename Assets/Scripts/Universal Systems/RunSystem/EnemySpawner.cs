using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles spawning enemies within a zone and tracking kill count.
/// Managed by RunManager, which configures it dynamically for each combat instance.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Seconds to wait before spawning the first wave, giving the player time to prepare.")]
    public float initialDelay = 2f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Runtime State (Hidden in Inspector to avoid confusion with InstanceZone data)
    private InstanceZone zone;
    private Transform player;
    private GameObject[] enemyPrefabs;
    private int totalEnemies;
    private int maxActiveEnemies;
    private float spawnInterval;

    private int spawned = 0;
    private int killed = 0;
    private float spawnTimer = 0f;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private bool isActive = false;

    public int Killed => killed;
    public int TotalEnemies => totalEnemies;
    public int Remaining => totalEnemies - killed;
    public bool IsCleared => killed >= totalEnemies;

    /// <summary>
    /// Fired when all enemies in this instance have been killed.
    /// </summary>
    public event System.Action OnAllEnemiesCleared;

    /// <summary>
    /// Begins the spawning sequence. Configures itself using data from the target zone.
    /// </summary>
    public void StartSpawning(InstanceZone targetZone, Transform playerTarget)
    {
        zone = targetZone;
        player = playerTarget;

        // Pull config from zone
        if (targetZone.enemyPrefabs != null && targetZone.enemyPrefabs.Length > 0)
            enemyPrefabs = targetZone.enemyPrefabs;
        else
            Debug.LogWarning($"[EnemySpawner] No enemy prefabs assigned in zone {targetZone.name}!");

        totalEnemies = targetZone.totalEnemies;
        maxActiveEnemies = targetZone.maxActiveEnemies;
        spawnInterval = targetZone.spawnInterval;

        spawned = 0;
        killed = 0;
        spawnTimer = 0f;
        activeEnemies.Clear();
        isActive = true;

        if (showDebugLogs)
            Debug.Log($"[EnemySpawner] Started. Total: {totalEnemies}, MaxActive: {maxActiveEnemies}. First wave in {initialDelay}s");

        // Delay the first wave so the player doesn't get hit immediately
        spawnTimer = -initialDelay;
    }

    /// <summary>
    /// Stops the spawner and cleans up any remaining enemies.
    /// </summary>
    public void StopAndCleanup()
    {
        isActive = false;

        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }
        activeEnemies.Clear();

        spawned = 0;
        killed = 0;
    }

    void Update()
    {
        if (!isActive) return;
        if (IsCleared) return;

        // Prune destroyed enemies from the active list
        activeEnemies.RemoveAll(e => e == null);

        // Check if we need to spawn more
        if (spawned < totalEnemies && activeEnemies.Count < maxActiveEnemies)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnWave();
            }
        }
    }

    private void SpawnWave()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        // Spawn enemies up to maxActive or totalEnemies, whichever is lower
        int toSpawn = Mathf.Min(maxActiveEnemies - activeEnemies.Count, totalEnemies - spawned);

        for (int i = 0; i < toSpawn; i++)
        {
            SpawnSingleEnemy();
        }
    }

    private void SpawnSingleEnemy()
    {
        if (zone == null) return;

        // Pick a random prefab
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        // Pick a spawn position
        Vector3 spawnPos;
        if (zone.spawnPoints != null && zone.spawnPoints.Length > 0)
        {
            Transform sp = zone.spawnPoints[Random.Range(0, zone.spawnPoints.Length)];
            spawnPos = sp != null ? sp.position : zone.GetRandomSpawnPosition();
        }
        else
        {
            spawnPos = zone.GetRandomSpawnPosition();
        }

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        spawned++;

        // --- Wire up EnemyMovement ---
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.target = player;
            movement.boundaryZone = zone;
        }

        // --- Wire up AimController (feeds targeting to EnemyCombat) ---
        AimController aimController = enemy.GetComponentInChildren<AimController>();
        if (aimController != null && aimController.aimMode == AimController.AimMode.Enemy)
        {
            aimController.target = player;
        }

        // --- Wire up EnemyCombat dependencies if not already set ---
        EnemyCombat combat = enemy.GetComponent<EnemyCombat>();
        if (combat != null)
        {
            if (combat.animController == null)
                combat.animController = enemy.GetComponentInChildren<AnimationController>();
            if (combat.aimController == null)
                combat.aimController = aimController;
            if (combat.combatSystem == null)
                combat.combatSystem = enemy.GetComponentInChildren<CombatSystem>();
        }

        // --- Subscribe to death event ---
        EntityStats stats = enemy.GetComponent<EntityStats>();
        if (stats != null)
        {
            stats.OnDeath += () => OnEnemyKilled(enemy);
        }

        activeEnemies.Add(enemy);

        if (showDebugLogs)
            Debug.Log($"[EnemySpawner] Spawned {prefab.name} at {spawnPos}. ({spawned}/{totalEnemies}) Active: {activeEnemies.Count}");
    }

    private void OnEnemyKilled(GameObject enemy)
    {
        killed++;
        activeEnemies.Remove(enemy);

        if (showDebugLogs)
            Debug.Log($"[EnemySpawner] Enemy killed. ({killed}/{totalEnemies}) Active: {activeEnemies.Count}");

        if (killed >= totalEnemies)
        {
            isActive = false;
            OnAllEnemiesCleared?.Invoke();
        }
    }
}
