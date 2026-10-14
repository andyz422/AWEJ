using UnityEngine;

// Keeps labels and sprites facing the camera.
public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.rotation = cam.transform.rotation;
        }
    }
}
