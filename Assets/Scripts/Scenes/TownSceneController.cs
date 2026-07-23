using UnityEngine;

public class TownSceneController : MonoBehaviour
{
    public Transform worldContainer;
    public Transform playerSpawn;

    public NPCInteraction storeDoor;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        SetupDoors();
    }

    void SetupDoors()
    {
        if (storeDoor != null)
        {
            storeDoor.isSceneTrigger = true;
            storeDoor.targetScene = "StoreScene";
        }
    }
}
