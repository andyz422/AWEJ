using UnityEngine;

public class Spinner : MonoBehaviour
{
    public Vector3 degreesPerSecond = new Vector3(0f, 90f, 0f);

    void Update()
    {
        transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
