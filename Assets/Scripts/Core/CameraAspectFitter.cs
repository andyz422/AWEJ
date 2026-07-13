using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectFitter : MonoBehaviour
{
    public float referenceOrthographicSize = 5f;
    public float minVisibleWidth = 0f;

    private Camera cam;
    private int lastScreenWidth;
    private int lastScreenHeight;

    void Awake()
    {
        cam = GetComponent<Camera>();
        Fit();
    }

    void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            Fit();
        }
    }

    void Fit()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float size = referenceOrthographicSize;

        if (minVisibleWidth > 0f && cam.aspect > 0f)
        {
            size = Mathf.Max(size, minVisibleWidth / (2f * cam.aspect));
        }

        cam.orthographicSize = size;
    }
}
