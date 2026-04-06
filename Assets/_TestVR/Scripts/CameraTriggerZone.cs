using UnityEngine;

public class CameraTriggerZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        DetectionObject obj = other.GetComponent<DetectionObject>();

        if (obj != null)
        {
            obj.EnterZone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DetectionObject obj = other.GetComponent<DetectionObject>();

        if (obj != null)
        {
            obj.ExitZone();
        }
    }
}
