using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("The exact name of the Hub scene in Build Settings.")]
    public string hubSceneName = "Hub";

    /// <summary>
    /// Call this from a UI Button's OnClick event to start the game.
    /// </summary>
    public void StartGame()
    {
        SceneManager.LoadScene(hubSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Quitting game.");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
