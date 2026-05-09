using UnityEngine;
using System.Collections.Generic;

public class MouseHoverTargeting : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask enemyLayer;
    public Material outlineMaterial;
    public float hoverRadius = 2.5f; 
    
    private Camera mainCam;
    
    private class TargetData
    {
        public GameObject gameObject;
        public GameObject outlineObject;
        public SpriteRenderer originalSprite;
        public SpriteRenderer outlineSprite;
    }

    private Dictionary<GameObject, TargetData> currentTargets = new Dictionary<GameObject, TargetData>();

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        if (mainCam == null) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.SphereCastAll(ray, hoverRadius, 100f, enemyLayer);

        HashSet<GameObject> hitObjects = new HashSet<GameObject>();

        foreach (RaycastHit hit in hits)
        {
            GameObject hitTarget = hit.collider.gameObject;
            hitObjects.Add(hitTarget);

            if (!currentTargets.ContainsKey(hitTarget))
            {
                // New target highlighted
                SpriteRenderer originalSprite = hitTarget.GetComponentInChildren<SpriteRenderer>();
                if (originalSprite != null && outlineMaterial != null)
                {
                    // Create a duplicate object to hold the outline behind the original sprite
                    GameObject outlineObj = new GameObject("HighlightOutline");
                    outlineObj.transform.SetParent(originalSprite.transform.parent, false);
                    
                    // Match transforms
                    outlineObj.transform.localPosition = originalSprite.transform.localPosition;
                    outlineObj.transform.localRotation = originalSprite.transform.localRotation;
                    outlineObj.transform.localScale = originalSprite.transform.localScale;

                    // Setup the outline SpriteRenderer
                    SpriteRenderer outlineSR = outlineObj.AddComponent<SpriteRenderer>();
                    outlineSR.sprite = originalSprite.sprite;
                    outlineSR.flipX = originalSprite.flipX;
                    outlineSR.flipY = originalSprite.flipY;
                    outlineSR.sortingLayerID = originalSprite.sortingLayerID;
                    outlineSR.sortingOrder = originalSprite.sortingOrder - 1; // Force it to render behind!
                    
                    // Assign the outline material
                    outlineSR.material = outlineMaterial;

                    TargetData data = new TargetData
                    {
                        gameObject = hitTarget,
                        outlineObject = outlineObj,
                        originalSprite = originalSprite,
                        outlineSprite = outlineSR
                    };

                    currentTargets[hitTarget] = data;
                }
            }
        }

        List<GameObject> toRemove = new List<GameObject>();
        foreach (var kvp in currentTargets)
        {
            if (kvp.Key == null || !hitObjects.Contains(kvp.Key))
            {
                // Unhighlight: Destroy the duplicate outline object
                if (kvp.Value.outlineObject != null)
                {
                    Destroy(kvp.Value.outlineObject);
                }
                toRemove.Add(kvp.Key);
            }
            else
            {
                // Sync animation frames and transform exactly every frame!
                TargetData data = kvp.Value;
                if (data.originalSprite != null && data.outlineSprite != null)
                {
                    data.outlineSprite.sprite = data.originalSprite.sprite;
                    data.outlineSprite.flipX = data.originalSprite.flipX;
                    data.outlineSprite.flipY = data.originalSprite.flipY;
                    
                    data.outlineObject.transform.localPosition = data.originalSprite.transform.localPosition;
                    data.outlineObject.transform.localRotation = data.originalSprite.transform.localRotation;
                    data.outlineObject.transform.localScale = data.originalSprite.transform.localScale;
                }
            }
        }

        foreach (GameObject obj in toRemove)
        {
            currentTargets.Remove(obj);
        }

        if (currentTargets.Count > 0)
        {
            if (CursorManager.Instance != null) CursorManager.Instance.SetAttackCursor();
        }
        else
        {
            if (CursorManager.Instance != null) CursorManager.Instance.SetDefaultCursor();
        }
    }
}
