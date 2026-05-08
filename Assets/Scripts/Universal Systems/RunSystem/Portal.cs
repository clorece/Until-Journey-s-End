using UnityEngine;

/// <summary>
/// A portal the player can interact with to travel to the next instance.
/// Displays an animated sprite for the portal body and an icon sprite above indicating
/// the instance type (Combat, Shop, Respite, Challenge).
/// </summary>
public class Portal : Interactable
{
    [Header("Portal Configuration")]
    public InstanceZone.InstanceType portalType = InstanceZone.InstanceType.Combat;

    [Header("Sprites")]
    [Tooltip("The main portal body sprite renderer.")]
    public SpriteRenderer portalSprite;

    [Tooltip("The icon sprite renderer floating above the portal (sword, heart, coin, etc).")]
    public SpriteRenderer iconSprite;

    [Header("Portal Animation")]
    [Tooltip("Sprite frames for the portal idle animation loop.")]
    public Sprite[] portalFrames;

    [Tooltip("Playback speed for the portal animation.")]
    public float framesPerSecond = 8f;

    [Header("Visual Tilt")]
    [Tooltip("Tilt rotation to align the sprite with the camera angle.")]
    public Vector3 visualRotation = new Vector3(30f, 0f, 0f);

    private int currentFrame;
    private float animTimer;

    [Header("Type Icons")]
    [Tooltip("Icon shown for Combat portals.")]
    public Sprite combatIcon;

    [Tooltip("Icon shown for Shop portals.")]
    public Sprite shopIcon;

    [Tooltip("Icon shown for Respite portals.")]
    public Sprite respiteIcon;

    [Tooltip("Icon shown for Challenge portals.")]
    public Sprite challengeIcon;

    /// <summary>
    /// Sets the portal type and updates the icon sprite accordingly.
    /// Called by PortalSpawner after instantiation.
    /// </summary>
    public void SetType(InstanceZone.InstanceType type)
    {
        portalType = type;
        UpdateIcon();
    }

    /// <summary>
    /// Configures the icon sprites from a shared set.
    /// Called by PortalSpawner so icons don't need to be set per-prefab.
    /// </summary>
    public void SetIcons(Sprite combat, Sprite shop, Sprite respite, Sprite challenge)
    {
        combatIcon = combat;
        shopIcon = shop;
        respiteIcon = respite;
        challengeIcon = challenge;
        UpdateIcon();
    }

    protected override void Start()
    {
        base.Start();
        // Apply the visual tilt to the whole object
        transform.rotation = Quaternion.Euler(visualRotation);
    }

    private void UpdateIcon()
    {
        if (iconSprite == null) return;

        switch (portalType)
        {
            case InstanceZone.InstanceType.Combat:
                iconSprite.sprite = combatIcon;
                break;
            case InstanceZone.InstanceType.Shop:
                iconSprite.sprite = shopIcon;
                break;
            case InstanceZone.InstanceType.Respite:
                iconSprite.sprite = respiteIcon;
                break;
            case InstanceZone.InstanceType.Challenge:
                iconSprite.sprite = challengeIcon;
                break;
            default:
                iconSprite.sprite = null;
                break;
        }
    }

    protected override void Update()
    {
        base.Update();
        AnimatePortal();
    }

    /// <summary>
    /// Loops through portalFrames at the configured FPS, cycling continuously.
    /// </summary>
    private void AnimatePortal()
    {
        if (portalFrames == null || portalFrames.Length == 0 || portalSprite == null) return;

        float fps = framesPerSecond > 0.1f ? framesPerSecond : 1f;
        float timePerFrame = 1f / fps;

        animTimer += Time.deltaTime;
        if (animTimer >= timePerFrame)
        {
            animTimer -= timePerFrame;
            currentFrame = (currentFrame + 1) % portalFrames.Length;
            portalSprite.sprite = portalFrames[currentFrame];
        }
    }

    protected override void OnInteract()
    {
        hasInteracted = true;
        Debug.Log($"[Portal] Player entered {portalType} portal.");

        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnPortalSelected(portalType);
        }
        else
        {
            Debug.LogError("[Portal] RunManager.Instance is null! Cannot transition.");
        }
    }
}
