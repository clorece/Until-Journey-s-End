using UnityEngine;
using TMPro;

/// <summary>
/// World-space health bar that floats above entities.
/// Replaces the old text-only HealthDisplay with a sprite-based fill bar.
/// 
/// SETUP (World-Space Canvas Prefab):
/// 1. Create a Canvas set to "World Space".
///    - Set its width/height small (e.g., 2x0.3 units).
///    - Layer: UI or Default (make sure the camera renders it).
/// 2. Add child structure:
///    [Canvas]
///      └─ HealthBar (this script)
///           ├─ Background (Image — use AttributesBar_41x9 or HealthBarPanel)
///           ├─ Fill (Image — use ValueRed_120x8, Image Type = Filled, Horizontal)
///           ├─ Text (TMP_Text, optional — "100/100")
///           └─ StatusEffectContainer (empty Transform for buff/debuff prefab icons)
/// 3. Make this a prefab and attach it as a child of each entity that has EntityStats.
/// 4. Drag references into the Inspector slots.
/// </summary>
public class HealthDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The fill Image for the health bar. Image Type = Filled, Fill Method = Horizontal.")]
    public UnityEngine.UI.Image healthFill;

    [Tooltip("Optional text showing 'current / max'.")]
    public TMP_Text textComponent;

    [Header("Status Effects")]
    [Tooltip("Transform container where buff/debuff prefab icons will be instantiated.")]
    public Transform statusEffectContainer;

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2.5f, 0);
    public bool alwaysFaceCamera = true;

    [Tooltip("If true, hides the bar when entity is at full health.")]
    public bool hideAtFullHealth = false;

    private EntityStats myStats;
    private Camera mainCam;
    private CanvasGroup canvasGroup;

    void Start()
    {
        myStats = GetComponentInParent<EntityStats>();
        mainCam = Camera.main;

        if (myStats == null)
        {
            myStats = GetComponent<EntityStats>();
        }

        if (myStats == null)
        {
            Debug.LogError("[HealthDisplay] Could not find EntityStats!");
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>();

        // Subscribe to events
        myStats.OnHealthChanged += UpdateBar;
        myStats.OnStatsChanged += UpdateBar;
 
        // Ensure the entire world-space Canvas and all its children match the parent entity's layer 
        // This allows the PostProcess system to find and render them without TAA blur.
        Canvas canvas = GetComponentInParent<Canvas>();
        GameObject rootToSet = (canvas != null) ? canvas.gameObject : gameObject;
        PostProcess.SetLayerRecursively(rootToSet, myStats.gameObject.layer);

        UpdateBar();
    }

    void OnDestroy()
    {
        if (myStats != null)
        {
            myStats.OnHealthChanged -= UpdateBar;
            myStats.OnStatsChanged -= UpdateBar;
        }
    }

    // Changed to LateUpdate to prevent jittering when the camera moves
    void LateUpdate()
    {
        if (myStats != null)
        {
            // 1. Follow the target
            transform.position = myStats.transform.position + offset;

            // 2. Billboard Effect
            if (alwaysFaceCamera && mainCam != null)
            {
                // This aligns the bar perfectly with the camera's angle
                transform.rotation = mainCam.transform.rotation;
            }
        }
    }

    void UpdateBar()
    {
        if (myStats == null) return;

        float current = myStats.CurrentHealth;
        float max = myStats.GetStatValue(StatType.MaxHealth);
        float percent = (max > 0) ? current / max : 0f;

        // Update fill bar
        if (healthFill != null)
            healthFill.fillAmount = percent;

        // Update optional text
        if (textComponent != null)
            textComponent.text = $"{Mathf.Ceil(current)} / {Mathf.Ceil(max)}";

        // Hide at full health if desired
        if (hideAtFullHealth && canvasGroup != null)
        {
            canvasGroup.alpha = (percent >= 1f) ? 0f : 1f;
        }

        // Color the fill bar based on health percentage
        if (healthFill != null)
        {
            if (percent < 0.3f)
                healthFill.color = new Color(0.9f, 0.2f, 0.2f); // Red
            else
                healthFill.color = Color.white; // Use the sprite's native color
        }
    }

    // ─────────────────────────────────────────────
    //  Status Effect API (for buff/debuff prefabs)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Adds a status effect icon to the container above the health bar.
    /// Returns the instantiated GameObject so you can destroy it when the effect expires.
    /// </summary>
    public GameObject AddStatusEffect(GameObject iconPrefab)
    {
        if (statusEffectContainer == null || iconPrefab == null) return null;

        GameObject icon = Instantiate(iconPrefab, statusEffectContainer);
        return icon;
    }

    /// <summary>
    /// Removes a specific status effect icon from the container.
    /// </summary>
    public void RemoveStatusEffect(GameObject iconInstance)
    {
        if (iconInstance != null)
            Destroy(iconInstance);
    }

    /// <summary>
    /// Clears all status effect icons.
    /// </summary>
    public void ClearAllStatusEffects()
    {
        if (statusEffectContainer == null) return;

        foreach (Transform child in statusEffectContainer)
        {
            Destroy(child.gameObject);
        }
    }
}