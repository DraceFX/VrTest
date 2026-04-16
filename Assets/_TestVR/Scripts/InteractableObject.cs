using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class InteractableObject : MonoBehaviour, ITriggerEnter
{
    [SerializeField] private string _id;
    private XRGrabInteractable _grab;

    public string Id { get => _id; set => _id = value; }
    public XRGrabInteractable Grab => _grab;

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
    }
}
