using UnityEngine;
using TMPro;

/// <summary>
/// Singleton that manages global UI state.
/// Handles toggling panels (e.g., Character Stats on Tab hold).
/// Attach to a persistent GameObject in the scene.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public enum UIState { Gameplay, Stats }

    [Header("Panel References")]
    [Tooltip("The CharacterStatsPanel root GameObject. Will be shown/hidden.")]
    public GameObject characterStatsPanel;

    [Header("Animation Settings")]
    public float slideDuration = 0.3f;
    public float hiddenOffsetX = -800f; // Increased to prevent peeking due to inertia

    private RectTransform statsRect;
    private Vector2 statsOriginalPos;
    private Coroutine slideCoroutine;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    private Canvas fadeCanvas;
    private UnityEngine.UI.Image fadeImage;

    [Header("Reward Popup Settings")]
    [Tooltip("Drop your JacquardaBastarda9-Regular SDF font here!")]
    public TMP_FontAsset rewardFont;
    public float rewardFontSize = 32f;
    public float rewardPopupDuration = 2.5f;
    private Canvas rewardCanvas;
    private TMP_Text rewardText;
    private Coroutine rewardCoroutine;

    [Header("Death Screen Settings")]
    public TMP_FontAsset deathFont;
    public float deathFadeInDuration = 2f;
    private Canvas deathCanvas;
    private CanvasGroup deathCanvasGroup;
    private UnityEngine.UI.Image deathBackground;
    private TMP_Text deathTextMain;
    private TMP_Text deathTextPrompt;
    private bool isWaitingForDeathInput = false;
    private System.Action onDeathPromptPressed;
    private Coroutine deathCoroutine;

    private UIState currentState = UIState.Gameplay;
    public UIState CurrentState => currentState;

    /// <summary>
    /// Fired when the UI state changes. Useful for pausing input, etc.
    /// </summary>
    public event System.Action<UIState> OnUIStateChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        SetupFadeCanvas();
        SetupRewardCanvas();
        SetupDeathCanvas();
    }

    private void SetupRewardCanvas()
    {
        GameObject canvasObj = new GameObject("RewardCanvas");
        canvasObj.transform.SetParent(transform);
        rewardCanvas = canvasObj.AddComponent<Canvas>();
        rewardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rewardCanvas.sortingOrder = 100; // Above HUD but below Fade

        GameObject textObj = new GameObject("RewardText");
        textObj.transform.SetParent(canvasObj.transform, false);
        rewardText = textObj.AddComponent<TextMeshProUGUI>();
        
        if (rewardFont != null)
        {
            rewardText.font = rewardFont;
        }

        rewardText.alignment = TextAlignmentOptions.Center;
        rewardText.fontSize = rewardFontSize;
        rewardText.color = new Color(1f, 1f, 1f, 0f); // White, transparent
        rewardText.fontStyle = FontStyles.Bold;

        // Optional: Add outline for readability
        rewardText.outlineWidth = 0.2f;
        rewardText.outlineColor = new Color32(0, 0, 0, 255);

        RectTransform rect = rewardText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -150); // A bit below center
        rect.sizeDelta = new Vector2(600, 300);
    }

    public void ShowRewardPopup(System.Collections.Generic.List<string> rewards)
    {
        if (rewardText == null) return;

        string combinedText = "";
        foreach (string r in rewards)
        {
            combinedText += r + "\n";
        }
        rewardText.text = combinedText.TrimEnd('\n');

        if (rewardCoroutine != null) StopCoroutine(rewardCoroutine);
        rewardCoroutine = StartCoroutine(RewardPopupRoutine());
    }

    private System.Collections.IEnumerator RewardPopupRoutine()
    {
        float fadeTime = 0.5f;
        float elapsed = 0f;
        
        // Fade in
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeTime);
            rewardText.color = new Color(1f, 1f, 1f, alpha); // White
            rewardText.outlineColor = new Color(0f, 0f, 0f, alpha); // Fade outline too
            yield return null;
        }

        // Hold
        yield return new WaitForSecondsRealtime(rewardPopupDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeTime);
            rewardText.color = new Color(1f, 1f, 1f, alpha);
            rewardText.outlineColor = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }
        rewardText.color = new Color(1f, 1f, 1f, 0f);
        rewardText.outlineColor = new Color(0f, 0f, 0f, 0f);
    }

    private void SetupDeathCanvas()
    {
        GameObject canvasObj = new GameObject("DeathCanvas");
        canvasObj.transform.SetParent(transform);
        deathCanvas = canvasObj.AddComponent<Canvas>();
        deathCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        deathCanvas.sortingOrder = 900; // Above everything except fade
        
        deathCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        deathCanvasGroup.alpha = 0f;
        deathCanvasGroup.interactable = false;
        deathCanvasGroup.blocksRaycasts = true;

        // Background
        GameObject bgObj = new GameObject("DeathBackground");
        bgObj.transform.SetParent(canvasObj.transform, false);
        deathBackground = bgObj.AddComponent<UnityEngine.UI.Image>();
        deathBackground.color = new Color(0, 0, 0, 0.9f); // Darker background
        RectTransform bgRect = deathBackground.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // YOU DIED Text
        GameObject mainTextObj = new GameObject("DeathTextMain");
        mainTextObj.transform.SetParent(canvasObj.transform, false);
        deathTextMain = mainTextObj.AddComponent<TextMeshProUGUI>();
        if (deathFont != null) deathTextMain.font = deathFont;
        deathTextMain.alignment = TextAlignmentOptions.Center;
        deathTextMain.fontSize = 120;
        deathTextMain.color = new Color(0.75f, 0.1f, 0.1f, 0f); // Rich red/maroon
        deathTextMain.fontStyle = FontStyles.Bold;
        deathTextMain.text = "YOU DIED";
        RectTransform mainRect = deathTextMain.GetComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainRect.pivot = new Vector2(0.5f, 0.5f);
        mainRect.anchoredPosition = new Vector2(0, 50);
        mainRect.sizeDelta = new Vector2(800, 200);

        // Prompt Text
        GameObject promptTextObj = new GameObject("DeathTextPrompt");
        promptTextObj.transform.SetParent(canvasObj.transform, false);
        deathTextPrompt = promptTextObj.AddComponent<TextMeshProUGUI>();
        if (deathFont != null) deathTextPrompt.font = deathFont;
        deathTextPrompt.alignment = TextAlignmentOptions.Center;
        deathTextPrompt.fontSize = 36;
        deathTextPrompt.color = new Color(0.7f, 0.7f, 0.7f, 0f); // Gray, starts hidden
        deathTextPrompt.text = "Press any key to go back to Hub";
        RectTransform promptRect = deathTextPrompt.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.5f);
        promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.anchoredPosition = new Vector2(0, -80);
        promptRect.sizeDelta = new Vector2(800, 100);

        deathCanvas.gameObject.SetActive(false);
    }

    public void ShowDeathScreen(System.Action onKeypressed)
    {
        onDeathPromptPressed = onKeypressed;
        isWaitingForDeathInput = false;

        if (deathCoroutine != null) StopCoroutine(deathCoroutine);
        deathCoroutine = StartCoroutine(DeathScreenRoutine());
    }

    private System.Collections.IEnumerator DeathScreenRoutine()
    {
        deathCanvas.gameObject.SetActive(true);
        deathCanvasGroup.alpha = 1f;

        // Reset alphas
        deathBackground.color = new Color(0, 0, 0, 0f);
        deathTextMain.color = new Color(0.75f, 0.1f, 0.1f, 0f);
        deathTextPrompt.color = new Color(0.7f, 0.7f, 0.7f, 0f);

        float elapsed = 0f;
        float bgFadeDuration = deathFadeInDuration * 0.5f;

        // 1. Fade in background slightly
        while (elapsed < bgFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / bgFadeDuration) * 0.9f;
            deathBackground.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        // 2. Fade in "YOU DIED"
        elapsed = 0f;
        while (elapsed < deathFadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / deathFadeInDuration);
            deathTextMain.color = new Color(0.75f, 0.1f, 0.1f, alpha);
            yield return null;
        }

        // Wait a bit for impact
        yield return new WaitForSecondsRealtime(0.75f);

        // 3. Fade in Prompt
        elapsed = 0f;
        float promptFade = 1f;
        while (elapsed < promptFade)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / promptFade);
            deathTextPrompt.color = new Color(0.7f, 0.7f, 0.7f, alpha);
            yield return null;
        }

        // Ready for input
        isWaitingForDeathInput = true;
    }

    private void SetupFadeCanvas()
    {
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);
        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999; // Very high sorting order to cover everything

        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>(); // Block raycasts during fade if needed

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        fadeImage = imageObj.AddComponent<UnityEngine.UI.Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // Start transparent

        RectTransform rect = fadeImage.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Fades the screen to black, invokes the midpoint action, and then fades back to clear.
    /// </summary>
    public void FadeAndCall(System.Action onMidpoint)
    {
        StartCoroutine(FadeRoutine(onMidpoint));
    }

    private System.Collections.IEnumerator FadeRoutine(System.Action onMidpoint)
    {
        float halfDur = fadeDuration / 2f;
        float elapsed = 0f;

        // Fade Out (to black)
        fadeImage.raycastTarget = true; // Block UI interaction during fade
        while (elapsed < halfDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / halfDur);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 1f);

        // Call the midpoint action (e.g., teleporting)
        onMidpoint?.Invoke();

        // Brief pause while black
        yield return new WaitForSecondsRealtime(0.1f);

        // Fade In (to clear)
        elapsed = 0f;
        while (elapsed < halfDur)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / halfDur);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, 0f);
        fadeImage.raycastTarget = false; // Allow UI interaction again
    }

    void Start()
    {
        if (characterStatsPanel != null)
        {
            statsRect = characterStatsPanel.GetComponent<RectTransform>();
            statsOriginalPos = statsRect.anchoredPosition;
            
            // Set initial position to hidden
            statsRect.anchoredPosition = statsOriginalPos + new Vector2(hiddenOffsetX, 0);
            characterStatsPanel.SetActive(false);
        }
    }

    void Update()
    {
        HandleTabInput();

        if (isWaitingForDeathInput && Input.anyKeyDown)
        {
            isWaitingForDeathInput = false;
            
            // Hide the death canvas immediately when the teleport fade starts, 
            // or let it stay until the fade finishes. We will just disable it.
            deathCanvas.gameObject.SetActive(false);
            
            onDeathPromptPressed?.Invoke();
        }
    }

    private void HandleTabInput()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ShowStatsPanel();
        }
        else if (Input.GetKeyUp(KeyCode.Tab))
        {
            HideStatsPanel();
        }
    }

    private void ShowStatsPanel()
    {
        if (characterStatsPanel == null) return;

        characterStatsPanel.SetActive(true);
        AnimatePanel(statsOriginalPos);
        SetState(UIState.Stats);
    }

    private void HideStatsPanel()
    {
        if (characterStatsPanel == null) return;

        AnimatePanel(statsOriginalPos + new Vector2(hiddenOffsetX, 0), () => {
            characterStatsPanel.SetActive(false);
        });
        SetState(UIState.Gameplay);
    }

    private void AnimatePanel(Vector2 targetPos, System.Action onComplete = null)
    {
        if (slideCoroutine != null) StopCoroutine(slideCoroutine);
        slideCoroutine = StartCoroutine(SlideRoutine(targetPos, onComplete));
    }

    private System.Collections.IEnumerator SlideRoutine(Vector2 targetPos, System.Action onComplete)
    {
        Vector2 startPos = statsRect.anchoredPosition;
        float elapsed = 0;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled so it works even if game is paused
            float t = elapsed / slideDuration;
            // Smooth step for nicer feel
            t = t * t * (3f - 2f * t);
            
            statsRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        statsRect.anchoredPosition = targetPos;
        onComplete?.Invoke();
    }

    private void SetState(UIState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        OnUIStateChanged?.Invoke(currentState);
    }
}
