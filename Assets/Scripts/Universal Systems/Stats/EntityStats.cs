using UnityEngine;
using System.Collections.Generic;

public class EntityStats : MonoBehaviour, IDamageable
{
    [Header("Scaling Settings")]
    [Tooltip("How much does 1 Attribute Point boost a stat? 0.01 = 1%")]
    [SerializeField] private float globalScalingFactor = 0.01f; 

    [Header("Universal Base Stats")]
    [SerializeField] private float baseMaxHealth = 100f;
    [SerializeField] private float baseMoveSpeed = 5f;
    [SerializeField] private float baseDashSpeed = 20f;
    [SerializeField] private float baseDefense = 5f; 
    [SerializeField] private float baseAttackSpeed = 20.0f;

    [Header("Damage Types (Base)")]
    [SerializeField] private float baseSlashAttack = 10f;
    [SerializeField] private float basePierceAttack = 10f;
    [SerializeField] private float baseMagicAttack = 10f;

    [Header("Player Attributes (Default 0 for Enemies)")]
    [SerializeField] private float baseActionPoints = 50f;
    [SerializeField] private float baseStrength = 0f;   // Scales Slash %
    [SerializeField] private float baseLuck = 0f;       // Scales Pierce %
    [SerializeField] private float baseFortitude = 0f;  // Scales Defense %
    [SerializeField] private float baseAgility = 0f;    // Scales Attack Speed %
    [SerializeField] private float baseImagination = 0f;// Scales Magic & AP %
    
    [Header("Crit Stats (Values in %, e.g. 5 = 5%)")]
    [SerializeField] private float baseCritRate = 5f; 
    [SerializeField] private float baseCritDamage = 50f; 

    private float currentHealth;
    private Dictionary<StatType, float> statModifiers = new Dictionary<StatType, float>();

    public float CurrentHealth => currentHealth;

    public event System.Action OnHealthChanged;
    public event System.Action<Vector3> OnHit; 
    public event System.Action OnDeath;

    void Awake()
    {
        InitializeModifiers();
        currentHealth = GetStatValue(StatType.MaxHealth);
    }

    private void InitializeModifiers()
    {
        foreach (StatType stat in System.Enum.GetValues(typeof(StatType)))
        {
            if (!statModifiers.ContainsKey(stat))
                statModifiers.Add(stat, 0f);
        }
    }

    public float GetStatValue(StatType type)
    {
        float strength = GetBaseWithModifier(StatType.Strength, baseStrength);
        float agility = GetBaseWithModifier(StatType.Agility, baseAgility);
        float fortitude = GetBaseWithModifier(StatType.Fortitude, baseFortitude);
        float luck = GetBaseWithModifier(StatType.Luck, baseLuck);
        float imagination = GetBaseWithModifier(StatType.Imagination, baseImagination);

        float finalValue = 0f;

        switch (type)
        {
            case StatType.MaxHealth:    
                finalValue = GetBaseWithModifier(type, baseMaxHealth); 
                break;
            case StatType.MoveSpeed:    
                finalValue = GetBaseWithModifier(type, baseMoveSpeed); 
                break;
            case StatType.DashSpeed:
                finalValue = GetBaseWithModifier(type, baseDashSpeed);
                break;


            case StatType.SlashAttack:  
                float slashMult = 1f + (strength * globalScalingFactor);
                finalValue = GetBaseWithModifier(type, baseSlashAttack) * slashMult; 
                break;
            
            case StatType.PierceAttack: 
                float pierceMult = 1f + (luck * globalScalingFactor);
                finalValue = GetBaseWithModifier(type, basePierceAttack) * pierceMult; 
                break;

            case StatType.MagicAttack:  
                float magicMult = 1f + (imagination * globalScalingFactor);
                finalValue = GetBaseWithModifier(type, baseMagicAttack) * magicMult; 
                break;

            case StatType.AttackSpeed:
                float speedMult = 1f + (agility * globalScalingFactor);
                finalValue = GetBaseWithModifier(type, baseAttackSpeed) * speedMult;
                break;

            case StatType.Defense:
                float defMult = 1f + (fortitude * globalScalingFactor);
                finalValue = GetBaseWithModifier(type, baseDefense) * defMult;
                break;

            case StatType.ActionPoints:
                float apMult = 1f + (imagination * globalScalingFactor);
                finalValue = GetBaseWithModifier(type, baseActionPoints) * apMult;
                break;

            case StatType.Strength:     finalValue = strength; break;
            case StatType.Agility:      finalValue = agility; break;
            case StatType.Fortitude:    finalValue = fortitude; break;
            case StatType.Luck:         finalValue = luck; break;
            case StatType.Imagination:  finalValue = imagination; break;
            case StatType.CritRate:     finalValue = GetBaseWithModifier(type, baseCritRate); break;
            case StatType.CritDamage:   finalValue = GetBaseWithModifier(type, baseCritDamage); break;
        }

        return finalValue;
    }

    private float GetBaseWithModifier(StatType type, float baseVal)
    {
        if (statModifiers.ContainsKey(type))
            return baseVal + statModifiers[type];
        return baseVal;
    }

    public float CalculateOutgoingDamage(StatType damageType)
    {
        float rawDamage = GetStatValue(damageType);
        float critRate = GetStatValue(StatType.CritRate);
        float critDmgBonus = GetStatValue(StatType.CritDamage);

        if (critRate > 100f)
        {
            float excess = critRate - 100f;
            critDmgBonus += excess; 
            critRate = 100f;
        }

        float roll = Random.Range(0f, 100f);
        if (roll <= critRate)
        {
            float bonusAmount = rawDamage * (critDmgBonus / 100f);
            return Mathf.Ceil(rawDamage + bonusAmount);
        }

        return rawDamage;
    }

    public bool TakeDamage(float damageAmount, Vector3 knockbackSource)
    {
        float defense = GetStatValue(StatType.Defense);
        float finalDamage = Mathf.Max(damageAmount - defense, 0f);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, GetStatValue(StatType.MaxHealth));

        Debug.Log($"[COMBAT] {gameObject.name} took {finalDamage} damage. Remaining HP: {currentHealth}/{GetStatValue(StatType.MaxHealth)}");
        OnHealthChanged?.Invoke();

        OnHit?.Invoke(knockbackSource); 

        if (currentHealth <= 0)
        {
            Debug.Log($"[COMBAT] {gameObject.name} DIED. Returning true.");
            Die();
            return true;
        }
        return false;
    }
    
    public void AddModifier(StatType type, float amount, float duration)
    {
        if (!statModifiers.ContainsKey(type)) statModifiers[type] = 0;
        statModifiers[type] += amount;
        if (duration > 0) StartCoroutine(RemoveModifierAfterTime(type, amount, duration));
    }

    private System.Collections.IEnumerator RemoveModifierAfterTime(StatType type, float amount, float duration)
    {
        yield return new WaitForSeconds(duration);
        statModifiers[type] -= amount;
    }

    private void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject); 
    }
}