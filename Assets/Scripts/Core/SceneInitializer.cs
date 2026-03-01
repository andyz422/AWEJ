using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private bool createUIOnStart = true;
    [SerializeField] private bool createManagersOnStart = true;

    void Awake()
    {
        if (createManagersOnStart)
        {
            EnsureManagers();
        }

        if (createUIOnStart)
        {
            CreateUI();
        }
    }

    void EnsureManagers()
    {
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        if (SceneTransition.Instance == null)
        {
            GameObject st = new GameObject("SceneTransition");
            st.AddComponent<SceneTransition>();
        }
    }

    void CreateUI()
    {
        if (FindAnyObjectByType<UISetup>() != null)
        {
            return;
        }

        UISetup.CreateGameUI();
    }
}
