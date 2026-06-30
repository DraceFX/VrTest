using System;
using UnityEngine;

public class TriggerStepCompletionProvider : MonoBehaviour, IStepCompletionProvider
{
    [SerializeField] private string _tag;
    [SerializeField] private string _id;
    [SerializeField] private bool _isNeedId = true;

    public event Action OnCompleted;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(_tag)) return;
        if (_isNeedId)
        {
            var obj = other.GetComponent<InteractableObject>();
            if (obj == null || obj.Id != _id) return;

            OnCompleted?.Invoke();
        }
        else
        {
            OnCompleted?.Invoke();
        }
    }

}
