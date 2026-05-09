using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [Header("Cursor Settings")]
    public Texture2D defaultCursor;
    public Texture2D attackCursor;
    
    [Tooltip("The pixel coordinate of the cursor's click point. (0,0) is top-left. For a crosshair, use the center pixel (e.g. 16,16 for a 32x32 image).")]
    public Vector2 defaultHotSpot = Vector2.zero;
    public Vector2 attackHotSpot = new Vector2(16, 16); 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetDefaultCursor();
    }

    public void SetDefaultCursor()
    {
        if (defaultCursor != null)
            Cursor.SetCursor(defaultCursor, defaultHotSpot, CursorMode.Auto);
    }

    public void SetAttackCursor()
    {
        if (attackCursor != null)
            Cursor.SetCursor(attackCursor, attackHotSpot, CursorMode.Auto);
    }
}
