using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current zone info at the top-center of the screen.
/// Format: "Zone # - ZoneType"
/// 
/// SETUP:
/// 1. Create a UI element anchored to Top-Center.
/// 2. Use TitleBox_64x16.png as the background Image (stretch to fit).
/// 3. Add a TMP_Text child for the zone label.
/// 4. Attach this script and drag the text into the Inspector.
/// </summary>
public class RunInfoDisplay : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The TMP_Text component that displays the zone info.")]
    public TMP_Text zoneText;

    [Header("Settings")]
    [Tooltip("If true, hides the display when no run is active.")]
    public bool hideWhenIdle = true;

    [Header("Animation Settings")]
    public float slideDuration = 0.5f;
    public float hiddenOffsetY = 400f; // Increased to prevent peeking due to inertia

    private RectTransform rectTransform;
    private Vector2 originalPos;
    private Coroutine slideCoroutine;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        originalPos = rectTransform.anchoredPosition;
        
        // Start hidden
        rectTransform.anchoredPosition = originalPos + new Vector2(0, hiddenOffsetY);
        if (canvasGroup != null) canvasGroup.alpha = 0;
    }

    void Start()
    {
        // Subscribe to RunManager events
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnZoneChanged += OnZoneChanged;
            RunManager.Instance.OnStateChanged += OnStateChanged;

            // Initialize with current state
            if (RunManager.Instance.IsInRun && RunManager.Instance.CurrentZone != null)
            {
                UpdateDisplay(RunManager.Instance.CurrentFloor, RunManager.Instance.CurrentZone.zoneType);
            }
            else
            {
                SetVisible(false);
            }
        }
        else
        {
            // RunManager not found — hide until it's available
            SetVisible(false);
        }
    }

    void OnDestroy()
    {
        if (RunManager.Instance != null)
        {
            RunManager.Instance.OnZoneChanged -= OnZoneChanged;
            RunManager.Instance.OnStateChanged -= OnStateChanged;
        }
    }

    private void OnZoneChanged(InstanceZone zone)
    {
        if (zone == null) return;

        // Don't display zone info for the hub
        if (zone.zoneType == InstanceZone.InstanceType.Hub)
        {
            SetVisible(false);
            return;
        }

        UpdateDisplay(RunManager.Instance.CurrentFloor, zone.zoneType);
        SetVisible(true);
    }

    private void OnStateChanged(RunManager.RunState state)
    {
        if (hideWhenIdle && state == RunManager.RunState.Idle)
        {
            SetVisible(false);
        }
    }

    /// <summary>
    /// Updates the display text.
    /// </summary>
    public void UpdateDisplay(int floor, InstanceZone.InstanceType zoneType)
    {
        if (zoneText != null)
        {
            string typeName = GetZoneTypeName(zoneType);
            zoneText.text = $"Zone {floor} - {typeName}";
        }
    }

    private string GetZoneTypeName(InstanceZone.InstanceType type)
    {
        switch (type)
        {
            case InstanceZone.InstanceType.Combat:    return "Combat";
            case InstanceZone.InstanceType.Shop:      return "Shop";
            case InstanceZone.InstanceType.Respite:   return "Respite";
            case InstanceZone.InstanceType.Challenge: return "Challenge";
            default: return type.ToString();
        }
    }

    private void SetVisible(bool visible)
    {
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        
        Vector2 targetPos = visible ? originalPos : originalPos + new Vector2(0, hiddenOffsetY);
        float targetAlpha = visible ? 1f : 0f;
        
        slideCoroutine = StartCoroutine(AnimateRoutine(targetPos, targetAlpha));
    }

    private System.Collections.IEnumerator AnimateRoutine(Vector2 targetPos, float targetAlpha)
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
        float elapsed = 0;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;
            t = t * t * (3f - 2f * t); // Smoothstep

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            
            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;
        if (canvasGroup != null) canvasGroup.alpha = targetAlpha;
    }
}
