using UnityEngine;

public enum LinkedBuff
{
    None,
    Strife,         // saber class
    Vengeance,      // swordsman class
    Arcana,         // mage class
    PerfectDraw     // archer class
}

[System.Serializable]
public struct AnimationFrames
{
    [Header("Visuals")]
    public Sprite[] sprites;
    public float framesPerSecond; 
    public bool loop; 
    
    [Header("Combat Configuration")]
    public bool isAttack; 
    public CombatSystem.AttackType shape; 
    public StatType damageStat;           
    
    [Header("Hitbox Settings")]
    [Range(0, 100f)] public float range;   
    [Range(0, 360f)] public float angleOrWidth; 
    public float knockback;

    [Header("Movement Logic")]
    public bool movesToHitbox; 

    [Header("Cooldown")]
    public float cooldown;

    [Header("Buff Link")]
    public LinkedBuff linkedBuff;
}

public class AnimationController : MonoBehaviour
{
    [Header("Base Animations")]
    public AnimationFrames idleClip;
    public AnimationFrames walkClip;
    
    [Header("Primary Combo")]
    public AnimationFrames[] attackClips; 
    public float comboResetTimer = 1.0f;

    [Header("Skill Slots")]
    public AnimationFrames[] skills; 

    [Header("Dependencies")]
    public PlayerMovement playerMovement;
    public EnemyMovement enemyMovement;
    public CombatSystem combatSystem; 
    public EntityStats myStats; 
    public KeybindManager keybinds;

    private SpriteRenderer spriteRenderer;
    private AnimationFrames currentAnimation;
    public AnimationFrames CurrentAnimation => currentAnimation;
    private int currentFrameIndex;
    private float timer;
    
    private bool isAttacking;
    private int comboIndex; 
    private float lastInputTime;
    private float[] cooldownTimer;
    public float[] CooldownTimer => cooldownTimer;

    public bool IsAttacking => isAttacking;
    public event System.Action<AnimationFrames> OnAttackStart;
    public event System.Action<AnimationFrames> OnAttackEnd;

    void Start()
    {
        // add Buffs component if missing, as it is required for Strife/etc logic
        if (GetComponent<Buffs>() == null)
        {
            Debug.Log($"[AnimationController] Auto-adding missing Buffs component to {gameObject.name}");
            gameObject.AddComponent<Buffs>();
        }

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerMovement == null) playerMovement = GetComponentInParent<PlayerMovement>();
        if (enemyMovement == null) enemyMovement = GetComponentInParent<EnemyMovement>();
        if (combatSystem == null) combatSystem = GetComponentInParent<CombatSystem>();
        if (myStats == null) myStats = GetComponentInParent<EntityStats>();

        // find keybinds (only relevant for player-controlled entities)
        if (keybinds == null) keybinds = GetComponentInParent<KeybindManager>();
            
        idleClip.loop = true;
        walkClip.loop = true;
        SetAnimation(idleClip);

