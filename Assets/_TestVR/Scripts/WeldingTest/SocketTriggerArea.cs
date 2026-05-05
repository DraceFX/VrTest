using UnityEngine;

public class SocketTriggerArea : MonoBehaviour
{
    public ElectrodeSocket socket; // Ссылка на родительский сокет

    private void OnTriggerEnter(Collider other)
    {
        Electrode electrode = other.GetComponent<Electrode>();
        if (electrode != null && socket.AttachedElectrode == null)
        {
            electrode.CurrentSocket = socket;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Electrode electrode = other.GetComponent<Electrode>();
        if (electrode != null && electrode.CurrentSocket == socket)
        {
            electrode.CurrentSocket = null;
        }
    }
}