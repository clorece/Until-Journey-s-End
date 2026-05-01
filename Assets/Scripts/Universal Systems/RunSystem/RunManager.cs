using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    public enum InstanceMode { Battle, Challenge, Shop, Respite, Boss }

    [Header("Scene References")]
    [Tooltip("The exact name of the Hub scene in Build Settings.")]
    public string hubSceneName = "Hub";

    [Header("Run State")]
    public string currentZoneName = "Zone0";
    public int currentFloor = 0;
    public InstanceMode nextMode = InstanceMode.Battle;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartRun(string firstZoneScene)
    {
        currentFloor = 1;
        nextMode = InstanceMode.Battle;
        SceneManager.LoadScene(firstZoneScene);
    }

    public void EnterNextInstance(string sceneName, InstanceMode mode)
    {
        currentFloor++;
        nextMode = mode;
        SceneManager.LoadScene(sceneName);
    }

    public void ReturnToHub()
    {
        currentFloor = 0;
        SceneManager.LoadScene(hubSceneName);
    }
}
