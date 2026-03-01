using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StoreSceneSetup : MonoBehaviour
{
    void Start()
    {
        SetupScene();
    }

    void SetupScene()
    {
        // Create EventSystem if needed
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // Create UI
        CreateStoreUI();

        Debug.Log("Store Scene Setup - Talk to shopkeeper or go to exit door");
    }

    void CreateStoreUI()
    {
        GameObject canvasObj = new GameObject("StoreCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(canvasObj.transform, false);

        RectTransform rect = titleObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1);
        rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(400, 60);

        Text text = titleObj.AddComponent<Text>();
        text.text = "STORE";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 48;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        // Instructions
        GameObject instrObj = new GameObject("Instructions");
        instrObj.transform.SetParent(canvasObj.transform, false);

        RectTransform instrRect = instrObj.AddComponent<RectTransform>();
        instrRect.anchorMin = new Vector2(0.5f, 0);
        instrRect.anchorMax = new Vector2(0.5f, 0);
        instrRect.pivot = new Vector2(0.5f, 0);
        instrRect.anchoredPosition = new Vector2(0, 20);
        instrRect.sizeDelta = new Vector2(600, 40);

        Text instrText = instrObj.AddComponent<Text>();
        instrText.text = "Press E near shopkeeper to talk - Walk to door to exit";
        instrText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        instrText.fontSize = 24;
        instrText.alignment = TextAnchor.MiddleCenter;
        instrText.color = Color.white;
    }
}
