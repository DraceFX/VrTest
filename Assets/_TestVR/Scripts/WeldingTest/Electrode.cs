using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Electrode : MonoBehaviour
{
    // Эти свойства устанавливаются извне
    public ElectrodeSocket CurrentSocket { get; set; }
    public ElectrodeSocket AttachedSocket { get; set; }
    [SerializeField] private XRGrabInteractable grabInteractable;
    public Rigidbody rb;

    [Header("Geometry")]
    public float length = 1f;

    [Header("Welding Source")]
    public float weldDistance = 0.215f;


    private void Awake()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnSelectEntering);
            grabInteractable.selectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnSelectEntering);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntering(SelectEnterEventArgs args)
    {
        if (AttachedSocket != null)
        {
            AttachedSocket.DetachElectrode(this);
        }
    }

    //Если электрод был отпущен, и мы находимся в зоне сокета – прикрепляем.
    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (CurrentSocket != null && AttachedSocket == null)
        {
            CurrentSocket.TryAttachElectrode(this);
        }
    }

    public void Burn(float amount)
    {
        length -= amount;
        length = Mathf.Max(length, 0f);

        transform.localScale = new Vector3(1f, 1f, length);
    }
}