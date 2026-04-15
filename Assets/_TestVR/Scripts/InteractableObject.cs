using UnityEngine;

public class InteractableObject : MonoBehaviour, ITriggerEnter
{
    [SerializeField] private string _tag;
    [SerializeField] private string _id;

    public string Id { get => _id; set => _id = value; }

    private void Start()
    {
        Debug.Log(Id);
    }
}
