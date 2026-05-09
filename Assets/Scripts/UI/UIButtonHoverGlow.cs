using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach this to a UI Button to display a separate child Image (like a transparent border) only while hovering.
/// </summary>
public class UIButtonHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("The child Image component containing the highlight border sprite (e.g., HighlightButton_60x23).")]
    public Image highlightBorder;

    void Start()
    {
        // Ensure the border is hidden by default when the scene starts
        if (highlightBorder != null)
        {
            highlightBorder.enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show the border when the mouse enters the button area
        if (highlightBorder != null)
        {
            highlightBorder.enabled = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Hide the border when the mouse leaves
        if (highlightBorder != null)
        {
            highlightBorder.enabled = false;
        }
    }
    
    private void OnDisable()
    {
        // Failsafe: hide if the menu gets closed while hovering
        if (highlightBorder != null)
        {
            highlightBorder.enabled = false;
        }
    }
}
