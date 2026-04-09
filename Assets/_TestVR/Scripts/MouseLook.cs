using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private InputActionReference _lookAction;
    [SerializeField] private float _sensitivity = 100f;
    [SerializeField] private Transform _playerBody;

    private float _xRotation = 0f;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        Vector2 look = _lookAction.action.ReadValue<Vector2>();

        float mouseX = look.x * _sensitivity * Time.deltaTime;
        float mouseY = look.y * _sensitivity * Time.deltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        _playerBody.Rotate(Vector3.up * mouseX);
    }
}
