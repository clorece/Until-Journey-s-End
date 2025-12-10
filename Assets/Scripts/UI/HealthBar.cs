using UnityEngine;
using TMPro;

public class HealthDisplay : MonoBehaviour
{
    [Header("References")]
    public TMP_Text textComponent; 

    [Header("Settings")]
    public Vector3 offset = new Vector3(0, 2.5f, 0); 
    public bool alwaysFaceCamera = true;

    private EntityStats myStats;
    private Camera mainCam;

    void Start()
    {
        myStats = GetComponentInParent<EntityStats>();
        mainCam = Camera.main;

        if (myStats == null)
        {
            // fallback: try finding it on the object itself if not on parent
            myStats = GetComponent<EntityStats>();
        }

        if (myStats == null)
        {
            Debug.LogError("HealthDisplay: Could not find EntityStats!");
            return;
        }

        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();

        // Subscribe to event
        myStats.OnHealthChanged += UpdateText;
        
        UpdateText();
    }

    void OnDestroy()
    {
        if (myStats != null)
            myStats.OnHealthChanged -= UpdateText;
    }

    // Changed to LateUpdate to prevent jittering when the camera moves
    void LateUpdate()
    {
        if (myStats != null)
        {
            // 1. Follow the target
            transform.position = myStats.transform.position + offset;

            // 2. Billboard Effect
            if (alwaysFaceCamera && mainCam != null)
            {
                // This aligns the text perfectly with the camera's angle
                transform.rotation = mainCam.transform.rotation;
                
                // OPTIONAL: If your text appears "backwards" (mirror image), 
                // swap the line above with this one instead:
                // transform.LookAt(transform.position - mainCam.transform.forward);
            }
        }
    }

    void UpdateText()
    {
        if (textComponent != null && myStats != null)
        {
            float current = myStats.CurrentHealth;
            float max = myStats.GetStatValue(StatType.MaxHealth);
            
            textComponent.text = $"{Mathf.Ceil(current)} / {Mathf.Ceil(max)}";
            
            float percent = current / max;
            if (percent < 0.3f) textComponent.color = Color.red;
            else textComponent.color = Color.white;
        }
    }
}