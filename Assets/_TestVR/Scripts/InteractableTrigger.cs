using UnityEngine;

public class InteractableTrigger : MonoBehaviour, IObjectEnter
{
    [SerializeField] private string _tag;
    [SerializeField] private string _id;

    public string TAG { get => _tag; set => _tag = value; }
    public string Id { get => _id; set => _id = value; }

    private void Start()
    {
        Debug.Log(Id);
    }

    private void OnTriggerEnter(Collider other)
    {
        
    }

    private void OnTriggerExit(Collider other)
    {
        
    }
}
