using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the bottom-left HUD: Health bar, AP bar, and Level indicator.
/// 
/// SETUP:
/// 1. Create a Canvas (Screen Space - Overlay).
/// 2. Add an empty "HUD" GameObject anchored to Bottom-Left.
/// 3. Add child Images for the bar backgrounds (HealthBarPanel, AttributesBar).
/// 4. Add child Images for the fill bars (ValueRed, ValueBlue) — set Image Type to "Filled", Fill Method to "Horizontal".
/// 5. Add a child Image for the portrait frame (CharacterBox) and level circle (BlackSmallCircleBox).
/// 6. Add TMP_Text inside the level circle for the level number.
/// 7. Attach this script to the HUD root and drag references into the Inspector.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("The player's EntityStats component.")]
    public EntityStats playerStats;

    [Header("Health Bar")]
    [Tooltip("The fill Image for the health bar. Set Image Type to 'Filled', Fill Method 'Horizontal'.")]
    public Image healthFill;

    [Tooltip("Optional text displaying 'current / max' HP.")]
    public TMP_Text healthText;

    [Header("Action Points Bar")]
    [Tooltip("The fill Image for the AP bar. Set Image Type to 'Filled', Fill Method 'Horizontal'.")]
    public Image apFill;

    [Tooltip("Optional text displaying 'current / max' AP.")]
    public TMP_Text apText;

    /*
    [Header("Level Display")]
    [Tooltip("Text component inside the level circle.")]
    public TMP_Text levelText;
    */

    // Cached values to avoid unnecessary updates
    private float lastHealthPercent = -1f;
    private float lastAPPercent = -1f;

    void Start()
    {
        if (playerStats == null)
        {
            // Try to find the player by tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerStats = player.GetComponent<EntityStats>();
        }

        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealthBar;
            playerStats.OnStatsChanged += RefreshAllBars;
            
            RefreshAllBars();
            // UpdateLevel(1); // Default level
        }
        else
        {
            Debug.LogWarning("[HUDController] No player EntityStats found!");
        }
    }

    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealthBar;
            playerStats.OnStatsChanged -= RefreshAllBars;
        }
    }

    private void RefreshAllBars()
    {
        UpdateHealthBar();
        UpdateAPBar();
    }

    void Update()
    {
        // AP might change every frame (e.g., regeneration), so poll it
        if (playerStats != null)
            UpdateAPBar();
    }

    /// <summary>
    /// Updates the health bar fill and optional text.
    /// </summary>
    public void UpdateHealthBar()
    {
        if (playerStats == null) return;

        float current = playerStats.CurrentHealth;
        float max = playerStats.GetStatValue(StatType.MaxHealth);
        float percent = (max > 0) ? current / max : 0f;
        
        if (healthFill != null)
            healthFill.fillAmount = percent;
        
        lastHealthPercent = percent;

        if (healthText != null)
            healthText.text = $"{Mathf.Ceil(current)} / {Mathf.Ceil(max)}";
    }

    /// <summary>
    /// Updates the AP bar fill and optional text.
    /// </summary>
    public void UpdateAPBar()
    {
        if (playerStats == null) return;

        float max = playerStats.GetStatValue(StatType.ActionPoints);
        // Note: EntityStats doesn't track current AP yet — 
        // for now we show the max as full. You can add current AP tracking later.
        float current = max;
        float percent = (max > 0) ? current / max : 0f;

        if (Mathf.Approximately(percent, lastAPPercent)) return;
        lastAPPercent = percent;

        if (apFill != null)
            apFill.fillAmount = percent;

        if (apText != null)
            apText.text = $"{Mathf.Ceil(current)} / {Mathf.Ceil(max)}";
    }

    /*
    /// <summary>
    /// Updates the level display. Call this from your leveling system.
    /// </summary>
    public void UpdateLevel(int level)
    {
        if (levelText != null)
            levelText.text = level.ToString();
    }
    */
}
