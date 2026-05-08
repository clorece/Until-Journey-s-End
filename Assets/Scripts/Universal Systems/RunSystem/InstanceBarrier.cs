using UnityEngine;

/// <summary>
/// Simple invisible wall placed at the edge of an InstanceZone during combat.
/// Contains a non-trigger BoxCollider to physically block the player and enemies.
/// Instantiated by RunManager and destroyed when the instance ends.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class InstanceBarrier : MonoBehaviour
{
    void Awake()
    {
        // Ensure the collider is NOT a trigger — we want physical blocking
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = false;
    }

    /// <summary>
    /// Creates 4 barrier walls around the given zone bounds.
    /// Returns the parent GameObject containing all 4 walls.
    /// </summary>
    public static GameObject CreateBarriersForZone(InstanceZone zone)
    {
        Bounds bounds = zone.GetBounds();
        float wallThickness = 1f;
        float wallHeight = zone.size.y;

        GameObject parent = new GameObject($"Barriers_{zone.gameObject.name}");
        parent.transform.position = bounds.center;

        // North wall (positive Z)
        CreateWall(parent.transform, "North",
            new Vector3(bounds.center.x, bounds.center.y, bounds.max.z + wallThickness / 2f),
            new Vector3(bounds.size.x + wallThickness * 2f, wallHeight, wallThickness));

        // South wall (negative Z)
        CreateWall(parent.transform, "South",
            new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - wallThickness / 2f),
            new Vector3(bounds.size.x + wallThickness * 2f, wallHeight, wallThickness));

        // East wall (positive X)
        CreateWall(parent.transform, "East",
            new Vector3(bounds.max.x + wallThickness / 2f, bounds.center.y, bounds.center.z),
            new Vector3(wallThickness, wallHeight, bounds.size.z + wallThickness * 2f));

        // West wall (negative X)
        CreateWall(parent.transform, "West",
            new Vector3(bounds.min.x - wallThickness / 2f, bounds.center.y, bounds.center.z),
            new Vector3(wallThickness, wallHeight, bounds.size.z + wallThickness * 2f));

        return parent;
    }

    private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject($"Barrier_{name}");
        wall.transform.parent = parent;
        wall.transform.position = position;

        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = size;
        col.isTrigger = false;

        wall.AddComponent<InstanceBarrier>();
    }
}
