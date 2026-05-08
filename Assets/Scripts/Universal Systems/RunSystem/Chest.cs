using UnityEngine;
using System.Collections;

/// <summary>
/// Reward chest that spawns at the center of a zone after all enemies are cleared.
/// Player presses F to open it and receive random stat boosts.
/// Uses a 4-frame sprite animation for opening, which can be played in reverse for closing.
/// </summary>
public class Chest : Interactable
{
    [Header("Chest Animation")]
    [Tooltip("4 sprite frames for the open animation (closed -> open). Played in reverse for close.")]
    public Sprite[] animationFrames;

    [Tooltip("Playback speed for the open/close animation.")]
    public float framesPerSecond = 8f;

    [Header("Reward Settings")]
    [Tooltip("Number of random stat boosts granted when opened.")]
    public int statBoostCount = 3;

    [Tooltip("The flat amount each stat boost adds.")]
    public float boostAmount = 5f;

    [Header("Visual Tilt")]
    [Tooltip("Tilt rotation to align the sprite with the camera angle.")]
    public Vector3 visualRotation = new Vector3(30f, 0f, 0f);

    private SpriteRenderer spriteRenderer;
    private bool isOpen = false;
    public bool IsOpen => isOpen;
    private Coroutine animCoroutine;
    private InstanceZone.InstanceType sourceZoneType = InstanceZone.InstanceType.Combat; // Default to combat

    /// <summary>
    /// Sets the type of zone this chest was spawned in to determine reward scaling.
    /// </summary>
    public void SetSourceZoneType(InstanceZone.InstanceType type)
    {
        sourceZoneType = type;
    }

    /// <summary>
    /// Fired when the chest is opened. For future card system integration.
    /// </summary>
    public event System.Action OnChestOpened;

    protected override void Start()
    {
        base.Start();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Start on the first frame (closed)
        if (spriteRenderer != null && animationFrames != null && animationFrames.Length > 0)
            spriteRenderer.sprite = animationFrames[0];

        // Apply the visual tilt
        transform.rotation = Quaternion.Euler(visualRotation);
    }

    protected override void OnInteract()
    {
        // Grant random stat boosts to the player BEFORE setting hasInteracted
        // so we don't "burn" the interaction if stats aren't found.
        if (player != null)
        {
            // Robust search: check object, then children, then parents
            EntityStats playerStats = player.GetComponent<EntityStats>();
            if (playerStats == null) playerStats = player.GetComponentInChildren<EntityStats>();
            if (playerStats == null) playerStats = player.GetComponentInParent<EntityStats>();

            if (playerStats != null)
            {
                hasInteracted = true;
                PlayOpenAnimation();
                GrantRandomStatBoosts(playerStats);
                OnChestOpened?.Invoke();
                Debug.Log($"[Chest] {gameObject.name} successfully granted {statBoostCount} boosts to {player.name}.");
            }
            else
            {
                Debug.LogError($"[Chest] {gameObject.name} could not find EntityStats on {player.name} or its children!");
            }
        }
        else
        {
            Debug.LogError($"[Interactable] {gameObject.name} has no player reference!");
        }
    }

    /// <summary>
    /// Plays the opening animation (frames 0 -> last).
    /// </summary>
    public void PlayOpenAnimation()
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(PlayAnimation(false));
        isOpen = true;
    }

    /// <summary>
    /// Plays the closing animation (frames last -> 0).
    /// Call this externally when resetting the chest or transitioning zones.
    /// </summary>
    public void PlayCloseAnimation()
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        animCoroutine = StartCoroutine(PlayAnimation(true));
        isOpen = false;
    }

    /// <summary>
    /// Coroutine that steps through the animation frames.
    /// If 'reverse' is true, plays from the last frame back to the first.
    /// </summary>
    private IEnumerator PlayAnimation(bool reverse)
    {
        if (animationFrames == null || animationFrames.Length == 0 || spriteRenderer == null)
            yield break;

        float fps = framesPerSecond > 0.1f ? framesPerSecond : 1f;
        float timePerFrame = 1f / fps;

        int start = reverse ? animationFrames.Length - 1 : 0;
        int end   = reverse ? -1 : animationFrames.Length;
        int step  = reverse ? -1 : 1;

        for (int i = start; i != end; i += step)
        {
            spriteRenderer.sprite = animationFrames[i];
            yield return new WaitForSeconds(timePerFrame);
        }

        animCoroutine = null;
    }

    private void GrantRandomStatBoosts(EntityStats stats)
    {
        // Pool of stats that make sense to boost
        StatType[] allPossibleStats = new StatType[]
        {
            StatType.MaxHealth,
            StatType.MoveSpeed,
            StatType.SlashAttack,
            StatType.PierceAttack,
            StatType.MagicAttack,
            StatType.AttackSpeed,
            StatType.Defense,
            StatType.Strength,
            StatType.Luck,
            StatType.Fortitude,
            StatType.Agility,
            StatType.Imagination,
            StatType.CritRate,
            StatType.CritDamage
        };

        System.Collections.Generic.List<StatType> pool = new System.Collections.Generic.List<StatType>();

        // Smart allocation: filter out damage types the player doesn't use
        foreach (StatType stat in allPossibleStats)
        {
            float val = stats.GetStatValue(stat);
            if ((stat == StatType.SlashAttack || stat == StatType.PierceAttack || stat == StatType.MagicAttack) && val <= 0.1f)
            {
                continue; // Skip useless damage stats
            }
            pool.Add(stat);
        }

        // Pick 'statBoostCount' random unique stats
        System.Collections.Generic.List<string> rewardTexts = new System.Collections.Generic.List<string>();

        for (int i = 0; i < statBoostCount && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            StatType chosenStat = pool[index];
            pool.RemoveAt(index);

            // Calculate boost amount based on zone type
            float currentVal = stats.GetStatValue(chosenStat);
            
            // Default 8%, but 10% for Challenge zones
            float percentBoost = 0.08f;
            if (sourceZoneType == InstanceZone.InstanceType.Challenge)
            {
                percentBoost = 0.10f;
            }
            
            float calculatedBoost = currentVal * percentBoost;

            float boostToApply;
            if (currentVal >= 10f)
            {
                // For larger stats like Health or Damage, round to nearest whole number, minimum 1
                boostToApply = Mathf.Max(1f, Mathf.Round(calculatedBoost));
            }
            else
            {
                // For smaller stats like MoveSpeed or small Attributes, round to 1 decimal, minimum 0.1
                boostToApply = Mathf.Max(0.1f, (float)System.Math.Round(calculatedBoost, 1));
            }

            // Apply the boost permanently (duration = 0 means permanent in EntityStats)
            stats.AddModifier(chosenStat, boostToApply, 0f);
            
            // Format for UI (Adds spaces before capital letters)
            string statName = System.Text.RegularExpressions.Regex.Replace(chosenStat.ToString(), "([a-z])([A-Z])", "$1 $2");
            rewardTexts.Add($"+{boostToApply} {statName}");
            
            Debug.Log($"[Chest] +{boostToApply} {statName} (was {currentVal})");
        }

        // Send to UI
        if (UIManager.Instance != null && rewardTexts.Count > 0)
        {
            UIManager.Instance.ShowRewardPopup(rewardTexts);
        }
    }
}
