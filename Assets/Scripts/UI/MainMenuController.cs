using UnityEngine;
using UnityEngine.SceneManagement;

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

    void Start()
    {
        // Ensure we start on the main panel
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
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
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
