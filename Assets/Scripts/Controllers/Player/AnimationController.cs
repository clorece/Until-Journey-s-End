using UnityEngine;

[System.Serializable]
public struct AnimationFrames
{
    public Sprite[] sprites;
    public float framesPerSecond;
    public bool loop; 
}

public class AnimationController : MonoBehaviour
{
    [Header("Animation Clips")]
    public AnimationFrames idleClip;
    public AnimationFrames walkClip;
    
    [Header("Combat Clips")]
    public AnimationFrames[] attackClips; 
    public float comboResetTimer = 1.0f;

    [Header("Dependencies")]
    public PlayerMovement playerMovement;

    // --- Private ---
    private SpriteRenderer spriteRenderer;
    private AnimationFrames currentAnimation;
    private int currentFrameIndex;
    private float timer;
    
    // Combat State Tracking
    private bool isAttacking;
    private int comboIndex; 
    private float lastInputTime;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerMovement == null)
            Debug.LogError("AnimationController: PlayerMovement dependency is missing!");
            
        idleClip.loop = true;
        walkClip.loop = true;

        SetAnimation(idleClip);
    }

    void Update()
    {
        // if currently attacking, focus ONLY on playing that animation
        if (isAttacking)
        {
            PlayOneShotAnimation();
            return; 
        }

        if (playerMovement == null) return;

        if (Input.GetMouseButtonDown(0)) 
        {
            TriggerAttack();
            return;
        }

        bool isMoving = playerMovement.IsMoving();

        if (isMoving && currentAnimation.sprites != walkClip.sprites)
        {
            SetAnimation(walkClip);
        }
        else if (!isMoving && currentAnimation.sprites != idleClip.sprites)
        {
            SetAnimation(idleClip);
        }

        PlayLoopingAnimation();
    }

    public void TriggerAttack()
    {
        if (attackClips.Length == 0) return;

        // check how much time has passed since the last attack input
        // if it has been too long, reset the combo back to the first swing
        if (Time.time - lastInputTime > comboResetTimer)
        {
            comboIndex = 0;
        }

        // update the last input time to current
        lastInputTime = Time.time;

        isAttacking = true;

        AnimationFrames attackToPlay = attackClips[comboIndex];
        attackToPlay.loop = false; 
        
        SetAnimation(attackToPlay);

        // prepare the index for the NEXT click
        comboIndex = (comboIndex + 1) % attackClips.Length;
    }

    private void PlayLoopingAnimation()
    {
        if (currentAnimation.sprites == null || currentAnimation.sprites.Length == 0) return;

        float timePerFrame = 1f / currentAnimation.framesPerSecond;
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

        float timePerFrame = 1f / currentAnimation.framesPerSecond;
        timer += Time.deltaTime;

        if (timer >= timePerFrame)
        {
            timer -= timePerFrame;
            
            if (currentFrameIndex >= currentAnimation.sprites.Length - 1)
            {
                isAttacking = false; 
                // attack finished, next frame Update() will pick up Idle/Walk
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