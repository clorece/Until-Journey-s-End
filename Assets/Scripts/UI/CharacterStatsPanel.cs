using UnityEngine;
using TMPro;

/// <summary>
/// Displays a vertical list of all player stats while Tab is held.
/// The UIManager handles showing/hiding this panel.
/// 
/// SETUP:
/// 1. Create a UI panel anchored to Middle-Left (or Left above HUD).
/// 2. Use RectangleBox_96x96.png as the background (9-sliced, stretch).
/// 3. Add TMP_Text children for each stat row — OR use a single TMP_Text 
///    and this script will format all stats into it.
/// 4. Attach this script to the panel root.
/// 5. Drag the player's EntityStats and the text component into the Inspector.
/// 6. Drag this panel's root GameObject into UIManager's "characterStatsPanel" slot.
/// </summary>
public class CharacterStatsPanel : MonoBehaviour
{
    [Header("Player Reference")]
    [Tooltip("The player's EntityStats component.")]
    public EntityStats playerStats;

    [Header("Display")]
    [Tooltip("A single TMP_Text that will display all stats as a formatted list.")]
    public TMP_Text statsText;

    void OnEnable()
    {
        if (playerStats == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerStats = player.GetComponent<EntityStats>();
        }

        if (playerStats != null)
        {
            playerStats.OnStatsChanged += RefreshStats;
        }

        // Refresh stats every time the panel is shown
        RefreshStats();
    }

    void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnStatsChanged -= RefreshStats;
        }
    }

    /// <summary>
    /// Rebuilds the stats display from current EntityStats values.
    /// </summary>
    public void RefreshStats()
    {

        if (playerStats == null || statsText == null) return;

        float maxHP = playerStats.GetStatValue(StatType.MaxHealth);
        float currentHP = playerStats.CurrentHealth;
        float moveSpeed = playerStats.GetStatValue(StatType.MoveSpeed);
        float defense = playerStats.GetStatValue(StatType.Defense);
        float atkSpeed = playerStats.GetStatValue(StatType.AttackSpeed);

        float slash = playerStats.GetStatValue(StatType.SlashAttack);
        float pierce = playerStats.GetStatValue(StatType.PierceAttack);
        float magic = playerStats.GetStatValue(StatType.MagicAttack);

        float str = playerStats.GetStatValue(StatType.Strength);
        float agi = playerStats.GetStatValue(StatType.Agility);
        float fort = playerStats.GetStatValue(StatType.Fortitude);
        float luck = playerStats.GetStatValue(StatType.Luck);
        float imag = playerStats.GetStatValue(StatType.Imagination);

        float critRate = playerStats.GetStatValue(StatType.CritRate);
        float critDmg = playerStats.GetStatValue(StatType.CritDamage);
        float ap = playerStats.GetStatValue(StatType.ActionPoints);

        statsText.text =
            $"<b>--- Vitals ---</b>\n" +
            $"HP: {Mathf.Ceil(currentHP)} / {Mathf.Ceil(maxHP)}\n" +
            $"AP: {Mathf.Ceil(ap)}\n" +
            $"Defense: {defense:F1}\n" +
            $"Move Speed: {moveSpeed:F1}\n" +
            $"\n" +
            $"<b>--- Attributes ---</b>\n" +
            $"Strength: {str:F0}\n" +
            $"Agility: {agi:F0}\n" +
            $"Fortitude: {fort:F0}\n" +
            $"Luck: {luck:F0}\n" +
            $"Imagination: {imag:F0}\n" +
            $"\n" +
            $"<b>--- Damage ---</b>\n" +
            $"Slash: {slash:F1}\n" +
            $"Pierce: {pierce:F1}\n" +
            $"Magic: {magic:F1}\n" +
            $"Atk Speed: {atkSpeed:F1}\n" +
            $"\n" +
            $"<b>--- Critical ---</b>\n" +
            $"Crit Rate: {critRate:F1}%\n" +
            $"Crit Damage: {critDmg:F1}%";
    }
}
