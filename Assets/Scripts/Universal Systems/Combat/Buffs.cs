using UnityEngine;

public class Buffs : MonoBehaviour
{
    public float duration;
    public float damageMultiplier;
    public float speedMultiplier;
    public float armorMultiplier;
    public float critChanceMultiplier;
    public float critDamageMultiplier;
    public float lifestealChanceMultiplier;
    public float lifestealAmountMultiplier;
    public float armorPenetrationMultiplier;
    public float armorPenetrationAmountMultiplier;

    private int stacks = 0; // all buffs that are stackable will use this as a count for the number of stacks

    private AnimationController animationController;
    private EntityStats entityStats;

    // track what we've applied so we can revert before applying new stack values
    private float appliedMoveSpeed = 0f;
    private float appliedDamage = 0f;

    enum BuffType
    {
        // universal
        Health,
        Regeneration,
        MoveSpeed,
        DashSpeed,
        SlashAttack,
        PierceAttack,
        MagicAttack,
        AttackSpeed,
        Defense,
        ActionPoints,
        Strength,
        Luck,
        Fortitude,
        Agility,
        Imagination,
        CritRate,
        CritDamage,
        Cooldown,

        // elemental
        FireDamage,
        IceDamage,
        PoisonDamage,
        LightningDamage,

        // class specific
        Strife,         // saber class
        Vengeance,      // swordsman class
        Arcana,         // mage class
        PerfectDraw,    // archer class
        Stance,         // monk class (will not implement as of yet, current focuses are saber, swordsman, mage, and archer) 
        Life            // sage class (will not implement as of yet, current focuses are saber, swordsman, mage, and archer)  

        // boss specific
        
        // 
    }

    private bool strifeKillConfirmed = false;
    


    void Start()
    {
        animationController = GetComponentInChildren<AnimationController>();
        
        entityStats = GetComponent<EntityStats>();
        if (entityStats == null) entityStats = GetComponentInParent<EntityStats>();
        
        if (entityStats == null) Debug.LogError($"[Buffs] EntityStats NOT FOUND on {gameObject.name} or parents!");

        if (animationController != null)
        {
            animationController.OnAttackStart += HandleAttackStart;
            animationController.OnAttackEnd += HandleAttackEnd;
        }

        CombatSystem combat = GetComponentInParent<CombatSystem>();
        if (combat != null)
        {
            combat.OnTargetKilled += HandleTargetKilled;
        }
        else
        {
            Debug.LogWarning($"[Buffs] CombatSystem not found on {gameObject.name} or parents.");
        }
    }

    void OnDestroy()
    {
        if (animationController != null)
        {
            animationController.OnAttackStart -= HandleAttackStart;
            animationController.OnAttackEnd -= HandleAttackEnd;
        }

        CombatSystem combat = GetComponentInChildren<CombatSystem>();
        if (combat != null)
        {
            combat.OnTargetKilled -= HandleTargetKilled;
        }
    }

    private void HandleAttackStart(AnimationFrames attack)
    {
        switch (attack.linkedBuff)
        {
            case LinkedBuff.Strife:
                strifeKillConfirmed = false;
                break;
            case LinkedBuff.Vengeance:
                // TODO: Implement Vengeance start logic
                break;
            case LinkedBuff.Arcana:
                // TODO: Implement Arcana start logic
                break;
            case LinkedBuff.PerfectDraw:
                // TODO: Implement PerfectDraw start logic
                break;
        }
    }

    private void HandleTargetKilled(GameObject target)
    {
        if (animationController == null) return;
        
        bool isAttacking = animationController.IsAttacking;
        LinkedBuff currentBuff = animationController.CurrentAnimation.linkedBuff;
        
        // Debug.Log($"[Buffs] Target Killed. CurrentBuff: {currentBuff}");

        switch (currentBuff)
        {
            case LinkedBuff.Strife:
                if (isAttacking)
                {
                    strifeKillConfirmed = true;
                    AddStrifeStack();
                }
                break;
            case LinkedBuff.Vengeance:
                // TODO: Implement Vengeance kill logic
                break;
            case LinkedBuff.Arcana:
                // TODO: Implement Arcana kill logic
                break;
            case LinkedBuff.PerfectDraw:
                // TODO: Implement PerfectDraw kill logic
                break;
        }
    }

    private void HandleAttackEnd(AnimationFrames attack)
    {
        switch (attack.linkedBuff)
        {
            case LinkedBuff.Strife:
                if (!strifeKillConfirmed)
                    ResetStrife();
                break;
            case LinkedBuff.Vengeance:
                // TODO: Implement Vengeance end logic
                break;
            case LinkedBuff.Arcana:
                // TODO: Implement Arcana end logic
                break;
            case LinkedBuff.PerfectDraw:
                // TODO: Implement PerfectDraw end logic
                break;
        }
    }

    #region Strife Logic
    private void AddStrifeStack()
    {
        if (stacks < 4) stacks++;

        // Reset cooldown for any skill with Strife linked
        for(int i=0; i<animationController.skills.Length; i++)
        {
            if(animationController.skills[i].linkedBuff == LinkedBuff.Strife)
                animationController.CooldownTimer[i] = 0f;
        }

        ApplyStrifeStats();
    }

    private void ResetStrife()
    {
        RevertStrifeModifiers();
        stacks = 0;
    }

    /*
    Strife: A stackable buff that increases both movement speed, damage, and gap closer range. With every final blow using Gap Closer, stacks will be increased by 1x, max stacks are 4x.
    Movement Speed Modifier = 4% / 8% / 12% / 16%
    Pierce Attack Modifier = 8% / 16% / 24% / 32%
    Gap Closer Range Modifier = 4% / 8% / 12% / 16%
    */

    private void ApplyStrifeStats()
    {
        // Revert old before applying new
        RevertStrifeModifiers();

        if (entityStats == null) return;

        float moveSpeedBuff = 0f;
        float damageBuff = 0f;

        float currentMoveSpeed = entityStats.GetStatValue(StatType.MoveSpeed);
        float currentPierce = entityStats.GetStatValue(StatType.PierceAttack);

        switch (stacks)
        {
            case 1:
                moveSpeedBuff = currentMoveSpeed * 0.04f;  
                damageBuff = currentPierce * 0.08f;   
                break;
            case 2:
                moveSpeedBuff = currentMoveSpeed * 0.08f;   
                damageBuff = currentPierce * 0.16f;   
                break;
            case 3:
                moveSpeedBuff = currentMoveSpeed * 0.12f;  
                damageBuff = currentPierce * 0.24f;   
                break;
            case 4:
                moveSpeedBuff = currentMoveSpeed * 0.16f;   
                damageBuff = currentPierce * 0.32f;   
                break;
        }

        entityStats.AddModifier(StatType.MoveSpeed, moveSpeedBuff, 0f);
        entityStats.AddModifier(StatType.PierceAttack, damageBuff, 0f);

        appliedMoveSpeed = moveSpeedBuff;
        appliedDamage = damageBuff;
    }

    private void RevertStrifeModifiers()
    {
        if (appliedMoveSpeed != 0f)
        {
            entityStats.AddModifier(StatType.MoveSpeed, -appliedMoveSpeed, 0f);
            appliedMoveSpeed = 0f;
        }

        if (appliedDamage != 0f)
        {
            entityStats.AddModifier(StatType.PierceAttack, -appliedDamage, 0f);
            appliedDamage = 0f;
        }
    }
    #endregion

    #region Vengeance Logic
    
    #endregion

    #region Arcana Logic
    
    #endregion

    #region Perfect Draw Logic
    
    #endregion
}