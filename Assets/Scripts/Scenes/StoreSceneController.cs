using UnityEngine;

public class StoreSceneController : MonoBehaviour
{
    public NPCInteraction shopkeeper;
    public NPCInteraction exitDoor;
    public Sprite[] shopkeeperDialogue;

    void Start()
    {
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        SetupStore();
    }

    void SetupStore()
    {
        if (shopkeeper != null && shopkeeperDialogue != null)
        {
            shopkeeper.dialogueSprites = shopkeeperDialogue;
        }

        if (exitDoor != null)
        {
            exitDoor.isSceneTrigger = true;
            exitDoor.targetScene = "TownScene";
        }
    }
}
