using UnityEngine;
using System.Collections;

public class InstanceManager : MonoBehaviour
{
    public static InstanceManager Instance { get; private set; }

    [Header("Instance Settings")]
    public RunManager.InstanceMode mode;
    public bool isCleared = false;

    [Header("References")]
    public EnemySpawner spawner;
    public Transform rewardSpawnPoint;
    public GameObject chestPrefab;
    public GameObject portalManagerPrefab;

    public event System.Action OnInstanceCleared;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Inherit mode from RunManager if it exists
        if (RunManager.Instance != null)
        {
            mode = RunManager.Instance.nextMode;
        }

        InitializeInstance();
    }

    private void InitializeInstance()
    {
        if (mode == RunManager.InstanceMode.Battle)
        {
            if (spawner != null)
            {
                spawner.OnAllEnemiesDead += HandleBattleCleared;
                spawner.StartSpawning();
            }
        }
        else
        {
            // For other modes, we might just spawn NPCs or shops immediately
            HandleBattleCleared(); 
        }
    }

    private void HandleBattleCleared()
    {
        if (isCleared) return;
        isCleared = true;

        Debug.Log("[InstanceManager] Instance Cleared! Spawning rewards and portals.");
        
        SpawnRewards();
        SpawnPortals();
        
        OnInstanceCleared?.Invoke();
    }

    private void SpawnRewards()
    {
        if (chestPrefab != null && rewardSpawnPoint != null)
        {
            Instantiate(chestPrefab, rewardSpawnPoint.position, rewardSpawnPoint.rotation);
        }
    }

    private void SpawnPortals()
    {
        if (portalManagerPrefab != null)
        {
            // PortalManager will handle randomization and placement
            Instantiate(portalManagerPrefab, transform.position, Quaternion.identity);
        }
    }
}
