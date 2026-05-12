using UnityEngine;

/// <summary>
/// Base class for objects the player can interact with by pressing the interact key (F).
/// Subclasses implement OnInteract() for specific behavior.
/// Automatically ensures a collider exists for physical collision.
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("How close the player must be to interact.")]
    public float interactRange = 2.0f;

    [Tooltip("Text shown when the player is in range (for future UI prompt).")]
    public string promptText = "Press F to Interact";

    [Header("Collision")]
    [Tooltip("Size of the auto-generated BoxCollider if none exists on the object.")]
    public Vector3 colliderSize = new Vector3(1f, 1f, 1f);

    protected Transform player;
    protected KeybindManager keybinds;
    protected bool hasInteracted = false;

    /// <summary>
    /// Whether the player is currently within interaction range.
    /// </summary>
    public bool IsPlayerInRange { get; private set; }

    protected virtual void Start()
    {
        FindPlayer();
        EnsureCollider();

        // Environmental sprites (Portals, Chests, etc.) should be sharp.
        // If they are on a Default layer, we move their visuals to the Entities layer for TAA exclusion.
        int sharpLayer = gameObject.layer;
        if (((1 << sharpLayer) & PostProcess.ExclusionLayers) == 0)
        {
            sharpLayer = LayerMask.NameToLayer("Entities");
        }

        if (sharpLayer != -1)
        {
            PostProcess.SetLayerRecursively(gameObject, sharpLayer);
        }
    }

    /// <summary>
    /// Locates the player and keybinds. Can be called again if Start() fires too early.
    /// </summary>
    private void FindPlayer()
    {
        // Pull the player reference from RunManager since it already has it
        if (RunManager.Instance != null && RunManager.Instance.player != null)
        {
            player = RunManager.Instance.player;
            keybinds = player.GetComponentInChildren<KeybindManager>();
            if (keybinds == null)
                keybinds = player.GetComponent<KeybindManager>();
        }

        if (player == null)
            Debug.LogWarning($"[Interactable] {gameObject.name}: Could not find Player — RunManager may not be ready yet.");
        if (keybinds == null && player != null)
            Debug.LogWarning($"[Interactable] {gameObject.name}: Player found but KeybindManager is missing!");
    }

    /// <summary>
    /// Adds a BoxCollider if no Collider exists on this GameObject.
    /// </summary>
    private void EnsureCollider()
    {
        if (GetComponent<Collider>() == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size = colliderSize;
            box.center = new Vector3(0f, colliderSize.y / 2f, 0f);
        }
    }

    protected virtual void Update()
    {
        // Retry finding player if Start() fired before the player existed
        if (player == null || keybinds == null)
        {
            FindPlayer();
            return;
        }

        if (hasInteracted) return;

        // Check distance
        Vector3 diff = player.position - transform.position;
        diff.y = 0f;
        float distance = diff.magnitude;

        IsPlayerInRange = distance <= interactRange;

        if (IsPlayerInRange && Input.GetKeyDown(keybinds.interact))
        {
            OnInteract();
        }
    }

    /// <summary>
    /// Called when the player presses the interact key while in range.
    /// Implement specific behavior in subclasses.
    /// </summary>
    protected abstract void OnInteract();

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
