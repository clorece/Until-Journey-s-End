using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Place this on a GameObject in the Hub scene alongside a "Menu Camera".
/// It shows the main menu UI over the live Hub, then transitions to the player camera on Play.
/// </summary>
public class MainMenuBuilder : MonoBehaviour
{
    [Header("Camera References")]
    [Tooltip("A secondary camera positioned to show a cinematic view of the Hub. Disable 'AudioListener' on this.")]
    public Camera menuCamera;

    [Tooltip("The player's gameplay camera (CameraFollow). Will be DISABLED on start, ENABLED on play.")]
    public Camera playerCamera;

    [Header("Player Control")]
    [Tooltip("The player's root GameObject. Will be frozen during the menu.")]
    public PlayerMovement playerMovement;

    [Header("Camera Transition")]
    public float transitionDuration = 1.5f;

    [Header("UI Sprites (Assign in Inspector)")]
    public Sprite buttonSprite;
    public Sprite titleBoxSprite;
    public Sprite panelSprite;
    public Sprite topPatternSprite;
    public Sprite bottomPatternSprite;
    public Sprite cornerKnotSprite;

    [Header("Customization")]
    public string gameTitle = "Until Journey's End";
    public Color backgroundColor = new Color(0.05f, 0.05f, 0.12f, 0.6f);
    public Color buttonNormalColor = new Color(0.18f, 0.15f, 0.25f, 1f);
    public Color buttonHighlightColor = new Color(0.35f, 0.25f, 0.45f, 1f);
    public Color buttonPressedColor = new Color(0.12f, 0.1f, 0.18f, 1f);
    public Color titleColor = new Color(0.95f, 0.85f, 0.6f, 1f);
    public Color buttonTextColor = new Color(0.9f, 0.85f, 0.7f, 1f);

    private Canvas canvas;
    private CanvasGroup fadeGroup;
    private GameObject canvasObj;

    void Start()
    {
        // Freeze the player during the menu
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Disable the player camera, enable the menu camera
        if (playerCamera != null)
            playerCamera.gameObject.SetActive(false);

        if (menuCamera != null)
            menuCamera.gameObject.SetActive(true);

        BuildMenu();
        StartCoroutine(FadeIn());
    }

    private void BuildMenu()
    {
        // --- Canvas (renders on the menu camera) ---
        canvasObj = new GameObject("MainMenuCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        fadeGroup = canvasObj.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;

        // --- Dim Overlay ---
        GameObject overlay = CreateUIElement("Overlay", canvasObj.transform);
        StretchFull(overlay.GetComponent<RectTransform>());
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = backgroundColor;

        // --- Center Panel ---
        GameObject centerPanel = CreateUIElement("CenterPanel", canvasObj.transform);
        RectTransform panelRT = centerPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(420, 500);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelImg = centerPanel.AddComponent<Image>();
        if (panelSprite != null)
        {
            panelImg.sprite = panelSprite;
            panelImg.type = Image.Type.Sliced;
            panelImg.pixelsPerUnitMultiplier = 0.5f;
        }
        panelImg.color = new Color(0.1f, 0.08f, 0.18f, 0.95f);

        // --- Decorations ---
        if (topPatternSprite != null)
        {
            GameObject topDeco = CreateUIElement("TopPattern", centerPanel.transform);
            RectTransform topRT = topDeco.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0.5f, 1f);
            topRT.anchorMax = new Vector2(0.5f, 1f);
            topRT.sizeDelta = new Vector2(300, 40);
            topRT.anchoredPosition = new Vector2(0, 10);
            Image topImg = topDeco.AddComponent<Image>();
            topImg.sprite = topPatternSprite;
            topImg.color = titleColor;
            topImg.preserveAspect = true;
        }

        if (bottomPatternSprite != null)
        {
            GameObject bottomDeco = CreateUIElement("BottomPattern", centerPanel.transform);
            RectTransform bottomRT = bottomDeco.GetComponent<RectTransform>();
            bottomRT.anchorMin = new Vector2(0.5f, 0f);
            bottomRT.anchorMax = new Vector2(0.5f, 0f);
            bottomRT.sizeDelta = new Vector2(350, 40);
            bottomRT.anchoredPosition = new Vector2(0, -10);
            Image bottomImg = bottomDeco.AddComponent<Image>();
            bottomImg.sprite = bottomPatternSprite;
            bottomImg.color = titleColor;
            bottomImg.preserveAspect = true;
        }

        if (cornerKnotSprite != null)
        {
            CreateCornerKnot(centerPanel.transform, new Vector2(0, 1), new Vector2(-15, 15));
            CreateCornerKnot(centerPanel.transform, new Vector2(1, 1), new Vector2(15, 15));
            CreateCornerKnot(centerPanel.transform, new Vector2(0, 0), new Vector2(-15, -15));
            CreateCornerKnot(centerPanel.transform, new Vector2(1, 0), new Vector2(15, -15));
        }

        // --- Title ---
        GameObject titleArea = CreateUIElement("TitleArea", centerPanel.transform);
        RectTransform titleAreaRT = titleArea.GetComponent<RectTransform>();
        titleAreaRT.anchorMin = new Vector2(0.5f, 1f);
        titleAreaRT.anchorMax = new Vector2(0.5f, 1f);
        titleAreaRT.sizeDelta = new Vector2(380, 80);
        titleAreaRT.anchoredPosition = new Vector2(0, -70);

        if (titleBoxSprite != null)
        {
            Image titleBG = titleArea.AddComponent<Image>();
            titleBG.sprite = titleBoxSprite;
            titleBG.type = Image.Type.Sliced;
            titleBG.pixelsPerUnitMultiplier = 0.5f;
            titleBG.color = new Color(0.15f, 0.12f, 0.22f, 0.8f);
        }

        GameObject titleObj = CreateUIElement("TitleText", titleArea.transform);
        StretchFull(titleObj.GetComponent<RectTransform>());
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = gameTitle;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = titleColor;

        // --- Buttons ---
        GameObject buttonsContainer = CreateUIElement("ButtonsContainer", centerPanel.transform);
        RectTransform buttonsRT = buttonsContainer.GetComponent<RectTransform>();
        buttonsRT.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsRT.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRT.sizeDelta = new Vector2(280, 240);
        buttonsRT.anchoredPosition = new Vector2(0, -30);

        VerticalLayoutGroup layout = buttonsContainer.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 20;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateMenuButton(buttonsContainer.transform, "Play", () => OnPlayPressed());
        CreateMenuButton(buttonsContainer.transform, "Settings", () => Debug.Log("[MainMenu] Settings - Not yet implemented."));
        CreateMenuButton(buttonsContainer.transform, "Quit", () => QuitGame());

        // --- Version ---
        GameObject versionObj = CreateUIElement("VersionText", canvasObj.transform);
        RectTransform versionRT = versionObj.GetComponent<RectTransform>();
        versionRT.anchorMin = new Vector2(1f, 0f);
        versionRT.anchorMax = new Vector2(1f, 0f);
        versionRT.sizeDelta = new Vector2(200, 30);
        versionRT.anchoredPosition = new Vector2(-110, 20);
        Text versionText = versionObj.AddComponent<Text>();
        versionText.text = "v0.1 - Alpha";
        versionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        versionText.fontSize = 14;
        versionText.alignment = TextAnchor.LowerRight;
        versionText.color = new Color(0.5f, 0.45f, 0.55f, 0.6f);
    }

