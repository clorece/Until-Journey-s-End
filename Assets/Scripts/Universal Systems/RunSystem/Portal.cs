using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Settings")]
    public RunManager.InstanceMode targetMode;
    public string nextSceneName; // In a real setup, we might pick this from a pool

    private bool isInteracted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isInteracted) return;

        // Check if it's the player
        if (other.CompareTag("Player"))
        {
            isInteracted = true;
            EnterPortal();
        }
    }

    private void EnterPortal()
    {
        Debug.Log($"[Portal] Entering portal to {targetMode} in {nextSceneName}");
        if (RunManager.Instance != null)
        {
            RunManager.Instance.EnterNextInstance(nextSceneName, targetMode);
        }
    }
}
