using UnityEngine;

/// <summary>
/// A campfire that appears in Respite zones.
/// Player presses F to rest and fully restore their health.
/// </summary>
public class Campfire : Interactable
{
    [Header("Campfire Visuals")]
    [Tooltip("Sprite frames for an idle animation loop. Leave empty for a static sprite.")]
    public Sprite[] animationFrames;

    [Tooltip("Playback speed for the animation.")]
    public float framesPerSecond = 6f;

    [Header("Visual Tilt")]
    [Tooltip("Tilt rotation to align the sprite with the camera angle.")]
    public Vector3 visualRotation = new Vector3(30f, 0f, 0f);

    private SpriteRenderer spriteRenderer;
    private int currentFrame;
    private float animTimer;

    protected override void Start()
    {
        base.Start();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null && animationFrames != null && animationFrames.Length > 0)
            spriteRenderer.sprite = animationFrames[0];

        // Apply the visual tilt
        transform.rotation = Quaternion.Euler(visualRotation);
    }

    protected override void Update()
    {
        base.Update();
        AnimateCampfire();
    }

    private void AnimateCampfire()
    {
        if (animationFrames == null || animationFrames.Length <= 1 || spriteRenderer == null) return;

        float fps = framesPerSecond > 0.1f ? framesPerSecond : 1f;
        float timePerFrame = 1f / fps;

        animTimer += Time.deltaTime;
        if (animTimer >= timePerFrame)
        {
            animTimer -= timePerFrame;
            currentFrame = (currentFrame + 1) % animationFrames.Length;
            spriteRenderer.sprite = animationFrames[currentFrame];
        }
    }

    protected override void OnInteract()
    {
        hasInteracted = true;

        if (player != null)
        {
            EntityStats playerStats = player.GetComponent<EntityStats>();
            if (playerStats != null)
            {
                playerStats.Heal(playerStats.GetStatValue(StatType.MaxHealth));
                Debug.Log("[Campfire] Player rested at the campfire. Fully healed!");
            }
        }
    }
}
