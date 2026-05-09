using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Simple controller for the Main Menu scene.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The name of the main game scene to load when Start is clicked.")]
    public string gameSceneName = "Overworld";
    [Tooltip("The name of the main menu scene to return to when Quit is clicked from the pause menu.")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Menu Type")]
    [Tooltip("Check this if this menu is copy/pasted into the Overworld scene to act as a Pause Menu.")]
    public bool isPauseMenu = false;

    [Header("Fading Transitions")]
    [Tooltip("A CanvasGroup covering the whole screen used to fade to black. Starts completely black (Alpha 1).")]
    public CanvasGroup fadeScreen;
    public float fadeDuration = 1f;

    [Header("Gameplay UI Transition")]
    [Tooltip("Optional: Drag your HUD/Gameplay UI panel here to slide it out when pausing.")]
    public RectTransform gameplayUIRect;
    private Vector2 gameplayUIOnPos;

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;
    public GameObject howToPlayPanel;

    [Header("Settings Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Transition Settings")]
    public float transitionDuration = 0.4f;
    public float offscreenX = -3000f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform mainRect;
    private RectTransform settingsRect;
    private RectTransform howToPlayRect;

    private Vector2 mainOnPos;
    private Vector2 settingsOnPos;
    private Vector2 howToPlayOnPos;

    void Start()
    {
        // Capture RectTransforms and their original "ON" positions
        if (gameplayUIRect != null)
        {
            gameplayUIOnPos = gameplayUIRect.anchoredPosition;
        }

        if (mainPanel != null)
        {
            mainRect = mainPanel.GetComponent<RectTransform>();
            mainOnPos = mainRect.anchoredPosition;
        }
        if (settingsPanel != null)
        {
            settingsRect = settingsPanel.GetComponent<RectTransform>();
            settingsOnPos = settingsRect.anchoredPosition;
            
            // Move offscreen immediately
            settingsPanel.SetActive(true);
            settingsRect.anchoredPosition = new Vector2(offscreenX, settingsOnPos.y);
        }
        if (howToPlayPanel != null)
        {
            howToPlayRect = howToPlayPanel.GetComponent<RectTransform>();
            howToPlayOnPos = howToPlayRect.anchoredPosition;
            
            // Move offscreen immediately
            howToPlayPanel.SetActive(true);
            howToPlayRect.anchoredPosition = new Vector2(offscreenX, howToPlayOnPos.y);
        }

        // Initialize Visibility
        if (isPauseMenu)
        {
            // If it's a pause menu, hide everything completely to start
            if (mainPanel != null) { mainPanel.SetActive(true); mainRect.anchoredPosition = new Vector2(offscreenX, mainOnPos.y); }
            if (settingsPanel != null) { settingsPanel.SetActive(true); settingsRect.anchoredPosition = new Vector2(offscreenX, settingsOnPos.y); }
            if (howToPlayPanel != null) { howToPlayPanel.SetActive(true); howToPlayRect.anchoredPosition = new Vector2(offscreenX, howToPlayOnPos.y); }
            
            // Fade in the Overworld scene when it loads!
            if (fadeScreen != null)
            {
                fadeScreen.alpha = 1f;
                fadeScreen.blocksRaycasts = true;
                StartCoroutine(FadeRoutine(1f, 0f, null));
            }
        }
        else
        {
            // If it's the main menu, show the main panel and start fading in!
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
                mainRect.anchoredPosition = mainOnPos;
            }
            if (fadeScreen != null)
            {
                fadeScreen.alpha = 1f;
                fadeScreen.blocksRaycasts = true;
                StartCoroutine(FadeRoutine(1f, 0f, null)); // Fade in!
            }
        }

        InitializeSliders();
    }

    void Update()
    {
        // Handle Escape key to pause/unpause in the Overworld
        if (isPauseMenu && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    private void TogglePauseMenu()
    {
        StopAllCoroutines();
        // If the main menu is offscreen, we are unpaused. So pause and slide it in!
        if (mainRect != null && mainRect.anchoredPosition.x < offscreenX + 100f)
        {
            // Set to 0.0001f instead of absolute 0f. 
            // This prevents "Divide By Zero" infinite loops in other scripts that might cause 1 FPS!
            Time.timeScale = 0.0001f; 
            
            // Slide Gameplay UI out
            if (gameplayUIRect != null)
                StartCoroutine(TransitionPanels(gameplayUIRect, gameplayUIOnPos, new Vector2(offscreenX, gameplayUIOnPos.y), false));

            StartCoroutine(TransitionPanels(mainRect, new Vector2(offscreenX, mainOnPos.y), mainOnPos, true));
        }
        else
        {
            // We are paused, so resume!
            ResumeGame();
        }
    }

    private void InitializeSliders()
    {
        if (SettingsManager.Instance == null) return;

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = SettingsManager.Instance.GetMasterVolume();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = SettingsManager.Instance.GetMusicVolume();
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = SettingsManager.Instance.GetSFXVolume();
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    public void OnMasterVolumeChanged(float value)
    {
        if (SettingsManager.Instance != null) SettingsManager.Instance.SetMasterVolume(value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        if (SettingsManager.Instance != null) SettingsManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (SettingsManager.Instance != null) SettingsManager.Instance.SetSFXVolume(value);
    }

    public void StartGame()
    {
        if (isPauseMenu)
        {
            ResumeGame();
        }
        else
        {
            // Start fading out, then load the scene when done
            if (fadeScreen != null)
            {
                fadeScreen.blocksRaycasts = true; // Block clicks while fading
                StartCoroutine(FadeRoutine(0f, 1f, () => SceneManager.LoadScene(gameSceneName)));
            }
            else
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f; // Unfreeze game
        StopAllCoroutines();
        
        // Slide Gameplay UI back in
        if (gameplayUIRect != null)
            StartCoroutine(TransitionPanels(gameplayUIRect, new Vector2(offscreenX, gameplayUIOnPos.y), gameplayUIOnPos, true));

        // Slide whatever panel is open completely off the screen
        if (mainPanel != null && mainRect.anchoredPosition.x > offscreenX + 100f) 
            StartCoroutine(TransitionPanels(mainRect, mainOnPos, new Vector2(offscreenX, mainOnPos.y), false));
            
        if (settingsPanel != null && settingsRect.anchoredPosition.x > offscreenX + 100f) 
            StartCoroutine(TransitionPanels(settingsRect, settingsOnPos, new Vector2(offscreenX, settingsOnPos.y), false));
            
        if (howToPlayPanel != null && howToPlayRect.anchoredPosition.x > offscreenX + 100f) 
            StartCoroutine(TransitionPanels(howToPlayRect, howToPlayOnPos, new Vector2(offscreenX, howToPlayOnPos.y), false));
    }

    public void OpenSettings()
    {
        StopAllCoroutines();
        if (mainRect != null) StartCoroutine(TransitionPanels(mainRect, mainOnPos, new Vector2(offscreenX, mainOnPos.y), false));
        if (settingsRect != null) StartCoroutine(TransitionPanels(settingsRect, new Vector2(offscreenX, settingsOnPos.y), settingsOnPos, true));
        if (howToPlayRect != null) StartCoroutine(TransitionPanels(howToPlayRect, howToPlayRect.anchoredPosition, new Vector2(offscreenX, howToPlayOnPos.y), false));
    }

    public void CloseSettings()
    {
        StopAllCoroutines();
        if (settingsRect != null) StartCoroutine(TransitionPanels(settingsRect, settingsOnPos, new Vector2(offscreenX, settingsOnPos.y), false));
        if (mainRect != null) StartCoroutine(TransitionPanels(mainRect, new Vector2(offscreenX, mainOnPos.y), mainOnPos, true));
        
        // Save preferences when leaving the settings menu
        if (SettingsManager.Instance != null) SettingsManager.Instance.SaveSettings();
    }

    public void OpenHowToPlay()
    {
        StopAllCoroutines();
        if (mainRect != null) StartCoroutine(TransitionPanels(mainRect, mainOnPos, new Vector2(offscreenX, mainOnPos.y), false));
        if (howToPlayRect != null) StartCoroutine(TransitionPanels(howToPlayRect, new Vector2(offscreenX, howToPlayOnPos.y), howToPlayOnPos, true));
        if (settingsRect != null) StartCoroutine(TransitionPanels(settingsRect, settingsRect.anchoredPosition, new Vector2(offscreenX, settingsOnPos.y), false));
    }

    public void CloseHowToPlay()
    {
        StopAllCoroutines();
        if (howToPlayRect != null) StartCoroutine(TransitionPanels(howToPlayRect, howToPlayOnPos, new Vector2(offscreenX, howToPlayOnPos.y), false));
        if (mainRect != null) StartCoroutine(TransitionPanels(mainRect, new Vector2(offscreenX, mainOnPos.y), mainOnPos, true));
    }

    private IEnumerator TransitionPanels(RectTransform panel, Vector2 start, Vector2 end, bool stateAfter)
    {
        if (panel == null) yield break;

        // Ensure it's active so we can see the transition
        panel.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            panel.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }

        panel.anchoredPosition = end;
        
        // If we were sliding it OFF, we can disable it now to save performance
        if (!stateAfter)
        {
            // Optional: panel.gameObject.SetActive(false); 
            // Keep it active if you want the "offscreen" position to persist without flicker
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        
        if (fadeScreen != null)
        {
            fadeScreen.blocksRaycasts = true;
            StartCoroutine(FadeRoutine(0f, 1f, ExecuteQuit));
        }
        else
        {
            ExecuteQuit();
        }
    }

    private void ExecuteQuit()
    {
        if (isPauseMenu)
        {
            // Unfreeze time before loading, otherwise the main menu will be frozen!
            Time.timeScale = 1f; 
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            // This stops the game if you are testing inside the Unity Editor
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // This stops the game in the final built .exe
            Application.Quit();
#endif
        }
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, System.Action onComplete)
    {
        // Optional: Yield one frame so the scene can fully initialize before we start fading
        yield return null;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            // Cap the delta time to 0.1s. 
            // When a scene loads, the first frame's unscaledDeltaTime can be massive (e.g. 2 seconds)
            // which causes the entire fade to skip instantly. This cap prevents that!
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            fadeScreen.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
            yield return null;
        }

        fadeScreen.alpha = endAlpha;
        
        // Unblock raycasts if we faded back to completely invisible
        if (endAlpha == 0f) fadeScreen.blocksRaycasts = false;

        onComplete?.Invoke();
    }
}
