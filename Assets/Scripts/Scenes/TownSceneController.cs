using UnityEngine;

public class TownSceneController : MonoBehaviour
{
    public Transform worldContainer;
    public Transform playerSpawn;

    public NPCInteraction storeDoor;
    public NPCInteraction battleDoor;

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

        if (battleDoor != null)
        {
            battleDoor.isSceneTrigger = true;
            battleDoor.targetScene = "BattleScene";
        }
    }
}
