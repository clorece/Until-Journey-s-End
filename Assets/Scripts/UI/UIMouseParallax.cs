using UnityEngine;

/// <summary>
/// Attaches to UI Image elements to create a 2D Parallax effect based on mouse movement.
/// </summary>
public class UIMouseParallax : MonoBehaviour
{
    [Header("Parallax Settings")]
    [Tooltip("How much this layer moves relative to the mouse. Negative values move opposite to the mouse. Closer objects should have a higher absolute value.")]
    public float parallaxFactor = -15f;
    
    [Tooltip("How smoothly the layer catches up to the target position.")]
    public float smoothing = 5f;

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Vector2 targetPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            startPosition = rectTransform.anchoredPosition;
        }
    }

    void Update()
    {
        if (rectTransform == null) return;

        // Get mouse position normalized to -1 to 1 range relative to screen center
        Vector2 mousePos = Input.mousePosition;
        float normalizedX = Mathf.Clamp((mousePos.x / Screen.width) * 2f - 1f, -1f, 1f);
        float normalizedY = Mathf.Clamp((mousePos.y / Screen.height) * 2f - 1f, -1f, 1f);

        // Calculate offset
        Vector2 offset = new Vector2(normalizedX, normalizedY) * parallaxFactor;
        targetPosition = startPosition + offset;

        // Apply smoothed movement (using unscaled delta time so it works even if time scale is 0)
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.unscaledDeltaTime * smoothing);
    }
}
