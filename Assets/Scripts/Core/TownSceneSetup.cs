using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TownSceneSetup : MonoBehaviour
{
    void Start()
    {
        SetupScene();
    }

    void SetupScene()
    {
        Debug.Log("TownSceneSetup.SetupScene() started");

        // Create EventSystem if needed
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // Create simple UI
        CreateGameUI();

        Debug.Log("Town Scene Setup Complete - Use WASD to move, approach doors to enter");
    }

    void CreateGameUI()
    {
        GameObject canvasObj = new GameObject("GameCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Instructions text
        GameObject textObj = new GameObject("Instructions");
        textObj.transform.SetParent(canvasObj.transform, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(600, 50);

        Text text = textObj.AddComponent<Text>();
        text.text = "WASD to move - Approach doors and press E to enter";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }
}