        cooldownTimer = new float[skills.Length];
    }

    void Update()
    {
        for (int i = 0; i < cooldownTimer.Length; i++)
        {
            if (cooldownTimer[i] > 0)
                cooldownTimer[i] -= Time.deltaTime;
        }

        if (isAttacking)
        {
            PlayOneShotAnimation();
            return; 
        }

        if (playerMovement != null && keybinds != null)
        {
            if (Input.GetKeyDown(keybinds.basicAttack)) 
            {
                TriggerComboAttack();
                return;
            }

            // we look at how many keys are defined in the manager
            for (int i = 0; i < keybinds.skillKeys.Length; i++)
            {
                if (Input.GetKeyDown(keybinds.skillKeys[i]))
                {
                    if (i < skills.Length)
                    {
                        TriggerSkill(i);
                        return;
                    }
                }
            }
        }

        bool isMoving = false;
        if (playerMovement != null)
            isMoving = playerMovement.isMoving;
        else if (enemyMovement != null)
            isMoving = enemyMovement.isMoving;

        if (isMoving && currentAnimation.sprites != walkClip.sprites)
            SetAnimation(walkClip);
        else if (!isMoving && currentAnimation.sprites != idleClip.sprites)
            SetAnimation(idleClip);

        PlayLoopingAnimation();
    }
    
    public void TriggerComboAttack()
    {
        if (attackClips.Length == 0) return;
        if (Time.time - lastInputTime > comboResetTimer) comboIndex = 0;
        lastInputTime = Time.time;
        ExecuteCombatMove(attackClips[comboIndex]);
        comboIndex = (comboIndex + 1) % attackClips.Length;
    }

    public void TriggerSkill(int index)
    {
        if (skills == null || index < 0 || index >= skills.Length) return;
        if (cooldownTimer[index] > 0f) return; // if the skill is on cooldown, return

        cooldownTimer[index] = skills[index].cooldown; // start cooldown
        Debug.Log($"Skill {index} Cooldown Started: {skills[index].cooldown}s");

        ExecuteCombatMove(skills[index]);
    }

    private void ExecuteCombatMove(AnimationFrames moveData)
    {
        isAttacking = true;
        SetAnimation(moveData);
        OnAttackStart?.Invoke(moveData);

        if (combatSystem != null && moveData.isAttack)
        {
            if (playerMovement != null && moveData.movesToHitbox)
            {
                float distance = moveData.range;
                float speed = 20f; 
                if (myStats != null) speed = myStats.GetStatValue(StatType.DashSpeed);
                if (speed <= 0.1f) speed = 20f; 

                float duration = distance / speed;

                Vector3 lungeDir = combatSystem.attackPoint.forward;
                lungeDir.y = 0; 
                lungeDir.Normalize();

                playerMovement.ApplyLunge(lungeDir, speed, duration);
            }

            if (moveData.shape == CombatSystem.AttackType.Cone)
                combatSystem.PerformConeAttack(moveData.range, moveData.angleOrWidth, moveData.knockback, moveData.damageStat);
            else if (moveData.shape == CombatSystem.AttackType.Line)
                combatSystem.PerformLineAttack(moveData.range, moveData.angleOrWidth, moveData.knockback, moveData.damageStat);
            else if (moveData.shape == CombatSystem.AttackType.Radial)
                combatSystem.PerformRadialAttack(transform.position, moveData.range, moveData.knockback, moveData.damageStat);
        }
    }

    private void PlayLoopingAnimation()
    {
        if (currentAnimation.sprites == null || currentAnimation.sprites.Length == 0) return;

        float finalFPS = currentAnimation.framesPerSecond;
        if (currentAnimation.sprites == walkClip.sprites && myStats != null)
        {
            float speedStat = myStats.GetStatValue(StatType.MoveSpeed);
            if (speedStat > 0.1f) finalFPS = speedStat;
        }
        if (finalFPS <= 0.1f) finalFPS = 1f;

        float timePerFrame = 1f / finalFPS;
        timer += Time.deltaTime;
        if (timer >= timePerFrame)
        {
            timer -= timePerFrame;
            currentFrameIndex = (currentFrameIndex + 1) % currentAnimation.sprites.Length;
            spriteRenderer.sprite = currentAnimation.sprites[currentFrameIndex];
        }
    }

    private void PlayOneShotAnimation()
    {
        if (currentAnimation.sprites == null || currentAnimation.sprites.Length == 0) return;

        float finalFPS = currentAnimation.framesPerSecond;
        if (isAttacking && myStats != null)
        {
            float attackSpeedStat = myStats.GetStatValue(StatType.AttackSpeed);
            if (attackSpeedStat > 0.1f) finalFPS = attackSpeedStat;
        }
        if (finalFPS <= 0.1f) finalFPS = 1f;

        float timePerFrame = 1f / finalFPS;
        timer += Time.deltaTime;
        if (timer >= timePerFrame)
        {
            timer -= timePerFrame;
            if (currentFrameIndex >= currentAnimation.sprites.Length - 1)
            {
                OnAttackEnd?.Invoke(currentAnimation);
                isAttacking = false; 
                SetAnimation(idleClip);
            }
            else
            {
                currentFrameIndex++;
                spriteRenderer.sprite = currentAnimation.sprites[currentFrameIndex];
            }
        }
    }

    private void SetAnimation(AnimationFrames newClip)
    {
        if (!isAttacking && currentAnimation.sprites == newClip.sprites) return;
        currentAnimation = newClip;
        currentFrameIndex = 0;
        timer = 0f;
        if (currentAnimation.sprites != null && currentAnimation.sprites.Length > 0)
            spriteRenderer.sprite = currentAnimation.sprites[0];
    }
}