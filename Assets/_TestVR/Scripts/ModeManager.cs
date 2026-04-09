using UnityEngine;
using UnityEngine.InputSystem;

public class ModeManager : MonoBehaviour
{
    public static bool IsPCMode = true;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset _pcActions;
    [SerializeField] private InputActionAsset _vrActions;

    private void Awake()
    {
        if (IsPCMode)
        {
            _pcActions.Enable();
            _vrActions.Disable();
        }
        else
        {
            _pcActions.Disable();
            _vrActions.Enable();
        }
    }
}
