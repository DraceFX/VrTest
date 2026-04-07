using UnityEngine;

public class CameraTriggerZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IDetection>(out IDetection obj))
        {
            obj.EnterZone();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<IDetection>(out IDetection obj))
        {
            obj.ExitZone();
        }
    }
}
