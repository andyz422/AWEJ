using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnGameStart()
    {
        // Create persistent managers
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        // Register for scene load events
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        Debug.Log("GameBootstrap: Loading scene " + sceneName);

        // Setup scene-specific content
        switch (sceneName)
        {
            case "MenuScene":
                SetupMenuScene();
                break;
            case "TownScene":
                SetupTownScene();
                break;
            case "BattleScene":
            case "Battle2Scene":
                SetupBattleScene();
                break;
            case "StoreScene":
                SetupStoreScene();
                break;
            case "GameOverScene":
                SetupGameOverScene();
                break;
        }
    }

    static void SetupMenuScene()
    {
        var existingSetup = Object.FindAnyObjectByType<MenuSceneSetup>();
        if (existingSetup == null)
        {
            Debug.Log("GameBootstrap: Creating MenuSceneSetup");
            GameObject setup = new GameObject("MenuSetup");
            setup.AddComponent<MenuSceneSetup>();
        }
        else
        {
            Debug.Log("GameBootstrap: MenuSceneSetup already exists in scene");
        }
    }

    static void SetupTownScene()
    {
        if (Object.FindAnyObjectByType<TownSceneSetup>() == null)
        {
            GameObject setup = new GameObject("TownSetup");
            setup.AddComponent<TownSceneSetup>();
        }
    }

    static void SetupBattleScene()
    {
        if (Object.FindAnyObjectByType<BattleSceneSetup>() == null)
        {
            GameObject setup = new GameObject("BattleSetup");
            setup.AddComponent<BattleSceneSetup>();
        }
    }

    static void SetupStoreScene()
    {
        if (Object.FindAnyObjectByType<StoreSceneSetup>() == null)
        {
            GameObject setup = new GameObject("StoreSetup");
            setup.AddComponent<StoreSceneSetup>();
        }
    }

    static void SetupGameOverScene()
    {
        if (Object.FindAnyObjectByType<GameOverSceneSetup>() == null)
        {
            GameObject setup = new GameObject("GameOverSetup");
            setup.AddComponent<GameOverSceneSetup>();
        }
    }
}
