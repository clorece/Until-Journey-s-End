using UnityEngine;

/// <summary>
/// Adds a subtle inertia effect to UI elements based on player movement.
/// Attach this to individual HUD elements (like the Portrait or Health Bar)
/// or the entire HUD parent.
/// </summary>
public class HUDInertia : MonoBehaviour
{
    [Header("Player Reference")]
    public Transform player;
    
    [Header("Settings")]
    [Tooltip("How much the UI shifts relative to movement. Keep this small (e.g., 5-15).")]
    public float shiftAmount = 10f;
    
    [Tooltip("How smoothly the UI returns to its original position.")]
    public float smoothness = 5f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector3 lastPlayerPosition;
    private Vector2 currentVelocity;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    void Start()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        if (player != null)
            lastPlayerPosition = player.position;
    }

    void LateUpdate()
    {
        if (player == null || rectTransform == null) return;

        // Calculate player delta movement in the XZ plane
        Vector3 delta = player.position - lastPlayerPosition;
        lastPlayerPosition = player.position;

        // Ignore massive leaps (teleportation) to prevent UI from flying across the screen
        if (delta.sqrMagnitude > 100f)
        {
            rectTransform.anchoredPosition = originalPosition;
            return;
        }

        // Map world movement to UI shift
        // We use X and Z world movement to drive X and Y UI shift
        Vector2 targetShift = new Vector2(-delta.x, -delta.z) * shiftAmount;

        // Smoothly interpolate back to original position + shift
        Vector2 targetPosition = originalPosition + targetShift;
        
        // Use SmoothDamp or Lerp for a natural feel
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition, 
            targetPosition, 
            Time.deltaTime * smoothness
        );

        // Gradually decay toward original position if no movement
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition, 
            originalPosition, 
            Time.deltaTime * (smoothness * 0.5f)
        );
    }
}
