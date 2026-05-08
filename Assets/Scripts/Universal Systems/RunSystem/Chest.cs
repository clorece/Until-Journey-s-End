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
        hasInteracted = true;

        // Play the open animation
        PlayOpenAnimation();

        // Grant random stat boosts to the player
        if (player != null)
        {
            EntityStats playerStats = player.GetComponent<EntityStats>();
            if (playerStats != null)
            {
                GrantRandomStatBoosts(playerStats);
            }
        }

        OnChestOpened?.Invoke();
        Debug.Log("[Chest] Opened! Stat boosts granted.");
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
        StatType[] boostableStats = new StatType[]
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

        // Pick 'statBoostCount' random unique stats
        StatType[] selected = new StatType[statBoostCount];
        System.Collections.Generic.List<StatType> pool = new System.Collections.Generic.List<StatType>(boostableStats);

        for (int i = 0; i < statBoostCount && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            selected[i] = pool[index];
            pool.RemoveAt(index);

            // Apply the boost permanently (duration = 0 means permanent in EntityStats)
            stats.AddModifier(selected[i], boostAmount, 0f);
            Debug.Log($"[Chest] +{boostAmount} {selected[i]}");
        }
    }
}
