using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Utility class that handles spawning portals with randomized unique types
/// and a reward chest at the designated positions within a zone.
/// </summary>
public class PortalSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [Tooltip("The portal prefab. Must have a Portal component with icons pre-configured.")]
    public GameObject portalPrefab;

    [Tooltip("The chest prefab. Must have a Chest component.")]
    public GameObject chestPrefab;

    [Tooltip("The campfire prefab for Respite zones. Must have a Campfire component.")]
    public GameObject campfirePrefab;

    private List<GameObject> spawnedObjects = new List<GameObject>();

    /// <summary>
    /// Spawns a center object (chest or campfire) and 3 portals with unique randomized types.
    /// Positions are relative to the zone center.
    /// Returns the list of spawned GameObjects for cleanup.
    /// </summary>
    public List<GameObject> SpawnRewards(InstanceZone zone)
    {
        CleanupSpawned();

        // Spawn the center object: campfire for Respite, chest for everything else
        if (zone.zoneType == InstanceZone.InstanceType.Respite && campfirePrefab != null)
        {
            Vector3 campfirePos = zone.GetCampfirePosition();
            GameObject campfire = Instantiate(campfirePrefab, campfirePos, Quaternion.identity);
            spawnedObjects.Add(campfire);
        }
        else if (chestPrefab != null)
        {
            Vector3 chestPos = zone.GetChestPosition();
            GameObject chest = Instantiate(chestPrefab, chestPos, Quaternion.identity);
            spawnedObjects.Add(chest);
        }

        // Spawn 3 portals with unique types, north of center
        if (portalPrefab != null)
        {
            InstanceZone.InstanceType[] types = GetRandomUniqueTypes(3);
            Vector3[] portalPositions = zone.GetPortalPositions();

            for (int i = 0; i < 3 && i < types.Length; i++)
            {
                GameObject portalObj = Instantiate(portalPrefab, portalPositions[i], Quaternion.identity);
                Portal portal = portalObj.GetComponent<Portal>();

                if (portal != null)
                {
                    // Icons are already set in the portal prefab, so we only set the type
                    portal.SetType(types[i]);
                }

                spawnedObjects.Add(portalObj);
            }
        }
        else
        {
            Debug.LogWarning("[PortalSpawner] Portal prefab is not assigned!");
        }

        return spawnedObjects;
    }

    /// <summary>
    /// Spawns a single portal of a specific type at a specific position.
    /// Used for Hub "Start Run" portals or fixed transitions.
    /// </summary>
    public GameObject SpawnSinglePortal(Vector3 position, InstanceZone.InstanceType type)
    {
        if (portalPrefab == null) return null;

        GameObject portalObj = Instantiate(portalPrefab, position, Quaternion.identity);
        Portal portal = portalObj.GetComponent<Portal>();

        if (portal != null)
        {
            portal.SetType(type);
        }

        spawnedObjects.Add(portalObj);
        return portalObj;
    }

    /// <summary>
    /// Destroys all previously spawned chests and portals.
    /// </summary>
    public void CleanupSpawned()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        spawnedObjects.Clear();
    }

    /// <summary>
    /// Returns an array of 'count' unique InstanceTypes, excluding Hub and disabled types.
    /// </summary>
    private InstanceZone.InstanceType[] GetRandomUniqueTypes(int count)
    {
        List<InstanceZone.InstanceType> pool = new List<InstanceZone.InstanceType>
        {
            InstanceZone.InstanceType.Combat,
            // InstanceZone.InstanceType.Shop, // Disabled until implemented
            InstanceZone.InstanceType.Respite,
            InstanceZone.InstanceType.Challenge
        };

        // Safety check: Don't try to take more than we have
        int actualCount = Mathf.Min(count, pool.Count);

        // Shuffle the pool
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            InstanceZone.InstanceType temp = pool[i];
            pool[i] = pool[j];
            pool[j] = temp;
        }

        // Take the first 'actualCount' entries
        InstanceZone.InstanceType[] result = new InstanceZone.InstanceType[actualCount];
        for (int i = 0; i < actualCount; i++)
        {
            result[i] = pool[i];
        }

        return result;
    }
}
