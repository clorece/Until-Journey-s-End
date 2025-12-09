using UnityEngine;

[System.Serializable]
public struct AnimationFrames
{
    public Sprite[] sprites;
    public float framesPerSecond; 
}

public class AnimationController : MonoBehaviour
{
    [Header("Animation Clips")]
    public AnimationFrames idleClip;
    public AnimationFrames walkClip;

    [Header("Dependencies")]
    public PlayerMovement playerMovement;

    // --- Private ---
    private SpriteRenderer spriteRenderer;
    private AnimationFrames currentAnimation;
    private int currentFrameIndex;
    private float timer;

    void Start()
    {
        // render sprite on start
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerMovement == null)
            Debug.LogError("BasicSpriteAnimator: PlayerMovement dependency is missing! Drag the Parent Player object here.");

        SetAnimation(idleClip);
    }

    void Update()
    {
        if (playerMovement == null) return;

        bool isMoving = playerMovement.IsMoving();

        // change between animation states

        if (isMoving && currentAnimation.sprites != walkClip.sprites)
        {
            SetAnimation(walkClip);
        }
        else if (!isMoving && currentAnimation.sprites != idleClip.sprites)
        {
            SetAnimation(idleClip);
        }

        PlayCurrentAnimation();
    }

    private void PlayCurrentAnimation()
    {
        if (currentAnimation.sprites == null || currentAnimation.sprites.Length == 0) return;

        // play the animation and set a time per frame to handle
        // framerate of the animation
        // globally we want this to be 10

        float timePerFrame = 1f / currentAnimation.framesPerSecond;
        timer += Time.deltaTime;

        if (timer >= timePerFrame)
        {
            // we reset back to the 0 index frame when the timer exceeds the frametime
            timer -= timePerFrame;
            currentFrameIndex = (currentFrameIndex + 1) % currentAnimation.sprites.Length;
            spriteRenderer.sprite = currentAnimation.sprites[currentFrameIndex];
        }
    }

    private void SetAnimation(AnimationFrames newClip)
    {
        currentAnimation = newClip;
        currentFrameIndex = 0;
        timer = 0f;
        if (currentAnimation.sprites != null && currentAnimation.sprites.Length > 0)
            spriteRenderer.sprite = currentAnimation.sprites[0];
    }
}