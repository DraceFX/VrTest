using UnityEngine;
using UnityEngine.InputSystem;

public class DebugInput : MonoBehaviour
{
    [SerializeField] private InputActionReference _select;
    private void Update()
    {
        if (_select.action.triggered)
        {
            Debug.Log("CLICK");
        }
    }
}
