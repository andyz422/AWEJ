using System;
using System.Collections.Generic;
using UnityEngine;

// Something the player can use with the Interact button when standing close (doors, pedestals, NPCs).
// AreaManager picks the nearest one each frame and shows its prompt.
public class Interactable : MonoBehaviour
{
    public static readonly List<Interactable> All = new List<Interactable>();

    public float radius = 2.5f;
    public Func<string> prompt;
    public Action onInteract;

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    public static Interactable Create(Transform parent, Vector3 localPosition, float radius, Func<string> prompt, Action onInteract)
    {
        GameObject obj = new GameObject("Interactable");
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;

        Interactable interactable = obj.AddComponent<Interactable>();
        interactable.radius = radius;
        interactable.prompt = prompt;
        interactable.onInteract = onInteract;
        return interactable;
    }
}
