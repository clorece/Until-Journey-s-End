using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Simple controller for the Main Menu scene.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The name of the main game scene to load when Start is clicked.")]
    public string gameSceneName = "GameScene";

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("Settings Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    void Start()
    {
        // Ensure we start on the main panel
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        InitializeSliders();
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
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        
        // Save preferences when leaving the settings menu
        if (SettingsManager.Instance != null) SettingsManager.Instance.SaveSettings();
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
