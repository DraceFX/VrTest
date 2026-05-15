using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WeldingMaskController : MonoBehaviour
{
    [Header("Углы маски")]
    [SerializeField] private float _upAngle = 0f;      // Поднятое состояние
    [SerializeField] private float _downAngle = -85f;  // Опущенное состояние
    [SerializeField] private float _snapThreshold = -45f; // Порог срабатывания авто-фиксации
    [SerializeField] private float _smoothSpeed = 8f;  // Скорость плавного возврата

    private XRGrabInteractable _grab;
    private Quaternion _upRot;
    private Quaternion _downRot;
    private Quaternion _targetRot;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();

        _upRot = Quaternion.Euler(_upAngle, 0, 0);
        _downRot = Quaternion.Euler(_downAngle, 0, 0);
        _targetRot = _upRot;
        transform.localRotation = _upRot;

        // Подписываемся на события захвата
        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs e)
    {
        // При захвате отключаем автоматический возврат
    }

    private void OnRelease(SelectExitEventArgs e)
    {
        // Определяем, в какую сторону "отпустить" маску
        float x = transform.localEulerAngles.x;
        if (x > 180f) x -= 360f; // Нормализуем в диапазон -180..180

        _targetRot = x < _snapThreshold ? _downRot : _upRot;
    }

    void LateUpdate()
    {
        // Плавный возврат к целевому углу
        if (!_grab.isSelected)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _targetRot, Time.deltaTime * _smoothSpeed);
        }

        // Жёсткое ограничение вращения
        float x = transform.localEulerAngles.x;
        if (x > 180f) x -= 360f;
        x = Mathf.Clamp(x, _downAngle, _upAngle);
        transform.localRotation = Quaternion.Euler(x, 0f, 0f);
    }
}
