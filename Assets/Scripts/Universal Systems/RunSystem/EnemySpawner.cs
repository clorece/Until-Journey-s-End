using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Quota Settings")]
    public int totalMonstersToSpawn = 10;
    public int maxActiveEnemies = 3;
    public float spawnInterval = 2f;

    [Header("Enemy Variety")]
    public List<GameObject> enemyPrefabs;
    public Transform[] spawnPoints;

    private int monstersSpawned = 0;
    private int monstersKilled = 0;
    private List<EntityStats> activeEnemies = new List<EntityStats>();

    public event System.Action OnAllEnemiesDead;

    public void StartSpawning()
    {
        StartCoroutine(SpawnRoutine());
    }

    private System.Collections.IEnumerator SpawnRoutine()
    {
        while (monstersSpawned < totalMonstersToSpawn)
        {
            if (activeEnemies.Count < maxActiveEnemies)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(spawnInterval);
            }
            yield return null;
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs.Count == 0 || spawnPoints.Length == 0) return;

        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject enemyObj = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        monstersSpawned++;

        EntityStats stats = enemyObj.GetComponent<EntityStats>();
        if (stats != null)
        {
            activeEnemies.Add(stats);
            stats.OnDeath += () => HandleEnemyDeath(stats);
        }
    }

    private void HandleEnemyDeath(EntityStats stats)
    {
        if (activeEnemies.Contains(stats))
        {
            activeEnemies.Remove(stats);
            monstersKilled++;

            if (monstersKilled >= totalMonstersToSpawn)
            {
                OnAllEnemiesDead?.Invoke();
            }
        }
    }
}
