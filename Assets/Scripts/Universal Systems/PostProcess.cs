using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Manages post-processing effects for the project.
/// This version uses a Renderer Feature approach instead of Camera Stacking
/// to ensure TAA remains functional in Unity 6.
/// </summary>
public class PostProcess : MonoBehaviour
{
    private static LayerMask _exclusionLayers;
    public static LayerMask ExclusionLayers
    {
        get
        {
            if (_exclusionLayers == 0)
            {
                _exclusionLayers = LayerMask.GetMask("Entities", "Players", "MiscSprites");
            }
            return _exclusionLayers;
        }
    }

    /// <summary>
    /// Helper to set the layer of a GameObject and all its children.
    /// </summary>
    public static void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    void Start()
    {
        var mainCamera = GetComponent<Camera>();
        var mainCameraData = mainCamera?.GetUniversalAdditionalCameraData();

        if (mainCameraData != null)
        {
            // Ensure TAA and required textures are enabled on the main camera
            mainCameraData.antialiasing = AntialiasingMode.TemporalAntiAliasing;
            mainCameraData.renderPostProcessing = true;
            mainCameraData.requiresDepthTexture = true;
            mainCameraData.requiresColorTexture = true;
            
            // Clear the camera stack to allow TAA to function in Unity 6
            mainCameraData.cameraStack.Clear();
            
            Debug.Log("[PostProcess] Main Camera configured for TAA. Camera Stack cleared to ensure compatibility.");
        }
    }
}
