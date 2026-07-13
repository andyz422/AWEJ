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
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }
}
