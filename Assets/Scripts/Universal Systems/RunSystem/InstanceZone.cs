using UnityEngine;

/// <summary>
/// Defines a rectangular arena boundary within the Overworld.
/// Place this on an empty GameObject positioned at the center of the desired zone.
/// Set 'size' in the Inspector to define the XZ extents.
/// 
/// Positions are hardcoded relative to the zone center:
/// - Player always spawns at the center
/// - Chest spawns at the center
/// - 3 portals spawn a few meters north (positive Z) of center, spread out on X
/// </summary>
public class InstanceZone : MonoBehaviour
{
    public enum InstanceType { Hub, Combat, Shop, Respite, Challenge }

    [Header("Zone Configuration")]
    public InstanceType zoneType = InstanceType.Combat;

    [Tooltip("Full XZ dimensions of the arena. Y is the wall height (used for barriers).")]
    public Vector3 size = new Vector3(20f, 5f, 20f);

    [Header("Spawn Configuration")]
    [Tooltip("Optional explicit spawn positions for enemies. If empty, random positions within bounds are used.")]
    public Transform[] spawnPoints;

    [Header("Portal Layout")]
    [Tooltip("How far north (positive Z) of center the portals spawn.")]
    public float portalOffsetZ = 4f;

    [Tooltip("Horizontal spacing between each portal on the X axis.")]
    public float portalSpacing = 3f;

    [Header("Combat Settings")]
    [Tooltip("Enemy prefabs to spawn in this zone. Only used for Combat zones.")]
    public GameObject[] enemyPrefabs;

    [Tooltip("Total number of enemies to spawn in this instance.")]
    public int totalEnemies = 10;

    [Tooltip("Maximum number of enemies alive at the same time.")]
    public int maxActiveEnemies = 3;

    [Tooltip("Seconds between spawn waves.")]
    public float spawnInterval = 2f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;

    [Tooltip("How much to lift portals above the ground.")]
    public float portalVerticalOffset = 1.0f;

    [Tooltip("How much to lift the chest above the ground.")]
    public float chestVerticalOffset = 0.5f;

    [Tooltip("How much to lift the campfire above the ground.")]
    public float campfireVerticalOffset = 0.5f;

    /// <summary>
    /// Returns an axis-aligned bounding box for this zone.
    /// </summary>
    public Bounds GetBounds()
    {
        return new Bounds(transform.position, size);
    }

    /// <summary>
    /// Returns a random XZ position within the zone bounds, with Y set via ground raycast.
    /// </summary>
    public Vector3 GetRandomSpawnPosition()
    {
        Bounds bounds = GetBounds();

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);
        float y = transform.position.y;

        // Raycast down to find ground height
        Vector3 rayOrigin = new Vector3(x, bounds.max.y + 5f, z);
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, size.y + 20f, groundLayer))
        {
            y = hit.point.y;
        }

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Checks if a position is within this zone's XZ boundaries.
    /// </summary>
    public bool IsInsideBounds(Vector3 pos)
    {
        Bounds bounds = GetBounds();
        return pos.x >= bounds.min.x && pos.x <= bounds.max.x &&
               pos.z >= bounds.min.z && pos.z <= bounds.max.z;
    }

    /// <summary>
    /// Clamps a position to stay within this zone's XZ boundaries.
    /// Y is left unchanged.
    /// </summary>
    public Vector3 ClampToBounds(Vector3 pos)
    {
        Bounds bounds = GetBounds();
        pos.x = Mathf.Clamp(pos.x, bounds.min.x, bounds.max.x);
        pos.z = Mathf.Clamp(pos.z, bounds.min.z, bounds.max.z);
        return pos;
    }

    /// <summary>
    /// Player always spawns at the zone center.
    /// </summary>
    public Vector3 GetPlayerSpawnPosition()
    {
        return GetGroundPosition(transform.position);
    }

    /// <summary>
    /// Chest always spawns at the zone center.
    /// </summary>
    public Vector3 GetChestPosition()
    {
        Vector3 pos = GetGroundPosition(transform.position);
        pos.y += chestVerticalOffset;
        return pos;
    }

    /// <summary>
    /// Campfire spawns at the zone center with its own vertical offset.
    /// </summary>
    public Vector3 GetCampfirePosition()
    {
        Vector3 pos = GetGroundPosition(transform.position);
        pos.y += campfireVerticalOffset;
        return pos;
    }

    /// <summary>
    /// Returns 3 portal positions north (positive Z) of center, evenly spaced on X.
    /// Layout: [-spacing, 0, +spacing] on X, all at portalOffsetZ north of center.
    /// </summary>
    public Vector3[] GetPortalPositions()
    {
        Vector3 center = transform.position;
        Vector3[] positions = new Vector3[3];

        positions[0] = GetGroundPosition(new Vector3(center.x - portalSpacing, center.y, center.z + portalOffsetZ));
        positions[1] = GetGroundPosition(new Vector3(center.x,                 center.y, center.z + portalOffsetZ));
        positions[2] = GetGroundPosition(new Vector3(center.x + portalSpacing, center.y, center.z + portalOffsetZ));

        for (int i = 0; i < positions.Length; i++)
            positions[i].y += portalVerticalOffset;

        return positions;
    }

    /// <summary>
    /// Raycasts to find the ground Y at a given XZ position.
    /// Falls back to the input Y if no ground is found.
    /// </summary>
    private Vector3 GetGroundPosition(Vector3 pos)
    {
        Vector3 rayOrigin = new Vector3(pos.x, pos.y + 10f, pos.z);
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 30f, groundLayer))
        {
            pos.y = hit.point.y;
        }
        return pos;
    }

    void OnDrawGizmos()
    {
        // Draw zone boundary
        switch (zoneType)
        {
            case InstanceType.Hub:
                Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f); // Green
                break;
            case InstanceType.Combat:
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.15f); // Red
                break;
            case InstanceType.Shop:
                Gizmos.color = new Color(1f, 0.85f, 0f, 0.15f); // Gold
                break;
            case InstanceType.Respite:
                Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.15f); // Blue
                break;
            case InstanceType.Challenge:
                Gizmos.color = new Color(0.8f, 0.2f, 1f, 0.15f); // Purple
                break;
        }

        Gizmos.DrawCube(transform.position, size);

        // Draw wireframe on top
        Color wireColor = Gizmos.color;
        wireColor.a = 0.6f;
        Gizmos.color = wireColor;
        Gizmos.DrawWireCube(transform.position, size);

        // Draw enemy spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform sp in spawnPoints)
            {
                if (sp != null)
                    Gizmos.DrawWireSphere(sp.position, 0.5f);
            }
        }

        // Draw player/chest spawn (center)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.7f);

        // Draw portal positions
        Gizmos.color = new Color(0.5f, 0f, 1f, 0.8f);
        Vector3 center = transform.position;
        Gizmos.DrawWireSphere(new Vector3(center.x - portalSpacing, center.y, center.z + portalOffsetZ), 0.4f);
        Gizmos.DrawWireSphere(new Vector3(center.x,                 center.y, center.z + portalOffsetZ), 0.4f);
        Gizmos.DrawWireSphere(new Vector3(center.x + portalSpacing, center.y, center.z + portalOffsetZ), 0.4f);
    }
}
