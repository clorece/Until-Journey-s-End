using UnityEngine;
using System.Collections.Generic;

public class EntityStats : MonoBehaviour, IDamageable
{
    [Header("Death Settings")]
    public bool destroyOnDeath = true;
    public float deathDelay = 0f;
    
    private bool isDead = false;
    public bool IsDead => isDead;

    [Header("Knockback Settings")]
    public bool immuneToKnockback = false;
    public bool IsKnockedBack { get; private set; }

    [Header("Debug")]
    public bool isInvincible = false;

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
        if (isDead) return false;

        float defense = GetStatValue(StatType.Defense);
        float finalDamage = Mathf.Max(damageAmount - defense, 0f);

        if (!isInvincible)
        {
            currentHealth -= finalDamage;
            currentHealth = Mathf.Clamp(currentHealth, 0, GetStatValue(StatType.MaxHealth));
        }

        Debug.Log($"[Combat] {gameObject.name} took {finalDamage} dmg. HP: {currentHealth}/{GetStatValue(StatType.MaxHealth)}");
        OnHealthChanged?.Invoke();

        if (!immuneToKnockback && knockbackSource.magnitude > 0.01f)
        {
            ApplyKnockback(knockbackSource);
        }

        OnHit?.Invoke(knockbackSource); 

        if (currentHealth <= 0)
        {
            Debug.Log($"[Combat] {gameObject.name} died.");
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

    private void ApplyKnockback(Vector3 knockbackVector)
    {
        if (isDead) return;
        StartCoroutine(KnockbackRoutine(knockbackVector));
    }

    private System.Collections.IEnumerator KnockbackRoutine(Vector3 knockbackVector)
    {
        IsKnockedBack = true;
        
        float duration = 0.35f; 
        float elapsed = 0f;
        
        Vector3 startPosition = transform.position;
        Vector3 horizontalDisplacement = new Vector3(knockbackVector.x, 0, knockbackVector.z);
        Vector3 targetPosition = startPosition + horizontalDisplacement;
        
        // Base hop height derived from strength, plus explicit upward force
        float peakHeight = (horizontalDisplacement.magnitude * 0.15f) + Mathf.Max(0, knockbackVector.y * 0.5f);

        while (elapsed < duration)
        {
            if (isDead) break;
            
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // Linear horizontal interpolation
            Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, t);
            
            // Quadratic rise and fall over duration: 4 * h * t * (1 - t)
            float verticalOffset = 4f * peakHeight * t * (1f - t);
            currentPos.y += verticalOffset;
            
            transform.position = currentPos;
            
            yield return null;
        }

        IsKnockedBack = false;
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke();
        
        if (destroyOnDeath)
        {
            if (deathDelay > 0f)
                Destroy(gameObject, deathDelay);
            else
                Destroy(gameObject); 
        }
    }
}