    // ========================
    //  TRANSITION LOGIC
    // ========================

    private void OnPlayPressed()
    {
        StartCoroutine(TransitionToGameplay());
    }

    private IEnumerator TransitionToGameplay()
    {
        // 1. Fade out the UI
        float fadeDuration = 0.5f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 0f;

        // 2. Lerp the menu camera to the player camera's position/rotation
        if (menuCamera != null && playerCamera != null)
        {
            // Temporarily enable player camera to read its target transform
            // We need to know WHERE it would be
            CameraFollow cameraFollow = playerCamera.GetComponent<CameraFollow>();

            Vector3 startPos = menuCamera.transform.position;
            Quaternion startRot = menuCamera.transform.rotation;

            // Calculate where the player camera WOULD be
            Vector3 targetPos = playerCamera.transform.position;
            Quaternion targetRot = playerCamera.transform.rotation;

            // If we have a CameraFollow, calculate the ideal position
            if (cameraFollow != null && cameraFollow.playerTarget != null && cameraFollow.orientationRef != null)
            {
                Vector3 pivotTarget = cameraFollow.playerTarget.position + cameraFollow.pivotOffset;
                Vector3 viewDir = cameraFollow.orientationRef.forward;
                targetPos = pivotTarget - (viewDir * cameraFollow.cameraDistance);
                targetRot = cameraFollow.orientationRef.rotation;
            }

            elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
                menuCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
                menuCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            // 3. Swap cameras
            menuCamera.gameObject.SetActive(false);
            playerCamera.gameObject.SetActive(true);
        }

        // 4. Enable player controls
        if (playerMovement != null)
            playerMovement.enabled = true;

        // 5. Clean up the menu
        Destroy(canvasObj);
        Destroy(gameObject); // Remove the menu builder itself
    }

    // ========================
    //  HELPERS
    // ========================

    private void QuitGame()
    {
        Debug.Log("[MainMenu] Quitting game.");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private void CreateMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = CreateUIElement("Btn_" + label.Replace(" ", ""), parent);
        RectTransform btnRT = btnObj.GetComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(280, 55);

        Image btnImg = btnObj.AddComponent<Image>();
        if (buttonSprite != null)
        {
            btnImg.sprite = buttonSprite;
            btnImg.type = Image.Type.Sliced;
            btnImg.pixelsPerUnitMultiplier = 0.5f;
        }
        btnImg.color = buttonNormalColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = buttonNormalColor;
        colors.highlightedColor = buttonHighlightColor;
        colors.pressedColor = buttonPressedColor;
        colors.selectedColor = buttonHighlightColor;
        colors.fadeDuration = 0.15f;
        btn.colors = colors;
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);

        GameObject textObj = CreateUIElement("Label", btnObj.transform);
        StretchFull(textObj.GetComponent<RectTransform>());
        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 22;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = buttonTextColor;
    }

    private void CreateCornerKnot(Transform parent, Vector2 anchor, Vector2 offset)
    {
        GameObject knot = CreateUIElement("CornerKnot", parent);
        RectTransform knotRT = knot.GetComponent<RectTransform>();
        knotRT.anchorMin = anchor;
        knotRT.anchorMax = anchor;
        knotRT.sizeDelta = new Vector2(40, 40);
        knotRT.anchoredPosition = offset;
        Image knotImg = knot.AddComponent<Image>();
        knotImg.sprite = cornerKnotSprite;
        knotImg.color = titleColor;
        knotImg.preserveAspect = true;
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private IEnumerator FadeIn()
    {
        float duration = 1.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        fadeGroup.alpha = 1f;
    }
}
